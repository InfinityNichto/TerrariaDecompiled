using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

[Serializable]
[DebuggerTypeProxy(typeof(System.Collections.Generic.IDictionaryDebugView<, >))]
[DebuggerDisplay("Count = {Count}")]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class SortedList<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary, ICollection, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>> where TKey : notnull
{
	private struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDisposable, IEnumerator, IDictionaryEnumerator
	{
		private readonly SortedList<TKey, TValue> _sortedList;

		private TKey _key;

		private TValue _value;

		private int _index;

		private readonly int _version;

		private readonly int _getEnumeratorRetType;

		object IDictionaryEnumerator.Key
		{
			get
			{
				if (_index == 0 || _index == _sortedList.Count + 1)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumOpCantHappen);
				}
				return _key;
			}
		}

		DictionaryEntry IDictionaryEnumerator.Entry
		{
			get
			{
				if (_index == 0 || _index == _sortedList.Count + 1)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumOpCantHappen);
				}
				return new DictionaryEntry(_key, _value);
			}
		}

		public KeyValuePair<TKey, TValue> Current => new KeyValuePair<TKey, TValue>(_key, _value);

		object IEnumerator.Current
		{
			get
			{
				if (_index == 0 || _index == _sortedList.Count + 1)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumOpCantHappen);
				}
				if (_getEnumeratorRetType == 2)
				{
					return new DictionaryEntry(_key, _value);
				}
				return new KeyValuePair<TKey, TValue>(_key, _value);
			}
		}

		object IDictionaryEnumerator.Value
		{
			get
			{
				if (_index == 0 || _index == _sortedList.Count + 1)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumOpCantHappen);
				}
				return _value;
			}
		}

		internal Enumerator(SortedList<TKey, TValue> sortedList, int getEnumeratorRetType)
		{
			_sortedList = sortedList;
			_index = 0;
			_version = _sortedList.version;
			_getEnumeratorRetType = getEnumeratorRetType;
			_key = default(TKey);
			_value = default(TValue);
		}

		public void Dispose()
		{
			_index = 0;
			_key = default(TKey);
			_value = default(TValue);
		}

		public bool MoveNext()
		{
			if (_version != _sortedList.version)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
			}
			if ((uint)_index < (uint)_sortedList.Count)
			{
				_key = _sortedList.keys[_index];
				_value = _sortedList.values[_index];
				_index++;
				return true;
			}
			_index = _sortedList.Count + 1;
			_key = default(TKey);
			_value = default(TValue);
			return false;
		}

		void IEnumerator.Reset()
		{
			if (_version != _sortedList.version)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
			}
			_index = 0;
			_key = default(TKey);
			_value = default(TValue);
		}
	}

	private sealed class SortedListKeyEnumerator : IEnumerator<TKey>, IDisposable, IEnumerator
	{
		private readonly SortedList<TKey, TValue> _sortedList;

		private int _index;

		private readonly int _version;

		private TKey _currentKey;

		public TKey Current => _currentKey;

		object IEnumerator.Current
		{
			get
			{
				if (_index == 0 || _index == _sortedList.Count + 1)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumOpCantHappen);
				}
				return _currentKey;
			}
		}

		internal SortedListKeyEnumerator(SortedList<TKey, TValue> sortedList)
		{
			_sortedList = sortedList;
			_version = sortedList.version;
		}

		public void Dispose()
		{
			_index = 0;
			_currentKey = default(TKey);
		}

		public bool MoveNext()
		{
			if (_version != _sortedList.version)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
			}
			if ((uint)_index < (uint)_sortedList.Count)
			{
				_currentKey = _sortedList.keys[_index];
				_index++;
				return true;
			}
			_index = _sortedList.Count + 1;
			_currentKey = default(TKey);
			return false;
		}

		void IEnumerator.Reset()
		{
			if (_version != _sortedList.version)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
			}
			_index = 0;
			_currentKey = default(TKey);
		}
	}

	private sealed class SortedListValueEnumerator : IEnumerator<TValue>, IDisposable, IEnumerator
	{
		private readonly SortedList<TKey, TValue> _sortedList;

		private int _index;

		private readonly int _version;

		private TValue _currentValue;

		public TValue Current => _currentValue;

		object IEnumerator.Current
		{
			get
			{
				if (_index == 0 || _index == _sortedList.Count + 1)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumOpCantHappen);
				}
				return _currentValue;
			}
		}

		internal SortedListValueEnumerator(SortedList<TKey, TValue> sortedList)
		{
			_sortedList = sortedList;
			_version = sortedList.version;
		}

		public void Dispose()
		{
			_index = 0;
			_currentValue = default(TValue);
		}

		public bool MoveNext()
		{
			if (_version != _sortedList.version)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
			}
			if ((uint)_index < (uint)_sortedList.Count)
			{
				_currentValue = _sortedList.values[_index];
				_index++;
				return true;
			}
			_index = _sortedList.Count + 1;
			_currentValue = default(TValue);
			return false;
		}

		void IEnumerator.Reset()
		{
			if (_version != _sortedList.version)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
			}
			_index = 0;
			_currentValue = default(TValue);
		}
	}

	[Serializable]
	[DebuggerTypeProxy(typeof(System.Collections.Generic.DictionaryKeyCollectionDebugView<, >))]
	[DebuggerDisplay("Count = {Count}")]
	public sealed class KeyList : IList<TKey>, ICollection<TKey>, IEnumerable<TKey>, IEnumerable, ICollection
	{
		private readonly SortedList<TKey, TValue> _dict;

		public int Count => _dict._size;

		public bool IsReadOnly => true;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => ((ICollection)_dict).SyncRoot;

		public TKey this[int index]
		{
			get
			{
				return _dict.GetKey(index);
			}
			set
			{
				throw new NotSupportedException(System.SR.NotSupported_KeyCollectionSet);
			}
		}

		internal KeyList(SortedList<TKey, TValue> dictionary)
		{
			_dict = dictionary;
		}

		public void Add(TKey key)
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}

		public void Clear()
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}

		public bool Contains(TKey key)
		{
			return _dict.ContainsKey(key);
		}

		public void CopyTo(TKey[] array, int arrayIndex)
		{
			Array.Copy(_dict.keys, 0, array, arrayIndex, _dict.Count);
		}

		void ICollection.CopyTo(Array array, int arrayIndex)
		{
			if (array != null && array.Rank != 1)
			{
				throw new ArgumentException(System.SR.Arg_RankMultiDimNotSupported, "array");
			}
			try
			{
				Array.Copy(_dict.keys, 0, array, arrayIndex, _dict.Count);
			}
			catch (ArrayTypeMismatchException)
			{
				throw new ArgumentException(System.SR.Argument_InvalidArrayType, "array");
			}
		}

		public void Insert(int index, TKey value)
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}

		public IEnumerator<TKey> GetEnumerator()
		{
			return new SortedListKeyEnumerator(_dict);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new SortedListKeyEnumerator(_dict);
		}

		public int IndexOf(TKey key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			int num = Array.BinarySearch(_dict.keys, 0, _dict.Count, key, _dict.comparer);
			if (num >= 0)
			{
				return num;
			}
			return -1;
		}

		public bool Remove(TKey key)
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}

		public void RemoveAt(int index)
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}
	}

	[Serializable]
	[DebuggerTypeProxy(typeof(System.Collections.Generic.DictionaryValueCollectionDebugView<, >))]
	[DebuggerDisplay("Count = {Count}")]
	public sealed class ValueList : IList<TValue>, ICollection<TValue>, IEnumerable<TValue>, IEnumerable, ICollection
	{
		private readonly SortedList<TKey, TValue> _dict;

		public int Count => _dict._size;

		public bool IsReadOnly => true;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => ((ICollection)_dict).SyncRoot;

		public TValue this[int index]
		{
			get
			{
				return _dict.GetByIndex(index);
			}
			set
			{
				throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
			}
		}

		internal ValueList(SortedList<TKey, TValue> dictionary)
		{
			_dict = dictionary;
		}

		public void Add(TValue key)
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}

		public void Clear()
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}

		public bool Contains(TValue value)
		{
			return _dict.ContainsValue(value);
		}

		public void CopyTo(TValue[] array, int arrayIndex)
		{
			Array.Copy(_dict.values, 0, array, arrayIndex, _dict.Count);
		}

		void ICollection.CopyTo(Array array, int index)
		{
			if (array != null && array.Rank != 1)
			{
				throw new ArgumentException(System.SR.Arg_RankMultiDimNotSupported, "array");
			}
			try
			{
				Array.Copy(_dict.values, 0, array, index, _dict.Count);
			}
			catch (ArrayTypeMismatchException)
			{
				throw new ArgumentException(System.SR.Argument_InvalidArrayType, "array");
			}
		}

		public void Insert(int index, TValue value)
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}

		public IEnumerator<TValue> GetEnumerator()
		{
			return new SortedListValueEnumerator(_dict);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new SortedListValueEnumerator(_dict);
		}

		public int IndexOf(TValue value)
		{
			return Array.IndexOf(_dict.values, value, 0, _dict.Count);
		}

		public bool Remove(TValue value)
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}

		public void RemoveAt(int index)
		{
			throw new NotSupportedException(System.SR.NotSupported_SortedListNestedWrite);
		}
	}

	private TKey[] keys;

	private TValue[] values;

	private int _size;

	private int version;

	private readonly IComparer<TKey> comparer;

	private KeyList keyList;

	private ValueList valueList;

	public int Capacity
	{
		get
		{
			return keys.Length;
		}
		set
		{
			if (value == keys.Length)
			{
				return;
			}
			if (value < _size)
			{
				throw new ArgumentOutOfRangeException("value", value, System.SR.ArgumentOutOfRange_SmallCapacity);
			}
			if (value > 0)
			{
				TKey[] destinationArray = new TKey[value];
				TValue[] destinationArray2 = new TValue[value];
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
				keys = Array.Empty<TKey>();
				values = Array.Empty<TValue>();
			}
		}
	}

	public IComparer<TKey> Comparer => comparer;

	public int Count => _size;

	public IList<TKey> Keys => GetKeyListHelper();

	ICollection<TKey> IDictionary<TKey, TValue>.Keys => GetKeyListHelper();

	ICollection IDictionary.Keys => GetKeyListHelper();

	IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => GetKeyListHelper();

	public IList<TValue> Values => GetValueListHelper();

	ICollection<TValue> IDictionary<TKey, TValue>.Values => GetValueListHelper();

	ICollection IDictionary.Values => GetValueListHelper();

	IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => GetValueListHelper();

	bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

	bool IDictionary.IsReadOnly => false;

	bool IDictionary.IsFixedSize => false;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	public TValue this[TKey key]
	{
		get
		{
			int num = IndexOfKey(key);
			if (num >= 0)
			{
				return values[num];
			}
			throw new KeyNotFoundException(System.SR.Format(System.SR.Arg_KeyNotFoundWithKey, key.ToString()));
		}
		set
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
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

	object? IDictionary.this[object key]
	{
		get
		{
			if (IsCompatibleKey(key))
			{
				int num = IndexOfKey((TKey)key);
				if (num >= 0)
				{
					return values[num];
				}
			}
			return null;
		}
		set
		{
			if (!IsCompatibleKey(key))
			{
				throw new ArgumentNullException("key");
			}
			if (value == null && default(TValue) != null)
			{
				throw new ArgumentNullException("value");
			}
			TKey key2 = (TKey)key;
			try
			{
				this[key2] = (TValue)value;
			}
			catch (InvalidCastException)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Arg_WrongType, value, typeof(TValue)), "value");
			}
		}
	}

	public SortedList()
	{
		keys = Array.Empty<TKey>();
		values = Array.Empty<TValue>();
		_size = 0;
		comparer = Comparer<TKey>.Default;
	}

	public SortedList(int capacity)
	{
		if (capacity < 0)
		{
			throw new ArgumentOutOfRangeException("capacity", capacity, System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		keys = new TKey[capacity];
		values = new TValue[capacity];
		comparer = Comparer<TKey>.Default;
	}

	public SortedList(IComparer<TKey>? comparer)
		: this()
	{
		if (comparer != null)
		{
			this.comparer = comparer;
		}
	}

	public SortedList(int capacity, IComparer<TKey>? comparer)
		: this(comparer)
	{
		Capacity = capacity;
	}

	public SortedList(IDictionary<TKey, TValue> dictionary)
		: this(dictionary, (IComparer<TKey>?)null)
	{
	}

	public SortedList(IDictionary<TKey, TValue> dictionary, IComparer<TKey>? comparer)
		: this(dictionary?.Count ?? 0, comparer)
	{
		if (dictionary == null)
		{
			throw new ArgumentNullException("dictionary");
		}
		int count = dictionary.Count;
		if (count != 0)
		{
			TKey[] array = keys;
			dictionary.Keys.CopyTo(array, 0);
			dictionary.Values.CopyTo(values, 0);
			if (count > 1)
			{
				comparer = Comparer;
				Array.Sort(array, values, comparer);
				for (int i = 1; i != array.Length; i++)
				{
					if (comparer.Compare(array[i - 1], array[i]) == 0)
					{
						throw new ArgumentException(System.SR.Format(System.SR.Argument_AddingDuplicate, array[i]));
					}
				}
			}
		}
		_size = count;
	}

	public void Add(TKey key, TValue value)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		int num = Array.BinarySearch(keys, 0, _size, key, comparer);
		if (num >= 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_AddingDuplicate, key), "key");
		}
		Insert(~num, key, value);
	}

	void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
	{
		Add(keyValuePair.Key, keyValuePair.Value);
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
	{
		int num = IndexOfKey(keyValuePair.Key);
		if (num >= 0 && EqualityComparer<TValue>.Default.Equals(values[num], keyValuePair.Value))
		{
			return true;
		}
		return false;
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
	{
		int num = IndexOfKey(keyValuePair.Key);
		if (num >= 0 && EqualityComparer<TValue>.Default.Equals(values[num], keyValuePair.Value))
		{
			RemoveAt(num);
			return true;
		}
		return false;
	}

	void IDictionary.Add(object key, object value)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (value == null && default(TValue) != null)
		{
			throw new ArgumentNullException("value");
		}
		if (!(key is TKey))
		{
			throw new ArgumentException(System.SR.Format(System.SR.Arg_WrongType, key, typeof(TKey)), "key");
		}
		if (!(value is TValue) && value != null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Arg_WrongType, value, typeof(TValue)), "value");
		}
		Add((TKey)key, (TValue)value);
	}

	private KeyList GetKeyListHelper()
	{
		if (keyList == null)
		{
			keyList = new KeyList(this);
		}
		return keyList;
	}

	private ValueList GetValueListHelper()
	{
		if (valueList == null)
		{
			valueList = new ValueList(this);
		}
		return valueList;
	}

	public void Clear()
	{
		version++;
		if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
		{
			Array.Clear(keys, 0, _size);
		}
		if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
		{
			Array.Clear(values, 0, _size);
		}
		_size = 0;
	}

	bool IDictionary.Contains(object key)
	{
		if (IsCompatibleKey(key))
		{
			return ContainsKey((TKey)key);
		}
		return false;
	}

	public bool ContainsKey(TKey key)
	{
		return IndexOfKey(key) >= 0;
	}

	public bool ContainsValue(TValue value)
	{
		return IndexOfValue(value) >= 0;
	}

	void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (arrayIndex < 0 || arrayIndex > array.Length)
		{
			throw new ArgumentOutOfRangeException("arrayIndex", arrayIndex, System.SR.ArgumentOutOfRange_Index);
		}
		if (array.Length - arrayIndex < Count)
		{
			throw new ArgumentException(System.SR.Arg_ArrayPlusOffTooSmall);
		}
		for (int i = 0; i < Count; i++)
		{
			KeyValuePair<TKey, TValue> keyValuePair = new KeyValuePair<TKey, TValue>(keys[i], values[i]);
			array[arrayIndex + i] = keyValuePair;
		}
	}

	void ICollection.CopyTo(Array array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (array.Rank != 1)
		{
			throw new ArgumentException(System.SR.Arg_RankMultiDimNotSupported, "array");
		}
		if (array.GetLowerBound(0) != 0)
		{
			throw new ArgumentException(System.SR.Arg_NonZeroLowerBound, "array");
		}
		if (index < 0 || index > array.Length)
		{
			throw new ArgumentOutOfRangeException("index", index, System.SR.ArgumentOutOfRange_Index);
		}
		if (array.Length - index < Count)
		{
			throw new ArgumentException(System.SR.Arg_ArrayPlusOffTooSmall);
		}
		if (array is KeyValuePair<TKey, TValue>[] array2)
		{
			for (int i = 0; i < Count; i++)
			{
				array2[i + index] = new KeyValuePair<TKey, TValue>(keys[i], values[i]);
			}
			return;
		}
		if (!(array is object[] array3))
		{
			throw new ArgumentException(System.SR.Argument_InvalidArrayType, "array");
		}
		try
		{
			for (int j = 0; j < Count; j++)
			{
				array3[j + index] = new KeyValuePair<TKey, TValue>(keys[j], values[j]);
			}
		}
		catch (ArrayTypeMismatchException)
		{
			throw new ArgumentException(System.SR.Argument_InvalidArrayType, "array");
		}
	}

	private void EnsureCapacity(int min)
	{
		int num = ((keys.Length == 0) ? 4 : (keys.Length * 2));
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

	private TValue GetByIndex(int index)
	{
		if (index < 0 || index >= _size)
		{
			throw new ArgumentOutOfRangeException("index", index, System.SR.ArgumentOutOfRange_Index);
		}
		return values[index];
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		return new Enumerator(this, 1);
	}

	IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
	{
		return new Enumerator(this, 1);
	}

	IDictionaryEnumerator IDictionary.GetEnumerator()
	{
		return new Enumerator(this, 2);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(this, 1);
	}

	private TKey GetKey(int index)
	{
		if (index < 0 || index >= _size)
		{
			throw new ArgumentOutOfRangeException("index", index, System.SR.ArgumentOutOfRange_Index);
		}
		return keys[index];
	}

	public int IndexOfKey(TKey key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		int num = Array.BinarySearch(keys, 0, _size, key, comparer);
		if (num < 0)
		{
			return -1;
		}
		return num;
	}

	public int IndexOfValue(TValue value)
	{
		return Array.IndexOf(values, value, 0, _size);
	}

	private void Insert(int index, TKey key, TValue value)
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

	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		int num = IndexOfKey(key);
		if (num >= 0)
		{
			value = values[num];
			return true;
		}
		value = default(TValue);
		return false;
	}

	public void RemoveAt(int index)
	{
		if (index < 0 || index >= _size)
		{
			throw new ArgumentOutOfRangeException("index", index, System.SR.ArgumentOutOfRange_Index);
		}
		_size--;
		if (index < _size)
		{
			Array.Copy(keys, index + 1, keys, index, _size - index);
			Array.Copy(values, index + 1, values, index, _size - index);
		}
		if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
		{
			keys[_size] = default(TKey);
		}
		if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
		{
			values[_size] = default(TValue);
		}
		version++;
	}

	public bool Remove(TKey key)
	{
		int num = IndexOfKey(key);
		if (num >= 0)
		{
			RemoveAt(num);
		}
		return num >= 0;
	}

	void IDictionary.Remove(object key)
	{
		if (IsCompatibleKey(key))
		{
			Remove((TKey)key);
		}
	}

	public void TrimExcess()
	{
		int num = (int)((double)keys.Length * 0.9);
		if (_size < num)
		{
			Capacity = _size;
		}
	}

	private static bool IsCompatibleKey(object key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		return key is TKey;
	}
}
