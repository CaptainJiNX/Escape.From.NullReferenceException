namespace ApiClient
{
	public class DamageStatistics
	{
		public DamageStatistics(string input)
		{
			var values = input.Split(';');
			WeaponName = values[0];
			Damage = int.Parse(values[1]);
			Strength = int.Parse(values[2]);
			Level = int.Parse(values[3]);
		}

		public DamageStatistics(string weaponName, int damage, int strength, int level)
		{
			WeaponName = weaponName.Replace(';',':');
			Damage = damage;
			Strength = strength;
			Level = level;
		}

		public string WeaponName { get; private set; }
		public int Damage { get; private set; }
		public int Strength { get; private set; }
		public int Level { get; private set; }

		public override string ToString()
		{
			return string.Format("{0};{1};{2};{3}", WeaponName, Damage, Strength, Level);
		}
	}
}