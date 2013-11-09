using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ApiClient
{
	public class Character
	{
		#pragma warning disable 649
		
		// This is just beacause GetCharacter and CreateCharacter has different property names for id.
		[JsonProperty("_id")] private string _idFromCreate;
		[JsonProperty("id")] private string _idFromGet;
		[JsonProperty("hp")] 
		private string _hitPoints;

		#pragma warning restore 649

		private IEnumerable<Item> _visibleItems;
		private IEnumerable<Item> _visibleEntities;
		private IEnumerable<Position> _visibleArea;

		public string Id { get { return _idFromGet ?? _idFromCreate; } }

		public string Name { get; set; }

		[JsonProperty("exp")]
		public int Experience { get; set; }

		public int Level { get; set; }

		[JsonProperty("str")]
		public int Strength { get; set; }

		[JsonProperty("int")]
		public int Intelligence { get; set; }

		[JsonProperty("wis")]
		public int Wisdom { get; set; }

		[JsonProperty("dex")]
		public int Dexterity { get; set; }

		[JsonProperty("con")]
		public int Constitution { get; set; }

		[JsonProperty("map")]
		public string CurrentMap { get; set; }

		public string[] Inventory { get; set; }

		[JsonProperty("x")]
		public int XPos { get; set; }

		[JsonProperty("y")]
		public int YPos { get; set; }

		public int HitPoints
		{
			get
			{
				if (_hitPoints == null) return 0;
				int value;
				return int.TryParse(_hitPoints.Split('/').FirstOrDefault(), out value) ? value : 0;
			}
		}

		public int MaxHitPoints
		{
			get
			{
				if (_hitPoints == null) return 0;
				int value;
				return int.TryParse(_hitPoints.Split('/').LastOrDefault(), out value) ? value : 0;
			}
		}

		[JsonProperty("ac")]
		public int ArmorClass { get; set; }
		[JsonProperty("allow")]
		public int PointsToAllocate { get; set; }
		public int Speed { get; set; }
		public int Light { get; set; }

		[JsonProperty("wieldedweapon")]
		public string WieldedWeaponId { get; set; }
		public string WieldedWeaponName { get; set; }
		[JsonProperty("equippedarmor")]
		public string EquippedArmorId { get; set; }
		public string EquippedArmorName { get; set; }

		public string Error { get; set; }

		public IEnumerable<Item> VisibleItems
		{
			get { return _visibleItems ?? new Item[0] ; }
		}

		public IEnumerable<Item> VisibleEntities
		{
			get { return _visibleEntities ?? new Item[0]; }
		}

		public IEnumerable<Position> VisibleArea
		{
			get { return _visibleArea ?? new Position[0]; }
		}

		public void Update(ScanResult result)
		{
			XPos = result.XPos;
			YPos = result.YPos;

			_visibleItems = result.Items;
			_visibleEntities = result.Entities;
			_visibleArea = result.ConvertAreaToPositions().Select(x => x.Item1);

			if (result.Updates != null)
			{
				var update = result.Updates.LastOrDefault(x => x.Inventory != null);
				if (update != null) Inventory = update.Inventory;
			}
		}
	}
}