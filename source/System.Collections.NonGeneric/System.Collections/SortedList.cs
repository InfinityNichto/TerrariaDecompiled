using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Collections;

[Serializable]
[DebuggerTypeProxy(typeof(SortedListDebugView))]
[DebuggerDisplay("Count = {Count}")]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class SortedList : IDictionary, ICollection, IEnumerable, ICloneable
{
	[Serializable]
	[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
	private sealed class SyncSortedList : SortedList
	{
		private readonly SortedList _list;

		private readonly object _root;

		public override int Count
		{
			get
			{
				lock (_root)
				{
					return _list.Count;
				}
			}
		}

		public override object SyncRoot => _root;

		public override bool IsReadOnly => _list.IsReadOnly;

		public override bool IsFixedSize => _list.IsFixedSize;

		public override bool IsSynchronized => true;

		public override object this[object key]
		{
			get
			{
				lock (_root)
				{
					return _list[key];
				}
			}
			set
			{
				lock (_root)
				{
					_list[key] = value;
				}
			}
		}

		public override int Capacity
		{
			get
			{
				lock (_root)
				{
					return _list.Capacity;
				}
			}
		}

		internal SyncSortedList(SortedList list)
		{
			_list = list;
			_root = list.SyncRoot;
		}

		public override void Add(object key, object value)
		{
			lock (_root)
			{
				_list.Add(key, value);
			}
		}

		public override void Clear()
		{
			lock (_root)
			{
				_list.Clear();
			}
		}

		public override object Clone()
		{
			lock (_root)
			{
				return _list.Clone();
			}
		}

		public override bool Contains(object key)
		{
			lock (_root)
			{
				return _list.Contains(key);
			}
		}

		public override bool ContainsKey(object key)
		{
			lock (_root)
			{
				return _list.ContainsKey(key);
			}
		}

		public override bool ContainsValue(object key)
		{
			lock (_root)
			{
				return _list.ContainsValue(key);
			}
		}

		public override void CopyTo(Array array, int index)
		{
			lock (_root)
			{
				_list.CopyTo(array, index);
			}
		}

		public override object GetByIndex(int index)
		{
			lock (_root)
			{
				return _list.GetByIndex(index);
			}
		}

		public override IDictionaryEnumerator GetEnumerator()
		{
			lock (_root)
			{
				return _list.GetEnumerator();
			}
		}

		public override object GetKey(int index)
		{
			lock (_root)
			{
				return _list.GetKey(index);
			}
		}

		public override IList GetKeyList()
		{
			lock (_root)
			{
				return _list.GetKeyList();
			}
		}

		public override IList GetValueList()
		{
			lock (_root)
			{
				return _list.GetValueList();
			}
		}

		public override int IndexOfKey(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", System.SR.ArgumentNull_Key);
			}
			lock (_root)
			{
				return _list.IndexOfKey(key);
			}
		}

		public override int IndexOfValue(object value)
		{
			lock (_root)
			{
				return _list.IndexOfValue(value);
			}
		}

		public override void RemoveAt(int index)
		{
			lock (_root)
			{
				_list.RemoveAt(index);
			}
		}

		public override void Remove(object key)
		{
			lock (_root)
			{
				_list.Remove(key);
			}
		}

		public override void SetByIndex(int index, object value)
		{
			lock (_root)
			{
				_list.SetByIndex(index, value);
			}
		}

		internal override System.Collections.KeyValuePairs[] ToKeyValuePairsArray()
		{
			return _list.ToKeyValuePairsArray();
		}

		public override void TrimToSize()
		{
			lock (_root)
			{
				_list.TrimToSize();
			}
		}
	}

	private sealed class SortedListEnumerator : IDictionaryEnumerator, IEnumerator, ICloneable
	{
		private readonly SortedList _sortedList;

		private object _key;

		private object _value;

		private int _index;

		private readonly int _startIndex;

		private readonly int _endIndex;

		private readonly int _version;

		private bool _current;

		private readonly int _getObjectRetType;

		public object Key
		{
			get
			{
				if (_version != _sortedList.version)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
				}
				if (!_current)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumOpCantHappen);
				}
				return _key;
			}
		}

		public DictionaryEntry Entry
		{
			get
			{
				if (_version != _sortedList.version)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
				}
				if (!_current)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumOpCantHappen);
				}
				return new DictionaryEntry(_key, _value);
			}
		}

		public object Current
		{
			get
			{
				if (!_current)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumOpCantHappen);
				}
				if (_getObjectRetType == 1)
				{
					return _key;
				}
				if (_getObjectRetType == 2)
				{
					return _value;
				}
				return new DictionaryEntry(_key, _value);
			}
		}

		public object Value
		{
			get
			{
				if (_version != _sortedList.version)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
				}
				if (!_current)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumOpCantHappen);
				}
				return _value;
			}
		}

		internal SortedListEnumerator(SortedList sortedList, int index, int count, int getObjRetType)
		{
			_sortedList = sortedList;
			_index = index;
			_startIndex = index;
			_endIndex = index + count;
			_version = sortedList.version;
			_getObjectRetType = getObjRetType;
			_current = false;
		}

		public object Clone()
		{
			return MemberwiseClone();
		}

		public bool MoveNext()
		{
			if (_version != _sortedList.version)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
			}
			if (_index < _endIndex)
			{
				_key = _sortedList.keys[_index];
				_value = _sortedList.values[_index];
				_index++;
				_current = true;
				return true;
			}
			_key = null;
			_value = null;
			_current = false;
			return false;
		}

		public void Reset()
		{
			if (_version != _sortedList.version)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
			}
			_index = _startIndex;
			_current = false;
			_key = null;
			_value = null;
		}
	}

	[Serializable]
	[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
	private sealed class KeyList : IList, ICollection, IEnumerable
	{
		private readonly SortedList sortedList;

		public int Count => sortedList._size;

		public bool IsReadOnly => true;

		public bool IsFixedSize => true;

		public bool IsSynchronized => sortedList.IsSynchronized;

		public object SyncRoot => sortedList.SyncRoot;

		public object this[int index]
		{
			get
			{
				return sortedList.GetKey(index);
			}
			set
			{
				throw new NotSupportedException(System.SR.NotSupported_KeyCollectionSet);
			}
		}

		internal KeyList(SortedList sortedList)
		{
			this.sortedList = sortedList;
		}

		public int Add(object key)
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}

		public void Clear()
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}

		public bool Contains(object key)
		{
			return sortedList.Contains(key);
		}

		public void CopyTo(Array array, int arrayIndex)
		{
			if (array != null && array.Rank != 1)
			{
				throw new ArgumentException(System.SR.Arg_RankMultiDimNotSupported, "array");
			}
			Array.Copy(sortedList.keys, 0, array, arrayIndex, sortedList.Count);
		}

		public void Insert(int index, object value)
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}

		public IEnumerator GetEnumerator()
		{
			return new SortedListEnumerator(sortedList, 0, sortedList.Count, 1);
		}

		public int IndexOf(object key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", System.SR.ArgumentNull_Key);
			}
			int num = Array.BinarySearch(sortedList.keys, 0, sortedList.Count, key, sortedList.comparer);
			if (num >= 0)
			{
				return num;
			}
			return -1;
		}

		public void Remove(object key)
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}

		public void RemoveAt(int index)
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}
	}

	[Serializable]
	[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
	private sealed class ValueList : IList, ICollection, IEnumerable
	{
		private readonly SortedList sortedList;

		public int Count => sortedList._size;

		public bool IsReadOnly => true;

		public bool IsFixedSize => true;

		public bool IsSynchronized => sortedList.IsSynchronized;

		public object SyncRoot => sortedList.SyncRoot;

		public object this[int index]
		{
			get
			{
				return sortedList.GetByIndex(index);
			}
			set
			{
				throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
			}
		}

		internal ValueList(SortedList sortedList)
		{
			this.sortedList = sortedList;
		}

		public int Add(object key)
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}

		public void Clear()
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}

		public bool Contains(object value)
		{
			return sortedList.ContainsValue(value);
		}

		public void CopyTo(Array array, int arrayIndex)
		{
			if (array != null && array.Rank != 1)
			{
				throw new ArgumentException(System.SR.Arg_RankMultiDimNotSupported, "array");
			}
			Array.Copy(sortedList.values, 0, array, arrayIndex, sortedList.Count);
		}

		public void Insert(int index, object value)
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}

		public IEnumerator GetEnumerator()
		{
			return new SortedListEnumerator(sortedList, 0, sortedList.Count, 2);
		}

		public int IndexOf(object value)
		{
			return Array.IndexOf(sortedList.values, value, 0, sortedList.Count);
		}

		public void Remove(object value)
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}

		public void RemoveAt(int index)
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}
	}

	internal sealed class SortedListDebugView
	{
		private readonly SortedList _sortedList;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public System.Collections.KeyValuePairs[] Items => _sortedList.ToKeyValuePairsArray();

		public SortedListDebugView(SortedList sortedList)
		{
			if (sortedList == null)
			{
				throw new ArgumentNullException("sortedList");
			}
			_sortedList = sortedList;
		}
	}

	private object[] keys;

	private object[] values;

	private int _size;

	private int version;

	private IComparer comparer;

	private KeyList keyList;

	private ValueList valueList;

	public virtual int Capacity
	{
		get
		{
			return keys.Length;
		}
		set
		{
			if (value < Count)
			{
				throw new ArgumentOutOfRangeException("value", System.SR.ArgumentOutOfRange_SmallCapacity);
			}
			if (value == keys.Length)
			{
				return;
			}
			if (value > 0)
			{
				object[] destinationArray = new object[value];
				object[] destinationArray2 = new object[value];
				if (_size > 0)
				{
					Array.Copy(keys, destinationArray, _size);
					Array.Copy(values, destinationArray2, _size);
				}
				keys = destinationArray;
				values = destinationArray2;
			}
			else
			{
				keys = Array.Empty<object>();
				values = Array.Empty<object>();
			}
		}
	}

	public virtual int Count => _size;

	public virtual ICollection Keys => GetKeyList();

	public virtual ICollection Values => GetValueList();

	public virtual bool IsReadOnly => false;

	public virtual bool IsFixedSize => false;

	public virtual bool IsSynchronized => false;

	public virtual object SyncRoot => this;

	public virtual object? this[object key]
	{
		get
		{
			int num = IndexOfKey(key);
			if (num >= 0)
			{
				return values[num];
			}
			return null;
		}
		set
		{
			if (key == null)
			{
				throw new ArgumentNullException("key", System.SR.ArgumentNull_Key);
			}
			int num = Array.BinarySearch(keys, 0, _size, key, comparer);
			if (num >= 0)
			{
				values[num] = value;
				version++;
			}
			else
			{
				Insert(~num, key, value);
			}
		}
	}

	public SortedList()
	{
		keys = Array.Empty<object>();
		values = Array.Empty<object>();
		_size = 0;
		comparer = new Comparer(CultureInfo.CurrentCulture);
	}

	public SortedList(int initialCapacity)
	{
		if (initialCapacity < 0)
		{
			throw new ArgumentOutOfRangeException("initialCapacity", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		keys = new object[initialCapacity];
		values = new object[initialCapacity];
		comparer = new Comparer(CultureInfo.CurrentCulture);
	}

	public SortedList(IComparer? comparer)
		: this()
	{
		if (comparer != null)
		{
			this.comparer = comparer;
		}
	}

	public SortedList(IComparer? comparer, int capacity)
		: this(comparer)
	{
		Capacity = capacity;
	}

	public SortedList(IDictionary d)
		: this(d, null)
	{
	}

	public SortedList(IDictionary d, IComparer? comparer)
		: this(comparer, d?.Count ?? 0)
	{
		if (d == null)
		{
			throw new ArgumentNullException("d", System.SR.ArgumentNull_Dictionary);
		}
		d.Keys.CopyTo(keys, 0);
		d.Values.CopyTo(values, 0);
		Array.Sort(keys, comparer);
		for (int i = 0; i < keys.Length; i++)
		{
			values[i] = d[keys[i]];
		}
		_size = d.Count;
	}

	public virtual void Add(object key, object? value)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key", System.SR.ArgumentNull_Key);
		}
		int num = Array.BinarySearch(keys, 0, _size, key, comparer);
		if (num >= 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_AddingDuplicate_OldAndNewKeys, GetKey(num), key));
		}
		Insert(~num, key, value);
	}

	public virtual void Clear()
	{
		version++;
		Array.Clear(keys, 0, _size);
		Array.Clear(values, 0, _size);
		_size = 0;
	}

	public virtual object Clone()
	{
		SortedList sortedList = new SortedList(_size);
		Array.Copy(keys, sortedList.keys, _size);
		Array.Copy(values, sortedList.values, _size);
		sortedList._size = _size;
		sortedList.version = version;
		sortedList.comparer = comparer;
		return sortedList;
	}

	public virtual bool Contains(object key)
	{
		return IndexOfKey(key) >= 0;
	}

	public virtual bool ContainsKey(object key)
	{
		return IndexOfKey(key) >= 0;
	}

	public virtual bool ContainsValue(object? value)
	{
		return IndexOfValue(value) >= 0;
	}

	public virtual void CopyTo(Array array, int arrayIndex)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array", System.SR.ArgumentNull_Array);
		}
		if (array.Rank != 1)
		{
			throw new ArgumentException(System.SR.Arg_RankMultiDimNotSupported, "array");
		}
		if (arrayIndex < 0)
		{
			throw new ArgumentOutOfRangeException("arrayIndex", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (array.Length - arrayIndex < Count)
		{
			throw new ArgumentException(System.SR.Arg_ArrayPlusOffTooSmall);
		}
		for (int i = 0; i < Count; i++)
		{
			DictionaryEntry dictionaryEntry = new DictionaryEntry(keys[i], values[i]);
			array.SetValue(dictionaryEntry, i + arrayIndex);
		}
	}

	internal virtual System.Collections.KeyValuePairs[] ToKeyValuePairsArray()
	{
		System.Collections.KeyValuePairs[] array = new System.Collections.KeyValuePairs[Count];
		for (int i = 0; i < Count; i++)
		{
			array[i] = new System.Collections.KeyValuePairs(keys[i], values[i]);
		}
		return array;
	}

	private void EnsureCapacity(int min)
	{
		int num = ((keys.Length == 0) ? 16 : (keys.Length * 2));
		if ((uint)num > Array.MaxLength)
		{
			num = Array.MaxLength;
		}
		if (num < min)
		{
			num = min;
		}
		Capacity = num;
	}

	public virtual object? GetByIndex(int index)
	{
		if (index < 0 || index >= Count)
		{
			throw new ArgumentOutOfRangeException("index", System.SR.ArgumentOutOfRange_Index);
		}
		return values[index];
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new SortedListEnumerator(this, 0, _size, 3);
	}

	public virtual IDictionaryEnumerator GetEnumerator()
	{
		return new SortedListEnumerator(this, 0, _size, 3);
	}

	public virtual object GetKey(int index)
	{
		if (index < 0 || index >= Count)
		{
			throw new ArgumentOutOfRangeException("index", System.SR.ArgumentOutOfRange_Index);
		}
		return keys[index];
	}

	public virtual IList GetKeyList()
	{
		if (keyList == null)
		{
			keyList = new KeyList(this);
		}
		return keyList;
	}

	public virtual IList GetValueList()
	{
		if (valueList == null)
		{
			valueList = new ValueList(this);
		}
		return valueList;
	}

	public virtual int IndexOfKey(object key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key", System.SR.ArgumentNull_Key);
		}
		int num = Array.BinarySearch(keys, 0, _size, key, comparer);
		if (num < 0)
		{
			return -1;
		}
		return num;
	}

	public virtual int IndexOfValue(object? value)
	{
		return Array.IndexOf<object>(values, value, 0, _size);
	}

	private void Insert(int index, object key, object value)
	{
		if (_size == keys.Length)
		{
			EnsureCapacity(_size + 1);
		}
		if (index < _size)
		{
			Array.Copy(keys, index, keys, index + 1, _size - index);
			Array.Copy(values, index, values, index + 1, _size - index);
		}
		keys[index] = key;
		values[index] = value;
		_size++;
		version++;
	}

	public virtual void RemoveAt(int index)
	{
		if (index < 0 || index >= Count)
		{
			throw new ArgumentOutOfRangeException("index", System.SR.ArgumentOutOfRange_Index);
		}
		_size--;
		if (index < _size)
		{
			Array.Copy(keys, index + 1, keys, index, _size - index);
			Array.Copy(values, index + 1, values, index, _size - index);
		}
		keys[_size] = null;
		values[_size] = null;
		version++;
	}

	public virtual void Remove(object key)
	{
		int num = IndexOfKey(key);
		if (num >= 0)
		{
			RemoveAt(num);
		}
	}

	public virtual void SetByIndex(int index, object? value)
	{
		if (index < 0 || index >= Count)
		{
			throw new ArgumentOutOfRangeException("index", System.SR.ArgumentOutOfRange_Index);
		}
		values[index] = value;
		version++;
	}

	public static SortedList Synchronized(SortedList list)
	{
		if (list == null)
		{
			throw new ArgumentNullException("list");
		}
		return new SyncSortedList(list);
	}

	public virtual void TrimToSize()
	{
		Capacity = _size;
	}
}
