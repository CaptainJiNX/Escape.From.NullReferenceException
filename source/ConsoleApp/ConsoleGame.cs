using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApiClient;
using Newtonsoft.Json.Linq;
using Attribute = ApiClient.Attribute;

namespace ConsoleApp
{
	class ConsoleGame
	{
		private readonly IClientWrapper _client;
		private readonly GameContext _context;
		private string _playerId;
		private string _player1Id;
		private string _player2Id;
		private string _player3Id;
		private readonly Console2 _console2;
		private readonly Dictionary<string, ConsoleArea> _maps = new Dictionary<string, ConsoleArea>();

		public ConsoleGame()
		{
			_client = new ClientWrapper(Guid.Parse(File.ReadAllText("apikey.txt")));
			_context = new GameContext(_client, new BinaryMapStorage());
			_console2 = new Console2(100, 36, ConsoleColor.DarkRed);
		}

		public void RunGame()
		{
			InitPlayers();

			RunGameLoop();

			Console.WriteLine("Done...");
			Console.ReadLine();
		}

		private ConsoleArea CreateMapArea(Map map)
		{
			var area = new ConsoleArea(60, 30);
			area.SetBorderStyle(ConsoleArea.BorderStyle.Double);
			area.SetBorderBackground(ConsoleColor.Black);
			area.SetBorderForeground(ConsoleColor.White);
			area.SetTitle(map.Name);

			FillArea(map, area, map.AllPositions);

			return area;
		}

		private ConsoleArea CreatePlayerArea(Character player)
		{
			var area = new ConsoleArea(40, 30);
			area.SetBorderStyle(ConsoleArea.BorderStyle.Single);
			area.SetBorderBackground(ConsoleColor.DarkRed);
			area.SetBorderForeground(ConsoleColor.Red);
			area.SetDefaultBackground(ConsoleColor.Black);
			area.SetDefaultForeground(ConsoleColor.Green);
			area.SetTitle(player.Name);

			area.Write(string.Format("STR: {0}", player.Strength), 1, 1);
			area.Write(string.Format("DEX: {0}", player.Dexterity), 1, 2);
			area.Write(string.Format("CON: {0}", player.Constitution), 1, 3);
			area.Write(string.Format("INT: {0}", player.Intelligence), 1, 4);
			area.Write(string.Format("WIS: {0}", player.Wisdom), 1, 5);
			area.Write(string.Format("XP: {0}", player.Experience), 1, 7);
			area.Write(string.Format("Lvl: {0}", player.Level), 1, 8);


			var hpCol = ConsoleColor.Green;
			if (player.HitPoints < player.MaxHitPoints)
			{
				hpCol = player.HitPoints < player.MaxHitPoints / 2 ? ConsoleColor.Red : ConsoleColor.Yellow;
			}

			area.Write(string.Format("HP: {0}/{1}", player.HitPoints, player.MaxHitPoints), 1, 10, hpCol);
			area.Write(string.Format("AC: {0}", player.ArmorClass), 1, 11);

			area.Write("Weapon: " + player.WieldedWeaponName, 1, 12);
			area.Write("Armor: " + player.EquippedArmorName, 1, 13);

			area.Write("Inventory", 1, 15);
			area.Write("=========", 1, 16);

			for (int i = 0; i < player.Inventory.Length; i++)
			{
				area.Write(string.Format("{0}: {1}", i, _client.GetInfoFor(player.Inventory[i]).Name), 1, 17 + i);
			}

			area.SetOffset(0, 0);

			return area;
		}

		private ConsoleArea CreateMessageArea()
		{
			var area = new ConsoleArea(100, 5);
			area.SetBorderStyle(ConsoleArea.BorderStyle.Single);
			area.SetBorderBackground(ConsoleColor.DarkYellow);
			area.SetBorderForeground(ConsoleColor.Yellow);
			area.SetDefaultBackground(ConsoleColor.DarkYellow);
			area.SetDefaultForeground(ConsoleColor.White);
			area.SetTitle(" Messages ");
			return area;
		}

		private ConsoleArea CreateDebugArea()
		{
			var area = new ConsoleArea(100, 1);
			area.SetDefaultBackground(ConsoleColor.Black);
			area.SetDefaultForeground(ConsoleColor.White);
			return area;
		}

		private ConsoleKeyInfo CreateMessagePopup(string title, string[] messages)
		{
			var max = Math.Max(messages.Max(x => x.Length), title.Length);
			var area = new ConsoleArea((short) (max + 4), (short) (messages.Count() + 2));
			area.SetBorderStyle(ConsoleArea.BorderStyle.Single);
			area.SetBorderBackground(ConsoleColor.DarkBlue);
			area.SetBorderForeground(ConsoleColor.Cyan);
			area.SetDefaultBackground(ConsoleColor.DarkBlue);
			area.SetDefaultForeground(ConsoleColor.Cyan);
			area.SetTitle(title);

			for (int i = 0; i < messages.Length; i++)
			{
				area.Write(messages[i], 2, i + 1);
			}

			_console2.DrawArea(area, 
				(short) (_console2.Width / 2 - (area.Width / 2)),
				(short) (_console2.Height / 2 - (area.Height / 2)));

			return Console.ReadKey(true);
		}

		private string CreateTextInputPopup(string title, string prompt)
		{
			var area = new ConsoleArea(70, 3);
			area.SetBorderStyle(ConsoleArea.BorderStyle.Single);
			area.SetBorderBackground(ConsoleColor.Black);
			area.SetBorderForeground(ConsoleColor.White);
			area.SetDefaultBackground(ConsoleColor.Black);
			area.SetDefaultForeground(ConsoleColor.White);
			area.SetTitle(title);

			area.Write(prompt, 1, 0);
			area.SetOffset(0, 0);

			var xPos = (short) (_console2.Width/2 - (area.Width/2));
			var yPos = (short) (_console2.Height/2 - (area.Height/2));

			_console2.DrawArea(area, xPos, yPos);
			
			Console.SetCursorPosition(xPos + prompt.Length + 3, yPos + 1);
			Console.CursorVisible = true;
			var result = Console.ReadLine();
			Console.CursorVisible = false;

			return result;
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
			var debugArea = CreateDebugArea();

			while (true)
			{
				_context.Scan(_playerId);

				var player = _context.GetPlayer(_playerId);
				var map = _context.GetMap(player.CurrentMap);
				var mapArea = UpdateMapArea(map, player, previousItemsAndEntities);
				UpdateMessageArea(messageArea);
				UpdateDebugArea(debugArea, player, map);

				previousItemsAndEntities = GetAllVisibleItemPositions(player);

				_console2.DrawArea(mapArea, 0, 0);
				_console2.DrawArea(messageArea, 0, mapArea.Height);
				_console2.DrawArea(CreatePlayerArea(player), mapArea.Width, 0);
				_console2.DrawArea(debugArea, 0, (short) (mapArea.Height + messageArea.Height));

				var key = Console.ReadKey(true);

				if (key.Key == ConsoleKey.Escape)
				{
					break;
				}

				HandleKeyPress(key, player);
			}

			Console.ResetColor();
			Console.Clear();
			Console.CursorVisible = true;
		}

		private void HandleKeyPress(ConsoleKeyInfo key, Character player)
		{
			switch (key.Key)
			{
				case ConsoleKey.P:
					AddResponseMessage(_client.Get(player.Id));
					break;
				case ConsoleKey.O:
					DropItem(player);
					break;
				case ConsoleKey.V:
					WieldWeapon(player);
					break;
				case ConsoleKey.R:
					EquipArmor(player);
					break;
				case ConsoleKey.T:
					UnequipArmor(player);
					break;
				case ConsoleKey.B:
					UnwieldWeapon(player);
					break;
				case ConsoleKey.F:
					QuaffPotion(player);
					break;
				case ConsoleKey.OemPlus:
					AllocatePoints(player);
					break;
				case ConsoleKey.U:
					AddResponseMessage(_client.LevelUp(player.Id));
					break;
				case ConsoleKey.N:
					AddResponseMessage(_client.LevelDown(player.Id));
					break;
				case ConsoleKey.I:
					CreateMessagePopup("Visible Items",
					                   player.VisibleItems.Any()
						                   ? player.VisibleItems.Select(x => x.Name).ToArray()
						                   : new[] {"You can't see any items here..."});
					break;
				case ConsoleKey.K:
					CreateMessagePopup("Visible Entities",
					                   player.VisibleEntities.Any()
						                   ? player.VisibleEntities.Select(x => x.Name).ToArray()
						                   : new[] {"You can't see any entities here..."});
					break;
				case ConsoleKey.OemMinus:
					var command = CreateTextInputPopup("What would you like do do?", "Type command:");
					HandleCommand(command);
					break;
				default:
					var direction = GetPlayerDirection(key.Key);

					if (direction != Direction.None)
					{
						_context.MovePlayer(_playerId, direction);
					}
					else
					{
						switch (key.KeyChar)
						{
							case '1':
								_playerId = _player1Id;
								break;
							case '2':
								_playerId = _player2Id;
								break;
							case '3':
								_playerId = _player3Id;
								break;
						}
					}
					break;
			}
		}

		private static IEnumerable<Position> GetAllVisibleItemPositions(Character player)
		{
			return player.VisibleItems.Concat(player.VisibleEntities)
			             .Select(x => new Position(x.XPos, x.YPos));
		}

		private void UpdateDebugArea(ConsoleArea debugArea, Character player, Map map)
		{
			debugArea.Clear();
			debugArea.Write(GetDebugInfo(player, map), 0, 0);
		}

		private void UpdateMessageArea(ConsoleArea messageArea)
		{
			messageArea.Clear();
			var messages = _context.Messages.Take(5).Select((m, i) => new {Text = m, Index = i});
			foreach (var message in messages)
			{
				messageArea.Write(message.Text, 0, message.Index);
			}
			messageArea.SetOffset(0, 0);
		}

		private ConsoleArea UpdateMapArea(Map map, Character player, IEnumerable<Position> otherPositionsToRedraw)
		{
			if (!_maps.ContainsKey(map.Name))
			{
				_maps[map.Name] = CreateMapArea(map);
			}
			else
			{
				FillArea(map, _maps[map.Name], player.VisibleArea.Concat(otherPositionsToRedraw));
			}

			var mapArea = _maps[map.Name];

			player.VisibleItems.ToList().ForEach(item => DrawItem(item, mapArea));
			player.VisibleEntities.ToList().ForEach(item => DrawEntity(item, mapArea));

			mapArea.SetTitle(GetMapAreaTitle(player, map));
			mapArea.CenterOffset(player.XPos, player.YPos);

			return mapArea;
		}

		private void HandleCommand(string command)
		{
			switch ((command ?? "").ToLowerInvariant())
			{
				case "findpath":
					FindPath();
					break;

				default:
					CreateMessagePopup("Unknown command", new[]
					{
						"Select one of the following",
						"---------------------------",
						"findpath"
					});
					break;
			}
		}

		private void FindPath()
		{
			var player = _context.GetPlayer(_playerId);
			var map = _context.GetMap(player.CurrentMap);
			var mapArea = CreateMapArea(map);

			_console2.DrawArea(CreatePlayerArea(player), mapArea.Width, 0);

			var startPos = SelectPosition(map, new Position(player.XPos, player.YPos), "Select start position");
			if (startPos == null) return;

			mapArea.Write("1", startPos.X, startPos.Y, ConsoleColor.Green, ConsoleColor.DarkGreen);
			mapArea.CenterOffset(startPos.X, startPos.Y);
			_console2.DrawArea(mapArea, 0, 0);

			var endPos = SelectPosition(map, startPos, "Select end position");
			if (endPos == null) return;

			mapArea.Write("2", endPos.X, endPos.Y, ConsoleColor.Green, ConsoleColor.DarkGreen);
			mapArea.CenterOffset(endPos.X, endPos.Y);
			_console2.DrawArea(mapArea, 0, 0);

			Console.ReadKey(true);
		}

		private Position SelectPosition(Map map, Position start, string title)
		{
			var mapArea = CreateMapArea(map);
			var messageArea = CreateMessageArea();
			mapArea.SetTitle(title);
			messageArea.SetTitle(title);

			var position = start;

			while (true)
			{
				var previousPosition = position;

				mapArea.Write("S", start.X, start.Y, ConsoleColor.Blue, ConsoleColor.DarkBlue);
				mapArea.Write("X", position.X, position.Y, ConsoleColor.Red, ConsoleColor.DarkRed);
				mapArea.CenterOffset(position.X, position.Y);

				messageArea.Clear();
				messageArea.Write(string.Format("Select position and press [ENTER] ([Escape] to abort)"), 1, 1);
				messageArea.Write(string.Format("Current position: {0}, {1}", position.X, position.Y), 1, 2);

				_console2.DrawArea(mapArea, 0, 0);
				_console2.DrawArea(messageArea, 0, mapArea.Height);

				switch (Console.ReadKey(true).Key)
				{
					case ConsoleKey.UpArrow:
						position = new Position(position.X, position.Y - 1);
						break;
					case ConsoleKey.DownArrow:
						position = new Position(position.X, position.Y + 1);
						break;
					case ConsoleKey.LeftArrow:
						position = new Position(position.X - 1, position.Y);
						break;
					case ConsoleKey.RightArrow:
						position = new Position(position.X + 1, position.Y);
						break;
					case ConsoleKey.Enter:
						return position;
					case ConsoleKey.Escape:
						return null;
				}

				FillArea(map, mapArea, new[] { previousPosition });
			}
		}

		private string GetDebugInfo(Character player, Map map)
		{
			var positionValue = (TileFlags)map.GetPositionValue(new Position(player.XPos, player.YPos));
			var debug = string.Format("Current: {0}", positionValue);

			var flags = Enum.GetValues(typeof (TileFlags))
			                .Cast<TileFlags>()
			                .Where(flag => flag != TileFlags.NOTHING)
			                .ToList();

			var exactMatches = flags.Where(flag => (positionValue & flag) == flag).ToList();

			foreach (var flag in flags)
			{
				var tileFlags = positionValue & flag;

				if (tileFlags > 0 && tileFlags != flag)
				{
					debug += string.Format(" | {0}:{1}", flag, tileFlags);
				}
			}

			debug += string.Format(" ({0})", string.Join("|", exactMatches));

			var label = (uint) (positionValue & TileFlags.LABEL);
			if (label > 0)
			{
				debug += string.Format(" LBL:'{0}'", Console2.Encoding.GetString(BitConverter.GetBytes(label)));
			}

			return debug;
		}

		private string GetMapAreaTitle(Character player, Map map)
		{
			var activeTile = map.GetPositionValue(new Position(player.XPos, player.YPos));
			var item = player.VisibleItems.FirstOrDefault(i => i.XPos == player.XPos && i.YPos == player.YPos);

			if (item != null)
			{
				return string.Format("{0}/{1} ({2},{3})", map.Name, item.Name, player.XPos, player.YPos);
			}

			var roomId = (activeTile & (uint) TileFlags.ROOM_ID);

			if (roomId > 0)
			{
				return string.Format("{0}/{1} ({2},{3})", map.Name, roomId, player.XPos, player.YPos);
			}

			return string.Format("{0} ({1},{2})", map.Name, player.XPos, player.YPos);
		}

		private void WieldWeapon(Character player)
		{
			var itemId = SelectFromInventory("Wield", player);
			if (itemId == null) return;
			AddResponseMessage(_client.Wield(itemId, player.Id));
		}

		private void DropItem(Character player)
		{
			var itemId = SelectFromInventory("Drop", player);
			if (itemId == null) return;
			AddResponseMessage(_client.Drop(itemId, player.Id));
		}

		private void UnwieldWeapon(Character player)
		{
			if (player.WieldedWeaponId == null) return;
			AddResponseMessage(_client.Unwield(player.WieldedWeaponId, player.Id));
		}

		private void EquipArmor(Character player)
		{
			var itemId = SelectFromInventory("Equip", player);
			if (itemId == null) return;
			AddResponseMessage(_client.Equip(itemId, player.Id));
		}

		private void UnequipArmor(Character player)
		{
			if (player.EquippedArmorId == null) return;
			AddResponseMessage(_client.Unequip(player.EquippedArmorId, player.Id));
		}

		private void QuaffPotion(Character player)
		{
			var itemId = SelectFromInventory("Quaff", player);
			if (itemId == null) return;
			AddResponseMessage(_client.Quaff(itemId, player.Id));
		}

		private void AllocatePoints(Character player)
		{
			var attr = SelectFromAttributes(player);
			if (attr == null) return;
			AddResponseMessage(_client.AllocatePoints((Attribute) Enum.Parse(typeof(Attribute), attr, true) , player.Id));
		}

		private void AddResponseMessage(JObject response)
		{
			var message = (response["success"] ?? response["error"]);
			if (message != null)
			{
				_context.AddMessage(message.ToObject<string>());
			}
		}

		private string SelectFromInventory(string action, Character player)
		{
			var inventory = player.Inventory.Select(x => _client.GetInfoFor(x)).ToList();

			if (!inventory.Any())
			{
				CreateMessagePopup("Sorry can't " + action, new[] { "You don't carry anything." });
				return null;
			}

			var formattedList = inventory.Select((x, i) => string.Format("{0}: {1}", i, x.Name));
			var selectedKey = CreateMessagePopup(action + " which item?", formattedList.ToArray());
			int choice;

			if (int.TryParse(selectedKey.KeyChar.ToString(), out choice))
			{
				if (choice >= 0 && choice < inventory.Count())
				{
					return inventory.ElementAt(choice).Id;
				}
			}

			return null;
		}

		private string SelectFromAttributes(Character player)
		{
			var attributes = new[]
			{
				new {Attribute = "STR", Value = player.Strength},
				new {Attribute = "DEX", Value = player.Dexterity},
				new {Attribute = "CON", Value = player.Constitution},
				new {Attribute = "INT", Value = player.Intelligence},
				new {Attribute = "WIS", Value = player.Wisdom}
			};

			var formattedList = attributes.Select((x, i) => string.Format("{0}: {1} ({2})", i, x.Attribute, x.Value));

			var selectedKey = CreateMessagePopup("Increase which attribute?", formattedList.ToArray());
			int choice;

			if (int.TryParse(selectedKey.KeyChar.ToString(), out choice))
			{
				if (choice >= 0 && choice < attributes.Count())
				{
					return attributes.ElementAt(choice).Attribute;
				}
			}

			return null;
		}

		private static Direction GetPlayerDirection(ConsoleKey key)
		{
			switch (key)
			{
				case ConsoleKey.Z:
					return Direction.DownLeft;
				case ConsoleKey.S:
				case ConsoleKey.DownArrow:
					return Direction.Down;
				case ConsoleKey.C:
					return Direction.DownRight;

				case ConsoleKey.A:
				case ConsoleKey.LeftArrow:
					return Direction.Left;
				case ConsoleKey.D:
				case ConsoleKey.RightArrow:
					return Direction.Right;

				case ConsoleKey.Q:
					return Direction.UpLeft;
				case ConsoleKey.W:
				case ConsoleKey.UpArrow:
					return Direction.Up;
				case ConsoleKey.E:
					return Direction.UpRight;

				default:
					return Direction.None;
			}
		}

		private void InitPlayers()
		{
			_player1Id = _context.Party.FirstOrDefault() ??
				_context.CreateNewCharacter("JiNX the first", 14, 14, 10, 10, 10);
			_player2Id = _context.Party.Skip(1).FirstOrDefault() ??
				_context.CreateNewCharacter("JiNX the second", 14, 10, 14, 10, 10);
			_player3Id = _context.Party.Skip(2).FirstOrDefault() ??
				_context.CreateNewCharacter("JiNX the third", 10, 10, 18, 10, 10);

			_playerId = _player1Id;
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
					var character = _client.GetCharacter(item.Id);

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

			var bgCol = (flags & TileFlags.LABEL) > 0 ? ConsoleColor.DarkBlue : ConsoleColor.Black;

			if (flags == TileFlags.NOTHING)
				return new Tile('#', ConsoleColor.DarkGray, bgCol);
			if ((flags & TileFlags.STAIR_UP) > 0)
				return new Tile('>', ConsoleColor.Cyan, bgCol);
			if ((flags & TileFlags.STAIR_DOWN) > 0)
				return new Tile('<', ConsoleColor.Cyan, bgCol);
			if ((flags & TileFlags.PORTCULLIS) > 0)
				return new Tile('€', backColor:bgCol);
			if ((flags & (TileFlags.DOOR1 | TileFlags.DOOR2 | TileFlags.DOOR3 | TileFlags.DOOR4)) > 0)
				return new Tile('+', ConsoleColor.White,bgCol);
			if ((flags & TileFlags.ARCH) > 0)
				return new Tile('~', backColor:bgCol);
			if ((flags & TileFlags.ENTRANCE) > 0)
				return new Tile('=', backColor:bgCol);
			if ((flags & TileFlags.PERIMETER) > 0)
				return new Tile('#', ConsoleColor.White,bgCol);
			if ((flags & TileFlags.CORRIDOR) > 0)
				return new Tile(' ', backColor: bgCol);
			if ((flags & TileFlags.ROOM) > 0)
				return new Tile(' ', backColor: bgCol);
			if ((flags & TileFlags.BLOCKED) > 0)
				return new Tile('¤', backColor: bgCol);
			return new Tile('_');
		}
	}
}