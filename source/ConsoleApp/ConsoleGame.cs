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
			InitPlayer();

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
			var area = new ConsoleArea((short) (max + 2), (short) (messages.Count() + 2));
			area.SetBorderStyle(ConsoleArea.BorderStyle.Single);
			area.SetBorderBackground(ConsoleColor.DarkBlue);
			area.SetBorderForeground(ConsoleColor.Cyan);
			area.SetDefaultBackground(ConsoleColor.DarkBlue);
			area.SetDefaultForeground(ConsoleColor.Cyan);
			area.SetTitle(title);

			for (int i = 0; i < messages.Length; i++)
			{
				area.Write(messages[i], 1, i + 1);
			}

			_console2.DrawArea(area, 
				(short) (_console2.Width / 2 - (area.Width / 2)),
				(short) (_console2.Height / 2 - (area.Height / 2)));

			return Console.ReadKey(true);
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

				mapArea.SetTitle(GetMapAreaTitle(player, map));

				messageArea.Clear();
				var messages = _context.Messages.Take(5).Select((m, i) => new { Text = m, Index = i });
				foreach (var message in messages)
				{
					messageArea.Write(message.Text, 0, message.Index);
				}

				messageArea.SetOffset(0, 0);
				mapArea.CenterOffset(player.XPos, player.YPos);

				_console2.DrawArea(mapArea, 0, 0);
				_console2.DrawArea(messageArea, 0, mapArea.Height);
				_console2.DrawArea(CreatePlayerArea(player), mapArea.Width, 0);

				debugArea.Clear();
				debugArea.Write(GetDebugInfo(player, map), 0, 0);
				_console2.DrawArea(debugArea, 0, (short) (mapArea.Height + messageArea.Height));

				var key = Console.ReadKey(true);

				if (key.Key == ConsoleKey.Escape)
				{
					break;
				}

				if (key.Key == ConsoleKey.P)
				{
					AddResponseMessage(_client.Get(player.Id));
					continue;
				}
				if (key.Key == ConsoleKey.O)
				{
					DropItem(player);
					continue;
				}
				if (key.Key == ConsoleKey.V)
				{
					WieldWeapon(player);
					continue;
				}
				if (key.Key == ConsoleKey.R)
				{
					EquipArmor(player);
					continue;
				}
				if (key.Key == ConsoleKey.T)
				{
					UnequipArmor(player);
					continue;
				}
				if (key.Key == ConsoleKey.B)
				{
					UnwieldWeapon(player);
					continue;
				}
				if (key.Key == ConsoleKey.F)
				{
					QuaffPotion(player);
					continue;
				}
				if (key.Key == ConsoleKey.OemPlus)
				{
					AllocatePoints(player);
					continue;
				}
				if (key.Key == ConsoleKey.U)
				{
					AddResponseMessage(_client.LevelUp(player.Id));
					continue;
				}
				if (key.Key == ConsoleKey.N)
				{
					AddResponseMessage(_client.LevelDown(player.Id));
					continue;
				}
				if (key.Key == ConsoleKey.I)
				{
					CreateMessagePopup("Visible Items",
					                   player.VisibleItems.Any()
						                   ? player.VisibleItems.Select(x => x.Name).ToArray()
						                   : new[] {"You can't see any items here..."});
					continue;
				}
				if (key.Key == ConsoleKey.K)
				{
					CreateMessagePopup("Visible Entities",
					                   player.VisibleEntities.Any()
						                   ? player.VisibleEntities.Select(x => x.Name).ToArray()
						                   : new[] {"You can't see any entities here..."});
					continue;
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