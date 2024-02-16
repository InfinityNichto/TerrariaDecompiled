using System.Runtime.CompilerServices;

namespace System.Collections.Specialized;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class HybridDictionary : IDictionary, ICollection, IEnumerable
{
	private ListDictionary list;

	private Hashtable hashtable;

	private readonly bool caseInsensitive;

	public object? this[object key]
	{
		get
		{
			ListDictionary listDictionary = list;
			if (hashtable != null)
			{
				return hashtable[key];
			}
			if (listDictionary != null)
			{
				return listDictionary[key];
			}
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			return null;
		}
		set
		{
			if (hashtable != null)
			{
				hashtable[key] = value;
			}
			else if (list != null)
			{
				if (list.Count >= 8)
				{
					ChangeOver();
					hashtable[key] = value;
				}
				else
				{
					list[key] = value;
				}
			}
			else
			{
				list = new ListDictionary(caseInsensitive ? StringComparer.OrdinalIgnoreCase : null);
				list[key] = value;
			}
		}
	}

	private ListDictionary List
	{
		get
		{
			if (list == null)
			{
				list = new ListDictionary(caseInsensitive ? StringComparer.OrdinalIgnoreCase : null);
			}
			return list;
		}
	}

	public int Count
	{
		get
		{
			ListDictionary listDictionary = list;
			if (hashtable != null)
			{
				return hashtable.Count;
			}
			return listDictionary?.Count ?? 0;
		}
	}

	public ICollection Keys
	{
		get
		{
			if (hashtable != null)
			{
				return hashtable.Keys;
			}
			return List.Keys;
		}
	}

	public bool IsReadOnly => false;

	public bool IsFixedSize => false;

	public bool IsSynchronized => false;

	public object SyncRoot => this;

	public ICollection Values
	{
		get
		{
			if (hashtable != null)
			{
				return hashtable.Values;
			}
			return List.Values;
		}
	}

	public HybridDictionary()
	{
	}

	public HybridDictionary(int initialSize)
		: this(initialSize, caseInsensitive: false)
	{
	}

	public HybridDictionary(bool caseInsensitive)
	{
		this.caseInsensitive = caseInsensitive;
	}

	public HybridDictionary(int initialSize, bool caseInsensitive)
	{
		this.caseInsensitive = caseInsensitive;
		if (initialSize >= 6)
		{
			if (caseInsensitive)
			{
				hashtable = new Hashtable(initialSize, StringComparer.OrdinalIgnoreCase);
			}
			else
			{
				hashtable = new Hashtable(initialSize);
			}
		}
	}

	private void ChangeOver()
	{
		IDictionaryEnumerator enumerator = list.GetEnumerator();
		Hashtable hashtable = ((!caseInsensitive) ? new Hashtable(13) : new Hashtable(13, StringComparer.OrdinalIgnoreCase));
		while (enumerator.MoveNext())
		{
			hashtable.Add(enumerator.Key, enumerator.Value);
		}
		this.hashtable = hashtable;
		list = null;
	}

	public void Add(object key, object? value)
	{
		if (hashtable != null)
		{
			hashtable.Add(key, value);
		}
		else if (list == null)
		{
			list = new ListDictionary(caseInsensitive ? StringComparer.OrdinalIgnoreCase : null);
			list.Add(key, value);
		}
		else if (list.Count + 1 >= 9)
		{
			ChangeOver();
			hashtable.Add(key, value);
		}
		else
		{
			list.Add(key, value);
		}
	}

	public void Clear()
	{
		if (this.hashtable != null)
		{
			Hashtable hashtable = this.hashtable;
			this.hashtable = null;
			hashtable.Clear();
		}
		if (list != null)
		{
			ListDictionary listDictionary = list;
			list = null;
			listDictionary.Clear();
		}
	}

	public bool Contains(object key)
	{
		ListDictionary listDictionary = list;
		if (hashtable != null)
		{
			return hashtable.Contains(key);
		}
		if (listDictionary != null)
		{
			return listDictionary.Contains(key);
		}
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		return false;
	}

	public void CopyTo(Array array, int index)
	{
		if (hashtable != null)
		{
			hashtable.CopyTo(array, index);
		}
		else
		{
			List.CopyTo(array, index);
		}
	}

	public IDictionaryEnumerator GetEnumerator()
	{
		if (hashtable != null)
		{
			return hashtable.GetEnumerator();
		}
		if (list == null)
		{
			list = new ListDictionary(caseInsensitive ? StringComparer.OrdinalIgnoreCase : null);
		}
		return list.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		if (hashtable != null)
		{
			return hashtable.GetEnumerator();
		}
		if (list == null)
		{
			list = new ListDictionary(caseInsensitive ? StringComparer.OrdinalIgnoreCase : null);
		}
		return list.GetEnumerator();
	}

	public void Remove(object key)
	{
		if (hashtable != null)
		{
			hashtable.Remove(key);
		}
		else if (list != null)
		{
			list.Remove(key);
		}
		else if (key == null)
		{
			throw new ArgumentNullException("key");
		}
	}
}
