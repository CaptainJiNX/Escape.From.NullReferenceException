using Newtonsoft.Json;

namespace ApiClient
{
	public class Item
	{
		[JsonProperty("_id")]
		public string Id { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
		[JsonProperty("x")]
		public int XPos { get; set; }
		[JsonProperty("y")]
		public int YPos { get; set; }

		public Position Position { get { return new Position(XPos, YPos); }}
	}
}