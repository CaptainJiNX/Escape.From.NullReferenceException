using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ApiClient
{
	public class Map
	{
		private readonly Dictionary<Position, int> _known = new Dictionary<Position, int>();

		private readonly Dictionary<string, Position> _items = new Dictionary<string, Position>();
		private readonly Dictionary<string, Position> _entities = new Dictionary<string, Position>();

		public Map(string name)
		{
			Name = name;
		}

		public string Name { get; private set; }

		public IEnumerable<Position> AllKnown
		{
			get { return _known.Keys; }
		}

		public IEnumerable<KeyValuePair<string, Position>> Items
		{
			get { return _items; }
		}

		public IEnumerable<KeyValuePair<string, Position>> Entities
		{
			get { return _entities; }
		}

		public void UpdateFromScan(JObject scanResult)
		{
			UpdateArea(scanResult);
			UpdateItems(scanResult);
			UpdateEntities(scanResult);
		}

		public void UpdateFromMovement(JObject movement)
		{
			UpdateFromScan(movement);
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
					UpdatePosition(new Position(x + bx, y + @by), columns[x].Value<int>());
				}
			}
		}

		private void UpdateItems(JObject scanResult)
		{
			_items.Clear();

			foreach (var item in scanResult["items"])
			{
				var xpos = item["x"].Value<int>();
				var ypos = item["y"].Value<int>();
				_items.Add(item["_id"].Value<string>(), new Position(xpos, ypos));
			}
		}

		private void UpdateEntities(JObject scanResult)
		{
			_entities.Clear();

			foreach (var entity in scanResult["entities"])
			{
				var xpos = entity["x"].Value<int>();
				var ypos = entity["y"].Value<int>();
				_entities.Add(entity["_id"].Value<string>(), new Position(xpos, ypos));
			}
		}

		private void UpdatePosition(Position pos, int val)
		{
			_known[pos] = val;
		}

		public int GetPositionValue(Position position)
		{
			if (_known.ContainsKey(position))
			{
				return _known[position];
			}

			return -1;
		}
	}
}