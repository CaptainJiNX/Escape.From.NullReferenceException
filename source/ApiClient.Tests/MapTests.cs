using NUnit.Framework;

namespace ApiClient.Tests
{
	[TestFixture]
	public class MapTests
	{
		[Test, Ignore("Just testing...")]
		public void ClosestUnknown_Test()
		{
			var map = new Map("TestMap");

			for (var y = 10; y <= 20; y++)
			{
				for (var x = 10; x <= 20; x++)
				{
					map.SetPositionValue(new Position(10, 10), (uint)TileFlags.ROOM);
				}
			}

			var pos = map.GetClosestWalkablePositionWithUnknownNeighbour(new Position(14, 11), p => true);

			Assert.AreEqual(14, pos.X);
			Assert.AreEqual(10, pos.Y);
		}
	}
}
