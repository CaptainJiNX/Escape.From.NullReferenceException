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

			foreach (var character in client.GetParty())
			{
				Console.WriteLine("--------------------------");
				Console.WriteLine(character);
			}

			Console.WriteLine("Done...");
			Console.ReadLine();
		}
	}
}
