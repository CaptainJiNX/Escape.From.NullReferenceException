using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp
{
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

		public enum BorderStyle
		{
			None,
			Single,
			Double
		}

		private class BorderBytes
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

		private readonly Dictionary<Pos, Console2.CharInfo> _allChars = new Dictionary<Pos, Console2.CharInfo>();

		private ConsoleColor _defaultForeground;
		private ConsoleColor _defaultBackground;

		private ConsoleColor _borderForeground;
		private ConsoleColor _borderBackground;
		private BorderBytes _border;

		private int _offsetX;
		private int _offsetY;
		private byte[] _title;

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

		public void SetBorderStyle(BorderStyle value)
		{
			_border = value != BorderStyle.None ? new BorderBytes(value) : null;
		}

		public void SetDefaultForeground(ConsoleColor value)
		{
			_defaultForeground = value;
		}

		public void SetDefaultBackground(ConsoleColor value)
		{
			_defaultBackground = value;
		}

		public void SetBorderForeground(ConsoleColor value)
		{
			_borderForeground = value;
		}

		public void SetBorderBackground(ConsoleColor value)
		{
			_borderBackground = value;
		}

		public ConsoleArea(short width, short height)
		{
			Width = width;
			Height = height;
		}

		public void CenterAt(int x, int y)
		{
			if (!_allChars.Any()) return;

			if (ActualWidth < Width)
			{
				_offsetX = MinX + (ActualWidth / 2) - (Width / 2);
			}
			else
			{
				_offsetX = Math.Max(MinX, Math.Min(MaxX, x - (Width / 2)));
			}

			if (ActualHeight < Height)
			{
				_offsetY = MinY + (ActualHeight / 2) - (Height / 2);
			}
			else
			{
				_offsetY = Math.Max(MinY, Math.Min(MaxY, y - (Height / 2)));
			}
		}

		private int MaxY
		{
			get { return _allChars.Keys.Max(p => p.Y) + BorderSize * 2 - Height; }
		}

		private int MinY
		{
			get { return _allChars.Keys.Min(p => p.Y) - BorderSize; }
		}

		private int MaxX
		{
			get { return _allChars.Keys.Max(p => p.X) + BorderSize * 2 - Width; }
		}

		private int MinX
		{
			get { return _allChars.Keys.Min(p => p.X) - BorderSize; }
		}

		private short BorderSize
		{
			get { return (short) (_border == null ? 0 : 1); }
		}

		private Console2.CharInfo Default
		{
			get { return new Console2.CharInfo((byte)' ', _defaultForeground, _defaultBackground); }
		}

		public Console2.CharInfo[] GetBuffer()
		{
			return GetBufferInternal().ToArray();
		}

		private Console2.CharInfo GetCharInfo(int x, int y)
		{
			Console2.CharInfo charInfo;
			return _allChars.TryGetValue(new Pos(x, y), out charInfo) ? charInfo : Default;
		}

		private void SetCharInfo(Console2.CharInfo charInfo, int x, int y)
		{
			_allChars[new Pos(x, y)] = charInfo;
		}

		private Console2.CharInfo GetBorder(byte borderByte)
		{
			return new Console2.CharInfo(borderByte, _borderForeground, _borderBackground);
		}

		private Console2.CharInfo GetBorderCharInfo(int x, int y)
		{
			if (y == _offsetY)
			{
				if (x == _offsetX)
				{
					return GetBorder(_border.TopLeft);
				}

				if (x == _offsetX + Width - 1)
				{
					return GetBorder(_border.TopRight);
				}

				if (_title != null)
				{
					var titleIndex = (x - _offsetX) - (Width / 2) + _title.Length / 2;

					if (titleIndex >= 0 && titleIndex < _title.Length)
					{
						return GetBorder(_title[titleIndex]);
					}
				}

				return GetBorder(_border.Horizontal);
			}

			if (y == _offsetY + Height - 1)
			{
				if (x == _offsetX)
				{
					return GetBorder(_border.BottomLeft);
				}

				if (x == _offsetX + Width - 1)
				{
					return GetBorder(_border.BottomRight);
				}

				return GetBorder(_border.Horizontal);
			}

			if (x == _offsetX || x == _offsetX + Width - 1)
			{
				return GetBorder(_border.Vertical);
			}

			return GetCharInfo(x, y);
		}


		private IEnumerable<Console2.CharInfo> GetBufferInternal()
		{
			for (int y = _offsetY; y < _offsetY + Height; y++)
			{
				for (int x = _offsetX; x < _offsetX + Width; x++)
				{
					yield return _border != null ? GetBorderCharInfo(x, y) : GetCharInfo(x, y);
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

		public void SetTitle(string title)
		{
			_title = Console2.Encoding.GetBytes(title);
		}

		public void Write(string value, int x, int y)
		{
			Write(value, x, y, _defaultForeground, _defaultBackground);
		}

		public void Write(string value, int x, int y, ConsoleColor foregroundColor)
		{
			Write(value, x, y, foregroundColor, _defaultBackground);
		}

		public void Write(string value, int x, int y, ConsoleColor foregroundColor, ConsoleColor backgroundColor)
		{
			if(value == null) return;
			var bytes = Console2.Encoding.GetBytes(value);

			for (int i = 0; i < bytes.Length; i++)
			{
				SetCharInfo(new Console2.CharInfo(bytes[i], foregroundColor, backgroundColor), x + i, y);
			}
		}

		public void Clear()
		{
			_allChars.Clear();
		}

		public void TopLeftAt(int x, int y)
		{
			_offsetX = Math.Max(MinX, Math.Min(MaxX, x));
			_offsetY = Math.Max(MinY, Math.Min(MaxY, y));
		}
	}
}