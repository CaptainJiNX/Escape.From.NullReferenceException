using Newtonsoft.Json;

namespace ApiClient
{
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
		[JsonProperty("x")]
		public int XPos { get; set; }
		[JsonProperty("y")]
		public int YPos { get; set; }
		public ScanUpdate[] Updates { get; set; }
		public string Error { get; set; }
	}
}