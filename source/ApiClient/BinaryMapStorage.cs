using System.Collections.Generic;
using System.IO;
using System.Linq;
using BinaryRage;

namespace ApiClient
{
	public class BinaryMapStorage : IMapStorage
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

		public Map Get(string mapName)
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

		public void Save(Map map)
		{
			AddMapName(map.Name);
			DB<Map>.Insert(map.Name, map, MapsLocation);
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