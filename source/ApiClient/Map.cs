using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiClient
{
	[Serializable]
	public class Map
	{
		private readonly Dictionary<Position, uint> _positions = new Dictionary<Position, uint>();
		private bool _hasChanges;

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
			foreach (var tuple in result.ConvertAreaToPositions())
			{
				UpdatePosition(tuple.Item1, tuple.Item2);
			}

			UpdatePosition(result.StairsDown, (uint) TileFlags.STAIR_DOWN);
			UpdatePosition(result.StairsUp, (uint) TileFlags.STAIR_UP);
		}

		private void UpdatePosition(Position pos, uint val)
		{
			if (pos == null || (_positions.ContainsKey(pos) && GetPositionValue(pos) == val)) return;
			_hasChanges = true;
			_positions[pos] = val;
		}

		public uint GetPositionValue(Position position)
		{
			return _positions.ContainsKey(position) ? _positions[position] : uint.MaxValue;
		}

		public bool HasChanges()
		{
			return _hasChanges;
		}

		public void ClearChanges()
		{
			_hasChanges = false;
		}

		public bool IsWalkable(Position pos, IEnumerable<Position> blocked)
		{
			var value = (TileFlags)GetPositionValue(pos);
			if (value == TileFlags.UNKNOWN) return true;
			if (blocked.Any(x => x.Equals(pos))) return false;
			if (value == TileFlags.NOTHING) return false;
			if ((value & (TileFlags.PERIMETER | TileFlags.BLOCKED)) > 0) return false;
			return true;
		}
	}
}