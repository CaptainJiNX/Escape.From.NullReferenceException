using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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

		public void MovePlayer(string playerId, Direction dir)
		{
			Update(playerId, _client.Move(playerId, dir));
		}

		private void Update(string playerId, ScanResult scanResult)
		{
			if (scanResult.Error != null)
			{
				AddMessage(scanResult.Error);
				return;
			}

			UpdatePlayer(playerId, scanResult);

			var map = GetOrAddMap(scanResult.Map);
			map.Update(scanResult);

			if (map.HasChanges())
			{
				_mapStorage.Store(map);
			}

			StoreDamageStatistics(scanResult, playerId);

			AddUpdateMessages(scanResult);
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
					var damage = int.Parse(match.Groups["damage"].Captures[0].Value);
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
			return _currentParty[playerId];
		}

		private void UpdatePlayer(string playerId, ScanResult result)
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

			var player = _currentParty[playerId];
			player.Update(result);
		}

		private void AddUpdateMessages(ScanResult result)
		{
			if (result.Updates == null) return;
			foreach (var update in result.Updates.Where(x => x.Message != null))
			{
				AddMessage(update.Message);
			}
		}

		public void AddMessage(string message)
		{
			if (!string.IsNullOrEmpty(message) && _messageLog.FirstOrDefault() != message)
			{
				_messageLog.AddFirst(message);
			}
		}

		public Map GetMap(string mapName)
		{
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
	}
}