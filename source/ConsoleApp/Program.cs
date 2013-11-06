using System;
using System.IO;
using Escape.From.NullReferenceException.ApiClient;

namespace Escape.From.NullReferenceException.ConsoleApp
{
	class Program
	{
		static void Main(string[] args)
		{
			var client = new ClientWrapper(Guid.Parse(File.ReadAllText("apikey.txt")));

			foreach (var character in client.GetParty())
			{
				Console.WriteLine("--------------------------");
				Console.WriteLine(character);
			}
		}
	}
}
