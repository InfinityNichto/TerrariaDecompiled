using System.Collections.Generic;

namespace System.Collections.Specialized;

internal sealed class DictionaryWrapper : IDictionary<string, string>, ICollection<KeyValuePair<string, string>>, IEnumerable<KeyValuePair<string, string>>, IEnumerable, IDictionary, ICollection
{
	private readonly Dictionary<string, string> _contents;

	public string this[string key]
	{
		get
		{
			return _contents[key];
		}
		set
		{
			_contents[key] = value;
		}
	}

	public object this[object key]
	{
		get
		{
			return this[(string)key];
		}
		set
		{
			this[(string)key] = (string)value;
		}
	}

	public ICollection<string> Keys => _contents.Keys;

	public ICollection<string> Values => _contents.Values;

	ICollection IDictionary.Keys => _contents.Keys;

	ICollection IDictionary.Values => _contents.Values;

	public int Count => _contents.Count;

	public bool IsReadOnly => ((IDictionary)_contents).IsReadOnly;

	public bool IsSynchronized => ((ICollection)_contents).IsSynchronized;

	public bool IsFixedSize => ((IDictionary)_contents).IsFixedSize;

	public object SyncRoot => ((ICollection)_contents).SyncRoot;

	public DictionaryWrapper(Dictionary<string, string> contents)
	{
		_contents = contents;
	}

	public void Add(string key, string value)
	{
		this[key] = value;
	}

	public void Add(KeyValuePair<string, string> item)
	{
		Add(item.Key, item.Value);
	}

	public void Add(object key, object value)
	{
		Add((string)key, (string)value);
	}

	public void Clear()
	{
		_contents.Clear();
	}

	public bool Contains(KeyValuePair<string, string> item)
	{
		if (_contents.ContainsKey(item.Key))
		{
			return _contents[item.Key] == item.Value;
		}
		return false;
	}

	public bool Contains(object key)
	{
		return ContainsKey((string)key);
	}

	public bool ContainsKey(string key)
	{
		return _contents.ContainsKey(key);
	}

	public bool ContainsValue(string value)
	{
		return _contents.ContainsValue(value);
	}

	public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
	{
		((ICollection<KeyValuePair<string, string>>)_contents).CopyTo(array, arrayIndex);
	}

	public void CopyTo(Array array, int index)
	{
		((ICollection)_contents).CopyTo(array, index);
	}

	public bool Remove(string key)
	{
		return _contents.Remove(key);
	}

	public void Remove(object key)
	{
		Remove((string)key);
	}

	public bool Remove(KeyValuePair<string, string> item)
	{
		if (!Contains(item))
		{
			return false;
		}
		return Remove(item.Key);
	}

	public bool TryGetValue(string key, out string value)
	{
		return _contents.TryGetValue(key, out value);
	}

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		return _contents.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _contents.GetEnumerator();
	}

	IDictionaryEnumerator IDictionary.GetEnumerator()
	{
		return _contents.GetEnumerator();
	}
}
