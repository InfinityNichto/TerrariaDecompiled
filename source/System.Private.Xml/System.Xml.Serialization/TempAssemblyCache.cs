using System.Collections.Generic;

namespace System.Xml.Serialization;

internal sealed class TempAssemblyCache
{
	private Dictionary<TempAssemblyCacheKey, TempAssembly> _cache = new Dictionary<TempAssemblyCacheKey, TempAssembly>();

	internal TempAssembly this[string ns, object o]
	{
		get
		{
			_cache.TryGetValue(new TempAssemblyCacheKey(ns, o), out var value);
			return value;
		}
	}

	internal void Add(string ns, object o, TempAssembly assembly)
	{
		TempAssemblyCacheKey key = new TempAssemblyCacheKey(ns, o);
		lock (this)
		{
			if (!_cache.TryGetValue(key, out var value) || value != assembly)
			{
				Dictionary<TempAssemblyCacheKey, TempAssembly> dictionary = new Dictionary<TempAssemblyCacheKey, TempAssembly>(_cache);
				dictionary[key] = assembly;
				_cache = dictionary;
			}
		}
	}
}
