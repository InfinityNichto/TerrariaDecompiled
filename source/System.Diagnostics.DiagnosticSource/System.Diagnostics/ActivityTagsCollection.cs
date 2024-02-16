using System.Collections;
using System.Collections.Generic;

namespace System.Diagnostics;

public class ActivityTagsCollection : IDictionary<string, object?>, ICollection<KeyValuePair<string, object?>>, IEnumerable<KeyValuePair<string, object?>>, IEnumerable
{
	public struct Enumerator : IEnumerator<KeyValuePair<string, object?>>, IEnumerator, IDisposable
	{
		private List<KeyValuePair<string, object>>.Enumerator _enumerator;

		public KeyValuePair<string, object?> Current => _enumerator.Current;

		object IEnumerator.Current => ((IEnumerator)_enumerator).Current;

		internal Enumerator(List<KeyValuePair<string, object>> list)
		{
			_enumerator = list.GetEnumerator();
		}

		public void Dispose()
		{
			_enumerator.Dispose();
		}

		public bool MoveNext()
		{
			return _enumerator.MoveNext();
		}

		void IEnumerator.Reset()
		{
			((IEnumerator)_enumerator).Reset();
		}
	}

	private List<KeyValuePair<string, object>> _list = new List<KeyValuePair<string, object>>();

	public object? this[string key]
	{
		get
		{
			int num = FindIndex(key);
			if (num >= 0)
			{
				return _list[num].Value;
			}
			return null;
		}
		set
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			int num = FindIndex(key);
			if (value == null)
			{
				if (num >= 0)
				{
					_list.RemoveAt(num);
				}
			}
			else if (num >= 0)
			{
				_list[num] = new KeyValuePair<string, object>(key, value);
			}
			else
			{
				_list.Add(new KeyValuePair<string, object>(key, value));
			}
		}
	}

	public ICollection<string> Keys
	{
		get
		{
			List<string> list = new List<string>(_list.Count);
			foreach (KeyValuePair<string, object> item in _list)
			{
				list.Add(item.Key);
			}
			return list;
		}
	}

	public ICollection<object?> Values
	{
		get
		{
			List<object> list = new List<object>(_list.Count);
			foreach (KeyValuePair<string, object> item in _list)
			{
				list.Add(item.Value);
			}
			return list;
		}
	}

	public bool IsReadOnly => false;

	public int Count => _list.Count;

	public ActivityTagsCollection()
	{
	}

	public ActivityTagsCollection(IEnumerable<KeyValuePair<string, object?>> list)
	{
		if (list == null)
		{
			throw new ArgumentNullException("list");
		}
		foreach (KeyValuePair<string, object> item in list)
		{
			if (item.Key != null)
			{
				this[item.Key] = item.Value;
			}
		}
	}

	public void Add(string key, object? value)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		int num = FindIndex(key);
		if (num >= 0)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.KeyAlreadyExist, key));
		}
		_list.Add(new KeyValuePair<string, object>(key, value));
	}

	public void Add(KeyValuePair<string, object?> item)
	{
		if (item.Key == null)
		{
			throw new ArgumentNullException("item");
		}
		int num = FindIndex(item.Key);
		if (num >= 0)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.KeyAlreadyExist, item.Key));
		}
		_list.Add(item);
	}

	public void Clear()
	{
		_list.Clear();
	}

	public bool Contains(KeyValuePair<string, object?> item)
	{
		return _list.Contains(item);
	}

	public bool ContainsKey(string key)
	{
		return FindIndex(key) >= 0;
	}

	public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
	{
		_list.CopyTo(array, arrayIndex);
	}

	IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
	{
		return new Enumerator(_list);
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_list);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(_list);
	}

	public bool Remove(string key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		int num = FindIndex(key);
		if (num >= 0)
		{
			_list.RemoveAt(num);
			return true;
		}
		return false;
	}

	public bool Remove(KeyValuePair<string, object?> item)
	{
		return _list.Remove(item);
	}

	public bool TryGetValue(string key, out object? value)
	{
		int num = FindIndex(key);
		if (num >= 0)
		{
			value = _list[num].Value;
			return true;
		}
		value = null;
		return false;
	}

	private int FindIndex(string key)
	{
		for (int i = 0; i < _list.Count; i++)
		{
			if (_list[i].Key == key)
			{
				return i;
			}
		}
		return -1;
	}
}
