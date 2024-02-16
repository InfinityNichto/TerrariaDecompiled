using System.Runtime.Serialization;
using System.Text;

namespace System.Collections.Specialized;

public class NameValueCollection : NameObjectCollectionBase
{
	private string[] _all;

	private string[] _allKeys;

	public string? this[string? name]
	{
		get
		{
			return Get(name);
		}
		set
		{
			Set(name, value);
		}
	}

	public string? this[int index] => Get(index);

	public virtual string?[] AllKeys
	{
		get
		{
			if (_allKeys == null)
			{
				_allKeys = BaseGetAllKeys();
			}
			return _allKeys;
		}
	}

	public NameValueCollection()
	{
	}

	public NameValueCollection(NameValueCollection col)
		: base(col?.Comparer)
	{
		Add(col);
	}

	[Obsolete("This constructor has been deprecated. Use NameValueCollection(IEqualityComparer) instead.")]
	public NameValueCollection(IHashCodeProvider? hashProvider, IComparer? comparer)
		: base(hashProvider, comparer)
	{
	}

	public NameValueCollection(int capacity)
		: base(capacity)
	{
	}

	public NameValueCollection(IEqualityComparer? equalityComparer)
		: base(equalityComparer)
	{
	}

	public NameValueCollection(int capacity, IEqualityComparer? equalityComparer)
		: base(capacity, equalityComparer)
	{
	}

	public NameValueCollection(int capacity, NameValueCollection col)
		: base(capacity, col?.Comparer)
	{
		if (col == null)
		{
			throw new ArgumentNullException("col");
		}
		base.Comparer = col.Comparer;
		Add(col);
	}

	[Obsolete("This constructor has been deprecated. Use NameValueCollection(Int32, IEqualityComparer) instead.")]
	public NameValueCollection(int capacity, IHashCodeProvider? hashProvider, IComparer? comparer)
		: base(capacity, hashProvider, comparer)
	{
	}

	protected NameValueCollection(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	protected void InvalidateCachedArrays()
	{
		_all = null;
		_allKeys = null;
	}

	private static string GetAsOneString(ArrayList list)
	{
		int num = list?.Count ?? 0;
		if (num == 1)
		{
			return (string)list[0];
		}
		if (num > 1)
		{
			StringBuilder stringBuilder = new StringBuilder((string)list[0]);
			for (int i = 1; i < num; i++)
			{
				stringBuilder.Append(',');
				stringBuilder.Append((string)list[i]);
			}
			return stringBuilder.ToString();
		}
		return null;
	}

	private static string[] GetAsStringArray(ArrayList list)
	{
		int num = list?.Count ?? 0;
		if (num == 0)
		{
			return null;
		}
		string[] array = new string[num];
		list.CopyTo(0, array, 0, num);
		return array;
	}

	public void Add(NameValueCollection c)
	{
		if (c == null)
		{
			throw new ArgumentNullException("c");
		}
		InvalidateCachedArrays();
		int count = c.Count;
		for (int i = 0; i < count; i++)
		{
			string key = c.GetKey(i);
			string[] values = c.GetValues(i);
			if (values != null)
			{
				for (int j = 0; j < values.Length; j++)
				{
					Add(key, values[j]);
				}
			}
			else
			{
				Add(key, null);
			}
		}
	}

	public virtual void Clear()
	{
		if (base.IsReadOnly)
		{
			throw new NotSupportedException(System.SR.CollectionReadOnly);
		}
		InvalidateCachedArrays();
		BaseClear();
	}

	public void CopyTo(Array dest, int index)
	{
		if (dest == null)
		{
			throw new ArgumentNullException("dest");
		}
		if (dest.Rank != 1)
		{
			throw new ArgumentException(System.SR.Arg_MultiRank, "dest");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", index, System.SR.ArgumentOutOfRange_NeedNonNegNum_Index);
		}
		if (dest.Length - index < Count)
		{
			throw new ArgumentException(System.SR.Arg_InsufficientSpace);
		}
		int count = Count;
		if (_all == null)
		{
			string[] array = new string[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = Get(i);
				dest.SetValue(array[i], i + index);
			}
			_all = array;
		}
		else
		{
			for (int j = 0; j < count; j++)
			{
				dest.SetValue(_all[j], j + index);
			}
		}
	}

	public bool HasKeys()
	{
		return InternalHasKeys();
	}

	internal virtual bool InternalHasKeys()
	{
		return BaseHasKeys();
	}

	public virtual void Add(string? name, string? value)
	{
		if (base.IsReadOnly)
		{
			throw new NotSupportedException(System.SR.CollectionReadOnly);
		}
		InvalidateCachedArrays();
		ArrayList arrayList = (ArrayList)BaseGet(name);
		if (arrayList == null)
		{
			arrayList = new ArrayList(1);
			if (value != null)
			{
				arrayList.Add(value);
			}
			BaseAdd(name, arrayList);
		}
		else if (value != null)
		{
			arrayList.Add(value);
		}
	}

	public virtual string? Get(string? name)
	{
		ArrayList list = (ArrayList)BaseGet(name);
		return GetAsOneString(list);
	}

	public virtual string[]? GetValues(string? name)
	{
		ArrayList list = (ArrayList)BaseGet(name);
		return GetAsStringArray(list);
	}

	public virtual void Set(string? name, string? value)
	{
		if (base.IsReadOnly)
		{
			throw new NotSupportedException(System.SR.CollectionReadOnly);
		}
		InvalidateCachedArrays();
		ArrayList arrayList = new ArrayList(1);
		arrayList.Add(value);
		BaseSet(name, arrayList);
	}

	public virtual void Remove(string? name)
	{
		InvalidateCachedArrays();
		BaseRemove(name);
	}

	public virtual string? Get(int index)
	{
		ArrayList list = (ArrayList)BaseGet(index);
		return GetAsOneString(list);
	}

	public virtual string[]? GetValues(int index)
	{
		ArrayList list = (ArrayList)BaseGet(index);
		return GetAsStringArray(list);
	}

	public virtual string? GetKey(int index)
	{
		return BaseGetKey(index);
	}
}
