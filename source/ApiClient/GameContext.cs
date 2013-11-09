using System.Collections.Generic;
using System.Linq;

namespace ApiClient
{
	public class GameContext
	{
		private readonly IClientWrapper _client;
		private readonly IMapStorage _mapStorage;

		private readonly Dictionary<string, Character> _currentParty = new Dictionary<string, Character>();
		private readonly Dictionary<string, Map> _currentMaps = new Dictionary<string, Map>();
		private readonly LinkedList<string> _messageLog = new LinkedList<string>();

		public GameContext(IClientWrapper client, IMapStorage mapStorage)
		{
			_client = client;
			_mapStorage = mapStorage;

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
				_mapStorage.Save(map);
			}

			AddUpdateMessages(scanResult);
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

		private void AddMessage(string message)
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
	}
}