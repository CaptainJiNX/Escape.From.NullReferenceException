using System;

namespace ConsoleApp
{
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			var console = new Console2(100, 30, ConsoleColor.DarkRed);

			var testArea = new ConsoleArea(47, 15);
			testArea.SetTitle(" --> ÅÄÖ <--- yes, it works!!! ");
			testArea.SetDefaultBackground(ConsoleColor.Black);
			testArea.SetDefaultForeground(ConsoleColor.Cyan);

			testArea.SetBorderStyle(ConsoleArea.BorderStyle.Double);
			testArea.SetBorderBackground(ConsoleColor.Black);
			testArea.SetBorderForeground(ConsoleColor.White);

			testArea.Write("Hello World!", 10, 10);
			testArea.Write("Hello World! ÅÄÖ åäö", 10, 11, ConsoleColor.Magenta);
			testArea.Write("Hello World!", 10, 12, ConsoleColor.Blue, ConsoleColor.DarkBlue);

			testArea.Write("A", 0, 0, ConsoleColor.Yellow);
			testArea.Write("Z", 2000, 2000, ConsoleColor.Yellow);

			int x = 0;
			int y = 0;

			var infoArea = new ConsoleArea(98, 3);
			infoArea.SetDefaultBackground(ConsoleColor.Black);
			infoArea.SetDefaultForeground(ConsoleColor.White);
			infoArea.SetBorderForeground(ConsoleColor.White);
			infoArea.SetBorderBackground(ConsoleColor.Black);
			infoArea.SetBorderStyle(ConsoleArea.BorderStyle.Single);
			infoArea.SetTitle(" Info ");

			while (true)
			{
				infoArea.Clear();
				infoArea.Write(string.Format("X:{0}, Y:{1}", x, y), 0, 0);
				infoArea.TopLeftAt(0, 0);

				console.DrawArea(infoArea, 1, 1);

				testArea.Write("*", x, y);
				testArea.CenterAt(x, y);

				console.DrawArea(testArea, 1, 5);
				console.DrawArea(testArea, 52, 5);
				
				var key = Console.ReadKey(true);

				if (key.Key == ConsoleKey.UpArrow) y--;
				else if (key.Key == ConsoleKey.DownArrow) y++;
				else if (key.Key == ConsoleKey.LeftArrow) x--;
				else if (key.Key == ConsoleKey.RightArrow) x++;
				else if (key.Key == ConsoleKey.Escape) break;
			}
		}
	}
}
