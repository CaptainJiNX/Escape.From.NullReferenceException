﻿using System;
using System.Net;
using Newtonsoft.Json.Linq;

namespace ApiClient
{
	public class ClientWrapper : IClientWrapper
	{
		private readonly Guid _sessionId;
		private readonly WebClient _client;

		public ClientWrapper(Guid sessionId)
		{
			_sessionId = sessionId;

			//Change SSL checks so that all checks pass
			ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
			_client = new WebClient();
		}

		public Guid GetSessionId()
		{
			return _sessionId;
		}

		public JObject GetCharTemplate()
		{
			return RunCommand("getchartemplate");
		}

		public Party GetParty()
		{
			return RunCommand("getparty").ToObject<Party>();
		}

		public Character GetCharacter(string charId)
		{
			return RunCommand("getcharacter", charId).ToObject<Character>();
		}

		public JObject DeleteCharacter(string charId)
		{
			return RunCommand("deletecharacter", charId);
		}

		public Character CreateCharacter(string name, int str, int con, int dex, int @int, int wis)
		{
			return RunCommand("createcharacter", string.Format("name:{0},str:{1},con:{2},dex:{3},int:{4},wis:{5}", name, str, con, dex, @int, wis)).ToObject<Character>();
		}

		public JObject AllocatePoints(Attribute attr, string charId)
		{
			return RunCommand("allocatepoints", attr.ToString().ToLowerInvariant(), charId);
		}

		public JObject Quaff(string itemId, string charId)
		{
			return RunCommand("quaff", itemId, charId);
		}

		public JObject Wield(string itemId, string charId)
		{
			return RunCommand("wield", itemId, charId);
		}

		public JObject Unwield(string itemId, string charId)
		{
			return RunCommand("unwield", itemId, charId);
		}

		public JObject Equip(string itemId, string charId)
		{
			return RunCommand("equip", itemId, charId);
		}

		public JObject Unequip(string itemId, string charId)
		{
			return RunCommand("unequip", itemId, charId);
		}

		public ScanResult Move(string charId, Direction direction)
		{
			var result = RunCommand("move", charId, direction.ToString().ToLowerInvariant());
			var scanResult = result.ToObject<ScanResult>();

			var success = result["success"];
			if (success != null && success.HasValues)
			{
				var movedTo = success["movedto"];
				if (movedTo != null)
				{
					scanResult.MoveSucceeded = true;
					scanResult.MovedTo = new Position(movedTo.Value<int>("x"), movedTo.Value<int>("y"));
				}
			}

			return scanResult;
		}

		public JObject Planeshift(string charId, string planeName)
		{
			return RunCommand("planeshift", charId, planeName);
		}

		public ScanResult Scan(string charId)
		{
			return RunCommand("scan", charId).ToObject<ScanResult>();
		}

		public ItemInfo GetInfoFor(string id)
		{
			return RunCommand("getinfofor", id).ToObject<ItemInfo>();
		}

		public JObject LevelUp(string charId)
		{
			return RunCommand("levelup", charId);
		}

		public JObject LevelDown(string charId)
		{
			return RunCommand("leveldown", charId);
		}

		public JObject Get(string charId)
		{
			return RunCommand("get", charId);
		}

		public JObject Drop(string itemId, string charId)
		{
			return RunCommand("drop", itemId, charId);
		}

		public HighScoreList GetHighScores()
		{
			return RunCommand("gethighscores").ToObject<HighScoreList>();
		}

		private JObject RunCommand(string command, params string[] args)
		{
			try
			{
				var commandUri = GetCommandUri(command, args);
				var response = _client.DownloadString(commandUri);

				return JObject.Parse(response);
			}
			catch (Exception ex)
			{
				return new JObject {{"error", new JValue(ex.Message)}};
			}
		}

		private string GetCommandUri(string command, params string[] args)
		{
			var baseUri = string.Format("https://genericwitticism.com:8000/api3/?session={0}&command={1}", _sessionId, command);

			if (args.Length > 0) baseUri += string.Format("&arg={0}", args[0]);
			if (args.Length > 1) baseUri += string.Format("&arg2={0}", args[1]);

			return baseUri;
		}
	}
}
