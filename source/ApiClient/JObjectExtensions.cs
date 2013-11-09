using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ApiClient
{
	public static class JObjectExtensions
	{
		public static IEnumerable<T> GetIEnumerable<T>(this JObject jObject, object key)
		{
			var value = jObject[key];
			return value != null
				       ? value.ToObject<IEnumerable<T>>()
				       : Enumerable.Empty<T>();
		}
	}
}