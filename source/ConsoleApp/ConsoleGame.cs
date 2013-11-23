using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApiClient;
using Attribute = ApiClient.Attribute;

namespace ConsoleApp
{
	class ConsoleGame
	{
		private readonly GameContext _context;
		private string _currentPlayerId;
		private string _player1Id;
		private string _player2Id;
		private string _player3Id;
		private readonly Console2 _console2;
		private readonly Dictionary<string, ConsoleArea> _maps = new Dictionary<string, ConsoleArea>();
		private static readonly string[] PlayerNames = new[] { "JiNX the first", "JiNX the second", "JiNX the third" };

		public ConsoleGame()
		{
			var client = new ClientWrapper(Guid.Parse(File.ReadAllText("apikey.txt")));
			_context = new GameContext(client, new BinaryMapStorage(), new DamageStatisticsStorage());
			_console2 = new Console2(120, 41, ConsoleColor.DarkRed);
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
			var area = new ConsoleArea(60+10, 30+3);
			area.SetBorderStyle(ConsoleArea.BorderStyle.Double);
			area.SetBorderBackground(ConsoleColor.Black);
			area.SetBorderForeground(ConsoleColor.White);
			area.SetTitle(map.Name);

			FillArea(map, area, map.AllPositions);

			return area;
		}

		private ConsoleArea CreatePlayerArea(Character player)
		{
			var area = new ConsoleArea(40+10, 30+3);
			area.SetBorderStyle(ConsoleArea.BorderStyle.Single);
			area.SetBorderBackground(ConsoleColor.DarkRed);
			area.SetBorderForeground(ConsoleColor.Red);
			area.SetDefaultBackground(ConsoleColor.Black);
			area.SetDefaultForeground(ConsoleColor.Green);
			area.SetTitle(player.Name);

			var row = 0;

			area.Write(string.Format("STR: {0}", player.Strength), 1, ++row);
			area.Write(string.Format("DEX: {0}", player.Dexterity), 1, ++row);
			area.Write(string.Format("CON: {0}", player.Constitution), 1, ++row);
			area.Write(string.Format("INT: {0}", player.Intelligence), 1, ++row);
			area.Write(string.Format("WIS: {0}", player.Wisdom), 1, ++row);

			row++;
			area.Write(string.Format("XP: {0}", player.Experience), 1, ++row);
			area.Write(string.Format("Lvl: {0}", player.Level), 1, ++row);


			var hpCol = ConsoleColor.Green;
			if (player.HitPoints < player.MaxHitPoints)
			{
				hpCol = player.HitPoints < player.MaxHitPoints / 2 ? ConsoleColor.Red : ConsoleColor.Yellow;
			}

			row++;
			area.Write(string.Format("HP: {0}/{1}", player.HitPoints, player.MaxHitPoints), 1, ++row, hpCol);
			area.Write(string.Format("AC: {0}", player.ArmorClass), 1, ++row);
			area.Write("Weapon: " + player.WieldedWeaponName, 1, ++row);
			area.Write("Armor: " + player.EquippedArmorName, 1, ++row);
			area.Write("Light: " + player.Light, 1, ++row);
			area.Write("Speed: " + player.Speed, 1, ++row);
			area.Write("Avail. pts: " + player.PointsToAllocate, 1, ++row);

			row++;
			area.Write("Inventory", 1, ++row);
			area.Write("=========", 1, ++row);

			for (int i = 0; i < player.Inventory.Length; i++)
			{
				area.Write(string.Format("{0}: {1}", i, _context.GetInfoFor(player.Inventory[i]).Name), 1, ++row);
			}

			area.SetOffset(0, 0);

			return area;
		}

		private ConsoleArea CreateMessageArea()
		{
			var area = new ConsoleArea(100+20, 5+2);
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
			var area = new ConsoleArea(100+20, 1);
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
				_context.Scan(_currentPlayerId);

				var player = _context.GetPlayer(_currentPlayerId);
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
				case ConsoleKey.Spacebar:
					break;
				case ConsoleKey.X:
					_context.Move(player.Id, _context.GetNextDirectionForPlayer(player.Id));
					break;
				case ConsoleKey.Y:
					_context.Scout(player.Id);
					break;
				case ConsoleKey.P:
					_context.PickUpItem(player.Id);
					break;
				case ConsoleKey.O:
					_context.DropItem(player.Id, SelectFromInventory("Drop", player));
					break;
				case ConsoleKey.V:
					_context.WieldWeapon(player.Id, SelectFromInventory("Wield", player));
					break;
				case ConsoleKey.R:
					_context.EquipArmor(player.Id, SelectFromInventory("Equip", player));
					break;
				case ConsoleKey.T:
					_context.UnequipArmor(player.Id, player.EquippedArmorId);
					break;
				case ConsoleKey.B:
					_context.UnwieldWeapon(player.Id, player.WieldedWeaponId);
					break;
				case ConsoleKey.F:
					_context.QuaffPotion(player.Id, SelectFromInventory("Quaff", player));
					break;
				case ConsoleKey.G:
					_context.QuickQuaff(player.Id, x => x.IsGaseousPotion);
					break;
				case ConsoleKey.H:
					_context.QuickQuaff(player.Id, x => x.IsHealingPotion);
					break;
				case ConsoleKey.OemPlus:
					IncreaseAttribute(player);
					break;
				case ConsoleKey.U:
					_context.MoveUp(player.Id);
					break;
				case ConsoleKey.N:
					_context.MoveDown(player.Id);
					break;
				case ConsoleKey.J:
					var plane = CreateTextInputPopup("Planeshift", "Shift to plane:");
					_context.Planeshift(player.Id, plane);
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
				case ConsoleKey.M:
					InitPlayers();
					break;
				default:
					var direction = GetPlayerDirection(key.Key);

					if (direction != Direction.None)
					{
						_context.Move(_currentPlayerId, direction);
					}
					else
					{
						switch (key.KeyChar)
						{
							case '1':
								_currentPlayerId = _player1Id;
								break;
							case '2':
								_currentPlayerId = _player2Id;
								break;
							case '3':
								_currentPlayerId = _player3Id;
								break;
							case '0':
								_context.ToggleAttackMode(_currentPlayerId);
								break;
							case '9':
								_context.TogglePvPMode(_currentPlayerId);
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

			var goalForPlayer = _context.GetGoalForPlayer(player.Id);
			if (goalForPlayer != null)
			{
				var tile = GetTile(map.GetPositionValue(goalForPlayer));
				mapArea.Write(tile.Character, goalForPlayer.X, goalForPlayer.Y, ConsoleColor.Green, ConsoleColor.DarkGreen);
			}

			player.VisibleItems.ToList().ForEach(item => DrawItem(item, mapArea));
			player.VisibleEntities.ToList().ForEach(item => DrawEntity(item, mapArea));

			DrawPlayer(player.Id, player.XPos, player.YPos, mapArea);

			mapArea.SetTitle(GetMapAreaTitle(player, map));
			mapArea.CenterOffset(player.XPos, player.YPos);

			return mapArea;
		}

		private void HandleCommand(string command)
		{
			var args = command.Split(' ').Skip(1);
			var cmd = command.Split(' ').FirstOrDefault();

			switch ((cmd ?? "").ToLowerInvariant())
			{
				case "fp":
				case "findpath":
					FindPath(args);
					break;

				case "fu":
				case "findup":
					FindPosition(TileFlags.STAIR_UP);
					break;

				case "fd":
				case "finddown":
					FindPosition(TileFlags.STAIR_DOWN);
					break;

				case "sg":
				case "setgoal":
					SetGoal(args);
					break;

				case "hs":
				case "highscores":
					ShowHighScores();
					break;

				default:
					CreateMessagePopup("Unknown command", new[]
					{
						"Select one of the following",
						"---------------------------",
						"findpath (fp) {x1,y1 {x2,y2}}",
						"findup (fu)",
						"finddown (fd)",
						"setgoal (sg) {x,y}",
						"highscores (hs)"
					});
					break;
			}
		}

		private void ShowHighScores()
		{
			var highScores = _context.GetHighScores();
			var messages = highScores.Scores.SelectMany(s => new[]
			{
				string.Format("{0}: {1}, {2} and {3}", s.Score, s.Name, s.Weapon, s.Armor),
				!string.IsNullOrEmpty(s.Info) ? string.Format("   ({0})", s.Info) : null
			});
			CreateMessagePopup("<-- Wall of fame -->", messages.Where(x => x != null).ToArray());
		}

		private void FindPosition(TileFlags tileToFind)
		{
			var player = _context.GetPlayer(_currentPlayerId);
			var map = _context.GetMap(player.CurrentMap);
			var mapArea = CreateMapArea(map);

			_console2.DrawArea(CreatePlayerArea(player), mapArea.Width, 0);

			var found = map.AllPositions
			                  .Select(p => new {Position = p, Flags = (TileFlags) map.GetPositionValue(p)})
							  .FirstOrDefault(x => (x.Flags & tileToFind) > 0);

			if (found == null)
				return;
			_context.SetGoalForPlayer(player.Id, found.Position);
			var path = PathFinder.CalculatePath(player.Position, found.Position, map.IsWalkable);

			foreach (var pathPos in path)
			{
				var tile = GetTile(map.GetPositionValue(pathPos));
				mapArea.Write(tile.Character, pathPos.X, pathPos.Y, ConsoleColor.Green, ConsoleColor.DarkGreen);
			}

			mapArea.CenterOffset(found.Position.X, found.Position.Y);
			_console2.DrawArea(mapArea, 0, 0);

			Console.ReadKey(true);
		}

		private void SetGoal(IEnumerable<string> args)
		{
			Position posArg1 = null;
			if (args != null && args.Any())
			{
				posArg1 = TryParsePosition(args.FirstOrDefault());
			}

			var player = _context.GetPlayer(_currentPlayerId);
			var map = _context.GetMap(player.CurrentMap);
			var mapArea = CreateMapArea(map);

			_console2.DrawArea(CreatePlayerArea(player), mapArea.Width, 0);

			var startPos = new Position(player.XPos, player.YPos);

			var endPos = posArg1 ?? SelectPosition(map, startPos, "Select goal for player");
			if (endPos == null) return;

			_context.SetGoalForPlayer(player.Id, endPos);
		}

		private Position TryParsePosition(string val)
		{
			if (string.IsNullOrEmpty(val)) return null;
			var split = val.Split(',');
			if (split.Length != 2) return null;

			int x;
			if (!int.TryParse(split[0], out x)) return null;
	
			int y;
			if (!int.TryParse(split[1], out y)) return null;

			return new Position(x, y);
		}

		private void FindPath(IEnumerable<string> args)
		{
			Position posArg1 = null;
			Position posArg2 = null;
			if (args != null && args.Any())
			{
				posArg1 = TryParsePosition(args.FirstOrDefault());
				posArg2 = TryParsePosition(args.Skip(1).FirstOrDefault());
			}

			var player = _context.GetPlayer(_currentPlayerId);
			var map = _context.GetMap(player.CurrentMap);
			var mapArea = CreateMapArea(map);

			_console2.DrawArea(CreatePlayerArea(player), mapArea.Width, 0);

			var startPos = posArg1 ?? SelectPosition(map, player.Position, "Select start position");
			if (startPos == null) return;

			mapArea.Write("1", startPos.X, startPos.Y, ConsoleColor.Green, ConsoleColor.DarkGreen);
			mapArea.CenterOffset(startPos.X, startPos.Y);
			_console2.DrawArea(mapArea, 0, 0);

			var endPos = posArg2 ?? SelectPosition(map, startPos, "Select end position");
			if (endPos == null) return;

			mapArea.Write("2", endPos.X, endPos.Y, ConsoleColor.Green, ConsoleColor.DarkGreen);
			mapArea.CenterOffset(endPos.X, endPos.Y);
			_console2.DrawArea(mapArea, 0, 0);
			var path = PathFinder.CalculatePath(startPos, endPos, map.IsWalkable);

			foreach (var pathPos in path)
			{
				var tile = GetTile(map.GetPositionValue(pathPos));
				mapArea.Write(tile.Character, pathPos.X, pathPos.Y, ConsoleColor.Green, ConsoleColor.DarkGreen);
			}

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
			var positionValue = (TileFlags)map.GetPositionValue(player.Position);
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
			var activeTile = map.GetPositionValue(player.Position);
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

		private void IncreaseAttribute(Character player)
		{
			var attr = SelectFromAttributes(player);
			if (attr == null) return;
			_context.IncreaseAttribute(player.Id, (Attribute) Enum.Parse(typeof (Attribute), attr, true));
		}

		private string SelectFromInventory(string action, Character player)
		{
			var inventory = player.Inventory.Select(x => _context.GetInfoFor(x)).ToList();

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
			_context.RefreshParty();
			var invalidPlayers = _context.Party.Where(x => !PlayerNames.Any(p => x.Name.StartsWith(p))).ToList();

			foreach (var player in invalidPlayers)
			{
				_context.DeleteCharacter(player.Id);
			}

			var player1 = _context.Party.FirstOrDefault(x => x.Name.StartsWith(PlayerNames[0]));
			var player2 = _context.Party.FirstOrDefault(x => x.Name.StartsWith(PlayerNames[1]));
			var player3 = _context.Party.FirstOrDefault(x => x.Name.StartsWith(PlayerNames[2]));

			_player1Id = (player1 ?? _context.CreateNewCharacter(PlayerNames[0], 12, 10, 16, 10, 10)).Id;
			_player2Id = (player2 ?? _context.CreateNewCharacter(PlayerNames[1], 14, 12, 12, 10, 10)).Id;
			_player3Id = (player3 ?? _context.CreateNewCharacter(PlayerNames[2], 10, 14, 14, 10, 10)).Id;

			_currentPlayerId = _player1Id;
		}

		private ConsoleColor GetItemColor(Item item)
		{
			var itemInfo = _context.GetInfoFor(item.Id);

			switch (itemInfo.SubType)
			{
				case "potion":
					return ConsoleColor.Cyan;
				case "weapon":
					return ConsoleColor.Yellow;
				case "armor":
					return ConsoleColor.Magenta;
				case "ring":
					return ConsoleColor.Gray;
				default:
					return ConsoleColor.DarkGray;
			}
		}

		private ConsoleColor GetPlayerColor(string playerId)
		{
			if (playerId == _currentPlayerId)
			{
				return ConsoleColor.Magenta;
			}

			var isFriendly = new[] { _player1Id, _player2Id, _player3Id }.Any(x => x == playerId);
			return isFriendly ? ConsoleColor.Green : ConsoleColor.Red;
		}

		private void DrawPlayer(string playerId, int xPos, int yPos, ConsoleArea area)
		{
			area.Write('@', xPos, yPos, GetPlayerColor(playerId));
		}

		private void DrawItem(Item item, ConsoleArea area)
		{
			area.Write(item.Name[0], item.XPos, item.YPos, GetItemColor(item));
		}

		private void DrawEntity(Item item, ConsoleArea area)
		{
			if (item.Type == "monster")
			{
				area.Write(item.Name[0], item.XPos, item.YPos, ConsoleColor.Red);
			}
			else if (item.Type == "character")
			{
				DrawPlayer(item.Id, item.XPos, item.YPos, area);
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
			if (flags == TileFlags.UNKNOWN)
				return new Tile('#', ConsoleColor.DarkBlue);
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