using System;
using System.Collections.Generic;
using System.Linq;
using ApiClient;

namespace ConsoleApp
{
	class Program
	{
		private static bool IsRoom(uint val)
		{
			return GetRoomId(val) > 0;
		}

		private static uint GetRoomId(uint val)
		{
			return (uint) TileFlags.ROOM_ID & val;
		}

		[STAThread]
		static void Main(string[] args)
		{
			var game = new ConsoleGame();
			game.RunGame();
			return;

			//PathFinder.CalculatePath()

			var mapStorage = new BinaryMapStorage();

			var map = mapStorage.GetAll().First();
			var allRooms = map.AllRooms.ToList();

			foreach (var room in allRooms)
			{
				var fromRoom = room;
				foreach (var otherRoom in allRooms.Except(new[]{fromRoom}))
				{
					var toRoom = otherRoom;

					var path = PathFinder.CalculatePath(fromRoom.Position, toRoom.Position,
					                         pos => map.IsWalkable(pos, fromRoom.RoomId, toRoom.RoomId));

					Console.WriteLine("{0} -->  {1}: {2} steps.", fromRoom.RoomId, toRoom.RoomId, path.Count());
				}
			}

			Console.ReadLine();
		}

		private static void PrintMap(Map map)
		{
			Console.WriteLine();
			Console.WriteLine(map.Name);
			Console.WriteLine("--------------------------");

			for (int y = 0; y < map.MaxPos.Y; y++)
			{
				for (int x = 0; x < map.MaxPos.X; x++)
				{
					Console.Write(ConsoleGame.GetTile(map.GetPositionValue(new Position(x, y))).Character);
				}

				Console.WriteLine();
			}

			Console.ReadKey();
		}
	}
}
