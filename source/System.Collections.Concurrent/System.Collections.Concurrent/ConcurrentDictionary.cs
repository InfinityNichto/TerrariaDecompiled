using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Collections.Concurrent;

[DebuggerTypeProxy(typeof(IDictionaryDebugView<, >))]
[DebuggerDisplay("Count = {Count}")]
public class ConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary, ICollection, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>> where TKey : notnull
{
	private sealed class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDisposable, IEnumerator
	{
		private readonly ConcurrentDictionary<TKey, TValue> _dictionary;

		private Node[] _buckets;

		private Node _node;

		private int _i;

		private int _state;

		public KeyValuePair<TKey, TValue> Current { get; private set; }

		object IEnumerator.Current => Current;

		public Enumerator(ConcurrentDictionary<TKey, TValue> dictionary)
		{
			_dictionary = dictionary;
			_i = -1;
		}

		public void Reset()
		{
			_buckets = null;
			_node = null;
			Current = default(KeyValuePair<TKey, TValue>);
			_i = -1;
			_state = 0;
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			switch (_state)
			{
			case 0:
				_buckets = _dictionary._tables._buckets;
				_i = -1;
				goto case 1;
			case 1:
			{
				Node[] buckets = _buckets;
				int num = ++_i;
				if ((uint)num >= (uint)buckets.Length)
				{
					break;
				}
				_node = Volatile.Read(ref buckets[num]);
				_state = 2;
				goto case 2;
			}
			case 2:
			{
				Node node = _node;
				if (node != null)
				{
					Current = new KeyValuePair<TKey, TValue>(node._key, node._value);
					_node = node._next;
					return true;
				}
				goto case 1;
			}
			}
			_state = 3;
			return false;
		}
	}

	private sealed class Node
	{
		internal readonly TKey _key;

		internal TValue _value;

		internal volatile Node _next;

		internal readonly int _hashcode;

		internal Node(TKey key, TValue value, int hashcode, Node next)
		{
			_key = key;
			_value = value;
			_next = next;
			_hashcode = hashcode;
		}
	}

	private sealed class Tables
	{
		internal readonly Node[] _buckets;

		internal readonly object[] _locks;

		internal readonly int[] _countPerLock;

		internal readonly ulong _fastModBucketsMultiplier;

		internal Tables(Node[] buckets, object[] locks, int[] countPerLock)
		{
			_buckets = buckets;
			_locks = locks;
			_countPerLock = countPerLock;
			_ = IntPtr.Size;
			_fastModBucketsMultiplier = System.Collections.HashHelpers.GetFastModMultiplier((uint)buckets.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ref Node GetBucket(int hashcode)
		{
			Node[] buckets = _buckets;
			if (IntPtr.Size == 8)
			{
				return ref buckets[System.Collections.HashHelpers.FastMod((uint)hashcode, (uint)buckets.Length, _fastModBucketsMultiplier)];
			}
			return ref buckets[(uint)hashcode % (uint)buckets.Length];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ref Node GetBucketAndLock(int hashcode, out uint lockNo)
		{
			Node[] buckets = _buckets;
			uint num = ((IntPtr.Size != 8) ? ((uint)hashcode % (uint)buckets.Length) : System.Collections.HashHelpers.FastMod((uint)hashcode, (uint)buckets.Length, _fastModBucketsMultiplier));
			lockNo = num % (uint)_locks.Length;
			return ref buckets[num];
		}
	}

	private sealed class DictionaryEnumerator : IDictionaryEnumerator, IEnumerator
	{
		private readonly IEnumerator<KeyValuePair<TKey, TValue>> _enumerator;

		public DictionaryEntry Entry => new DictionaryEntry(_enumerator.Current.Key, _enumerator.Current.Value);

		public object Key => _enumerator.Current.Key;

		public object Value => _enumerator.Current.Value;

		public object Current => Entry;

		internal DictionaryEnumerator(ConcurrentDictionary<TKey, TValue> dictionary)
		{
			_enumerator = dictionary.GetEnumerator();
		}

		public bool MoveNext()
		{
			return _enumerator.MoveNext();
		}

		public void Reset()
		{
			_enumerator.Reset();
		}
	}

	private volatile Tables _tables;

	private readonly IEqualityComparer<TKey> _comparer;

	private readonly EqualityComparer<TKey> _defaultComparer;

	private readonly bool _growLockArray;

	private int _budget;

	private static readonly bool s_isValueWriteAtomic = IsValueWriteAtomic();

	public TValue this[TKey key]
	{
		get
		{
			if (!TryGetValue(key, out var value))
			{
				ThrowKeyNotFoundException(key);
			}
			return value;
		}
		set
		{
			if (key == null)
			{
				System.ThrowHelper.ThrowKeyNullException();
			}
			TryAddInternal(key, null, value, updateIfExists: true, acquireLock: true, out var _);
		}
	}

	public IEqualityComparer<TKey> Comparer => _comparer ?? _defaultComparer;

	public int Count
	{
		get
		{
			int locksAcquired = 0;
			try
			{
				AcquireAllLocks(ref locksAcquired);
				return GetCountInternal();
			}
			finally
			{
				ReleaseLocks(0, locksAcquired);
			}
		}
	}

	public bool IsEmpty
	{
		get
		{
			if (!AreAllBucketsEmpty())
			{
				return false;
			}
			int locksAcquired = 0;
			try
			{
				AcquireAllLocks(ref locksAcquired);
				return AreAllBucketsEmpty();
			}
			finally
			{
				ReleaseLocks(0, locksAcquired);
			}
		}
	}

	public ICollection<TKey> Keys => GetKeys();

	IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => GetKeys();

	public ICollection<TValue> Values => GetValues();

	IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => GetValues();

	bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

	bool IDictionary.IsFixedSize => false;

	bool IDictionary.IsReadOnly => false;

	ICollection IDictionary.Keys => GetKeys();

	ICollection IDictionary.Values => GetValues();

	object? IDictionary.this[object key]
	{
		get
		{
			if (key == null)
			{
				System.ThrowHelper.ThrowKeyNullException();
			}
			if (key is TKey key2 && TryGetValue(key2, out var value))
			{
				return value;
			}
			return null;
		}
		set
		{
			if (key == null)
			{
				System.ThrowHelper.ThrowKeyNullException();
			}
			if (!(key is TKey))
			{
				throw new ArgumentException(System.SR.ConcurrentDictionary_TypeOfKeyIncorrect);
			}
			ThrowIfInvalidObjectValue(value);
			this[(TKey)key] = (TValue)value;
		}
	}

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot
	{
		get
		{
			throw new NotSupportedException(System.SR.ConcurrentCollection_SyncRoot_NotSupported);
		}
	}

	private static int DefaultConcurrencyLevel => Environment.ProcessorCount;

	private static bool IsValueWriteAtomic()
	{
		if (!typeof(TValue).IsValueType || typeof(TValue) == typeof(IntPtr) || typeof(TValue) == typeof(UIntPtr))
		{
			return true;
		}
		switch (Type.GetTypeCode(typeof(TValue)))
		{
		case TypeCode.Boolean:
		case TypeCode.Char:
		case TypeCode.SByte:
		case TypeCode.Byte:
		case TypeCode.Int16:
		case TypeCode.UInt16:
		case TypeCode.Int32:
		case TypeCode.UInt32:
		case TypeCode.Single:
			return true;
		case TypeCode.Int64:
		case TypeCode.UInt64:
		case TypeCode.Double:
			return IntPtr.Size == 8;
		default:
			return false;
		}
	}

	public ConcurrentDictionary()
		: this(DefaultConcurrencyLevel, 31, growLockArray: true, (IEqualityComparer<TKey>)null)
	{
	}

	public ConcurrentDictionary(int concurrencyLevel, int capacity)
		: this(concurrencyLevel, capacity, growLockArray: false, (IEqualityComparer<TKey>)null)
	{
	}

	public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
		: this(collection, (IEqualityComparer<TKey>?)null)
	{
	}

	public ConcurrentDictionary(IEqualityComparer<TKey>? comparer)
		: this(DefaultConcurrencyLevel, 31, growLockArray: true, comparer)
	{
	}

	public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer)
		: this(comparer)
	{
		if (collection == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("collection");
		}
		InitializeFromCollection(collection);
	}

	public ConcurrentDictionary(int concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer)
		: this(concurrencyLevel, 31, growLockArray: false, comparer)
	{
		if (collection == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("collection");
		}
		InitializeFromCollection(collection);
	}

	private void InitializeFromCollection(IEnumerable<KeyValuePair<TKey, TValue>> collection)
	{
		foreach (KeyValuePair<TKey, TValue> item in collection)
		{
			if (item.Key == null)
			{
				System.ThrowHelper.ThrowKeyNullException();
			}
			if (!TryAddInternal(item.Key, null, item.Value, updateIfExists: false, acquireLock: false, out var _))
			{
				throw new ArgumentException(System.SR.ConcurrentDictionary_SourceContainsDuplicateKeys);
			}
		}
		if (_budget == 0)
		{
			Tables tables = _tables;
			_budget = tables._buckets.Length / tables._locks.Length;
		}
	}

	public ConcurrentDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey>? comparer)
		: this(concurrencyLevel, capacity, growLockArray: false, comparer)
	{
	}

	internal ConcurrentDictionary(int concurrencyLevel, int capacity, bool growLockArray, IEqualityComparer<TKey> comparer)
	{
		if (concurrencyLevel < 1)
		{
			throw new ArgumentOutOfRangeException("concurrencyLevel", System.SR.ConcurrentDictionary_ConcurrencyLevelMustBePositive);
		}
		if (capacity < 0)
		{
			throw new ArgumentOutOfRangeException("capacity", System.SR.ConcurrentDictionary_CapacityMustNotBeNegative);
		}
		if (capacity < concurrencyLevel)
		{
			capacity = concurrencyLevel;
		}
		object[] array = new object[concurrencyLevel];
		array[0] = array;
		for (int i = 1; i < array.Length; i++)
		{
			array[i] = new object();
		}
		int[] countPerLock = new int[array.Length];
		Node[] array2 = new Node[capacity];
		_tables = new Tables(array2, array, countPerLock);
		_defaultComparer = EqualityComparer<TKey>.Default;
		if (comparer != null && comparer != _defaultComparer && comparer != StringComparer.Ordinal)
		{
			_comparer = comparer;
		}
		_growLockArray = growLockArray;
		_budget = array2.Length / array.Length;
	}

	public bool TryAdd(TKey key, TValue value)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		TValue resultingValue;
		return TryAddInternal(key, null, value, updateIfExists: false, acquireLock: true, out resultingValue);
	}

	public bool ContainsKey(TKey key)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		TValue value;
		return TryGetValue(key, out value);
	}

	public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		return TryRemoveInternal(key, out value, matchValue: false, default(TValue));
	}

	public bool TryRemove(KeyValuePair<TKey, TValue> item)
	{
		if (item.Key == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("item", System.SR.ConcurrentDictionary_ItemKeyIsNull);
		}
		TValue value;
		return TryRemoveInternal(item.Key, out value, matchValue: true, item.Value);
	}

	private bool TryRemoveInternal(TKey key, [MaybeNullWhen(false)] out TValue value, bool matchValue, TValue oldValue)
	{
		IEqualityComparer<TKey> comparer = _comparer;
		int num = comparer?.GetHashCode(key) ?? key.GetHashCode();
		while (true)
		{
			Tables tables = _tables;
			object[] locks = tables._locks;
			uint lockNo;
			ref Node bucketAndLock = ref tables.GetBucketAndLock(num, out lockNo);
			lock (locks[lockNo])
			{
				if (tables != _tables)
				{
					continue;
				}
				Node node = null;
				for (Node node2 = bucketAndLock; node2 != null; node2 = node2._next)
				{
					if (num == node2._hashcode && (comparer?.Equals(node2._key, key) ?? _defaultComparer.Equals(node2._key, key)))
					{
						if (matchValue && !EqualityComparer<TValue>.Default.Equals(oldValue, node2._value))
						{
							value = default(TValue);
							return false;
						}
						if (node == null)
						{
							Volatile.Write(ref bucketAndLock, node2._next);
						}
						else
						{
							node._next = node2._next;
						}
						value = node2._value;
						tables._countPerLock[lockNo]--;
						return true;
					}
					node = node2;
				}
				break;
			}
		}
		value = default(TValue);
		return false;
	}

	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		Tables tables = _tables;
		IEqualityComparer<TKey> comparer = _comparer;
		if (comparer == null)
		{
			int hashCode = key.GetHashCode();
			if (typeof(TKey).IsValueType)
			{
				for (Node node = Volatile.Read(ref tables.GetBucket(hashCode)); node != null; node = node._next)
				{
					if (hashCode == node._hashcode && EqualityComparer<TKey>.Default.Equals(node._key, key))
					{
						value = node._value;
						return true;
					}
				}
			}
			else
			{
				for (Node node2 = Volatile.Read(ref tables.GetBucket(hashCode)); node2 != null; node2 = node2._next)
				{
					if (hashCode == node2._hashcode && _defaultComparer.Equals(node2._key, key))
					{
						value = node2._value;
						return true;
					}
				}
			}
		}
		else
		{
			int hashCode2 = comparer.GetHashCode(key);
			for (Node node3 = Volatile.Read(ref tables.GetBucket(hashCode2)); node3 != null; node3 = node3._next)
			{
				if (hashCode2 == node3._hashcode && comparer.Equals(node3._key, key))
				{
					value = node3._value;
					return true;
				}
			}
		}
		value = default(TValue);
		return false;
	}

	private bool TryGetValueInternal(TKey key, int hashcode, [MaybeNullWhen(false)] out TValue value)
	{
		Tables tables = _tables;
		IEqualityComparer<TKey> comparer = _comparer;
		if (comparer == null)
		{
			if (typeof(TKey).IsValueType)
			{
				for (Node node = Volatile.Read(ref tables.GetBucket(hashcode)); node != null; node = node._next)
				{
					if (hashcode == node._hashcode && EqualityComparer<TKey>.Default.Equals(node._key, key))
					{
						value = node._value;
						return true;
					}
				}
			}
			else
			{
				for (Node node2 = Volatile.Read(ref tables.GetBucket(hashcode)); node2 != null; node2 = node2._next)
				{
					if (hashcode == node2._hashcode && _defaultComparer.Equals(node2._key, key))
					{
						value = node2._value;
						return true;
					}
				}
			}
		}
		else
		{
			for (Node node3 = Volatile.Read(ref tables.GetBucket(hashcode)); node3 != null; node3 = node3._next)
			{
				if (hashcode == node3._hashcode && comparer.Equals(node3._key, key))
				{
					value = node3._value;
					return true;
				}
			}
		}
		value = default(TValue);
		return false;
	}

	public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		return TryUpdateInternal(key, null, newValue, comparisonValue);
	}

	private bool TryUpdateInternal(TKey key, int? nullableHashcode, TValue newValue, TValue comparisonValue)
	{
		IEqualityComparer<TKey> comparer = _comparer;
		int num = nullableHashcode ?? comparer?.GetHashCode(key) ?? key.GetHashCode();
		EqualityComparer<TValue> @default = EqualityComparer<TValue>.Default;
		while (true)
		{
			Tables tables = _tables;
			object[] locks = tables._locks;
			uint lockNo;
			ref Node bucketAndLock = ref tables.GetBucketAndLock(num, out lockNo);
			lock (locks[lockNo])
			{
				if (tables != _tables)
				{
					continue;
				}
				Node node = null;
				for (Node node2 = bucketAndLock; node2 != null; node2 = node2._next)
				{
					if (num == node2._hashcode && (comparer?.Equals(node2._key, key) ?? _defaultComparer.Equals(node2._key, key)))
					{
						if (@default.Equals(node2._value, comparisonValue))
						{
							if (s_isValueWriteAtomic)
							{
								node2._value = newValue;
							}
							else
							{
								Node node3 = new Node(node2._key, newValue, num, node2._next);
								if (node == null)
								{
									Volatile.Write(ref bucketAndLock, node3);
								}
								else
								{
									node._next = node3;
								}
							}
							return true;
						}
						return false;
					}
					node = node2;
				}
				return false;
			}
		}
	}

	public void Clear()
	{
		int locksAcquired = 0;
		try
		{
			AcquireAllLocks(ref locksAcquired);
			if (!AreAllBucketsEmpty())
			{
				Tables tables = _tables;
				Tables tables2 = (_tables = new Tables(new Node[31], tables._locks, new int[tables._countPerLock.Length]));
				_budget = Math.Max(1, tables2._buckets.Length / tables2._locks.Length);
			}
		}
		finally
		{
			ReleaseLocks(0, locksAcquired);
		}
	}

	void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
	{
		if (array == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("array");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", System.SR.ConcurrentDictionary_IndexIsNegative);
		}
		int locksAcquired = 0;
		try
		{
			AcquireAllLocks(ref locksAcquired);
			int num = 0;
			int[] countPerLock = _tables._countPerLock;
			for (int i = 0; i < countPerLock.Length; i++)
			{
				if (num < 0)
				{
					break;
				}
				num += countPerLock[i];
			}
			if (array.Length - num < index || num < 0)
			{
				throw new ArgumentException(System.SR.ConcurrentDictionary_ArrayNotLargeEnough);
			}
			CopyToPairs(array, index);
		}
		finally
		{
			ReleaseLocks(0, locksAcquired);
		}
	}

	public KeyValuePair<TKey, TValue>[] ToArray()
	{
		int locksAcquired = 0;
		try
		{
			AcquireAllLocks(ref locksAcquired);
			int num = 0;
			int[] countPerLock = _tables._countPerLock;
			for (int i = 0; i < countPerLock.Length; i++)
			{
				num = checked(num + countPerLock[i]);
			}
			if (num == 0)
			{
				return Array.Empty<KeyValuePair<TKey, TValue>>();
			}
			KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[num];
			CopyToPairs(array, 0);
			return array;
		}
		finally
		{
			ReleaseLocks(0, locksAcquired);
		}
	}

	private void CopyToPairs(KeyValuePair<TKey, TValue>[] array, int index)
	{
		Node[] buckets = _tables._buckets;
		for (int i = 0; i < buckets.Length; i++)
		{
			for (Node node = buckets[i]; node != null; node = node._next)
			{
				array[index] = new KeyValuePair<TKey, TValue>(node._key, node._value);
				index++;
			}
		}
	}

	private void CopyToEntries(DictionaryEntry[] array, int index)
	{
		Node[] buckets = _tables._buckets;
		for (int i = 0; i < buckets.Length; i++)
		{
			for (Node node = buckets[i]; node != null; node = node._next)
			{
				array[index] = new DictionaryEntry(node._key, node._value);
				index++;
			}
		}
	}

	private void CopyToObjects(object[] array, int index)
	{
		Node[] buckets = _tables._buckets;
		for (int i = 0; i < buckets.Length; i++)
		{
			for (Node node = buckets[i]; node != null; node = node._next)
			{
				array[index] = new KeyValuePair<TKey, TValue>(node._key, node._value);
				index++;
			}
		}
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		return new Enumerator(this);
	}

	private bool TryAddInternal(TKey key, int? nullableHashcode, TValue value, bool updateIfExists, bool acquireLock, out TValue resultingValue)
	{
		IEqualityComparer<TKey> comparer = _comparer;
		int num = nullableHashcode ?? comparer?.GetHashCode(key) ?? key.GetHashCode();
		checked
		{
			Tables tables;
			bool flag;
			while (true)
			{
				tables = _tables;
				object[] locks = tables._locks;
				uint lockNo;
				ref Node bucketAndLock = ref tables.GetBucketAndLock(num, out lockNo);
				flag = false;
				bool lockTaken = false;
				try
				{
					if (acquireLock)
					{
						Monitor.Enter(locks[lockNo], ref lockTaken);
					}
					if (tables != _tables)
					{
						continue;
					}
					Node node = null;
					for (Node node2 = bucketAndLock; node2 != null; node2 = node2._next)
					{
						if (num == node2._hashcode && (comparer?.Equals(node2._key, key) ?? _defaultComparer.Equals(node2._key, key)))
						{
							if (updateIfExists)
							{
								if (s_isValueWriteAtomic)
								{
									node2._value = value;
								}
								else
								{
									Node node3 = new Node(node2._key, value, num, node2._next);
									if (node == null)
									{
										Volatile.Write(ref bucketAndLock, node3);
									}
									else
									{
										node._next = node3;
									}
								}
								resultingValue = value;
							}
							else
							{
								resultingValue = node2._value;
							}
							return false;
						}
						node = node2;
					}
					Node value2 = new Node(key, value, num, bucketAndLock);
					Volatile.Write(ref bucketAndLock, value2);
					tables._countPerLock[lockNo]++;
					if (tables._countPerLock[lockNo] > _budget)
					{
						flag = true;
					}
					break;
				}
				finally
				{
					if (lockTaken)
					{
						Monitor.Exit(locks[lockNo]);
					}
				}
			}
			if (flag)
			{
				GrowTable(tables);
			}
			resultingValue = value;
			return true;
		}
	}

	[DoesNotReturn]
	private static void ThrowKeyNotFoundException(TKey key)
	{
		throw new KeyNotFoundException(System.SR.Format(System.SR.Arg_KeyNotFoundWithKey, key.ToString()));
	}

	private int GetCountInternal()
	{
		int num = 0;
		int[] countPerLock = _tables._countPerLock;
		for (int i = 0; i < countPerLock.Length; i++)
		{
			num += countPerLock[i];
		}
		return num;
	}

	public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		if (valueFactory == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("valueFactory");
		}
		int num = _comparer?.GetHashCode(key) ?? key.GetHashCode();
		if (!TryGetValueInternal(key, num, out var value))
		{
			TryAddInternal(key, num, valueFactory(key), updateIfExists: false, acquireLock: true, out value);
		}
		return value;
	}

	public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		if (valueFactory == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("valueFactory");
		}
		int num = _comparer?.GetHashCode(key) ?? key.GetHashCode();
		if (!TryGetValueInternal(key, num, out var value))
		{
			TryAddInternal(key, num, valueFactory(key, factoryArgument), updateIfExists: false, acquireLock: true, out value);
		}
		return value;
	}

	public TValue GetOrAdd(TKey key, TValue value)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		int num = _comparer?.GetHashCode(key) ?? key.GetHashCode();
		if (!TryGetValueInternal(key, num, out var value2))
		{
			TryAddInternal(key, num, value, updateIfExists: false, acquireLock: true, out value2);
		}
		return value2;
	}

	public TValue AddOrUpdate<TArg>(TKey key, Func<TKey, TArg, TValue> addValueFactory, Func<TKey, TValue, TArg, TValue> updateValueFactory, TArg factoryArgument)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		if (addValueFactory == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("addValueFactory");
		}
		if (updateValueFactory == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("updateValueFactory");
		}
		int num = _comparer?.GetHashCode(key) ?? key.GetHashCode();
		TValue resultingValue;
		while (true)
		{
			if (TryGetValueInternal(key, num, out var value))
			{
				TValue val = updateValueFactory(key, value, factoryArgument);
				if (TryUpdateInternal(key, num, val, value))
				{
					return val;
				}
			}
			else if (TryAddInternal(key, num, addValueFactory(key, factoryArgument), updateIfExists: false, acquireLock: true, out resultingValue))
			{
				break;
			}
		}
		return resultingValue;
	}

	public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		if (addValueFactory == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("addValueFactory");
		}
		if (updateValueFactory == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("updateValueFactory");
		}
		int num = _comparer?.GetHashCode(key) ?? key.GetHashCode();
		TValue resultingValue;
		while (true)
		{
			if (TryGetValueInternal(key, num, out var value))
			{
				TValue val = updateValueFactory(key, value);
				if (TryUpdateInternal(key, num, val, value))
				{
					return val;
				}
			}
			else if (TryAddInternal(key, num, addValueFactory(key), updateIfExists: false, acquireLock: true, out resultingValue))
			{
				break;
			}
		}
		return resultingValue;
	}

	public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		if (updateValueFactory == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("updateValueFactory");
		}
		int num = _comparer?.GetHashCode(key) ?? key.GetHashCode();
		TValue resultingValue;
		while (true)
		{
			if (TryGetValueInternal(key, num, out var value))
			{
				TValue val = updateValueFactory(key, value);
				if (TryUpdateInternal(key, num, val, value))
				{
					return val;
				}
			}
			else if (TryAddInternal(key, num, addValue, updateIfExists: false, acquireLock: true, out resultingValue))
			{
				break;
			}
		}
		return resultingValue;
	}

	void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
	{
		if (!TryAdd(key, value))
		{
			throw new ArgumentException(System.SR.ConcurrentDictionary_KeyAlreadyExisted);
		}
	}

	bool IDictionary<TKey, TValue>.Remove(TKey key)
	{
		TValue value;
		return TryRemove(key, out value);
	}

	void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
	{
		((IDictionary<TKey, TValue>)this).Add(keyValuePair.Key, keyValuePair.Value);
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
	{
		if (!TryGetValue(keyValuePair.Key, out var value))
		{
			return false;
		}
		return EqualityComparer<TValue>.Default.Equals(value, keyValuePair.Value);
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
	{
		return TryRemove(keyValuePair);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	void IDictionary.Add(object key, object value)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		if (!(key is TKey))
		{
			throw new ArgumentException(System.SR.ConcurrentDictionary_TypeOfKeyIncorrect);
		}
		ThrowIfInvalidObjectValue(value);
		((IDictionary<TKey, TValue>)this).Add((TKey)key, (TValue)value);
	}

	bool IDictionary.Contains(object key)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		if (key is TKey key2)
		{
			return ContainsKey(key2);
		}
		return false;
	}

	IDictionaryEnumerator IDictionary.GetEnumerator()
	{
		return new DictionaryEnumerator(this);
	}

	void IDictionary.Remove(object key)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		if (key is TKey key2)
		{
			TryRemove(key2, out var _);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ThrowIfInvalidObjectValue(object value)
	{
		if (value != null)
		{
			if (!(value is TValue))
			{
				System.ThrowHelper.ThrowValueNullException();
			}
		}
		else if (default(TValue) != null)
		{
			System.ThrowHelper.ThrowValueNullException();
		}
	}

	void ICollection.CopyTo(Array array, int index)
	{
		if (array == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("array");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", System.SR.ConcurrentDictionary_IndexIsNegative);
		}
		int locksAcquired = 0;
		try
		{
			AcquireAllLocks(ref locksAcquired);
			Tables tables = _tables;
			int num = 0;
			int[] countPerLock = tables._countPerLock;
			for (int i = 0; i < countPerLock.Length; i++)
			{
				if (num < 0)
				{
					break;
				}
				num += countPerLock[i];
			}
			if (array.Length - num < index || num < 0)
			{
				throw new ArgumentException(System.SR.ConcurrentDictionary_ArrayNotLargeEnough);
			}
			if (array is KeyValuePair<TKey, TValue>[] array2)
			{
				CopyToPairs(array2, index);
				return;
			}
			if (array is DictionaryEntry[] array3)
			{
				CopyToEntries(array3, index);
				return;
			}
			if (array is object[] array4)
			{
				CopyToObjects(array4, index);
				return;
			}
			throw new ArgumentException(System.SR.ConcurrentDictionary_ArrayIncorrectType, "array");
		}
		finally
		{
			ReleaseLocks(0, locksAcquired);
		}
	}

	private bool AreAllBucketsEmpty()
	{
		int[] countPerLock = _tables._countPerLock;
		for (int i = 0; i < countPerLock.Length; i++)
		{
			if (countPerLock[i] != 0)
			{
				return false;
			}
		}
		return true;
	}

	private void GrowTable(Tables tables)
	{
		int locksAcquired = 0;
		try
		{
			AcquireLocks(0, 1, ref locksAcquired);
			if (tables != _tables)
			{
				return;
			}
			long num = 0L;
			for (int i = 0; i < tables._countPerLock.Length; i++)
			{
				num += tables._countPerLock[i];
			}
			if (num < tables._buckets.Length / 4)
			{
				_budget = 2 * _budget;
				if (_budget < 0)
				{
					_budget = int.MaxValue;
				}
				return;
			}
			int j = 0;
			bool flag = false;
			try
			{
				for (j = checked(tables._buckets.Length * 2 + 1); j % 3 == 0 || j % 5 == 0 || j % 7 == 0; j = checked(j + 2))
				{
				}
				if (j > Array.MaxLength)
				{
					flag = true;
				}
			}
			catch (OverflowException)
			{
				flag = true;
			}
			if (flag)
			{
				j = Array.MaxLength;
				_budget = int.MaxValue;
			}
			AcquireLocks(1, tables._locks.Length, ref locksAcquired);
			object[] array = tables._locks;
			if (_growLockArray && tables._locks.Length < 1024)
			{
				array = new object[tables._locks.Length * 2];
				Array.Copy(tables._locks, array, tables._locks.Length);
				for (int k = tables._locks.Length; k < array.Length; k++)
				{
					array[k] = new object();
				}
			}
			Node[] array2 = new Node[j];
			int[] array3 = new int[array.Length];
			Tables tables2 = new Tables(array2, array, array3);
			Node[] buckets = tables._buckets;
			checked
			{
				foreach (Node node in buckets)
				{
					Node node2 = node;
					while (node2 != null)
					{
						Node next = node2._next;
						uint lockNo;
						ref Node bucketAndLock = ref tables2.GetBucketAndLock(node2._hashcode, out lockNo);
						bucketAndLock = new Node(node2._key, node2._value, node2._hashcode, bucketAndLock);
						array3[lockNo]++;
						node2 = next;
					}
				}
			}
			_budget = Math.Max(1, array2.Length / array.Length);
			_tables = tables2;
		}
		finally
		{
			ReleaseLocks(0, locksAcquired);
		}
	}

	private void AcquireAllLocks(ref int locksAcquired)
	{
		if (CDSCollectionETWBCLProvider.Log.IsEnabled())
		{
			CDSCollectionETWBCLProvider.Log.ConcurrentDictionary_AcquiringAllLocks(_tables._buckets.Length);
		}
		AcquireLocks(0, 1, ref locksAcquired);
		AcquireLocks(1, _tables._locks.Length, ref locksAcquired);
	}

	private void AcquireLocks(int fromInclusive, int toExclusive, ref int locksAcquired)
	{
		object[] locks = _tables._locks;
		for (int i = fromInclusive; i < toExclusive; i++)
		{
			bool lockTaken = false;
			try
			{
				Monitor.Enter(locks[i], ref lockTaken);
			}
			finally
			{
				if (lockTaken)
				{
					locksAcquired++;
				}
			}
		}
	}

	private void ReleaseLocks(int fromInclusive, int toExclusive)
	{
		Tables tables = _tables;
		for (int i = fromInclusive; i < toExclusive; i++)
		{
			Monitor.Exit(tables._locks[i]);
		}
	}

	private ReadOnlyCollection<TKey> GetKeys()
	{
		int locksAcquired = 0;
		try
		{
			AcquireAllLocks(ref locksAcquired);
			int countInternal = GetCountInternal();
			if (countInternal < 0)
			{
				System.ThrowHelper.ThrowOutOfMemoryException();
			}
			List<TKey> list = new List<TKey>(countInternal);
			Node[] buckets = _tables._buckets;
			for (int i = 0; i < buckets.Length; i++)
			{
				for (Node node = buckets[i]; node != null; node = node._next)
				{
					list.Add(node._key);
				}
			}
			return new ReadOnlyCollection<TKey>(list);
		}
		finally
		{
			ReleaseLocks(0, locksAcquired);
		}
	}

	private ReadOnlyCollection<TValue> GetValues()
	{
		int locksAcquired = 0;
		try
		{
			AcquireAllLocks(ref locksAcquired);
			int countInternal = GetCountInternal();
			if (countInternal < 0)
			{
				System.ThrowHelper.ThrowOutOfMemoryException();
			}
			List<TValue> list = new List<TValue>(countInternal);
			Node[] buckets = _tables._buckets;
			for (int i = 0; i < buckets.Length; i++)
			{
				for (Node node = buckets[i]; node != null; node = node._next)
				{
					list.Add(node._value);
				}
			}
			return new ReadOnlyCollection<TValue>(list);
		}
		finally
		{
			ReleaseLocks(0, locksAcquired);
		}
	}
}
