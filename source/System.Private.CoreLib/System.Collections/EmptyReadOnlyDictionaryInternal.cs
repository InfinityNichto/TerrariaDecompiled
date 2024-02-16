namespace System.Collections;

internal sealed class EmptyReadOnlyDictionaryInternal : IDictionary, ICollection, IEnumerable
{
	private sealed class NodeEnumerator : IDictionaryEnumerator, IEnumerator
	{
		public object Current
		{
			get
			{
				throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
			}
		}

		public object Key
		{
			get
			{
				throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
			}
		}

		public object Value
		{
			get
			{
				throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
			}
		}

		public DictionaryEntry Entry
		{
			get
			{
				throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
			}
		}

		public bool MoveNext()
		{
			return false;
		}

		public void Reset()
		{
		}
	}

	public int Count => 0;

	public object SyncRoot => this;

	public bool IsSynchronized => false;

	public object this[object key]
	{
		get
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", SR.ArgumentNull_Key);
			}
			return null;
		}
		set
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", SR.ArgumentNull_Key);
			}
			if (!key.GetType().IsSerializable)
			{
				throw new ArgumentException(SR.Argument_NotSerializable, "key");
			}
			if (value != null && !value.GetType().IsSerializable)
			{
				throw new ArgumentException(SR.Argument_NotSerializable, "value");
			}
			throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
		}
	}

	public ICollection Keys => Array.Empty<object>();

	public ICollection Values => Array.Empty<object>();

	public bool IsReadOnly => true;

	public bool IsFixedSize => true;

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new NodeEnumerator();
	}

	public void CopyTo(Array array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (array.Rank != 1)
		{
			throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (array.Length - index < Count)
		{
			throw new ArgumentException(SR.ArgumentOutOfRange_Index, "index");
		}
	}

	public bool Contains(object key)
	{
		return false;
	}

	public void Add(object key, object value)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key", SR.ArgumentNull_Key);
		}
		if (!key.GetType().IsSerializable)
		{
			throw new ArgumentException(SR.Argument_NotSerializable, "key");
		}
		if (value != null && !value.GetType().IsSerializable)
		{
			throw new ArgumentException(SR.Argument_NotSerializable, "value");
		}
		throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
	}

	public void Clear()
	{
		throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
	}

	public IDictionaryEnumerator GetEnumerator()
	{
		return new NodeEnumerator();
	}

	public void Remove(object key)
	{
		throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
	}
}
