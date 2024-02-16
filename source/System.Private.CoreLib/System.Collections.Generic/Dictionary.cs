using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Internal.Runtime.CompilerServices;

namespace System.Collections.Generic;

[Serializable]
[DebuggerTypeProxy(typeof(IDictionaryDebugView<, >))]
[DebuggerDisplay("Count = {Count}")]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class Dictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary, ICollection, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, ISerializable, IDeserializationCallback where TKey : notnull
{
	internal static class CollectionsMarshalHelper
	{
		public static ref TValue GetValueRefOrAddDefault(Dictionary<TKey, TValue> dictionary, TKey key, out bool exists)
		{
			if (key == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
			}
			if (dictionary._buckets == null)
			{
				dictionary.Initialize(0);
			}
			Entry[] entries = dictionary._entries;
			IEqualityComparer<TKey> comparer = dictionary._comparer;
			uint num = (uint)(comparer?.GetHashCode(key) ?? key.GetHashCode());
			uint num2 = 0u;
			ref int bucket = ref dictionary.GetBucket(num);
			int num3 = bucket - 1;
			if (comparer == null)
			{
				if (typeof(TKey).IsValueType)
				{
					while ((uint)num3 < (uint)entries.Length)
					{
						if (entries[num3].hashCode == num && EqualityComparer<TKey>.Default.Equals(entries[num3].key, key))
						{
							exists = true;
							return ref entries[num3].value;
						}
						num3 = entries[num3].next;
						num2++;
						if (num2 > (uint)entries.Length)
						{
							ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
						}
					}
				}
				else
				{
					EqualityComparer<TKey> @default = EqualityComparer<TKey>.Default;
					while ((uint)num3 < (uint)entries.Length)
					{
						if (entries[num3].hashCode == num && @default.Equals(entries[num3].key, key))
						{
							exists = true;
							return ref entries[num3].value;
						}
						num3 = entries[num3].next;
						num2++;
						if (num2 > (uint)entries.Length)
						{
							ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
						}
					}
				}
			}
			else
			{
				while ((uint)num3 < (uint)entries.Length)
				{
					if (entries[num3].hashCode == num && comparer.Equals(entries[num3].key, key))
					{
						exists = true;
						return ref entries[num3].value;
					}
					num3 = entries[num3].next;
					num2++;
					if (num2 > (uint)entries.Length)
					{
						ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
					}
				}
			}
			int num4;
			if (dictionary._freeCount > 0)
			{
				num4 = dictionary._freeList;
				dictionary._freeList = -3 - entries[dictionary._freeList].next;
				dictionary._freeCount--;
			}
			else
			{
				int count = dictionary._count;
				if (count == entries.Length)
				{
					dictionary.Resize();
					bucket = ref dictionary.GetBucket(num);
				}
				num4 = count;
				dictionary._count = count + 1;
				entries = dictionary._entries;
			}
			ref Entry reference = ref entries[num4];
			reference.hashCode = num;
			reference.next = bucket - 1;
			reference.key = key;
			reference.value = default(TValue);
			bucket = num4 + 1;
			dictionary._version++;
			if (!typeof(TKey).IsValueType && num2 > 100 && comparer is NonRandomizedStringEqualityComparer)
			{
				dictionary.Resize(entries.Length, forceNewHashCodes: true);
				exists = false;
				return ref dictionary.FindValue(key);
			}
			exists = false;
			return ref reference.value;
		}
	}

	private struct Entry
	{
		public uint hashCode;

		public int next;

		public TKey key;

		public TValue value;
	}

	public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDisposable, IEnumerator, IDictionaryEnumerator
	{
		private readonly Dictionary<TKey, TValue> _dictionary;

		private readonly int _version;

		private int _index;

		private KeyValuePair<TKey, TValue> _current;

		private readonly int _getEnumeratorRetType;

		public KeyValuePair<TKey, TValue> Current => _current;

		object? IEnumerator.Current
		{
			get
			{
				if (_index == 0 || _index == _dictionary._count + 1)
				{
					ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
				}
				if (_getEnumeratorRetType == 1)
				{
					return new DictionaryEntry(_current.Key, _current.Value);
				}
				return new KeyValuePair<TKey, TValue>(_current.Key, _current.Value);
			}
		}

		DictionaryEntry IDictionaryEnumerator.Entry
		{
			get
			{
				if (_index == 0 || _index == _dictionary._count + 1)
				{
					ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
				}
				return new DictionaryEntry(_current.Key, _current.Value);
			}
		}

		object IDictionaryEnumerator.Key
		{
			get
			{
				if (_index == 0 || _index == _dictionary._count + 1)
				{
					ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
				}
				return _current.Key;
			}
		}

		object? IDictionaryEnumerator.Value
		{
			get
			{
				if (_index == 0 || _index == _dictionary._count + 1)
				{
					ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
				}
				return _current.Value;
			}
		}

		internal Enumerator(Dictionary<TKey, TValue> dictionary, int getEnumeratorRetType)
		{
			_dictionary = dictionary;
			_version = dictionary._version;
			_index = 0;
			_getEnumeratorRetType = getEnumeratorRetType;
			_current = default(KeyValuePair<TKey, TValue>);
		}

		public bool MoveNext()
		{
			if (_version != _dictionary._version)
			{
				ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
			}
			while ((uint)_index < (uint)_dictionary._count)
			{
				ref Entry reference = ref _dictionary._entries[_index++];
				if (reference.next >= -1)
				{
					_current = new KeyValuePair<TKey, TValue>(reference.key, reference.value);
					return true;
				}
			}
			_index = _dictionary._count + 1;
			_current = default(KeyValuePair<TKey, TValue>);
			return false;
		}

		public void Dispose()
		{
		}

		void IEnumerator.Reset()
		{
			if (_version != _dictionary._version)
			{
				ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
			}
			_index = 0;
			_current = default(KeyValuePair<TKey, TValue>);
		}
	}

	[DebuggerTypeProxy(typeof(DictionaryKeyCollectionDebugView<, >))]
	[DebuggerDisplay("Count = {Count}")]
	public sealed class KeyCollection : ICollection<TKey>, IEnumerable<TKey>, IEnumerable, ICollection, IReadOnlyCollection<TKey>
	{
		public struct Enumerator : IEnumerator<TKey>, IDisposable, IEnumerator
		{
			private readonly Dictionary<TKey, TValue> _dictionary;

			private int _index;

			private readonly int _version;

			private TKey _currentKey;

			public TKey Current => _currentKey;

			object? IEnumerator.Current
			{
				get
				{
					if (_index == 0 || _index == _dictionary._count + 1)
					{
						ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
					}
					return _currentKey;
				}
			}

			internal Enumerator(Dictionary<TKey, TValue> dictionary)
			{
				_dictionary = dictionary;
				_version = dictionary._version;
				_index = 0;
				_currentKey = default(TKey);
			}

			public void Dispose()
			{
			}

			public bool MoveNext()
			{
				if (_version != _dictionary._version)
				{
					ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
				}
				while ((uint)_index < (uint)_dictionary._count)
				{
					ref Entry reference = ref _dictionary._entries[_index++];
					if (reference.next >= -1)
					{
						_currentKey = reference.key;
						return true;
					}
				}
				_index = _dictionary._count + 1;
				_currentKey = default(TKey);
				return false;
			}

			void IEnumerator.Reset()
			{
				if (_version != _dictionary._version)
				{
					ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
				}
				_index = 0;
				_currentKey = default(TKey);
			}
		}

		private readonly Dictionary<TKey, TValue> _dictionary;

		public int Count => _dictionary.Count;

		bool ICollection<TKey>.IsReadOnly => true;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

		public KeyCollection(Dictionary<TKey, TValue> dictionary)
		{
			if (dictionary == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
			}
			_dictionary = dictionary;
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(_dictionary);
		}

		public void CopyTo(TKey[] array, int index)
		{
			if (array == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
			}
			if (index < 0 || index > array.Length)
			{
				ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
			}
			if (array.Length - index < _dictionary.Count)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
			}
			int count = _dictionary._count;
			Entry[] entries = _dictionary._entries;
			for (int i = 0; i < count; i++)
			{
				if (entries[i].next >= -1)
				{
					array[index++] = entries[i].key;
				}
			}
		}

		void ICollection<TKey>.Add(TKey item)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);
		}

		void ICollection<TKey>.Clear()
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);
		}

		bool ICollection<TKey>.Contains(TKey item)
		{
			return _dictionary.ContainsKey(item);
		}

		bool ICollection<TKey>.Remove(TKey item)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);
			return false;
		}

		IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
		{
			return new Enumerator(_dictionary);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(_dictionary);
		}

		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
			}
			if (array.Rank != 1)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
			}
			if (array.GetLowerBound(0) != 0)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);
			}
			if ((uint)index > (uint)array.Length)
			{
				ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
			}
			if (array.Length - index < _dictionary.Count)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
			}
			if (array is TKey[] array2)
			{
				CopyTo(array2, index);
				return;
			}
			object[] array3 = array as object[];
			if (array3 == null)
			{
				ThrowHelper.ThrowArgumentException_Argument_InvalidArrayType();
			}
			int count = _dictionary._count;
			Entry[] entries = _dictionary._entries;
			try
			{
				for (int i = 0; i < count; i++)
				{
					if (entries[i].next >= -1)
					{
						array3[index++] = entries[i].key;
					}
				}
			}
			catch (ArrayTypeMismatchException)
			{
				ThrowHelper.ThrowArgumentException_Argument_InvalidArrayType();
			}
		}
	}

	[DebuggerTypeProxy(typeof(DictionaryValueCollectionDebugView<, >))]
	[DebuggerDisplay("Count = {Count}")]
	public sealed class ValueCollection : ICollection<TValue>, IEnumerable<TValue>, IEnumerable, ICollection, IReadOnlyCollection<TValue>
	{
		public struct Enumerator : IEnumerator<TValue>, IDisposable, IEnumerator
		{
			private readonly Dictionary<TKey, TValue> _dictionary;

			private int _index;

			private readonly int _version;

			private TValue _currentValue;

			public TValue Current => _currentValue;

			object? IEnumerator.Current
			{
				get
				{
					if (_index == 0 || _index == _dictionary._count + 1)
					{
						ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
					}
					return _currentValue;
				}
			}

			internal Enumerator(Dictionary<TKey, TValue> dictionary)
			{
				_dictionary = dictionary;
				_version = dictionary._version;
				_index = 0;
				_currentValue = default(TValue);
			}

			public void Dispose()
			{
			}

			public bool MoveNext()
			{
				if (_version != _dictionary._version)
				{
					ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
				}
				while ((uint)_index < (uint)_dictionary._count)
				{
					ref Entry reference = ref _dictionary._entries[_index++];
					if (reference.next >= -1)
					{
						_currentValue = reference.value;
						return true;
					}
				}
				_index = _dictionary._count + 1;
				_currentValue = default(TValue);
				return false;
			}

			void IEnumerator.Reset()
			{
				if (_version != _dictionary._version)
				{
					ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
				}
				_index = 0;
				_currentValue = default(TValue);
			}
		}

		private readonly Dictionary<TKey, TValue> _dictionary;

		public int Count => _dictionary.Count;

		bool ICollection<TValue>.IsReadOnly => true;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

		public ValueCollection(Dictionary<TKey, TValue> dictionary)
		{
			if (dictionary == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
			}
			_dictionary = dictionary;
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(_dictionary);
		}

		public void CopyTo(TValue[] array, int index)
		{
			if (array == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
			}
			if ((uint)index > array.Length)
			{
				ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
			}
			if (array.Length - index < _dictionary.Count)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
			}
			int count = _dictionary._count;
			Entry[] entries = _dictionary._entries;
			for (int i = 0; i < count; i++)
			{
				if (entries[i].next >= -1)
				{
					array[index++] = entries[i].value;
				}
			}
		}

		void ICollection<TValue>.Add(TValue item)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);
		}

		bool ICollection<TValue>.Remove(TValue item)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);
			return false;
		}

		void ICollection<TValue>.Clear()
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);
		}

		bool ICollection<TValue>.Contains(TValue item)
		{
			return _dictionary.ContainsValue(item);
		}

		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
		{
			return new Enumerator(_dictionary);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(_dictionary);
		}

		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
			}
			if (array.Rank != 1)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
			}
			if (array.GetLowerBound(0) != 0)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);
			}
			if ((uint)index > (uint)array.Length)
			{
				ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
			}
			if (array.Length - index < _dictionary.Count)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
			}
			if (array is TValue[] array2)
			{
				CopyTo(array2, index);
				return;
			}
			object[] array3 = array as object[];
			if (array3 == null)
			{
				ThrowHelper.ThrowArgumentException_Argument_InvalidArrayType();
			}
			int count = _dictionary._count;
			Entry[] entries = _dictionary._entries;
			try
			{
				for (int i = 0; i < count; i++)
				{
					if (entries[i].next >= -1)
					{
						array3[index++] = entries[i].value;
					}
				}
			}
			catch (ArrayTypeMismatchException)
			{
				ThrowHelper.ThrowArgumentException_Argument_InvalidArrayType();
			}
		}
	}

	private int[] _buckets;

	private Entry[] _entries;

	private ulong _fastModMultiplier;

	private int _count;

	private int _freeList;

	private int _freeCount;

	private int _version;

	private IEqualityComparer<TKey> _comparer;

	private KeyCollection _keys;

	private ValueCollection _values;

	public IEqualityComparer<TKey> Comparer
	{
		get
		{
			if (typeof(TKey) == typeof(string))
			{
				return (IEqualityComparer<TKey>)IInternalStringEqualityComparer.GetUnderlyingEqualityComparer((IEqualityComparer<string>)_comparer);
			}
			return _comparer ?? EqualityComparer<TKey>.Default;
		}
	}

	public int Count => _count - _freeCount;

	public KeyCollection Keys => _keys ?? (_keys = new KeyCollection(this));

	ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

	IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

	public ValueCollection Values => _values ?? (_values = new ValueCollection(this));

	ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

	IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

	public TValue this[TKey key]
	{
		get
		{
			ref TValue reference = ref FindValue(key);
			if (!Unsafe.IsNullRef(ref reference))
			{
				return reference;
			}
			ThrowHelper.ThrowKeyNotFoundException(key);
			return default(TValue);
		}
		set
		{
			bool flag = TryInsert(key, value, InsertionBehavior.OverwriteExisting);
		}
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	bool IDictionary.IsFixedSize => false;

	bool IDictionary.IsReadOnly => false;

	ICollection IDictionary.Keys => Keys;

	ICollection IDictionary.Values => Values;

	object? IDictionary.this[object key]
	{
		get
		{
			if (IsCompatibleKey(key))
			{
				ref TValue reference = ref FindValue((TKey)key);
				if (!Unsafe.IsNullRef(ref reference))
				{
					return reference;
				}
			}
			return null;
		}
		set
		{
			if (key == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
			}
			ThrowHelper.IfNullAndNullsAreIllegalThenThrow<TValue>(value, ExceptionArgument.value);
			try
			{
				TKey key2 = (TKey)key;
				try
				{
					this[key2] = (TValue)value;
				}
				catch (InvalidCastException)
				{
					ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(TValue));
				}
			}
			catch (InvalidCastException)
			{
				ThrowHelper.ThrowWrongKeyTypeArgumentException(key, typeof(TKey));
			}
		}
	}

	public Dictionary()
		: this(0, (IEqualityComparer<TKey>?)null)
	{
	}

	public Dictionary(int capacity)
		: this(capacity, (IEqualityComparer<TKey>?)null)
	{
	}

	public Dictionary(IEqualityComparer<TKey>? comparer)
		: this(0, comparer)
	{
	}

	public Dictionary(int capacity, IEqualityComparer<TKey>? comparer)
	{
		if (capacity < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
		}
		if (capacity > 0)
		{
			Initialize(capacity);
		}
		if (comparer != null && comparer != EqualityComparer<TKey>.Default)
		{
			_comparer = comparer;
		}
		if (typeof(TKey) == typeof(string))
		{
			IEqualityComparer<string> stringComparer = NonRandomizedStringEqualityComparer.GetStringComparer(_comparer);
			if (stringComparer != null)
			{
				_comparer = (IEqualityComparer<TKey>)stringComparer;
			}
		}
	}

	public Dictionary(IDictionary<TKey, TValue> dictionary)
		: this(dictionary, (IEqualityComparer<TKey>?)null)
	{
	}

	public Dictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer)
		: this(dictionary?.Count ?? 0, comparer)
	{
		if (dictionary == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
		}
		AddRange(dictionary);
	}

	public Dictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
		: this(collection, (IEqualityComparer<TKey>?)null)
	{
	}

	public Dictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer)
		: this((collection as ICollection<KeyValuePair<TKey, TValue>>)?.Count ?? 0, comparer)
	{
		if (collection == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
		}
		AddRange(collection);
	}

	private void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> collection)
	{
		if (collection.GetType() == typeof(Dictionary<TKey, TValue>))
		{
			Dictionary<TKey, TValue> dictionary = (Dictionary<TKey, TValue>)collection;
			if (dictionary.Count == 0)
			{
				return;
			}
			Entry[] entries = dictionary._entries;
			if (dictionary._comparer == _comparer)
			{
				CopyEntries(entries, dictionary._count);
				return;
			}
			int count = dictionary._count;
			for (int i = 0; i < count; i++)
			{
				if (entries[i].next >= -1)
				{
					Add(entries[i].key, entries[i].value);
				}
			}
			return;
		}
		foreach (KeyValuePair<TKey, TValue> item in collection)
		{
			Add(item.Key, item.Value);
		}
	}

	protected Dictionary(SerializationInfo info, StreamingContext context)
	{
		HashHelpers.SerializationInfoTable.Add(this, info);
	}

	public void Add(TKey key, TValue value)
	{
		bool flag = TryInsert(key, value, InsertionBehavior.ThrowOnExisting);
	}

	void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
	{
		Add(keyValuePair.Key, keyValuePair.Value);
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
	{
		ref TValue reference = ref FindValue(keyValuePair.Key);
		if (!Unsafe.IsNullRef(ref reference) && EqualityComparer<TValue>.Default.Equals(reference, keyValuePair.Value))
		{
			return true;
		}
		return false;
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
	{
		ref TValue reference = ref FindValue(keyValuePair.Key);
		if (!Unsafe.IsNullRef(ref reference) && EqualityComparer<TValue>.Default.Equals(reference, keyValuePair.Value))
		{
			Remove(keyValuePair.Key);
			return true;
		}
		return false;
	}

	public void Clear()
	{
		int count = _count;
		if (count > 0)
		{
			Array.Clear(_buckets);
			_count = 0;
			_freeList = -1;
			_freeCount = 0;
			Array.Clear(_entries, 0, count);
		}
	}

	public bool ContainsKey(TKey key)
	{
		return !Unsafe.IsNullRef(ref FindValue(key));
	}

	public bool ContainsValue(TValue value)
	{
		Entry[] entries = _entries;
		if (value == null)
		{
			for (int i = 0; i < _count; i++)
			{
				if (entries[i].next >= -1 && entries[i].value == null)
				{
					return true;
				}
			}
		}
		else if (typeof(TValue).IsValueType)
		{
			for (int j = 0; j < _count; j++)
			{
				if (entries[j].next >= -1 && EqualityComparer<TValue>.Default.Equals(entries[j].value, value))
				{
					return true;
				}
			}
		}
		else
		{
			EqualityComparer<TValue> @default = EqualityComparer<TValue>.Default;
			for (int k = 0; k < _count; k++)
			{
				if (entries[k].next >= -1 && @default.Equals(entries[k].value, value))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if ((uint)index > (uint)array.Length)
		{
			ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
		}
		if (array.Length - index < Count)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
		}
		int count = _count;
		Entry[] entries = _entries;
		for (int i = 0; i < count; i++)
		{
			if (entries[i].next >= -1)
			{
				array[index++] = new KeyValuePair<TKey, TValue>(entries[i].key, entries[i].value);
			}
		}
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(this, 2);
	}

	IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
	{
		return new Enumerator(this, 2);
	}

	public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.info);
		}
		info.AddValue("Version", _version);
		info.AddValue("Comparer", Comparer, typeof(IEqualityComparer<TKey>));
		info.AddValue("HashSize", (_buckets != null) ? _buckets.Length : 0);
		if (_buckets != null)
		{
			KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[Count];
			CopyTo(array, 0);
			info.AddValue("KeyValuePairs", array, typeof(KeyValuePair<TKey, TValue>[]));
		}
	}

	internal ref TValue FindValue(TKey key)
	{
		if (key == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
		}
		ref Entry reference = ref Unsafe.NullRef<Entry>();
		if (_buckets != null)
		{
			IEqualityComparer<TKey> comparer = _comparer;
			if (comparer == null)
			{
				uint hashCode = (uint)key.GetHashCode();
				int bucket = GetBucket(hashCode);
				Entry[] entries = _entries;
				uint num = 0u;
				if (typeof(TKey).IsValueType)
				{
					bucket--;
					while ((uint)bucket < (uint)entries.Length)
					{
						reference = ref entries[bucket];
						if (reference.hashCode != hashCode || !EqualityComparer<TKey>.Default.Equals(reference.key, key))
						{
							bucket = reference.next;
							num++;
							if (num <= (uint)entries.Length)
							{
								continue;
							}
							goto IL_0171;
						}
						goto IL_0176;
					}
				}
				else
				{
					EqualityComparer<TKey> @default = EqualityComparer<TKey>.Default;
					bucket--;
					while ((uint)bucket < (uint)entries.Length)
					{
						reference = ref entries[bucket];
						if (reference.hashCode != hashCode || !@default.Equals(reference.key, key))
						{
							bucket = reference.next;
							num++;
							if (num <= (uint)entries.Length)
							{
								continue;
							}
							goto IL_0171;
						}
						goto IL_0176;
					}
				}
			}
			else
			{
				uint hashCode2 = (uint)comparer.GetHashCode(key);
				int bucket2 = GetBucket(hashCode2);
				Entry[] entries2 = _entries;
				uint num2 = 0u;
				bucket2--;
				while ((uint)bucket2 < (uint)entries2.Length)
				{
					reference = ref entries2[bucket2];
					if (reference.hashCode != hashCode2 || !comparer.Equals(reference.key, key))
					{
						bucket2 = reference.next;
						num2++;
						if (num2 <= (uint)entries2.Length)
						{
							continue;
						}
						goto IL_0171;
					}
					goto IL_0176;
				}
			}
		}
		return ref Unsafe.NullRef<TValue>();
		IL_0171:
		ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
		goto IL_0176;
		IL_0176:
		return ref reference.value;
	}

	private int Initialize(int capacity)
	{
		int prime = HashHelpers.GetPrime(capacity);
		int[] buckets = new int[prime];
		Entry[] entries = new Entry[prime];
		_freeList = -1;
		_fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)prime);
		_buckets = buckets;
		_entries = entries;
		return prime;
	}

	private bool TryInsert(TKey key, TValue value, InsertionBehavior behavior)
	{
		if (key == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
		}
		if (_buckets == null)
		{
			Initialize(0);
		}
		Entry[] entries = _entries;
		IEqualityComparer<TKey> comparer = _comparer;
		uint num = (uint)(comparer?.GetHashCode(key) ?? key.GetHashCode());
		uint num2 = 0u;
		ref int bucket = ref GetBucket(num);
		int num3 = bucket - 1;
		if (comparer == null)
		{
			if (typeof(TKey).IsValueType)
			{
				while ((uint)num3 < (uint)entries.Length)
				{
					if (entries[num3].hashCode == num && EqualityComparer<TKey>.Default.Equals(entries[num3].key, key))
					{
						switch (behavior)
						{
						case InsertionBehavior.OverwriteExisting:
							entries[num3].value = value;
							return true;
						case InsertionBehavior.ThrowOnExisting:
							ThrowHelper.ThrowAddingDuplicateWithKeyArgumentException(key);
							break;
						}
						return false;
					}
					num3 = entries[num3].next;
					num2++;
					if (num2 > (uint)entries.Length)
					{
						ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
					}
				}
			}
			else
			{
				EqualityComparer<TKey> @default = EqualityComparer<TKey>.Default;
				while ((uint)num3 < (uint)entries.Length)
				{
					if (entries[num3].hashCode == num && @default.Equals(entries[num3].key, key))
					{
						switch (behavior)
						{
						case InsertionBehavior.OverwriteExisting:
							entries[num3].value = value;
							return true;
						case InsertionBehavior.ThrowOnExisting:
							ThrowHelper.ThrowAddingDuplicateWithKeyArgumentException(key);
							break;
						}
						return false;
					}
					num3 = entries[num3].next;
					num2++;
					if (num2 > (uint)entries.Length)
					{
						ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
					}
				}
			}
		}
		else
		{
			while ((uint)num3 < (uint)entries.Length)
			{
				if (entries[num3].hashCode == num && comparer.Equals(entries[num3].key, key))
				{
					switch (behavior)
					{
					case InsertionBehavior.OverwriteExisting:
						entries[num3].value = value;
						return true;
					case InsertionBehavior.ThrowOnExisting:
						ThrowHelper.ThrowAddingDuplicateWithKeyArgumentException(key);
						break;
					}
					return false;
				}
				num3 = entries[num3].next;
				num2++;
				if (num2 > (uint)entries.Length)
				{
					ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
				}
			}
		}
		int num4;
		if (_freeCount > 0)
		{
			num4 = _freeList;
			_freeList = -3 - entries[_freeList].next;
			_freeCount--;
		}
		else
		{
			int count = _count;
			if (count == entries.Length)
			{
				Resize();
				bucket = ref GetBucket(num);
			}
			num4 = count;
			_count = count + 1;
			entries = _entries;
		}
		ref Entry reference = ref entries[num4];
		reference.hashCode = num;
		reference.next = bucket - 1;
		reference.key = key;
		reference.value = value;
		bucket = num4 + 1;
		_version++;
		if (!typeof(TKey).IsValueType && num2 > 100 && comparer is NonRandomizedStringEqualityComparer)
		{
			Resize(entries.Length, forceNewHashCodes: true);
		}
		return true;
	}

	public virtual void OnDeserialization(object? sender)
	{
		HashHelpers.SerializationInfoTable.TryGetValue(this, out var value);
		if (value == null)
		{
			return;
		}
		int @int = value.GetInt32("Version");
		int int2 = value.GetInt32("HashSize");
		_comparer = (IEqualityComparer<TKey>)value.GetValue("Comparer", typeof(IEqualityComparer<TKey>));
		if (int2 != 0)
		{
			Initialize(int2);
			KeyValuePair<TKey, TValue>[] array = (KeyValuePair<TKey, TValue>[])value.GetValue("KeyValuePairs", typeof(KeyValuePair<TKey, TValue>[]));
			if (array == null)
			{
				ThrowHelper.ThrowSerializationException(ExceptionResource.Serialization_MissingKeys);
			}
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].Key == null)
				{
					ThrowHelper.ThrowSerializationException(ExceptionResource.Serialization_NullKey);
				}
				Add(array[i].Key, array[i].Value);
			}
		}
		else
		{
			_buckets = null;
		}
		_version = @int;
		HashHelpers.SerializationInfoTable.Remove(this);
	}

	private void Resize()
	{
		Resize(HashHelpers.ExpandPrime(_count), forceNewHashCodes: false);
	}

	private void Resize(int newSize, bool forceNewHashCodes)
	{
		Entry[] array = new Entry[newSize];
		int count = _count;
		Array.Copy(_entries, array, count);
		if (!typeof(TKey).IsValueType && forceNewHashCodes)
		{
			_comparer = (IEqualityComparer<TKey>)((NonRandomizedStringEqualityComparer)_comparer).GetRandomizedEqualityComparer();
			for (int i = 0; i < count; i++)
			{
				if (array[i].next >= -1)
				{
					array[i].hashCode = (uint)_comparer.GetHashCode(array[i].key);
				}
			}
			if (_comparer == EqualityComparer<TKey>.Default)
			{
				_comparer = null;
			}
		}
		_buckets = new int[newSize];
		_fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);
		for (int j = 0; j < count; j++)
		{
			if (array[j].next >= -1)
			{
				ref int bucket = ref GetBucket(array[j].hashCode);
				array[j].next = bucket - 1;
				bucket = j + 1;
			}
		}
		_entries = array;
	}

	public bool Remove(TKey key)
	{
		if (key == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
		}
		if (_buckets != null)
		{
			uint num = 0u;
			uint num2 = (uint)(_comparer?.GetHashCode(key) ?? key.GetHashCode());
			ref int bucket = ref GetBucket(num2);
			Entry[] entries = _entries;
			int num3 = -1;
			int num4 = bucket - 1;
			while (num4 >= 0)
			{
				ref Entry reference = ref entries[num4];
				if (reference.hashCode == num2 && (_comparer?.Equals(reference.key, key) ?? EqualityComparer<TKey>.Default.Equals(reference.key, key)))
				{
					if (num3 < 0)
					{
						bucket = reference.next + 1;
					}
					else
					{
						entries[num3].next = reference.next;
					}
					reference.next = -3 - _freeList;
					if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
					{
						reference.key = default(TKey);
					}
					if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
					{
						reference.value = default(TValue);
					}
					_freeList = num4;
					_freeCount++;
					return true;
				}
				num3 = num4;
				num4 = reference.next;
				num++;
				if (num > (uint)entries.Length)
				{
					ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
				}
			}
		}
		return false;
	}

	public bool Remove(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		if (key == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
		}
		if (_buckets != null)
		{
			uint num = 0u;
			uint num2 = (uint)(_comparer?.GetHashCode(key) ?? key.GetHashCode());
			ref int bucket = ref GetBucket(num2);
			Entry[] entries = _entries;
			int num3 = -1;
			int num4 = bucket - 1;
			while (num4 >= 0)
			{
				ref Entry reference = ref entries[num4];
				if (reference.hashCode == num2 && (_comparer?.Equals(reference.key, key) ?? EqualityComparer<TKey>.Default.Equals(reference.key, key)))
				{
					if (num3 < 0)
					{
						bucket = reference.next + 1;
					}
					else
					{
						entries[num3].next = reference.next;
					}
					value = reference.value;
					reference.next = -3 - _freeList;
					if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
					{
						reference.key = default(TKey);
					}
					if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
					{
						reference.value = default(TValue);
					}
					_freeList = num4;
					_freeCount++;
					return true;
				}
				num3 = num4;
				num4 = reference.next;
				num++;
				if (num > (uint)entries.Length)
				{
					ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
				}
			}
		}
		value = default(TValue);
		return false;
	}

	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		ref TValue reference = ref FindValue(key);
		if (!Unsafe.IsNullRef(ref reference))
		{
			value = reference;
			return true;
		}
		value = default(TValue);
		return false;
	}

	public bool TryAdd(TKey key, TValue value)
	{
		return TryInsert(key, value, InsertionBehavior.None);
	}

	void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
	{
		CopyTo(array, index);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (array.Rank != 1)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
		}
		if (array.GetLowerBound(0) != 0)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);
		}
		if ((uint)index > (uint)array.Length)
		{
			ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
		}
		if (array.Length - index < Count)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
		}
		if (array is KeyValuePair<TKey, TValue>[] array2)
		{
			CopyTo(array2, index);
			return;
		}
		if (array is DictionaryEntry[] array3)
		{
			Entry[] entries = _entries;
			for (int i = 0; i < _count; i++)
			{
				if (entries[i].next >= -1)
				{
					array3[index++] = new DictionaryEntry(entries[i].key, entries[i].value);
				}
			}
			return;
		}
		object[] array4 = array as object[];
		if (array4 == null)
		{
			ThrowHelper.ThrowArgumentException_Argument_InvalidArrayType();
		}
		try
		{
			int count = _count;
			Entry[] entries2 = _entries;
			for (int j = 0; j < count; j++)
			{
				if (entries2[j].next >= -1)
				{
					array4[index++] = new KeyValuePair<TKey, TValue>(entries2[j].key, entries2[j].value);
				}
			}
		}
		catch (ArrayTypeMismatchException)
		{
			ThrowHelper.ThrowArgumentException_Argument_InvalidArrayType();
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(this, 2);
	}

	public int EnsureCapacity(int capacity)
	{
		if (capacity < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
		}
		int num = ((_entries != null) ? _entries.Length : 0);
		if (num >= capacity)
		{
			return num;
		}
		_version++;
		if (_buckets == null)
		{
			return Initialize(capacity);
		}
		int prime = HashHelpers.GetPrime(capacity);
		Resize(prime, forceNewHashCodes: false);
		return prime;
	}

	public void TrimExcess()
	{
		TrimExcess(Count);
	}

	public void TrimExcess(int capacity)
	{
		if (capacity < Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
		}
		int prime = HashHelpers.GetPrime(capacity);
		Entry[] entries = _entries;
		int num = ((entries != null) ? entries.Length : 0);
		if (prime < num)
		{
			int count = _count;
			_version++;
			Initialize(prime);
			CopyEntries(entries, count);
		}
	}

	private void CopyEntries(Entry[] entries, int count)
	{
		Entry[] entries2 = _entries;
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			uint hashCode = entries[i].hashCode;
			if (entries[i].next >= -1)
			{
				ref Entry reference = ref entries2[num];
				reference = entries[i];
				ref int bucket = ref GetBucket(hashCode);
				reference.next = bucket - 1;
				bucket = num + 1;
				num++;
			}
		}
		_count = num;
		_freeCount = 0;
	}

	private static bool IsCompatibleKey(object key)
	{
		if (key == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
		}
		return key is TKey;
	}

	void IDictionary.Add(object key, object value)
	{
		if (key == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
		}
		ThrowHelper.IfNullAndNullsAreIllegalThenThrow<TValue>(value, ExceptionArgument.value);
		try
		{
			TKey key2 = (TKey)key;
			try
			{
				Add(key2, (TValue)value);
			}
			catch (InvalidCastException)
			{
				ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(TValue));
			}
		}
		catch (InvalidCastException)
		{
			ThrowHelper.ThrowWrongKeyTypeArgumentException(key, typeof(TKey));
		}
	}

	bool IDictionary.Contains(object key)
	{
		if (IsCompatibleKey(key))
		{
			return ContainsKey((TKey)key);
		}
		return false;
	}

	IDictionaryEnumerator IDictionary.GetEnumerator()
	{
		return new Enumerator(this, 1);
	}

	void IDictionary.Remove(object key)
	{
		if (IsCompatibleKey(key))
		{
			Remove((TKey)key);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ref int GetBucket(uint hashCode)
	{
		int[] buckets = _buckets;
		return ref buckets[HashHelpers.FastMod(hashCode, (uint)buckets.Length, _fastModMultiplier)];
	}
}
