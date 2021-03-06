﻿using System.Collections.Generic;
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

		public Position Position { get { return new Position(XPos, YPos); }}

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
			if (result.Updates != null)
			{
				var charUpdate = result.Updates
									   .Where(x => x.Character != null)
									   .LastOrDefault(x => x.Character.Id == Id);
				if (charUpdate != null)
				{
					Update(charUpdate.Character);
				}

				var inventoryUpdate = result.Updates.LastOrDefault(x => x.Inventory != null);
				if (inventoryUpdate != null) Inventory = inventoryUpdate.Inventory;
			}


			XPos = result.XPos;
			YPos = result.YPos;
			CurrentMap = result.Map;

			_visibleItems = result.Items;
			_visibleEntities = result.Entities;
			_visibleArea = result.ConvertAreaToPositions().Select(x => x.Item1);
		}

		private void Update(Character uc)
		{
			ArmorClass = uc.ArmorClass;
			_hitPoints = uc._hitPoints;

			Strength = uc.Strength;
			Dexterity = uc.Dexterity;
			Constitution = uc.Constitution;
			Intelligence = uc.Intelligence;
			Wisdom = uc.Wisdom;

			Experience = uc.Experience;
			Level = uc.Level;

			Light = uc.Light;
			Speed = uc.Speed;

			EquippedArmorId = uc.EquippedArmorId;
			EquippedArmorName = uc.EquippedArmorName;
			WieldedWeaponId = uc.WieldedWeaponId;
			WieldedWeaponName = uc.WieldedWeaponName;

			PointsToAllocate = uc.PointsToAllocate;
		}
	}
}