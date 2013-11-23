using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace ApiClient
{
	public class GameContext
	{
		private readonly IClientWrapper _client;
		private readonly ISimpleStorage<Map> _mapStorage;
		private readonly ISimpleStorage<DamageStatistics> _damageStorage;

		private readonly Dictionary<string, Character> _currentParty = new Dictionary<string, Character>();
		private readonly Dictionary<string, Map> _currentMaps = new Dictionary<string, Map>();
		private readonly HashSet<string> _exploredMaps = new HashSet<string>(); 
		private readonly LinkedList<string> _messageLog = new LinkedList<string>();
		private readonly Dictionary<string, ItemInfo> _currentItems = new Dictionary<string, ItemInfo>();

		// Move this crap in to character instead...
		private readonly Dictionary<string, Position> _goals = new Dictionary<string, Position>();
		private readonly Dictionary<string, Position> _tmpGoals = new Dictionary<string, Position>();
		private readonly HashSet<string> _attackingPlayers = new HashSet<string>(); 
		private readonly HashSet<string> _pvpPlayers = new HashSet<string>(); 
		private readonly HashSet<string> _gaseousPlayers = new HashSet<string>(); 
		//.....

		private static readonly Character NullCharacter = new Character
		{
			Name = "Null Character",
			Inventory = new string[0]
		};

		public GameContext(IClientWrapper client, 
			ISimpleStorage<Map> mapStorage, 
			ISimpleStorage<DamageStatistics> damageStorage)
		{
			_client = client;
			_mapStorage = mapStorage;
			_damageStorage = damageStorage;

			Initialize();
		}

		private void Initialize()
		{
			var party = _client.GetParty();

			if (party.Characters != null)
			{
				foreach (var playerId in party.Characters)
				{
					_currentParty.Add(playerId, _client.GetCharacter(playerId));
				}
			}

			foreach (var map in _mapStorage.GetAll())
			{
				_currentMaps.Add(map.Name, map);
			}
		}

		public IEnumerable<Character> Party
		{
			get { return _currentParty.Keys.Select(GetPlayer); }
		}

		public IEnumerable<string> Messages
		{
			get { return _messageLog; }
		}

		public void DeleteCharacter(string playerId)
		{
			var response = _client.DeleteCharacter(playerId);

			if (response["error"] != null)
			{
				_currentParty.Remove(playerId);
			}

			AddResponseMessage(response);
		}

		public Character CreateNewCharacter(string name, int str, int con, int dex, int @int, int wis)
		{
			var character = _client.CreateCharacter(name, str, con, dex, @int, wis);
			if (character.Error != null)
			{
				AddMessage(character.Error);
				return null;
			}

			_currentParty.Add(character.Id, character);
			return character;
		}

		public void Scan(string playerId)
		{
			Update(playerId, _client.Scan(playerId));
		}

		public void Scout(string playerId)
		{
			if (string.IsNullOrEmpty(playerId)) return;

			if ((GetTempGoalForPlayer(playerId) ?? GetGoalForPlayer(playerId)) == null)
			{
				var player = GetPlayer(playerId);
				var map = GetOrAddMap(player.CurrentMap);

				Func<Position, bool> hasWalkablePath = pos => PathFinder.CalculatePath(player.Position, pos, p => PlayerCanWalkHere(player, map, p)).Any();
				var nextPos = map.GetClosestWalkablePositionWithUnknownNeighbour(player.Position, hasWalkablePath);

				if (nextPos == null)
				{
					if (!_exploredMaps.Contains(map.Name))
					{
						_exploredMaps.Add(map.Name);
						AddMessage(string.Format("Map [{0}] is now fully explored! Yay!", map.Name));
					}
					nextPos = map.GetRandomWalkablePosition(hasWalkablePath);
				}

				SetGoalForPlayer(playerId, nextPos);
			}

			Move(playerId, GetNextDirectionForPlayer(playerId));
		}

		public void Move(string playerId, Direction dir)
		{
			if (string.IsNullOrEmpty(playerId) || dir == Direction.None) return;
			var moveResult = _client.Move(playerId, dir);

			if (moveResult.Error != null)
			{
				AddMessage(moveResult.Error);
				return;
			}

			if (PlayerIsGaseous(playerId))
			{
				var player = GetPlayer(playerId);
				var expectedPosition = GetNextPosition(player.Position, dir);

				if (GaseousPlayerCanWalkHere(player, expectedPosition) && !moveResult.MoveSucceeded)
				{
					RemoveGaseousMode(playerId);
				}
			}

			Update(playerId, moveResult);
		}

		private Position GetNextPosition(Position pos, Direction dir)
		{
			return pos.GetNeighbours().FirstOrDefault(next => GetDirection(pos, next) == dir);
		}

		public void ToggleAttackMode(string playerId)
		{
			if(String.IsNullOrEmpty(playerId)) return;
			var player = GetPlayer(playerId);

			RemoveTempGoalForPlayer(playerId);

			if (PlayerHasAttackMode(playerId))
			{
				_attackingPlayers.Remove(playerId);
				AddMessage(String.Format("{0} [{1}] is now avoiding monsters...", player.Name, player.Id));
			}
			else
			{
				_attackingPlayers.Add(playerId);
				AddMessage(String.Format("{0} [{1}] is now attacking monsters!", player.Name, player.Id));
			}
		}

		public void TogglePvPMode(string playerId)
		{
			if (String.IsNullOrEmpty(playerId)) return;
			var player = GetPlayer(playerId);

			RemoveTempGoalForPlayer(playerId);

			if (PlayerHasPvPMode(playerId))
			{
				_pvpPlayers.Remove(playerId);
				AddMessage(String.Format("{0} [{1}] is now avoiding other players...", player.Name, player.Id));
			}
			else
			{
				_pvpPlayers.Add(playerId);
				AddMessage(String.Format("{0} [{1}] is now attacking other players!!!", player.Name, player.Id));
			}

		}

		private bool PlayerHasPvPMode(string playerId)
		{
			return _pvpPlayers.Contains(playerId);
		}

		private bool PlayerHasAttackMode(string playerId)
		{
			return _attackingPlayers.Contains(playerId);
		}

		private bool PlayerIsGaseous(string playerId)
		{
			return _gaseousPlayers.Contains(playerId);
		}

		private void SetGaseousMode(string playerId)
		{
			if(String.IsNullOrEmpty(playerId)) return;
			if (PlayerIsGaseous(playerId)) return;
			_gaseousPlayers.Add(playerId);
			var player = GetPlayer(playerId);
			AddMessage(String.Format("{0} [{1}] can now walk through walls.", player.Name, player.Id));
		}

		private void RemoveGaseousMode(string playerId)
		{
			if (String.IsNullOrEmpty(playerId)) return;
			if (!PlayerIsGaseous(playerId)) return;
			_gaseousPlayers.Remove(playerId);
			var player = GetPlayer(playerId);
			AddMessage(String.Format("{0} [{1}] can not walk through walls anymore...", player.Name, player.Id));
		}

		public void SetGoalForPlayer(string playerId, Position goal)
		{
			if (goal == null || playerId == null) return;

			var player = GetPlayer(playerId);
			_goals[player.Id] = goal;
			AddMessage(String.Format("New goal for player {0} [{1}] set to ({2},{3})", player.Name, player.Id, goal.X, goal.Y));
		}

		private void SetTempGoalForPlayer(string playerId, Position goal)
		{
			_tmpGoals[playerId] = goal;
		}

		private void RemoveGoalForPlayer(string playerId)
		{
			if (playerId == null) return;

			var player = GetPlayer(playerId);
			_goals.Remove(playerId);
			AddMessage(String.Format("Removed goal for player {0} [{1}]", player.Name, player.Id));
		}

		private void RemoveTempGoalForPlayer(string playerId)
		{
			if (playerId == null) return;
			_tmpGoals.Remove(playerId);
		}

		public Position GetGoalForPlayer(string playerId)
		{
			Position goal;
			return _goals.TryGetValue(playerId ?? string.Empty, out goal) ? goal : null;
		}

		private Position GetTempGoalForPlayer(string playerId)
		{
			Position goal;
			return _tmpGoals.TryGetValue(playerId ?? string.Empty, out goal) ? goal : null;
		}

		private bool GaseousPlayerCanWalkHere(Character player, Position pos)
		{
			var entities = player.VisibleEntities.Where(item => item.Id != player.Id);
			var boulders = player.VisibleItems.Where(item => GetInfoFor(item.Id).SubType == "boulder");
			var blocked = entities.Concat(boulders).Select(item => new Position(item.XPos, item.YPos));
			return !blocked.Any(x => x.Equals(pos));
		}

		private bool IsFriendly(string playerId)
		{
			return _currentParty.ContainsKey(playerId);
		}


		private IEnumerable<Item> GetEnemyCharacters(Character player)
		{
			return player.VisibleEntities.Where(x => x.Type == "character" && !IsFriendly(x.Id));
		}

		private IEnumerable<Item> GetFriends(Character player)
		{
			return player.VisibleEntities.Where(x => x.Type == "character" && IsFriendly(x.Id) && x.Id != player.Id);
		} 

		private IEnumerable<Item> GetMonsters(Character player)
		{
			return player.VisibleEntities.Where(x => x.Type == "monster");
		} 

		private bool PlayerCanWalkHere(Character player, Map map, Position pos)
		{
			var friends = GetFriends(player);
			var enemyCharacters = GetEnemyCharacters(player);
			var monsters = GetMonsters(player);
			var boulders = player.VisibleItems.Where(item => GetInfoFor(item.Id).SubType == "boulder");

			var blockingItems = friends.Concat(boulders);

			if (!PlayerHasPvPMode(player.Id))
			{
				blockingItems = blockingItems.Concat(enemyCharacters);
			}

			if (!PlayerHasAttackMode(player.Id))
			{
				blockingItems = blockingItems.Concat(monsters);
			}

			var blockedPositions = blockingItems.Select(item => item.Position);

			return !blockedPositions.Any(x => x.Equals(pos)) && (PlayerIsGaseous(player.Id) || map.IsWalkable(pos));
		}

		public Direction GetNextDirectionForPlayer(string playerId)
		{
			var goalForPlayer = GetTempGoalForPlayer(playerId) ?? GetGoalForPlayer(playerId);

			if(goalForPlayer == null)
				return Direction.None;

			var player = GetPlayer(playerId);
			var map = GetMap(player.CurrentMap);
			var startPos = player.Position;

			var nextPosition = PathFinder.CalculatePath(startPos, goalForPlayer, p => PlayerCanWalkHere(player, map, p))
				.Skip(1)
				.FirstOrDefault();

			return GetDirection(startPos, nextPosition);
		}

		private Direction GetDirection(Position from, Position to)
		{
			if (from == null || to == null) return Direction.None;

			if (to.X > from.X)
			{
				if(to.Y > from.Y) return Direction.DownRight;
				if(to.Y < from.Y) return Direction.UpRight;
				return Direction.Right;
			}

			if (to.X < from.X)
			{
				if(to.Y > from.Y) return Direction.DownLeft;
				if(to.Y < from.Y) return Direction.UpLeft;
				return Direction.Left;
			}

			if (to.Y > from.Y) return Direction.Down;
			if (to.Y < from.Y) return Direction.Up;

			return Direction.None;
		}

		private void Update(string playerId, ScanResult scanResult)
		{
			if (scanResult.Error != null)
			{
				if (scanResult.Error == "Unable to find character object" ||
					scanResult.Error == "could not find a character by that id")
				{
					if (_currentParty.ContainsKey(playerId))
					{
						_currentParty.Remove(playerId);
					}

					return;
				}

				AddMessage(scanResult.Error);
				return;
			}

			var player = UpdatePlayer(playerId, scanResult);

			var map = GetOrAddMap(scanResult.Map);
			map.Update(scanResult);

			if (map.HasChanges())
			{
				_mapStorage.Store(map);
			}

			StoreDamageStatistics(scanResult, playerId);
			AddUpdateMessages(scanResult);

			if (player.Position.Equals(GetGoalForPlayer(playerId)))
			{
				RemoveGoalForPlayer(playerId);
			}

			if (player.Position.Equals(GetTempGoalForPlayer(playerId)))
			{
				RemoveTempGoalForPlayer(playerId);
			}

			if (PlayerHasAttackMode(playerId) || PlayerHasPvPMode(playerId))
			{
				if (player.HitPoints < (player.MaxHitPoints/2))
				{
					QuickQuaff(playerId, x => x.IsHealingPotion);
				}

				// Just some temporary ai stuff for the player...
				//------------------------------------------------
				var numberOfItems = player.Inventory.Length;
				if (numberOfItems < 10)
				{
					var potion = player.VisibleItems
					                   .Where(item => Equals(item.Position, player.Position))
					                   .Select(item => GetInfoFor(item.Id))
					                   .Where(item => item.IsPotion)
					                   .FirstOrDefault(item => !item.IsGaseousPotion);
					if (potion != null)
					{
						PickUpItem(playerId);
						numberOfItems++;
					}
				}

				var monsterPositions = GetMonsters(player)
					.OrderBy(x => x.Position.Distance(player.Position))
					.Select(x => x.Position);

				var enemyPositions = GetEnemyCharacters(player)
					.OrderBy(x => x.Position.Distance(player.Position))
					.Select(x => x.Position);

				var potionPositions = player.VisibleItems
				                      .Where(item => GetInfoFor(item.Id).IsPotion)
				                      .Where(item => !GetInfoFor(item.Id).IsGaseousPotion)
				                      .OrderBy(item => item.Position.Distance(player.Position))
				                      .Select(x => x.Position);

				Position potionPos = null;
				Position enemyPos = null;
				Position monsterPos = null;

				if (numberOfItems < 10)
				{
					potionPos = potionPositions.FirstOrDefault();
				}

				if (PlayerHasAttackMode(playerId))
				{
					monsterPos = monsterPositions.FirstOrDefault();
				}

				if (PlayerHasPvPMode(playerId))
				{
					enemyPos = enemyPositions.FirstOrDefault();
				}

				var nextPos = new[]{potionPos,monsterPos,enemyPos}
					.Where(pos => pos != null)
					.OrderBy(pos => pos.Distance(player.Position))
					.FirstOrDefault();

				if (nextPos != null)
				{
					SetTempGoalForPlayer(playerId, nextPos);
				}
				else
				{
					RemoveTempGoalForPlayer(playerId);
				}

				//------------------------------------------------

			}

			if (player.HitPoints <= 0)
			{
				AddMessage(string.Format("{0} [{1}] is dead!!! AAArrrghhh!!!", player.Name, player.Id));
				DeleteCharacter(playerId);
			}
		}

		private void StoreDamageStatistics(ScanResult result, string playerId)
		{
			if (result.Updates == null) return;
			var player = GetPlayer(playerId);

			try
			{
				foreach (var update in result.Updates)
				{
					if (update.Message == null) continue;
					var match = Regex.Match(update.Message, "\\[" + playerId + "\\]\\sscored\\sa\\shit\\s\\[damage\\s(?<damage>\\d+)\\]");
					if (!match.Success) continue;
					var damage = Int32.Parse(match.Groups["damage"].Captures[0].Value);
					var stats = new DamageStatistics(player.WieldedWeaponName, damage, player.Strength, player.Level);
					_damageStorage.Store(stats);
				}
			}
			catch (Exception ex)
			{
				AddMessage(ex.Message);
			}
		}

		public Character GetPlayer(string playerId)
		{
			if (String.IsNullOrEmpty(playerId)) return NullCharacter;
			Character player;
			return _currentParty.TryGetValue(playerId, out player) ? player : NullCharacter;
		}

		private Character UpdatePlayer(string playerId, ScanResult result)
		{
			var player = GetPlayer(playerId);
			player.Update(result);
			return player;
		}

		private void AddUpdateMessages(ScanResult result)
		{
			if (result.Updates == null) return;
			foreach (var update in result.Updates.Where(x => x.Message != null))
			{
				AddMessage(update.Message);
			}
		}

		private void AddMessage(string message)
		{
			if (!String.IsNullOrEmpty(message) && _messageLog.FirstOrDefault() != message)
			{
				_messageLog.AddFirst(message);
			}
		}

		public Map GetMap(string mapName)
		{
			if (String.IsNullOrEmpty(mapName)) return _currentMaps.Values.First();
			Map map;
			return _currentMaps.TryGetValue(mapName, out map) ? map : null;
		}

		private Map GetOrAddMap(string mapName)
		{
			var map = GetMap(mapName);

			if (map == null)
			{
				map = new Map(mapName);
				_currentMaps.Add(mapName, map);
			}

			return map;
		}

		public ItemInfo GetInfoFor(string itemId)
		{
			if (_currentItems.ContainsKey(itemId))
			{
				return _currentItems[itemId];
			}

			var itemInfo = _client.GetInfoFor(itemId);

			if (itemInfo.Type == "item")
			{
				_currentItems[itemInfo.Id] = itemInfo;
			}

			return itemInfo;
		}

		public void AddResponseMessage(JObject response)
		{
			var message = (response["success"] ?? response["error"]);
			if (message != null)
			{
				AddMessage(message.ToObject<string>());
			}
		}

		public void IncreaseAttribute(string playerId, Attribute attribute)
		{
			AddResponseMessage(_client.AllocatePoints(attribute, playerId));
		}

		public void QuaffPotion(string playerId, string itemId)
		{
			if (String.IsNullOrEmpty(itemId)) return;
			
			if (GetInfoFor(itemId).IsGaseousPotion)
			{
				SetGaseousMode(playerId);
			}

			AddResponseMessage(_client.Quaff(itemId, playerId));
		}

		public void QuickQuaff(string playerId, Func<ItemInfo, bool> predicate)
		{
			if (string.IsNullOrEmpty(playerId)) return;
			var player = GetPlayer(playerId);

			var potion = player.Inventory
			                   .Select(GetInfoFor)
			                   .FirstOrDefault(predicate);

			if (potion != null)
			{
				QuaffPotion(player.Id, potion.Id);
			}
		}

		public void PickUpItem(string playerId)
		{
			AddResponseMessage(_client.Get(playerId));
		}

		public void MoveUp(string playerId)
		{
			RemoveGoalForPlayer(playerId);
			RemoveTempGoalForPlayer(playerId);
			AddResponseMessage(_client.LevelUp(playerId));
		}

		public void MoveDown(string playerId)
		{
			RemoveGoalForPlayer(playerId);
			RemoveTempGoalForPlayer(playerId);
			AddResponseMessage(_client.LevelDown(playerId));
		}

		public void Planeshift(string playerId, string plane)
		{
			if (String.IsNullOrEmpty(plane)) return;
			AddResponseMessage(_client.Planeshift(playerId, plane));
		}

		public HighScoreList GetHighScores()
		{
			return _client.GetHighScores();
		}

		public void WieldWeapon(string playerId, string itemId)
		{
			if (String.IsNullOrEmpty(itemId)) return;
			AddResponseMessage(_client.Wield(itemId, playerId));
		}

		public void DropItem(string playerId, string itemId)
		{
			if (String.IsNullOrEmpty(itemId)) return;
			AddResponseMessage(_client.Drop(itemId, playerId));
		}

		public void UnwieldWeapon(string playerId, string itemId)
		{
			if (String.IsNullOrEmpty(itemId)) return;
			AddResponseMessage(_client.Unwield(itemId, playerId));
		}

		public void EquipArmor(string playerId, string itemId)
		{
			if (String.IsNullOrEmpty(itemId)) return;
			AddResponseMessage(_client.Equip(itemId, playerId));
		}

		public void UnequipArmor(string playerId, string itemId)
		{
			if (String.IsNullOrEmpty(itemId)) return;
			AddResponseMessage(_client.Unequip(itemId, playerId));
		}
	}
}