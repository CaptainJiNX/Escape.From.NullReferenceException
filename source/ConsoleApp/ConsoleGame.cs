using System;
using System.IO;
using System.Linq;
using ApiClient;

namespace ConsoleApp
{
	class ConsoleGame
	{
		private readonly IClientWrapper _client;
		private readonly GameContext _context;
		private string _playerId;

		public ConsoleGame()
		{
			_client = new ClientWrapper(Guid.Parse(File.ReadAllText("apikey.txt")));

			_context = new GameContext(_client, new BinaryMapStorage());
		}

		public void RunGame()
		{
			InitPlayer();

			RunGameLoop();

			Console.WriteLine("Done...");
			Console.ReadLine();
		}

		private void RunGameLoop()
		{
			ResetColor();
			Console.Clear();
			Console.CursorVisible = false;

			while (true)
			{
				_context.Scan(_playerId);
				//Console.Clear();
				ResetColor();

				var player = _context.GetPlayer(_playerId);
				DrawMap(_context.GetMap(player.CurrentMap));

				player.VisibleItems.ToList().ForEach(DrawItem);
				player.VisibleEntities.ToList().ForEach(DrawEntity);

				var messages = _context.Messages.Take(3).Select((m, i) => new {Text = m, Index = i});
				foreach (var message in messages)
				{
					Console.SetCursorPosition(0, Console.WindowTop + message.Index);
					Console.Write(message.Text);
				}

				var key = Console.ReadKey(true);
				
				if (key.Key == ConsoleKey.Escape)
				{
					break;
				}

				var direction = GetPlayerDirection(key.Key);

				if (direction != Direction.None)
				{
					_context.MovePlayer(_playerId, direction);
				}
			}

			Console.ResetColor();
			Console.Clear();
			Console.CursorVisible = true;
		}

		private static Direction GetPlayerDirection(ConsoleKey key)
		{
			switch (key)
			{
				case ConsoleKey.Z:
					return Direction.DownLeft;
				case ConsoleKey.X:
					return Direction.Down;
				case ConsoleKey.C:
					return Direction.DownRight;

				case ConsoleKey.A:
					return Direction.Left;
				case ConsoleKey.D:
					return Direction.Right;

				case ConsoleKey.Q:
					return Direction.UpLeft;
				case ConsoleKey.W:
					return Direction.Up;
				case ConsoleKey.E:
					return Direction.UpRight;

				default:
					return Direction.None;
			}
		}

		private void ResetColor()
		{
			Console.ResetColor();
			Console.BackgroundColor = ConsoleColor.Black;
		}

		private void DrawMap(Map map)
		{
			foreach (var position in map.AllPositions)
			{
				DrawTile(position, map.GetPositionValue(position));
			}
		}

		private void InitPlayer()
		{
			_playerId = _context.Party.FirstOrDefault() ?? 
				_context.CreateNewCharacter("Blahonga", 14, 14, 10, 10, 10);
		}

		private void DrawTile(Position pos, uint tileValue)
		{
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.SetCursorPosition(pos.X, pos.Y + 4);
			Console.Write(GetTileChar(tileValue));
			ResetColor();
		}

		private void DrawItem(Item item)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.SetCursorPosition(item.XPos, item.YPos + 4);
			Console.Write(item.Name[0]);
			ResetColor();
		}

		private void DrawEntity(Item item)
		{
			Console.SetCursorPosition(item.XPos, item.YPos + 4);

			if (item.Id == _playerId)
			{
				Console.ForegroundColor = ConsoleColor.Magenta;
				Console.Write('@');
			}
			else
			{
				if (item.Type == "monster")
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.Write(item.Name[0]);
				}
				else if (item.Type == "character")
				{
					var character =_client.GetCharacter(item.Id);

					Console.ForegroundColor = character.Error != null ?
						ConsoleColor.Red : 
						ConsoleColor.Green;

					Console.Write('@');
				}
			}

			ResetColor();
		}

		private char GetTileChar(uint tileValue)
		{
			var flags = (TileFlags)tileValue;

			if (flags == TileFlags.NOTHING)
				return '#';

			if ((flags & TileFlags.BLOCKED) > 0)
				return '¤';

			if ((flags & TileFlags.PERIMETER) > 0)
				return '#';

			if ((flags & TileFlags.DOOR1) > 0)
				return '1';
			if ((flags & TileFlags.DOOR2) > 0)
				return '2';
			if ((flags & TileFlags.DOOR3) > 0)
				return '3';
			if ((flags & TileFlags.DOOR4) > 0)
				return '4';

			if ((flags & TileFlags.ARCH) > 0)
				return '~';

			if ((flags & TileFlags.PORTCULLIS) > 0)
				return '€';

			if ((flags & TileFlags.CORRIDOR) > 0)
				return ' ';

			if ((flags & TileFlags.ROOM) > 0)
				return ' ';

			if ((flags & TileFlags.ENTRANCE) > 0)
				return '=';

			if ((flags & TileFlags.STAIR_DOWN) > 0)
				return '<';

			if ((flags & TileFlags.STAIR_UP) > 0)
				return '>';

			if ((flags & TileFlags.ROOM_ID) > 0)
				return 'I';

			if ((flags & TileFlags.LABEL) > 0)
				return 'L';

			return '*';
		}
	}
}