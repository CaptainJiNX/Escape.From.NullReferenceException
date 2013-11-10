using System;

namespace ConsoleApp
{
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			var game = new ConsoleGame();
			game.RunGame();
		}
	}
}
