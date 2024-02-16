namespace System.Collections;

public abstract class DictionaryBase : IDictionary, ICollection, IEnumerable
{
	private Hashtable _hashtable;

	protected Hashtable InnerHashtable
	{
		get
		{
			if (_hashtable == null)
			{
				_hashtable = new Hashtable();
			}
			return _hashtable;
		}
	}

	protected IDictionary Dictionary => this;

	public int Count
	{
		get
		{
			if (_hashtable != null)
			{
				return _hashtable.Count;
			}
			return 0;
		}
	}

	bool IDictionary.IsReadOnly => InnerHashtable.IsReadOnly;

	bool IDictionary.IsFixedSize => InnerHashtable.IsFixedSize;

	bool ICollection.IsSynchronized => InnerHashtable.IsSynchronized;

	ICollection IDictionary.Keys => InnerHashtable.Keys;

	object ICollection.SyncRoot => InnerHashtable.SyncRoot;

	ICollection IDictionary.Values => InnerHashtable.Values;

	object? IDictionary.this[object key]
	{
		get
		{
			object obj = InnerHashtable[key];
			OnGet(key, obj);
			return obj;
		}
		set
		{
			OnValidate(key, value);
			bool flag = true;
			object obj = InnerHashtable[key];
			if (obj == null)
			{
				flag = InnerHashtable.Contains(key);
			}
			OnSet(key, obj, value);
			InnerHashtable[key] = value;
			try
			{
				OnSetComplete(key, obj, value);
			}
			catch
			{
				if (flag)
				{
					InnerHashtable[key] = obj;
				}
				else
				{
					InnerHashtable.Remove(key);
				}
				throw;
			}
		}
	}

	public void CopyTo(Array array, int index)
	{
		InnerHashtable.CopyTo(array, index);
	}

	bool IDictionary.Contains(object key)
	{
		return InnerHashtable.Contains(key);
	}

	void IDictionary.Add(object key, object value)
	{
		OnValidate(key, value);
		OnInsert(key, value);
		InnerHashtable.Add(key, value);
		try
		{
			OnInsertComplete(key, value);
		}
		catch
		{
			InnerHashtable.Remove(key);
			throw;
		}
	}

	public void Clear()
	{
		OnClear();
		InnerHashtable.Clear();
		OnClearComplete();
	}

	void IDictionary.Remove(object key)
	{
		if (InnerHashtable.Contains(key))
		{
			object value = InnerHashtable[key];
			OnValidate(key, value);
			OnRemove(key, value);
			InnerHashtable.Remove(key);
			try
			{
				OnRemoveComplete(key, value);
			}
			catch
			{
				InnerHashtable.Add(key, value);
				throw;
			}
		}
	}

	public IDictionaryEnumerator GetEnumerator()
	{
		return InnerHashtable.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return InnerHashtable.GetEnumerator();
	}

	protected virtual object? OnGet(object key, object? currentValue)
	{
		return currentValue;
	}

	protected virtual void OnSet(object key, object? oldValue, object? newValue)
	{
	}

	protected virtual void OnInsert(object key, object? value)
	{
	}

	protected virtual void OnClear()
	{
	}

	protected virtual void OnRemove(object key, object? value)
	{
	}

	protected virtual void OnValidate(object key, object? value)
	{
	}

	protected virtual void OnSetComplete(object key, object? oldValue, object? newValue)
	{
	}

	protected virtual void OnInsertComplete(object key, object? value)
	{
	}

	protected virtual void OnClearComplete()
	{
	}

	protected virtual void OnRemoveComplete(object key, object? value)
	{
	}
}
