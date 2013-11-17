using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ApiClient
{
	public class HighScoreList
	{
		[JsonProperty("success")]
		public ScoreInfo[] Scores { get; set; }
		public string Error { get; set; }
	}

	public class ScoreInfo
	{
		public int Score { get; set; }
		public string Id { get; set; }
		public string Name { get; set; }
		public string Armor { get; set; }
		public string Weapon { get; set; }
		public string Info { get; set; }
	}

	public class ScanResult
	{
		[JsonProperty("area")]
		public uint[][] VisibleArea { get; set; }
		[JsonProperty("bx")]
		public int XOff { get; set; }
		[JsonProperty("by")]
		public int YOff { get; set; }
		public string Map { get; set; }
		public Item[] Items { get; set; }
		public Item[] Entities { get; set; }
		public Position StairsDown { get; set; }
		public Position StairsUp { get; set; }
		[JsonProperty("x")]
		public int XPos { get; set; }
		[JsonProperty("y")]
		public int YPos { get; set; }
		public ScanUpdate[] Updates { get; set; }
		public string Error { get; set; }

		[JsonIgnore]
		public Position MovedTo { get; set; }
		[JsonIgnore]
		public bool MoveSucceeded { get; set; }

		public IEnumerable<Tuple<Position, uint>> ConvertAreaToPositions()
		{
			if (VisibleArea == null) yield break;

			for (var y = 0; y < VisibleArea.Length; y++)
			{
				for (var x = 0; x < VisibleArea[y].Length; x++)
				{
					yield return new Tuple<Position, uint>(new Position(x + XOff, y + YOff), VisibleArea[y][x]);
				}
			}
		}
	}
}