using System.Collections.Generic;

namespace ApiClient
{
	public interface IMapStorage
	{
		IEnumerable<Map> GetAll();
		Map Get(string mapName);
		void Save(Map map);
	}
}