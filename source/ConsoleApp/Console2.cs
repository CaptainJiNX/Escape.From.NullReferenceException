using System;
using System.IO;
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
			Clear();
		}

		public static Encoding Encoding { get { return _encoding; } }

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
}