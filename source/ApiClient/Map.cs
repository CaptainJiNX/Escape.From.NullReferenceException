using System;
using System.Collections.Generic;

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
		}

		private void UpdatePosition(Position pos, uint val)
		{
			if (_positions.ContainsKey(pos) && GetPositionValue(pos) == val) return;
			_hasChanges = true;
			_positions[pos] = val;
		}

		public uint GetPositionValue(Position position)
		{
			return _positions.ContainsKey(position) ? _positions[position] : 0;
		}

		public bool HasChanges()
		{
			return _hasChanges;
		}

		public void ClearChanges()
		{
			_hasChanges = false;
		}
	}
}