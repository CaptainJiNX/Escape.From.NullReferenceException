using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ApiClient
{
	public class Character
	{
		private IEnumerable<Item> _visibleItems;
		private IEnumerable<Item> _visibleEntities;

		public Character(JObject created)
		{
			Id = created["_id"].Value<string>();
			Name = created["name"].Value<string>();
			Experience = created["exp"].Value<int>();
			Level = created["level"].Value<int>();

			Strength = created["str"].Value<int>();
			Intelligence = created["int"].Value<int>();
			Wisdom = created["wis"].Value<int>();
			Dexterity = created["dex"].Value<int>();
			Constitution = created["con"].Value<int>();

			CurrentMap = created["map"].Value<string>();
			Inventory = created["inventory"].Values<string>();

			XPos = created["x"].Value<int>();
			YPos = created["y"].Value<int>();
			HitPoints = created["hp"].Value<int>();
			ArmorClass = created["ac"].Value<int>();
			PointsToAllocate = created["alloc"].Value<int>();
			Speed = created["speed"].Value<int>();
			Light = created["light"].Value<int>();

			WieldedWeaponId = created["wieldedweapon"].Value<string>();
			EquippedArmorId = created["equippedarmor"].Value<string>();
			Type = created["type"].Value<string>();
			Hates = created["hates"].Values<string>();
			AttackType = created["attack"]["type"].Value<string>();
			AttackDamage = created["attack"]["damage"].Values<int>();

			TeamAbbreviation = created["teamabbrev"].Value<string>();
			TeamId = created["teamid"].Value<string>();

			MaxHitPoints = created["maxhp"].Value<int>();
			Resource = created["resource"].Value<string>();

			// "updates":[] ??
		}

		public string Id { get; private set; }
		public string Name { get; private set; }
		public int Experience { get; private set; }
		public int Level { get; private set; }

		public int Strength { get; private set; }
		public int Intelligence { get; private set; }
		public int Wisdom { get; private set; }
		public int Dexterity { get; private set; }
		public int Constitution { get; private set; }

		public string CurrentMap { get; private set; }
		public IEnumerable<string> Inventory { get; private set; }

		public int XPos { get; private set; }
		public int YPos { get; private set; }
		public int HitPoints { get; private set; }
		public int ArmorClass { get; private set; }
		public int PointsToAllocate { get; private set; }
		public int Speed { get; private set; }
		public int Light { get; private set; }

		public string WieldedWeaponId { get; private set; }
		public string EquippedArmorId { get; private set; }
		public string Type { get; private set; }
		public IEnumerable<string> Hates { get; private set; }
		public string AttackType { get; private set; }
		public IEnumerable<int> AttackDamage { get; private set; }

		public string TeamAbbreviation { get; private set; }

		public string TeamId { get; private set; }
		public int MaxHitPoints { get; private set; }
		public string Resource { get; private set; }

		public IEnumerable<Item> VisibleItems
		{
			get { return _visibleItems ?? new Item[0]; }
		}

		public IEnumerable<Item> VisibleEntities
		{
			get { return _visibleEntities ?? new Item[0]; }
		}

		public void UpdateFromScan(JObject scanResult)
		{
			CurrentMap = scanResult.Value<string>("map");
			XPos = scanResult.Value<int>("x");
			YPos = scanResult.Value<int>("y");
			_visibleItems = scanResult.Values<Item>("items");
			_visibleEntities = scanResult.Values<Item>("entities");
		}

		public void UpdateFromMovement(JObject movement)
		{
			UpdateFromScan(movement);
		}
	}
}