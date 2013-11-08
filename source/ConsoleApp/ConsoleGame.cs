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
		private Character _player;

		public ConsoleGame()
		{
			_client = new ClientWrapper(Guid.Parse(File.ReadAllText("apikey.txt")));
			_context = new GameContext(_client);
		}

		public void RunGame()
		{
			ResetColor();
			Console.Clear();
			Console.CursorVisible = false;

			DeleteAllCharacters();
			CreatePlayer();

			RunGameLoop();

			DeleteAllCharacters();

			Console.CursorVisible = true;
			Console.WriteLine("Done...");
			Console.ReadLine();
		}

		private void RunGameLoop()
		{
			while (true)
			{
				_context.Scan(_player);
				//Console.Clear();

				DrawMap(_context.GetMap(_player.CurrentMap));

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
					_context.MovePlayer(_player, direction);
				}
			}
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
			ResetColor();
			// Console.Clear();

			foreach (var position in map.AllKnown)
			{
				DrawTile(position, map.GetPositionValue(position));
			}

			foreach (var item in map.Items)
			{
				DrawItem(item.Value, _client.GetInfoFor(item.Key));
			}

			foreach (var item in map.Entities)
			{
				DrawEntity(item.Value, _client.GetInfoFor(item.Key));
			}
		}

		private void CreatePlayer()
		{
			_context.AddCharacter("Blahonga", 14, 14, 10, 10, 10);
			_player = _context.Party.First();
		}

		private void DrawTile(Position pos, uint tileValue)
		{
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.SetCursorPosition(pos.X, pos.Y + 4);
			Console.Write(GetTileChar(tileValue));
			ResetColor();
		}

		private void DrawItem(Position pos, ItemInfo item)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.SetCursorPosition(pos.X, pos.Y + 4);
			Console.Write(item.Name[0]);
			ResetColor();
		}

		private void DrawEntity(Position pos, ItemInfo item)
		{
			Console.SetCursorPosition(pos.X, pos.Y + 4);

			if (item.Id == _player.Id)
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

					Console.ForegroundColor = character["error"] != null ?
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

		private void DeleteAllCharacters()
		{
			var party = _client.GetParty();

			foreach (var charId in party["characters"].Values<string>())
			{
				Console.WriteLine(_client.DeleteCharacter(charId));
			}
		}
	}
}