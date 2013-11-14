using System.Collections.Generic;

namespace ApiClient
{
	public interface ISimpleStorage<T>
	{
		IEnumerable<T> GetAll();
		void Store(T value);
	}
}