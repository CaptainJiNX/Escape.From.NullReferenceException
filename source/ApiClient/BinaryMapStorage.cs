using System.Collections.Generic;
using System.IO;
using System.Linq;
using BinaryRage;

namespace ApiClient
{
	public class BinaryMapStorage : ISimpleStorage<Map>
	{
		private HashSet<string> _mapNames;

		private const string MapNamesKey = "MapNames";
		private const string StorageLocation = "MapStorage";
		private const string MapsLocation = StorageLocation + "\\Maps";

		private HashSet<string> MapNames
		{
			get { return _mapNames ?? (_mapNames = LoadMapNames()); }
		}

		private void AddMapName(string mapName)
		{
			if (MapNames.Contains(mapName)) return;
			MapNames.Add(mapName);
			SaveMapNames();
		}

		public IEnumerable<Map> GetAll()
		{
			return MapNames.Select(Get);
		}

		private Map Get(string mapName)
		{
			try
			{
				return DB<Map>.Get(mapName, MapsLocation);
			}
			catch(DirectoryNotFoundException)
			{
				return new Map(mapName);
			}
		}

		public void Store(Map value)
		{
			AddMapName(value.Name);
			DB<Map>.Insert(value.Name, value, MapsLocation);
			value.ClearChanges();
		}

		private HashSet<string> LoadMapNames()
		{
			try
			{
				return DB<HashSet<string>>.Get(MapNamesKey, StorageLocation);
			}
			catch (DirectoryNotFoundException)
			{
				return new HashSet<string>();
			}
		}

		private void SaveMapNames()
		{
			DB<HashSet<string>>.Insert(MapNamesKey, MapNames, StorageLocation);
		}
	}
}