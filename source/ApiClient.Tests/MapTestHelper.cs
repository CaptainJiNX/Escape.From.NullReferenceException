using System.Collections.Generic;
using NUnit.Framework;

namespace ApiClient.Tests
{
	static class MapTestHelper
	{
		public static void AssertClosestWalkablePosition(IEnumerable<string> mapArea, IEnumerable<string> startPosition, IEnumerable<string> expectedPosition)
		{
			Map map = CreateMap(mapArea);
			Position start = GetPosition(startPosition);
			Position expected = GetPosition(expectedPosition);

			Position result = map.GetClosestWalkablePositionWithUnknownNeighbour(start, p => true);

			Assert.AreEqual(expected, result);
		}

		private static Position GetPosition(IEnumerable<string> xMarksTheSpot)
		{
			var y = 0;
			foreach (var row in xMarksTheSpot)
			{
				var x = 0;
				foreach (var col in row)
				{
					if (col == 'X')
					{
						return new Position(x, y);
					}
					x++;
				}
				y++;
			}
			return null;
		}

		private static TileFlags GetTile(char c)
		{
			switch (c)
			{
				case '*':
					return TileFlags.PERIMETER;
				case '#':
					return TileFlags.NOTHING;
				case ' ':
					return TileFlags.ROOM;
				case '+':
					return TileFlags.DOOR1;
				case '?':
					return TileFlags.UNKNOWN;
				default:
					return TileFlags.UNKNOWN;
			}
		}

		private static Map CreateMap(IEnumerable<string> area)
		{
			var map = new Map("TestMap");

			var y = 0;
			foreach (var row in area)
			{
				var x = 0;
				foreach (var col in row)
				{
					var pos = new Position(x, y);
					map.SetPositionValue(pos, (uint) GetTile(col));
					x++;
				}
				y++;
			}

			return map;
		}
	}
}