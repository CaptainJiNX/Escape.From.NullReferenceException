using NUnit.Framework;

namespace ApiClient.Tests
{
	[TestFixture]
	public class MapTests
	{
		[Test]
		public void Should_find_closest_unknown_tile_in_corridors_heading_north_to_south()
		{
			var mapArea = new[]
			{
				"???",
				"# #",
				"# #",
				"# #",
				"# #",
				"???"
			};

			var startPosition = new[]
			{
				"???",
				"# #",
				"#X#",
				"# #",
				"# #",
				"???"
			};

			var expectedPosition = new[]
			{
				"???",
				"#X#",
				"# #",
				"# #",
				"# #",
				"???"
			};

			MapTestHelper.AssertClosestWalkablePosition(mapArea, startPosition, expectedPosition);
		}

		[Test]
		public void Should_find_closest_unknown_tile_in_corridors_heading_east_to_west()
		{
			var mapArea = new[]
			{
				"?######?",
				"?      ?",
				"?######?"
			};

			var startPosition = new[]
			{
				"?######?",
				"?   X  ?",
				"?######?"
			};

			var expectedPosition = new[]
			{
				"?######?",
				"?     X?",
				"?######?"
			};

			MapTestHelper.AssertClosestWalkablePosition(mapArea, startPosition, expectedPosition);
		}

		[Test]
		public void Should_find_closest_unknown_tile_in_rooms()
		{
			var mapArea = new[]
			{
				"#???#",
				"** *****************#",
				"*                  *#",
				"*                  *?",
				"*                   ?",
				"*                  *?",
				"********************#"
			};

			var startPosition = new[]
			{
				"#???#",
				"** *****************#",
				"*                  *#",
				"*            X     *?",
				"*                   ?",
				"*                  *?",
				"********************#"
			};

			var expectedPosition = new[]
			{
				"#???#",
				"** *****************#",
				"*                  *#",
				"*                  *?",
				"*                  X?",
				"*                  *?",
				"********************#"
			};

			MapTestHelper.AssertClosestWalkablePosition(mapArea, startPosition, expectedPosition);
		}

		[Test]
		public void Should_find_closest_unknown_tile_far_away()
		{
			var mapArea = new[]
			{
				"#####################*******",
				"*****################*     *",
				"*   *######          +     *",
				"*   *###### #########*     *?",
				"*   +       #########*     +?",
				"*   *################*******?",
				"*****"
			};

			var startPosition = new[]
			{
				"#####################*******",
				"*****################*     *",
				"*X  *######          +     *",
				"*   *###### #########*     *?",
				"*   +       #########*     +?",
				"*   *################*******?",
				"*****"
			};

			var expectedPosition = new[]
			{
				"#####################*******",
				"*****################*     *",
				"*   *######          +     *",
				"*   *###### #########*     *?",
				"*   +       #########*     X?",
				"*   *################*******?",
				"*****"
			};

			MapTestHelper.AssertClosestWalkablePosition(mapArea, startPosition, expectedPosition);
		}
	}
}
