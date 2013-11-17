using System;
using System.Collections.Generic;
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
		private readonly LinkedList<string> _messageLog = new LinkedList<string>();
		private readonly Dictionary<string, ItemInfo> _currentItems = new Dictionary<string, ItemInfo>();
		private readonly Dictionary<string, Position> _goals = new Dictionary<string, Position>();
		private readonly HashSet<string> _attackingPlayers = new HashSet<string>(); 

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

		public IEnumerable<string> Party
		{
			get { return _currentParty.Keys; }
		}

		public IEnumerable<string> Messages
		{
			get { return _messageLog; }
		}

		public string CreateNewCharacter(string name, int str, int con, int dex, int @int, int wis)
		{
			var character = _client.CreateCharacter(name, str, con, dex, @int, wis);
			if (character.Error != null)
			{
				AddMessage(character.Error);
				return null;
			}

			_currentParty.Add(character.Id, character);
			return character.Id;
		}

		public void Scan(string playerId)
		{
			Update(playerId, _client.Scan(playerId));
		}

		public void Move(string playerId, Direction dir)
		{
			if (dir == Direction.None) return;
			Update(playerId, _client.Move(playerId, dir));
		}

		public void SetAttackMode(string playerId)
		{
			if(String.IsNullOrEmpty(playerId)) return;
			if (_attackingPlayers.Contains(playerId)) return;
			_attackingPlayers.Add(playerId);
			AddMessage(String.Format("[{0}] is now attacking.", playerId));
		}

		public void SetDefenseMode(string playerId)
		{
			if (String.IsNullOrEmpty(playerId)) return;
			if (!_attackingPlayers.Contains(playerId)) return;
			_attackingPlayers.Remove(playerId);
			AddMessage(String.Format("[{0}] is now avoiding monsters.", playerId));
		}

		public void SetGoalForPlayer(string playerId, Position goal)
		{
			if (goal == null || playerId == null) return;

			var player = GetPlayer(playerId);
			_goals[player.Id] = goal;
			AddMessage(String.Format("New goal for player {0} [{1}] set to ({2},{3})", player.Name, player.Id, goal.X, goal.Y));
		}

		private void RemoveGoalForPlayer(string playerId)
		{
			if (playerId == null) return;

			var player = GetPlayer(playerId);
			_goals.Remove(playerId);
			AddMessage(String.Format("Removed goal for player {0} [{1}]", player.Name, player.Id));
		}

		public Position GetGoalForPlayer(string playerId)
		{
			Position goal;
			return _goals.TryGetValue(playerId, out goal) ? goal : null;
		}

		private bool PlayerCanWalkHere(Character player, Map map, Position pos)
		{
			var entities = player.VisibleEntities.Where(item => item.Id != player.Id);

			if (_attackingPlayers.Contains(player.Id))
			{
				entities = entities.Where(x => x.Type != "monster");
			}

			var boulders = player.VisibleItems.Where(item => GetInfoFor(item.Id).SubType == "boulder");
			var blocked = entities.Concat(boulders).Select(item => new Position(item.XPos, item.YPos));

			return !blocked.Any(x => x.Equals(pos)) && map.IsWalkable(pos);
		}

		public Direction GetNextDirectionForPlayer(string playerId)
		{
			var goalForPlayer = GetGoalForPlayer(playerId);

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
			if (result.Updates != null)
			{
				var update = result.Updates
				                       .Where(x => x.Character != null)
				                       .LastOrDefault(x => x.Character.Id == playerId);

				if (update != null)
				{
					_currentParty[playerId] = update.Character;
				}
			}

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
			AddResponseMessage(_client.Quaff(itemId, playerId));
		}

		public void PickUpItem(string playerId)
		{
			AddResponseMessage(_client.Get(playerId));
		}

		public void MoveUp(string playerId)
		{
			AddResponseMessage(_client.LevelUp(playerId));
		}

		public void MoveDown(string playerId)
		{
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