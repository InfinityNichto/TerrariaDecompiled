using System.Collections.Concurrent;

namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class NameCache
{
	private static readonly ConcurrentDictionary<string, object> s_ht = new ConcurrentDictionary<string, object>();

	private string _name;

	internal object GetCachedValue(string name)
	{
		_name = name;
		if (!s_ht.TryGetValue(name, out var value))
		{
			return null;
		}
		return value;
	}

	internal void SetCachedValue(object value)
	{
		s_ht[_name] = value;
	}
}
