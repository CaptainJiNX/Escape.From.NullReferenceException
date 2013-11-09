using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ApiClient
{
	public class Map
	{
		private readonly Dictionary<Position, uint> _known = new Dictionary<Position, uint>();

		public Map(string name)
		{
			Name = name;
		}

		public string Name { get; private set; }

		public IEnumerable<Position> AllKnown
		{
			get { return _known.Keys; }
		}

		public void UpdateFromScan(JObject scanResult)
		{
			UpdateArea(scanResult);
		}

		public void UpdateFromMovement(JObject movement)
		{
			UpdateArea(movement);
		}

		private void UpdateArea(JObject scanResult)
		{
			var rows = scanResult["area"];
			if (rows == null) return;

			var bx = scanResult.Value<int>("bx");
			var by = scanResult.Value<int>("by");

			for (int y = 0; y < rows.Count(); y++)
			{
				for (int x = 0; x < rows[y].Count(); x++)
				{
					UpdatePosition(new Position(x + bx, y + by), rows[y][x].Value<uint>());
				}
			}
		}

		private void UpdatePosition(Position pos, uint val)
		{
			_known[pos] = val;
		}

		public uint GetPositionValue(Position position)
		{
			return _known.ContainsKey(position) ? _known[position] : 0;
		}
	}
}