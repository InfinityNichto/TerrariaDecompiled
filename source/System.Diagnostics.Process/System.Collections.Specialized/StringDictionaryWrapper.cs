using System.Collections.Generic;

namespace System.Collections.Specialized;

internal sealed class StringDictionaryWrapper : StringDictionary
{
	private readonly DictionaryWrapper _contents;

	public override string this[string key]
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

	public override int Count => _contents.Count;

	public override ICollection Keys => ((IDictionary)_contents).Keys;

	public override ICollection Values => ((IDictionary)_contents).Values;

	public override bool IsSynchronized => false;

	public override object SyncRoot => _contents.SyncRoot;

	public StringDictionaryWrapper(DictionaryWrapper contents)
	{
		_contents = contents;
	}

	public override void Add(string key, string value)
	{
		if (_contents.ContainsKey(key))
		{
			throw new ArgumentException();
		}
		_contents.Add(key, value);
	}

	public override void Clear()
	{
		_contents.Clear();
	}

	public override bool ContainsKey(string key)
	{
		return _contents.ContainsKey(key);
	}

	public override bool ContainsValue(string value)
	{
		return _contents.ContainsValue(value);
	}

	public override void CopyTo(Array array, int index)
	{
		_contents.CopyTo(array, index);
	}

	public override IEnumerator GetEnumerator()
	{
		foreach (KeyValuePair<string, string> content in _contents)
		{
			yield return new DictionaryEntry(content.Key, content.Value);
		}
	}

	public override void Remove(string key)
	{
		_contents.Remove(key);
	}
}
