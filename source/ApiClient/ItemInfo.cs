using Newtonsoft.Json;

namespace ApiClient
{
	public class ItemInfo
	{
		[JsonProperty("_id")]
		public string Id { get; set; }
		public string Description { get; set; }
		public string Name{ get; set; }
		public string Type{ get; set; }
		public string SubType{ get; set; }
		[JsonProperty("hp")]
		public int? HitPoints { get; set; }
		[JsonProperty("ac")]
		public int? ArmorClass { get; set; }
		public int? Speed { get; set; }
		// "special":null

		public bool IsPotion
		{
			get { return SubType == "potion"; }
		}

		public bool IsGaseousPotion
		{
			get { return IsPotion && Name.ToLowerInvariant().Contains("gaseous"); }
		}

		public bool IsHealingPotion
		{
			get { return IsPotion && Name.ToLowerInvariant().Contains("healing"); }
		}
	}
}