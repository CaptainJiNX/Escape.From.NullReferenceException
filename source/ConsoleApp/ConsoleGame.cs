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
		private readonly Console2 _console2; 

		public ConsoleGame()
		{
			_client = new ClientWrapper(Guid.Parse(File.ReadAllText("apikey.txt")));
			_context = new GameContext(_client, new BinaryMapStorage());
			_console2 = new Console2(100, 30, ConsoleColor.DarkRed);
		}

		public void RunGame()
		{
			InitPlayer();

			RunGameLoop();

			Console.WriteLine("Done...");
			Console.ReadLine();
		}

		private readonly Dictionary<string, ConsoleArea> _maps = new Dictionary<string, ConsoleArea>();

		private ConsoleArea CreateMapArea(Map map)
		{
			var area = new ConsoleArea(60, 25);
			area.SetBorderStyle(ConsoleArea.BorderStyle.Double);
			area.SetBorderBackground(ConsoleColor.Black);
			area.SetBorderForeground(ConsoleColor.White);
			area.SetTitle(map.Name);

			FillArea(map, area, map.AllPositions);

			return area;
		}

		private ConsoleArea CreateMessageArea()
		{
			var area = new ConsoleArea(100, 5);
			area.SetBorderStyle(ConsoleArea.BorderStyle.Single);
			area.SetBorderBackground(ConsoleColor.DarkBlue);
			area.SetBorderForeground(ConsoleColor.Cyan);
			area.SetDefaultBackground(ConsoleColor.DarkBlue);
			area.SetDefaultForeground(ConsoleColor.Cyan);
			area.SetTitle(" Messages ");
			return area;
		}

		private void FillArea(Map map, ConsoleArea area, IEnumerable<Position> positions)
		{
			foreach (var position in positions)
			{
				var tile = GetTile(map.GetPositionValue(position));
				area.Write(tile.Character, position.X, position.Y, tile.ForeColor, tile.BackColor);
			}
		}

		private void RunGameLoop()
		{
			var previousItemsAndEntities = Enumerable.Empty<Position>();
			var messageArea = CreateMessageArea();

			while (true)
			{
				_context.Scan(_playerId);

				var player = _context.GetPlayer(_playerId);
				var map = _context.GetMap(player.CurrentMap);
				
				if (!_maps.ContainsKey(map.Name))
				{
					_maps[map.Name] = CreateMapArea(map);
				}
				else
				{
					FillArea(map, _maps[map.Name], player.VisibleArea.Concat(previousItemsAndEntities));
				}

				var mapArea = _maps[map.Name];

				var items = player.VisibleItems.ToList();
				var entities = player.VisibleEntities.ToList();

				player.VisibleItems.ToList().ForEach(item => DrawItem(item, mapArea));
				player.VisibleEntities.ToList().ForEach(item => DrawEntity(item, mapArea));

				previousItemsAndEntities = items.Concat(entities).Select(x => new Position(x.XPos, x.YPos));

				messageArea.Clear();
				var messages = _context.Messages.Take(5).Select((m, i) => new {Text = m, Index = i});
				foreach (var message in messages)
				{
					messageArea.Write(message.Text, 0, message.Index);
				}

				messageArea.SetOffset(0, 0);
				mapArea.CenterOffset(player.XPos, player.YPos);

				_console2.DrawArea(mapArea, 0, 0);
				_console2.DrawArea(messageArea, 0, mapArea.Height);

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

		private void InitPlayer()
		{
			_playerId = _context.Party.FirstOrDefault() ?? 
				_context.CreateNewCharacter("Blahonga", 14, 14, 10, 10, 10);
		}

		private void DrawItem(Item item, ConsoleArea area)
		{
			area.Write(item.Name[0], item.XPos, item.YPos, ConsoleColor.Yellow);
		}

		private void DrawEntity(Item item, ConsoleArea area)
		{
			if (item.Id == _playerId)
			{
				area.Write('@', item.XPos, item.YPos, ConsoleColor.Magenta);
			}
			else
			{
				if (item.Type == "monster")
				{
					area.Write(item.Name[0], item.XPos, item.YPos, ConsoleColor.Red);
				}
				else if (item.Type == "character")
				{
					var character =_client.GetCharacter(item.Id);

					var color = character.Error != null ?
						ConsoleColor.Red : 
						ConsoleColor.Green;

					area.Write('@', item.XPos, item.YPos, color);
				}
			}
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