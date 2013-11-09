using System;
using System.Collections.Generic;

namespace ApiClient
{
	[Serializable]
	public class Map
	{
		private readonly Dictionary<Position, uint> _positions = new Dictionary<Position, uint>();

		public Map(string name)
		{
			Name = name;
		}

		public string Name { get; private set; }

		public IEnumerable<Position> AllPositions
		{
			get { return _positions.Keys; }
		}

		public void Update(ScanResult result)
		{
			var area = result.VisibleArea;

			if (area == null) return;

			for (var y = 0; y < area.Length; y++)
			{
				for (var x = 0; x < area[y].Length; x++)
				{
					UpdatePosition(new Position(x + result.XOff, y + result.YOff), area[y][x]);
				}
			}
		}

		private void UpdatePosition(Position pos, uint val)
		{
			_positions[pos] = val;
		}

		public uint GetPositionValue(Position position)
		{
			return _positions.ContainsKey(position) ? _positions[position] : 0;
		}
	}
}