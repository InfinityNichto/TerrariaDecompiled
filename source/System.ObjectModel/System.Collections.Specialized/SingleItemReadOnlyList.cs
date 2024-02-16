namespace System.Collections.Specialized;

internal sealed class SingleItemReadOnlyList : IList, ICollection, IEnumerable
{
	private readonly object _item;

	public object this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			return _item;
		}
		set
		{
			throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
		}
	}

	public bool IsFixedSize => true;

	public bool IsReadOnly => true;

	public int Count => 1;

	public bool IsSynchronized => false;

	public object SyncRoot => this;

	public SingleItemReadOnlyList(object item)
	{
		_item = item;
	}

	public IEnumerator GetEnumerator()
	{
		yield return _item;
	}

	public bool Contains(object value)
	{
		if (_item != null)
		{
			return _item.Equals(value);
		}
		return value == null;
	}

	public int IndexOf(object value)
	{
		if (!Contains(value))
		{
			return -1;
		}
		return 0;
	}

	public void CopyTo(Array array, int index)
	{
		CollectionHelpers.ValidateCopyToArguments(1, array, index);
		array.SetValue(_item, index);
	}

	public int Add(object value)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	public void Clear()
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	public void Insert(int index, object value)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	public void Remove(object value)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	public void RemoveAt(int index)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}
}
