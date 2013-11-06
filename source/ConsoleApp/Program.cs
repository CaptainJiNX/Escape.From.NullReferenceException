using System;
using System.IO;
using ApiClient;

namespace ConsoleApp
{
	class Program
	{
		static void Main(string[] args)
		{
			var client = new ClientWrapper(Guid.Parse(File.ReadAllText("apikey.txt")));

			Console.WriteLine(client.CreateCharacter("Captain JiNX", 15, 13, 10, 10, 10));
			Console.WriteLine();

			var party = client.GetParty();
			Console.WriteLine(party);
			Console.WriteLine();

			foreach (var charId in party["characters"].Values<string>())
			{
				Console.WriteLine("CHARACTER");
				Console.WriteLine("=========");
				var character = client.GetCharacter(charId);
				Console.WriteLine(character);

				Console.WriteLine("Inventory");
				Console.WriteLine("---------");

				foreach (var itemId in character["inventory"].Values<string>())
				{
					var item = client.GetInfoFor(itemId);
					Console.WriteLine(item);
				}

				Console.WriteLine();

				Console.WriteLine(client.DeleteCharacter(charId));
			}

			Console.WriteLine("Done...");
			Console.ReadLine();
		}
	}
}
