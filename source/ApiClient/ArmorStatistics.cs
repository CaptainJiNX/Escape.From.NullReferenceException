namespace ApiClient
{
	public class ArmorStatistics
	{
		public ArmorStatistics(string input)
		{
			var values = input.Split(';');
			ArmorName = values[0];
			ArmorClass = int.Parse(values[1]);
		}

		public ArmorStatistics(string armorName, int armorClass)
		{
			ArmorName = (armorName ?? "none").Replace(';',':');
			ArmorClass = armorClass;
		}

		public string ArmorName { get; private set; }
		public int ArmorClass { get; private set; }

		public override string ToString()
		{
			return string.Format("{0};{1}", ArmorName, ArmorClass);
		}
	}
}