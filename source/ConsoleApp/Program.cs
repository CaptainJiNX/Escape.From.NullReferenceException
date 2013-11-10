using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace ConsoleApp
{
	public class Console2
	{
		private readonly short _width;
		private readonly short _height;
		private readonly SafeFileHandle _handle;
		private static readonly Encoding _encoding = Encoding.GetEncoding(437);
		private readonly CharInfo[] _buffer;

		public Console2(short width, short height, ConsoleColor backgroundColor)
		{
			_width = width;
			_height = height;
			_buffer = new CharInfo[width * height];

			for (int i = 0; i < _buffer.Length; i++)
			{
				_buffer[i] = new CharInfo((byte)' ', ConsoleColor.Black, backgroundColor);
			}

			InitConsole(width, height);

			_handle = CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
		}

		private static void InitConsole(int width, int height)
		{
			Console.SetWindowSize(width, height);
			Console.SetBufferSize(width, height);
			Console.CursorVisible = false;
			Console.OutputEncoding = _encoding;
		}

		[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern SafeFileHandle CreateFile(
			string fileName,
			[MarshalAs(UnmanagedType.U4)] uint fileAccess,
			[MarshalAs(UnmanagedType.U4)] uint fileShare,
			IntPtr securityAttributes,
			[MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
			[MarshalAs(UnmanagedType.U4)] int flags,
			IntPtr template);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool WriteConsoleOutput(
			SafeFileHandle hConsoleOutput,
			CharInfo[] lpBuffer,
			Coord dwBufferSize,
			Coord dwBufferCoord,
			ref SmallRect lpWriteRegion);

		[StructLayout(LayoutKind.Sequential)]
		public struct Coord
		{
			public short X;
			public short Y;

			public Coord(short X, short Y)
			{
				this.X = X;
				this.Y = Y;
			}
		};

		[StructLayout(LayoutKind.Explicit)]
		public struct CharUnion
		{
			[FieldOffset(0)]
			public char UnicodeChar;
			[FieldOffset(0)]
			public byte AsciiChar;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct CharInfo
		{
			public CharInfo(byte c, ConsoleColor foreColor, ConsoleColor bgCol)
			{
				Char = new CharUnion { AsciiChar = c };
				Attributes = (short)((short)foreColor + (short)bgCol * 16);
			}

			[FieldOffset(0)]
			public CharUnion Char;
			[FieldOffset(2)]
			public short Attributes;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SmallRect
		{
			public short Left;
			public short Top;
			public short Right;
			public short Bottom;
		}

		public void DrawArea(ConsoleArea area, short x, short y)
		{ 
			DrawBuffer(area.GetBuffer(), x, y, area.Width, area.Height);
		}

		public void Clear()
		{
			DrawBuffer(_buffer, 0, 0, _width, _height);
		}

		private void DrawBuffer(CharInfo[] buffer, short x, short y, short width, short height)
		{
			var rect = new SmallRect {Left = x, Top = y, Right = (short) (x + width - 1), Bottom = (short) (y + height - 1)};
			WriteConsoleOutput(_handle, buffer, new Coord(width, height), new Coord(0, 0), ref rect);
		}
	}

	public enum BorderStyle
	{
		None,
		Single,
		Double
	}

	public class BorderBytes
	{
		public BorderBytes(BorderStyle style)
		{
			switch (style)
			{
				case BorderStyle.Single:
					TopLeft = 218;
					TopRight = 191;
					BottomRight = 217;
					BottomLeft = 192;
					Horizontal = 196;
					Vertical = 179;
					break;

				case BorderStyle.Double:
					TopLeft = 201;
					TopRight = 187;
					BottomRight = 188;
					BottomLeft = 200;
					Horizontal = 205;
					Vertical = 186;
					break;
			}
		}

		public byte TopLeft { get; private set; }
		public byte TopRight { get; private set; }
		public byte BottomRight { get; private set; }
		public byte BottomLeft { get; private set; }
		public byte Horizontal { get; private set; }
		public byte Vertical { get; private set; }
	}

	public class ConsoleArea
	{

		private struct Pos
		{
			public Pos(int x, int y)
			{
				X = x;
				Y = y;
			}

			public int X;
			public int Y;
		}

		private readonly Dictionary<Pos, Console2.CharInfo> _allChars = new Dictionary<Pos, Console2.CharInfo>();
		private BorderBytes _border;

		private int _offsetX;
		private int _offsetY;
		private BorderStyle _borderStyle;

		private int ActualWidth
		{
			get { return GetDiff(p => p.X) + BorderSize * 2; }
		}
		private int ActualHeight
		{
			get { return GetDiff(p => p.Y) + BorderSize * 2; }
		}

		public short Width { get; private set; }
		public short Height { get; private set; }

		public BorderStyle BorderStyle
		{
			get { return _borderStyle; }
			set
			{
				_borderStyle = value;
				_border = value != BorderStyle.None ? new BorderBytes(value) : null;
			}
		}

		public ConsoleColor BorderForeground { get; set; }
		public ConsoleColor BorderBackground { get; set; }

		public ConsoleArea(short width, short height)
		{
			Width = width;
			Height = height;
		}

		public void CenterAt(int x, int y)
		{
			if (!_allChars.Any()) return;

			var minX = _allChars.Keys.Min(p => p.X) - BorderSize;
			var maxX = _allChars.Keys.Max(p => p.X) - Width + BorderSize * 2;
			var minY = _allChars.Keys.Min(p => p.Y) - BorderSize;
			var maxY = _allChars.Keys.Max(p => p.Y) - Height + BorderSize * 2;

			if (ActualWidth < Width)
			{
				_offsetX = minX + (ActualWidth / 2) - (Width / 2);
			}
			else
			{
				_offsetX = Math.Max(minX, Math.Min(maxX, x - (Width / 2)));
			}

			if (ActualHeight < Height)
			{
				_offsetY = minY + (ActualHeight / 2) - (Height / 2);
			}
			else
			{
				_offsetY = Math.Max(minY, Math.Min(maxY, y - (Height / 2)));
			}
		}

		private short BorderSize
		{
			get { return (short) (_border == null ? 0 : 1); }
		}

		public Console2.CharInfo Default { get; set; }
		
		public Console2.CharInfo[] GetBuffer()
		{
			return GetBufferInternal().ToArray();
		}

		public Console2.CharInfo GetCharInfo(int x, int y)
		{
			Console2.CharInfo charInfo;
			return _allChars.TryGetValue(new Pos(x, y), out charInfo) ? charInfo : Default;
		}

		public void SetCharInfo(Console2.CharInfo charInfo, int x, int y)
		{
			_allChars[new Pos(x, y)] = charInfo;
		}

		private IEnumerable<Console2.CharInfo> GetBufferInternal()
		{
			for (int y = _offsetY; y < _offsetY + Height; y++)
			{
				for (int x = _offsetX; x < _offsetX + Width; x++)
				{
					if (_border != null)
					{
						if (y == _offsetY)
						{
							if (x == _offsetX)
								yield return new Console2.CharInfo(_border.TopLeft, BorderForeground, BorderBackground);
							else if (x == _offsetX + Width - 1)
								yield return new Console2.CharInfo(_border.TopRight, BorderForeground, BorderBackground);
							else
								yield return new Console2.CharInfo(_border.Horizontal, BorderForeground, BorderBackground);
							continue;
						}
						
						if (y == _offsetY + Height - 1)
						{
							if (x == _offsetX)
								yield return new Console2.CharInfo(_border.BottomLeft, BorderForeground, BorderBackground);
							else if (x == _offsetX + Width - 1)
								yield return new Console2.CharInfo(_border.BottomRight, BorderForeground, BorderBackground);
							else
								yield return new Console2.CharInfo(_border.Horizontal, BorderForeground, BorderBackground);
							continue;
						}

						if (x == _offsetX || x == _offsetX + Width - 1)
						{
							yield return new Console2.CharInfo(_border.Vertical, BorderForeground, BorderBackground);
							continue;
						}
					}

					yield return GetCharInfo(x, y);
				}
			}
		}

		private int GetDiff(Func<Pos, int> selector)
		{
			if (!_allChars.Any()) return 0;

			var max = _allChars.Keys.Max(selector);
			var min = _allChars.Keys.Min(selector);
			return (max - min) + 1;
		}
	}

	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			var console = new Console2(100, 30, ConsoleColor.DarkRed);

			var area = new ConsoleArea(50, 15)
			{
				BorderStyle = BorderStyle.Double,
				BorderBackground = ConsoleColor.Black,
				BorderForeground = ConsoleColor.White,
				Default = new Console2.CharInfo((byte) ' ', ConsoleColor.Black, ConsoleColor.Black)
			};

			area.SetCharInfo(new Console2.CharInfo((byte)'A', ConsoleColor.Yellow, ConsoleColor.Black), 0, 0);
			area.SetCharInfo(new Console2.CharInfo((byte)'Z', ConsoleColor.Yellow, ConsoleColor.Black), 2000, 2000);

			area.CenterAt(10, 10);

			short x = 0;
			short y = 0;

			while (true)
			{
				console.Clear();
				console.DrawArea(area, x, y);

				var key = Console.ReadKey();

				if (key.Key == ConsoleKey.UpArrow) y--;
				else if (key.Key == ConsoleKey.DownArrow) y++;
				else if (key.Key == ConsoleKey.LeftArrow) x--;
				else if (key.Key == ConsoleKey.RightArrow) x++;
				else if (key.Key == ConsoleKey.Escape) break;
			}
		}
	}
}
