using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Internal.Runtime.CompilerServices;

namespace System.Collections.Generic;

[Serializable]
[DebuggerTypeProxy(typeof(ICollectionDebugView<>))]
[DebuggerDisplay("Count = {Count}")]
[TypeForwardedFrom("System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class HashSet<T> : ICollection<T>, IEnumerable<T>, IEnumerable, ISet<T>, IReadOnlyCollection<T>, IReadOnlySet<T>, ISerializable, IDeserializationCallback
{
	private struct Entry
	{
		public int HashCode;

		public int Next;

		public T Value;
	}

	public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
	{
		private readonly HashSet<T> _hashSet;

		private readonly int _version;

		private int _index;

		private T _current;

		public T Current => _current;

		object? IEnumerator.Current
		{
			get
			{
				if (_index == 0 || _index == _hashSet._count + 1)
				{
					ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
				}
				return _current;
			}
		}

		internal Enumerator(HashSet<T> hashSet)
		{
			_hashSet = hashSet;
			_version = hashSet._version;
			_index = 0;
			_current = default(T);
		}

		public bool MoveNext()
		{
			if (_version != _hashSet._version)
			{
				ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
			}
			while ((uint)_index < (uint)_hashSet._count)
			{
				ref Entry reference = ref _hashSet._entries[_index++];
				if (reference.Next >= -1)
				{
					_current = reference.Value;
					return true;
				}
			}
			_index = _hashSet._count + 1;
			_current = default(T);
			return false;
		}

		public void Dispose()
		{
		}

		void IEnumerator.Reset()
		{
			if (_version != _hashSet._version)
			{
				ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
			}
			_index = 0;
			_current = default(T);
		}
	}

	private int[] _buckets;

	private Entry[] _entries;

	private ulong _fastModMultiplier;

	private int _count;

	private int _freeList;

	private int _freeCount;

	private int _version;

	private IEqualityComparer<T> _comparer;

	public int Count => _count - _freeCount;

	bool ICollection<T>.IsReadOnly => false;

	public IEqualityComparer<T> Comparer
	{
		get
		{
			if (typeof(T) == typeof(string))
			{
				return (IEqualityComparer<T>)IInternalStringEqualityComparer.GetUnderlyingEqualityComparer((IEqualityComparer<string>)_comparer);
			}
			return _comparer ?? EqualityComparer<T>.Default;
		}
	}

	public HashSet()
		: this((IEqualityComparer<T>?)null)
	{
	}

	public HashSet(IEqualityComparer<T>? comparer)
	{
		if (comparer != null && comparer != EqualityComparer<T>.Default)
		{
			_comparer = comparer;
		}
		if (typeof(T) == typeof(string))
		{
			IEqualityComparer<string> stringComparer = NonRandomizedStringEqualityComparer.GetStringComparer(_comparer);
			if (stringComparer != null)
			{
				_comparer = (IEqualityComparer<T>)stringComparer;
			}
		}
	}

	public HashSet(int capacity)
		: this(capacity, (IEqualityComparer<T>?)null)
	{
	}

	public HashSet(IEnumerable<T> collection)
		: this(collection, (IEqualityComparer<T>?)null)
	{
	}

	public HashSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer)
		: this(comparer)
	{
		if (collection == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
		}
		if (collection is HashSet<T> hashSet && EqualityComparersAreEqual(this, hashSet))
		{
			ConstructFrom(hashSet);
			return;
		}
		if (collection is ICollection<T> { Count: var count } && count > 0)
		{
			Initialize(count);
		}
		UnionWith(collection);
		if (_count > 0 && _entries.Length / _count > 3)
		{
			TrimExcess();
		}
	}

	public HashSet(int capacity, IEqualityComparer<T>? comparer)
		: this(comparer)
	{
		if (capacity < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
		}
		if (capacity > 0)
		{
			Initialize(capacity);
		}
	}

	protected HashSet(SerializationInfo info, StreamingContext context)
	{
		HashHelpers.SerializationInfoTable.Add(this, info);
	}

	private void ConstructFrom(HashSet<T> source)
	{
		if (source.Count == 0)
		{
			return;
		}
		int num = source._buckets.Length;
		int num2 = HashHelpers.ExpandPrime(source.Count + 1);
		if (num2 >= num)
		{
			_buckets = (int[])source._buckets.Clone();
			_entries = (Entry[])source._entries.Clone();
			_freeList = source._freeList;
			_freeCount = source._freeCount;
			_count = source._count;
			_fastModMultiplier = source._fastModMultiplier;
			return;
		}
		Initialize(source.Count);
		Entry[] entries = source._entries;
		for (int i = 0; i < source._count; i++)
		{
			ref Entry reference = ref entries[i];
			if (reference.Next >= -1)
			{
				AddIfNotPresent(reference.Value, out var _);
			}
		}
	}

	void ICollection<T>.Add(T item)
	{
		AddIfNotPresent(item, out var _);
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

	public bool Contains(T item)
	{
		return FindItemIndex(item) >= 0;
	}

	private int FindItemIndex(T item)
	{
		int[] buckets = _buckets;
		if (buckets != null)
		{
			Entry[] entries = _entries;
			uint num = 0u;
			IEqualityComparer<T> comparer = _comparer;
			if (comparer == null)
			{
				int num2 = item?.GetHashCode() ?? 0;
				if (typeof(T).IsValueType)
				{
					int num3 = GetBucketRef(num2) - 1;
					while (num3 >= 0)
					{
						ref Entry reference = ref entries[num3];
						if (reference.HashCode == num2 && EqualityComparer<T>.Default.Equals(reference.Value, item))
						{
							return num3;
						}
						num3 = reference.Next;
						num++;
						if (num > (uint)entries.Length)
						{
							ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
						}
					}
				}
				else
				{
					EqualityComparer<T> @default = EqualityComparer<T>.Default;
					int num4 = GetBucketRef(num2) - 1;
					while (num4 >= 0)
					{
						ref Entry reference2 = ref entries[num4];
						if (reference2.HashCode == num2 && @default.Equals(reference2.Value, item))
						{
							return num4;
						}
						num4 = reference2.Next;
						num++;
						if (num > (uint)entries.Length)
						{
							ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
						}
					}
				}
			}
			else
			{
				int num5 = ((item != null) ? comparer.GetHashCode(item) : 0);
				int num6 = GetBucketRef(num5) - 1;
				while (num6 >= 0)
				{
					ref Entry reference3 = ref entries[num6];
					if (reference3.HashCode == num5 && comparer.Equals(reference3.Value, item))
					{
						return num6;
					}
					num6 = reference3.Next;
					num++;
					if (num > (uint)entries.Length)
					{
						ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
					}
				}
			}
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ref int GetBucketRef(int hashCode)
	{
		int[] buckets = _buckets;
		return ref buckets[HashHelpers.FastMod((uint)hashCode, (uint)buckets.Length, _fastModMultiplier)];
	}

	public bool Remove(T item)
	{
		if (_buckets != null)
		{
			Entry[] entries = _entries;
			uint num = 0u;
			int num2 = -1;
			int num3 = ((item != null) ? (_comparer?.GetHashCode(item) ?? item.GetHashCode()) : 0);
			ref int bucketRef = ref GetBucketRef(num3);
			int num4 = bucketRef - 1;
			while (num4 >= 0)
			{
				ref Entry reference = ref entries[num4];
				if (reference.HashCode == num3 && (_comparer?.Equals(reference.Value, item) ?? EqualityComparer<T>.Default.Equals(reference.Value, item)))
				{
					if (num2 < 0)
					{
						bucketRef = reference.Next + 1;
					}
					else
					{
						entries[num2].Next = reference.Next;
					}
					reference.Next = -3 - _freeList;
					if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
					{
						reference.Value = default(T);
					}
					_freeList = num4;
					_freeCount++;
					return true;
				}
				num2 = num4;
				num4 = reference.Next;
				num++;
				if (num > (uint)entries.Length)
				{
					ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
				}
			}
		}
		return false;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.info);
		}
		info.AddValue("Version", _version);
		info.AddValue("Comparer", Comparer, typeof(IEqualityComparer<T>));
		info.AddValue("Capacity", (_buckets != null) ? _buckets.Length : 0);
		if (_buckets != null)
		{
			T[] array = new T[Count];
			CopyTo(array);
			info.AddValue("Elements", array, typeof(T[]));
		}
	}

	public virtual void OnDeserialization(object? sender)
	{
		HashHelpers.SerializationInfoTable.TryGetValue(this, out var value);
		if (value == null)
		{
			return;
		}
		int @int = value.GetInt32("Capacity");
		_comparer = (IEqualityComparer<T>)value.GetValue("Comparer", typeof(IEqualityComparer<T>));
		_freeList = -1;
		_freeCount = 0;
		if (@int != 0)
		{
			_buckets = new int[@int];
			_entries = new Entry[@int];
			_fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)@int);
			T[] array = (T[])value.GetValue("Elements", typeof(T[]));
			if (array == null)
			{
				ThrowHelper.ThrowSerializationException(ExceptionResource.Serialization_MissingKeys);
			}
			for (int i = 0; i < array.Length; i++)
			{
				AddIfNotPresent(array[i], out var _);
			}
		}
		else
		{
			_buckets = null;
		}
		_version = value.GetInt32("Version");
		HashHelpers.SerializationInfoTable.Remove(this);
	}

	public bool Add(T item)
	{
		int location;
		return AddIfNotPresent(item, out location);
	}

	public bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue)
	{
		if (_buckets != null)
		{
			int num = FindItemIndex(equalValue);
			if (num >= 0)
			{
				actualValue = _entries[num].Value;
				return true;
			}
		}
		actualValue = default(T);
		return false;
	}

	public void UnionWith(IEnumerable<T> other)
	{
		if (other == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
		}
		foreach (T item in other)
		{
			AddIfNotPresent(item, out var _);
		}
	}

	public void IntersectWith(IEnumerable<T> other)
	{
		if (other == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
		}
		if (Count == 0 || other == this)
		{
			return;
		}
		if (other is ICollection<T> collection)
		{
			if (collection.Count == 0)
			{
				Clear();
				return;
			}
			if (other is HashSet<T> hashSet && EqualityComparersAreEqual(this, hashSet))
			{
				IntersectWithHashSetWithSameComparer(hashSet);
				return;
			}
		}
		IntersectWithEnumerable(other);
	}

	public void ExceptWith(IEnumerable<T> other)
	{
		if (other == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
		}
		if (Count == 0)
		{
			return;
		}
		if (other == this)
		{
			Clear();
			return;
		}
		foreach (T item in other)
		{
			Remove(item);
		}
	}

	public void SymmetricExceptWith(IEnumerable<T> other)
	{
		if (other == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
		}
		if (Count == 0)
		{
			UnionWith(other);
		}
		else if (other == this)
		{
			Clear();
		}
		else if (other is HashSet<T> hashSet && EqualityComparersAreEqual(this, hashSet))
		{
			SymmetricExceptWithUniqueHashSet(hashSet);
		}
		else
		{
			SymmetricExceptWithEnumerable(other);
		}
	}

	public bool IsSubsetOf(IEnumerable<T> other)
	{
		if (other == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
		}
		if (Count == 0 || other == this)
		{
			return true;
		}
		if (other is HashSet<T> hashSet && EqualityComparersAreEqual(this, hashSet))
		{
			if (Count > hashSet.Count)
			{
				return false;
			}
			return IsSubsetOfHashSetWithSameComparer(hashSet);
		}
		var (num, num2) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: false);
		if (num == Count)
		{
			return num2 >= 0;
		}
		return false;
	}

	public bool IsProperSubsetOf(IEnumerable<T> other)
	{
		if (other == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
		}
		if (other == this)
		{
			return false;
		}
		if (other is ICollection<T> collection)
		{
			if (collection.Count == 0)
			{
				return false;
			}
			if (Count == 0)
			{
				return collection.Count > 0;
			}
			if (other is HashSet<T> hashSet && EqualityComparersAreEqual(this, hashSet))
			{
				if (Count >= hashSet.Count)
				{
					return false;
				}
				return IsSubsetOfHashSetWithSameComparer(hashSet);
			}
		}
		var (num, num2) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: false);
		if (num == Count)
		{
			return num2 > 0;
		}
		return false;
	}

	public bool IsSupersetOf(IEnumerable<T> other)
	{
		if (other == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
		}
		if (other == this)
		{
			return true;
		}
		if (other is ICollection<T> collection)
		{
			if (collection.Count == 0)
			{
				return true;
			}
			if (other is HashSet<T> hashSet && EqualityComparersAreEqual(this, hashSet) && hashSet.Count > Count)
			{
				return false;
			}
		}
		return ContainsAllElements(other);
	}

	public bool IsProperSupersetOf(IEnumerable<T> other)
	{
		if (other == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
		}
		if (Count == 0 || other == this)
		{
			return false;
		}
		if (other is ICollection<T> collection)
		{
			if (collection.Count == 0)
			{
				return true;
			}
			if (other is HashSet<T> hashSet && EqualityComparersAreEqual(this, hashSet))
			{
				if (hashSet.Count >= Count)
				{
					return false;
				}
				return ContainsAllElements(hashSet);
			}
		}
		var (num, num2) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: true);
		if (num < Count)
		{
			return num2 == 0;
		}
		return false;
	}

	public bool Overlaps(IEnumerable<T> other)
	{
		if (other == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
		}
		if (Count == 0)
		{
			return false;
		}
		if (other == this)
		{
			return true;
		}
		foreach (T item in other)
		{
			if (Contains(item))
			{
				return true;
			}
		}
		return false;
	}

	public bool SetEquals(IEnumerable<T> other)
	{
		if (other == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
		}
		if (other == this)
		{
			return true;
		}
		if (other is HashSet<T> hashSet && EqualityComparersAreEqual(this, hashSet))
		{
			if (Count != hashSet.Count)
			{
				return false;
			}
			return ContainsAllElements(hashSet);
		}
		if (Count == 0 && other is ICollection<T> { Count: >0 })
		{
			return false;
		}
		var (num, num2) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: true);
		if (num == Count)
		{
			return num2 == 0;
		}
		return false;
	}

	public void CopyTo(T[] array)
	{
		CopyTo(array, 0, Count);
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		CopyTo(array, arrayIndex, Count);
	}

	public void CopyTo(T[] array, int arrayIndex, int count)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (arrayIndex < 0)
		{
			throw new ArgumentOutOfRangeException("arrayIndex", arrayIndex, SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", count, SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (arrayIndex > array.Length || count > array.Length - arrayIndex)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
		}
		Entry[] entries = _entries;
		for (int i = 0; i < _count; i++)
		{
			if (count == 0)
			{
				break;
			}
			ref Entry reference = ref entries[i];
			if (reference.Next >= -1)
			{
				array[arrayIndex++] = reference.Value;
				count--;
			}
		}
	}

	public int RemoveWhere(Predicate<T> match)
	{
		if (match == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
		}
		Entry[] entries = _entries;
		int num = 0;
		for (int i = 0; i < _count; i++)
		{
			ref Entry reference = ref entries[i];
			if (reference.Next >= -1)
			{
				T value = reference.Value;
				if (match(value) && Remove(value))
				{
					num++;
				}
			}
		}
		return num;
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
		if (_buckets == null)
		{
			return Initialize(capacity);
		}
		int prime = HashHelpers.GetPrime(capacity);
		Resize(prime, forceNewHashCodes: false);
		return prime;
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
		if (!typeof(T).IsValueType && forceNewHashCodes)
		{
			_comparer = (IEqualityComparer<T>)((NonRandomizedStringEqualityComparer)_comparer).GetRandomizedEqualityComparer();
			for (int i = 0; i < count; i++)
			{
				ref Entry reference = ref array[i];
				if (reference.Next >= -1)
				{
					reference.HashCode = ((reference.Value != null) ? _comparer.GetHashCode(reference.Value) : 0);
				}
			}
			if (_comparer == EqualityComparer<T>.Default)
			{
				_comparer = null;
			}
		}
		_buckets = new int[newSize];
		_fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);
		for (int j = 0; j < count; j++)
		{
			ref Entry reference2 = ref array[j];
			if (reference2.Next >= -1)
			{
				ref int bucketRef = ref GetBucketRef(reference2.HashCode);
				reference2.Next = bucketRef - 1;
				bucketRef = j + 1;
			}
		}
		_entries = array;
	}

	public void TrimExcess()
	{
		int count = Count;
		int prime = HashHelpers.GetPrime(count);
		Entry[] entries = _entries;
		int num = ((entries != null) ? entries.Length : 0);
		if (prime >= num)
		{
			return;
		}
		int count2 = _count;
		_version++;
		Initialize(prime);
		Entry[] entries2 = _entries;
		int num2 = 0;
		for (int i = 0; i < count2; i++)
		{
			int hashCode = entries[i].HashCode;
			if (entries[i].Next >= -1)
			{
				ref Entry reference = ref entries2[num2];
				reference = entries[i];
				ref int bucketRef = ref GetBucketRef(hashCode);
				reference.Next = bucketRef - 1;
				bucketRef = num2 + 1;
				num2++;
			}
		}
		_count = count;
		_freeCount = 0;
	}

	public static IEqualityComparer<HashSet<T>> CreateSetComparer()
	{
		return new HashSetEqualityComparer<T>();
	}

	private int Initialize(int capacity)
	{
		int prime = HashHelpers.GetPrime(capacity);
		int[] buckets = new int[prime];
		Entry[] entries = new Entry[prime];
		_freeList = -1;
		_buckets = buckets;
		_entries = entries;
		_fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)prime);
		return prime;
	}

	private bool AddIfNotPresent(T value, out int location)
	{
		if (_buckets == null)
		{
			Initialize(0);
		}
		Entry[] entries = _entries;
		IEqualityComparer<T> comparer = _comparer;
		uint num = 0u;
		ref int reference = ref Unsafe.NullRef<int>();
		int num2;
		if (comparer == null)
		{
			num2 = value?.GetHashCode() ?? 0;
			reference = ref GetBucketRef(num2);
			int num3 = reference - 1;
			if (typeof(T).IsValueType)
			{
				while (num3 >= 0)
				{
					ref Entry reference2 = ref entries[num3];
					if (reference2.HashCode == num2 && EqualityComparer<T>.Default.Equals(reference2.Value, value))
					{
						location = num3;
						return false;
					}
					num3 = reference2.Next;
					num++;
					if (num > (uint)entries.Length)
					{
						ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
					}
				}
			}
			else
			{
				EqualityComparer<T> @default = EqualityComparer<T>.Default;
				while (num3 >= 0)
				{
					ref Entry reference3 = ref entries[num3];
					if (reference3.HashCode == num2 && @default.Equals(reference3.Value, value))
					{
						location = num3;
						return false;
					}
					num3 = reference3.Next;
					num++;
					if (num > (uint)entries.Length)
					{
						ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
					}
				}
			}
		}
		else
		{
			num2 = ((value != null) ? comparer.GetHashCode(value) : 0);
			reference = ref GetBucketRef(num2);
			int num4 = reference - 1;
			while (num4 >= 0)
			{
				ref Entry reference4 = ref entries[num4];
				if (reference4.HashCode == num2 && comparer.Equals(reference4.Value, value))
				{
					location = num4;
					return false;
				}
				num4 = reference4.Next;
				num++;
				if (num > (uint)entries.Length)
				{
					ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
				}
			}
		}
		int num5;
		if (_freeCount > 0)
		{
			num5 = _freeList;
			_freeCount--;
			_freeList = -3 - entries[_freeList].Next;
		}
		else
		{
			int count = _count;
			if (count == entries.Length)
			{
				Resize();
				reference = ref GetBucketRef(num2);
			}
			num5 = count;
			_count = count + 1;
			entries = _entries;
		}
		ref Entry reference5 = ref entries[num5];
		reference5.HashCode = num2;
		reference5.Next = reference - 1;
		reference5.Value = value;
		reference = num5 + 1;
		_version++;
		location = num5;
		if (!typeof(T).IsValueType && num > 100 && comparer is NonRandomizedStringEqualityComparer)
		{
			Resize(entries.Length, forceNewHashCodes: true);
			location = FindItemIndex(value);
		}
		return true;
	}

	private bool ContainsAllElements(IEnumerable<T> other)
	{
		foreach (T item in other)
		{
			if (!Contains(item))
			{
				return false;
			}
		}
		return true;
	}

	internal bool IsSubsetOfHashSetWithSameComparer(HashSet<T> other)
	{
		using (Enumerator enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				T current = enumerator.Current;
				if (!other.Contains(current))
				{
					return false;
				}
			}
		}
		return true;
	}

	private void IntersectWithHashSetWithSameComparer(HashSet<T> other)
	{
		Entry[] entries = _entries;
		for (int i = 0; i < _count; i++)
		{
			ref Entry reference = ref entries[i];
			if (reference.Next >= -1)
			{
				T value = reference.Value;
				if (!other.Contains(value))
				{
					Remove(value);
				}
			}
		}
	}

	private void IntersectWithEnumerable(IEnumerable<T> other)
	{
		int count = _count;
		int num = BitHelper.ToIntArrayLength(count);
		Span<int> span = stackalloc int[100];
		BitHelper bitHelper = ((num <= 100) ? new BitHelper(span.Slice(0, num), clear: true) : new BitHelper(new int[num], clear: false));
		foreach (T item in other)
		{
			int num2 = FindItemIndex(item);
			if (num2 >= 0)
			{
				bitHelper.MarkBit(num2);
			}
		}
		for (int i = 0; i < count; i++)
		{
			ref Entry reference = ref _entries[i];
			if (reference.Next >= -1 && !bitHelper.IsMarked(i))
			{
				Remove(reference.Value);
			}
		}
	}

	private void SymmetricExceptWithUniqueHashSet(HashSet<T> other)
	{
		foreach (T item in other)
		{
			if (!Remove(item))
			{
				AddIfNotPresent(item, out var _);
			}
		}
	}

	private void SymmetricExceptWithEnumerable(IEnumerable<T> other)
	{
		int count = _count;
		int num = BitHelper.ToIntArrayLength(count);
		Span<int> span = stackalloc int[50];
		BitHelper bitHelper = ((num <= 50) ? new BitHelper(span.Slice(0, num), clear: true) : new BitHelper(new int[num], clear: false));
		Span<int> span2 = stackalloc int[50];
		BitHelper bitHelper2 = ((num <= 50) ? new BitHelper(span2.Slice(0, num), clear: true) : new BitHelper(new int[num], clear: false));
		foreach (T item in other)
		{
			if (AddIfNotPresent(item, out var location))
			{
				bitHelper2.MarkBit(location);
			}
			else if (location < count && !bitHelper2.IsMarked(location))
			{
				bitHelper.MarkBit(location);
			}
		}
		for (int i = 0; i < count; i++)
		{
			if (bitHelper.IsMarked(i))
			{
				Remove(_entries[i].Value);
			}
		}
	}

	private (int UniqueCount, int UnfoundCount) CheckUniqueAndUnfoundElements(IEnumerable<T> other, bool returnIfUnfound)
	{
		if (_count == 0)
		{
			int num = 0;
			using (IEnumerator<T> enumerator = other.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					T current = enumerator.Current;
					num++;
				}
			}
			return (UniqueCount: 0, UnfoundCount: num);
		}
		int count = _count;
		int num2 = BitHelper.ToIntArrayLength(count);
		Span<int> span = stackalloc int[100];
		BitHelper bitHelper = ((num2 <= 100) ? new BitHelper(span.Slice(0, num2), clear: true) : new BitHelper(new int[num2], clear: false));
		int num3 = 0;
		int num4 = 0;
		foreach (T item in other)
		{
			int num5 = FindItemIndex(item);
			if (num5 >= 0)
			{
				if (!bitHelper.IsMarked(num5))
				{
					bitHelper.MarkBit(num5);
					num4++;
				}
			}
			else
			{
				num3++;
				if (returnIfUnfound)
				{
					break;
				}
			}
		}
		return (UniqueCount: num4, UnfoundCount: num3);
	}

	internal static bool EqualityComparersAreEqual(HashSet<T> set1, HashSet<T> set2)
	{
		return set1.Comparer.Equals(set2.Comparer);
	}
}
