using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

namespace ApiClient
{
	public class ClientWrapper
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

		public JObject GetCharTemplate()
		{
			return RunCommand("getchartemplate");
		}

		public IEnumerable<JObject> GetParty()
		{
			var party = RunCommand("getparty");

			return party["characters"].Select(id => GetCharacter((string)id));
		}

		public JObject GetCharacter(string characterId)
		{
			return RunCommand("getcharacter", characterId);
		}

		public JObject DeleteCharacter(string characterId)
		{
			return RunCommand("deletecharacter", characterId);
		}

		private JObject RunCommand(string command, params string[] args)
		{
			return JObject.Parse(_client.DownloadString(GetCommandUri(command, args)));
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
