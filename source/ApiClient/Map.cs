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
			var bx = scanResult["bx"].Value<int>();
			var by = scanResult["by"].Value<int>();

			var rows = scanResult["area"].ToArray();

			for (int y = 0; y < rows.Length; y++)
			{
				var columns = rows[y].ToArray();
				for (int x = 0; x < columns.Length; x++)
				{
					UpdatePosition(new Position(x + bx, y + @by), columns[x].Value<uint>());
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