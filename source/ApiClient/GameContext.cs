using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ApiClient
{
	public class GameContext
	{
		private readonly IClientWrapper _client;
		private readonly List<Character> _currentParty = new List<Character>();
		private readonly List<Map> _currentMaps = new List<Map>();
		private readonly LinkedList<string> _messageLog = new LinkedList<string>();

		public GameContext(IClientWrapper client)
		{
			_client = client;
		}

		public IEnumerable<Character> Party
		{
			get { return _currentParty; }
		}

		public IEnumerable<string> Messages
		{
			get { return _messageLog; }
		}

		public void AddCharacter(string name, int str, int con, int dex, int @int, int wis)
		{
			var created = _client.CreateCharacter(name, str, con, dex, @int, wis);
			ThrowIfError(created);

			var character = new Character(created);
			_currentParty.Add(character);
		}

		public void Scan(Character player)
		{
			var scanResult = _client.Scan(player.Id);
			if (HasError(scanResult)) return;

			player.UpdateFromScan(scanResult);

			var map = GetOrAddMap(player.CurrentMap);
			map.UpdateFromScan(scanResult);

			AddUpdateMessages(scanResult);
		}

		public void MovePlayer(Character player, Direction dir)
		{
			var movement = _client.Move(player.Id, dir);
			if (HasError(movement)) return;

			player.UpdateFromMovement(movement);

			var map = GetOrAddMap(player.CurrentMap);
			map.UpdateFromMovement(movement);

			AddUpdateMessages(movement);
		}

		private void AddUpdateMessages(JObject scanResult)
		{
			var updates = scanResult["updates"];
			if (updates == null) return;
			foreach (var update in updates)
			{
				AddMessage(update.Value<string>("message"));
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
			return _currentMaps.FirstOrDefault(x => x.Name == mapName);
		}

		private Map GetOrAddMap(string mapName)
		{
			var map = GetMap(mapName);

			if (map == null)
			{
				map = new Map(mapName);
				_currentMaps.Add(map);
			}

			return map;
		}

		private static void ThrowIfError(JObject jObject)
		{
			if (jObject["error"] != null)
			{
				throw new ApplicationException(jObject["error"].ToString());
			}
		}

		private bool HasError(JObject jObject)
		{
			if (jObject["error"] == null)
			{
				return false;
			}

			AddMessage(jObject["error"].ToString());
			return true;
		}
	}
}