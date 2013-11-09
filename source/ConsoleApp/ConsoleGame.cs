using System;
using System.Collections.Generic;
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

			var player = _context.GetPlayer(_playerId);
			var map = _context.GetMap(player.CurrentMap);
			DrawMap(map, map.AllPositions);

			var previousItemsAndEntities = Enumerable.Empty<Position>();

			while (true)
			{
				_context.Scan(_playerId);
				ResetColor();

				player = _context.GetPlayer(_playerId);
				map = _context.GetMap(player.CurrentMap);
				DrawMap(map, player.VisibleArea.Concat(previousItemsAndEntities));

				var items = player.VisibleItems.ToList();
				var entities = player.VisibleEntities.ToList();

				player.VisibleItems.ToList().ForEach(DrawItem);
				player.VisibleEntities.ToList().ForEach(DrawEntity);

				previousItemsAndEntities = items.Concat(entities).Select(x => new Position(x.XPos, x.YPos));

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

		private void DrawMap(Map map, IEnumerable<Position> positions)
		{
			foreach (var position in positions.OrderBy(x => x.Y).ThenBy(x => x.X))
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
			var tile = GetTile(tileValue);
			Console.ForegroundColor = tile.ForeColor;
			Console.BackgroundColor = tile.BackColor;
			Console.SetCursorPosition(pos.X, pos.Y + 4);
			Console.Write(tile.Character);
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

		private class Tile
		{
			public Tile(char character,
 				ConsoleColor foreColor = ConsoleColor.Gray,
				ConsoleColor backColor = ConsoleColor.Black)
			{
				Character = character;
				ForeColor = foreColor;
				BackColor = backColor;
			}

			public char Character { get; private set; }
			public ConsoleColor ForeColor { get; private set; }
			public ConsoleColor BackColor { get; private set; }
		}

		private Tile GetTile(uint tileValue)
		{
			var flags = (TileFlags)tileValue;

			//if ((flags & TileFlags.LABEL) > 0)
			//	return new Tile('L');
			//if ((flags & TileFlags.ROOM_ID) > 0)
			//	return new Tile('I');

			if (flags == TileFlags.NOTHING)
				return new Tile('#', ConsoleColor.DarkGray);
			if ((flags & TileFlags.STAIR_UP) > 0)
				return new Tile('>', ConsoleColor.Cyan);
			if ((flags & TileFlags.STAIR_DOWN) > 0)
				return new Tile('<', ConsoleColor.Cyan);
			if ((flags & TileFlags.PORTCULLIS) > 0)
				return new Tile('€');
			if ((flags & (TileFlags.DOOR1 | TileFlags.DOOR2 | TileFlags.DOOR3 | TileFlags.DOOR4)) > 0)
				return new Tile('+', ConsoleColor.White);
			if ((flags & TileFlags.ARCH) > 0)
				return new Tile('~');
			if ((flags & TileFlags.ENTRANCE) > 0)
				return new Tile('=');
			if ((flags & TileFlags.PERIMETER) > 0)
				return new Tile('#', ConsoleColor.White);
			if ((flags & TileFlags.CORRIDOR) > 0)
				return new Tile(' ');
			if ((flags & TileFlags.ROOM) > 0)
				return new Tile(' ');
			if ((flags & TileFlags.BLOCKED) > 0)
				return new Tile('¤');
			return new Tile('_');
		}
	}
}