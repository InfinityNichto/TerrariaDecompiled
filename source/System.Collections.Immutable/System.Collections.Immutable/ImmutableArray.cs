using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Collections.Immutable;

public static class ImmutableArray
{
	internal static readonly byte[] TwoElementArray = new byte[2];

	public static ImmutableArray<T> Create<T>()
	{
		return ImmutableArray<T>.Empty;
	}

	public static ImmutableArray<T> Create<T>(T item)
	{
		T[] items = new T[1] { item };
		return new ImmutableArray<T>(items);
	}

	public static ImmutableArray<T> Create<T>(T item1, T item2)
	{
		T[] items = new T[2] { item1, item2 };
		return new ImmutableArray<T>(items);
	}

	public static ImmutableArray<T> Create<T>(T item1, T item2, T item3)
	{
		T[] items = new T[3] { item1, item2, item3 };
		return new ImmutableArray<T>(items);
	}

	public static ImmutableArray<T> Create<T>(T item1, T item2, T item3, T item4)
	{
		T[] items = new T[4] { item1, item2, item3, item4 };
		return new ImmutableArray<T>(items);
	}

	public static ImmutableArray<T> CreateRange<T>(IEnumerable<T> items)
	{
		Requires.NotNull(items, "items");
		if (items is IImmutableArray immutableArray)
		{
			Array array = immutableArray.Array;
			if (array == null)
			{
				throw new InvalidOperationException(System.SR.InvalidOperationOnDefaultArray);
			}
			return new ImmutableArray<T>((T[])array);
		}
		if (items.TryGetCount(out var count))
		{
			return new ImmutableArray<T>(items.ToArray(count));
		}
		return new ImmutableArray<T>(items.ToArray());
	}

	public static ImmutableArray<T> Create<T>(params T[]? items)
	{
		if (items == null || items.Length == 0)
		{
			return ImmutableArray<T>.Empty;
		}
		T[] array = new T[items.Length];
		Array.Copy(items, array, items.Length);
		return new ImmutableArray<T>(array);
	}

	public static ImmutableArray<T> Create<T>(T[] items, int start, int length)
	{
		Requires.NotNull(items, "items");
		Requires.Range(start >= 0 && start <= items.Length, "start");
		Requires.Range(length >= 0 && start + length <= items.Length, "length");
		if (length == 0)
		{
			return Create<T>();
		}
		T[] array = new T[length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = items[start + i];
		}
		return new ImmutableArray<T>(array);
	}

	public static ImmutableArray<T> Create<T>(ImmutableArray<T> items, int start, int length)
	{
		Requires.Range(start >= 0 && start <= items.Length, "start");
		Requires.Range(length >= 0 && start + length <= items.Length, "length");
		if (length == 0)
		{
			return Create<T>();
		}
		if (start == 0 && length == items.Length)
		{
			return items;
		}
		T[] array = new T[length];
		Array.Copy(items.array, start, array, 0, length);
		return new ImmutableArray<T>(array);
	}

	public static ImmutableArray<TResult> CreateRange<TSource, TResult>(ImmutableArray<TSource> items, Func<TSource, TResult> selector)
	{
		Requires.NotNull(selector, "selector");
		int length = items.Length;
		if (length == 0)
		{
			return Create<TResult>();
		}
		TResult[] array = new TResult[length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = selector(items[i]);
		}
		return new ImmutableArray<TResult>(array);
	}

	public static ImmutableArray<TResult> CreateRange<TSource, TResult>(ImmutableArray<TSource> items, int start, int length, Func<TSource, TResult> selector)
	{
		int length2 = items.Length;
		Requires.Range(start >= 0 && start <= length2, "start");
		Requires.Range(length >= 0 && start + length <= length2, "length");
		Requires.NotNull(selector, "selector");
		if (length == 0)
		{
			return Create<TResult>();
		}
		TResult[] array = new TResult[length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = selector(items[i + start]);
		}
		return new ImmutableArray<TResult>(array);
	}

	public static ImmutableArray<TResult> CreateRange<TSource, TArg, TResult>(ImmutableArray<TSource> items, Func<TSource, TArg, TResult> selector, TArg arg)
	{
		Requires.NotNull(selector, "selector");
		int length = items.Length;
		if (length == 0)
		{
			return Create<TResult>();
		}
		TResult[] array = new TResult[length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = selector(items[i], arg);
		}
		return new ImmutableArray<TResult>(array);
	}

	public static ImmutableArray<TResult> CreateRange<TSource, TArg, TResult>(ImmutableArray<TSource> items, int start, int length, Func<TSource, TArg, TResult> selector, TArg arg)
	{
		int length2 = items.Length;
		Requires.Range(start >= 0 && start <= length2, "start");
		Requires.Range(length >= 0 && start + length <= length2, "length");
		Requires.NotNull(selector, "selector");
		if (length == 0)
		{
			return Create<TResult>();
		}
		TResult[] array = new TResult[length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = selector(items[i + start], arg);
		}
		return new ImmutableArray<TResult>(array);
	}

	public static ImmutableArray<T>.Builder CreateBuilder<T>()
	{
		return Create<T>().ToBuilder();
	}

	public static ImmutableArray<T>.Builder CreateBuilder<T>(int initialCapacity)
	{
		return new ImmutableArray<T>.Builder(initialCapacity);
	}

	public static ImmutableArray<TSource> ToImmutableArray<TSource>(this IEnumerable<TSource> items)
	{
		if (items is ImmutableArray<TSource>)
		{
			return (ImmutableArray<TSource>)(object)items;
		}
		return CreateRange(items);
	}

	public static ImmutableArray<TSource> ToImmutableArray<TSource>(this ImmutableArray<TSource>.Builder builder)
	{
		Requires.NotNull(builder, "builder");
		return builder.ToImmutable();
	}

	public static int BinarySearch<T>(this ImmutableArray<T> array, T value)
	{
		return Array.BinarySearch(array.array, value);
	}

	public static int BinarySearch<T>(this ImmutableArray<T> array, T value, IComparer<T>? comparer)
	{
		return Array.BinarySearch(array.array, value, comparer);
	}

	public static int BinarySearch<T>(this ImmutableArray<T> array, int index, int length, T value)
	{
		return Array.BinarySearch(array.array, index, length, value);
	}

	public static int BinarySearch<T>(this ImmutableArray<T> array, int index, int length, T value, IComparer<T>? comparer)
	{
		return Array.BinarySearch(array.array, index, length, value, comparer);
	}
}
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[System.Runtime.Versioning.NonVersionable]
public readonly struct ImmutableArray<T> : IReadOnlyList<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, IList<T>, ICollection<T>, IEquatable<ImmutableArray<T>>, IList, ICollection, IImmutableArray, IStructuralComparable, IStructuralEquatable, IImmutableList<T>
{
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(ImmutableArrayBuilderDebuggerProxy<>))]
	public sealed class Builder : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IReadOnlyList<T>, IReadOnlyCollection<T>
	{
		private T[] _elements;

		private int _count;

		public int Capacity
		{
			get
			{
				return _elements.Length;
			}
			set
			{
				if (value < _count)
				{
					throw new ArgumentException(System.SR.CapacityMustBeGreaterThanOrEqualToCount, "value");
				}
				if (value == _elements.Length)
				{
					return;
				}
				if (value > 0)
				{
					T[] array = new T[value];
					if (_count > 0)
					{
						Array.Copy(_elements, array, _count);
					}
					_elements = array;
				}
				else
				{
					_elements = ImmutableArray<T>.Empty.array;
				}
			}
		}

		public int Count
		{
			get
			{
				return _count;
			}
			set
			{
				Requires.Range(value >= 0, "value");
				if (value < _count)
				{
					if (_count - value > 64)
					{
						Array.Clear(_elements, value, _count - value);
					}
					else
					{
						for (int i = value; i < Count; i++)
						{
							_elements[i] = default(T);
						}
					}
				}
				else if (value > _count)
				{
					EnsureCapacity(value);
				}
				_count = value;
			}
		}

		public T this[int index]
		{
			get
			{
				if (index >= Count)
				{
					ThrowIndexOutOfRangeException();
				}
				return _elements[index];
			}
			set
			{
				if (index >= Count)
				{
					ThrowIndexOutOfRangeException();
				}
				_elements[index] = value;
			}
		}

		bool ICollection<T>.IsReadOnly => false;

		internal Builder(int capacity)
		{
			Requires.Range(capacity >= 0, "capacity");
			_elements = new T[capacity];
			_count = 0;
		}

		internal Builder()
			: this(8)
		{
		}

		private static void ThrowIndexOutOfRangeException()
		{
			throw new IndexOutOfRangeException();
		}

		public ref readonly T ItemRef(int index)
		{
			if (index >= Count)
			{
				ThrowIndexOutOfRangeException();
			}
			return ref _elements[index];
		}

		public ImmutableArray<T> ToImmutable()
		{
			return new ImmutableArray<T>(ToArray());
		}

		public ImmutableArray<T> MoveToImmutable()
		{
			if (Capacity != Count)
			{
				throw new InvalidOperationException(System.SR.CapacityMustEqualCountOnMove);
			}
			T[] elements = _elements;
			_elements = ImmutableArray<T>.Empty.array;
			_count = 0;
			return new ImmutableArray<T>(elements);
		}

		public void Clear()
		{
			Count = 0;
		}

		public void Insert(int index, T item)
		{
			Requires.Range(index >= 0 && index <= Count, "index");
			EnsureCapacity(Count + 1);
			if (index < Count)
			{
				Array.Copy(_elements, index, _elements, index + 1, Count - index);
			}
			_count++;
			_elements[index] = item;
		}

		public void Add(T item)
		{
			int num = _count + 1;
			EnsureCapacity(num);
			_elements[_count] = item;
			_count = num;
		}

		public void AddRange(IEnumerable<T> items)
		{
			Requires.NotNull(items, "items");
			if (items.TryGetCount(out var count))
			{
				EnsureCapacity(Count + count);
				if (items.TryCopyTo(_elements, _count))
				{
					_count += count;
					return;
				}
			}
			foreach (T item in items)
			{
				Add(item);
			}
		}

		public void AddRange(params T[] items)
		{
			Requires.NotNull(items, "items");
			int count = Count;
			Count += items.Length;
			Array.Copy(items, 0, _elements, count, items.Length);
		}

		public void AddRange<TDerived>(TDerived[] items) where TDerived : T
		{
			Requires.NotNull(items, "items");
			int count = Count;
			Count += items.Length;
			Array.Copy(items, 0, _elements, count, items.Length);
		}

		public void AddRange(T[] items, int length)
		{
			Requires.NotNull(items, "items");
			Requires.Range(length >= 0 && length <= items.Length, "length");
			int count = Count;
			Count += length;
			Array.Copy(items, 0, _elements, count, length);
		}

		public void AddRange(ImmutableArray<T> items)
		{
			AddRange(items, items.Length);
		}

		public void AddRange(ImmutableArray<T> items, int length)
		{
			Requires.Range(length >= 0, "length");
			if (items.array != null)
			{
				AddRange(items.array, length);
			}
		}

		public void AddRange<TDerived>(ImmutableArray<TDerived> items) where TDerived : T
		{
			if (items.array != null)
			{
				this.AddRange<TDerived>(items.array);
			}
		}

		public void AddRange(Builder items)
		{
			Requires.NotNull(items, "items");
			AddRange(items._elements, items.Count);
		}

		public void AddRange<TDerived>(ImmutableArray<TDerived>.Builder items) where TDerived : T
		{
			Requires.NotNull(items, "items");
			AddRange<TDerived>(items._elements, items.Count);
		}

		public bool Remove(T element)
		{
			int num = IndexOf(element);
			if (num >= 0)
			{
				RemoveAt(num);
				return true;
			}
			return false;
		}

		public void RemoveAt(int index)
		{
			Requires.Range(index >= 0 && index < Count, "index");
			if (index < Count - 1)
			{
				Array.Copy(_elements, index + 1, _elements, index, Count - index - 1);
			}
			Count--;
		}

		public bool Contains(T item)
		{
			return IndexOf(item) >= 0;
		}

		public T[] ToArray()
		{
			if (Count == 0)
			{
				return ImmutableArray<T>.Empty.array;
			}
			T[] array = new T[Count];
			Array.Copy(_elements, array, Count);
			return array;
		}

		public void CopyTo(T[] array, int index)
		{
			Requires.NotNull(array, "array");
			Requires.Range(index >= 0 && index + Count <= array.Length, "index");
			Array.Copy(_elements, 0, array, index, Count);
		}

		private void EnsureCapacity(int capacity)
		{
			if (_elements.Length < capacity)
			{
				int newSize = Math.Max(_elements.Length * 2, capacity);
				Array.Resize(ref _elements, newSize);
			}
		}

		public int IndexOf(T item)
		{
			return IndexOf(item, 0, _count, EqualityComparer<T>.Default);
		}

		public int IndexOf(T item, int startIndex)
		{
			return IndexOf(item, startIndex, Count - startIndex, EqualityComparer<T>.Default);
		}

		public int IndexOf(T item, int startIndex, int count)
		{
			return IndexOf(item, startIndex, count, EqualityComparer<T>.Default);
		}

		public int IndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer)
		{
			if (count == 0 && startIndex == 0)
			{
				return -1;
			}
			Requires.Range(startIndex >= 0 && startIndex < Count, "startIndex");
			Requires.Range(count >= 0 && startIndex + count <= Count, "count");
			equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
			if (equalityComparer == EqualityComparer<T>.Default)
			{
				return Array.IndexOf(_elements, item, startIndex, count);
			}
			for (int i = startIndex; i < startIndex + count; i++)
			{
				if (equalityComparer.Equals(_elements[i], item))
				{
					return i;
				}
			}
			return -1;
		}

		public int LastIndexOf(T item)
		{
			if (Count == 0)
			{
				return -1;
			}
			return LastIndexOf(item, Count - 1, Count, EqualityComparer<T>.Default);
		}

		public int LastIndexOf(T item, int startIndex)
		{
			if (Count == 0 && startIndex == 0)
			{
				return -1;
			}
			Requires.Range(startIndex >= 0 && startIndex < Count, "startIndex");
			return LastIndexOf(item, startIndex, startIndex + 1, EqualityComparer<T>.Default);
		}

		public int LastIndexOf(T item, int startIndex, int count)
		{
			return LastIndexOf(item, startIndex, count, EqualityComparer<T>.Default);
		}

		public int LastIndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer)
		{
			if (count == 0 && startIndex == 0)
			{
				return -1;
			}
			Requires.Range(startIndex >= 0 && startIndex < Count, "startIndex");
			Requires.Range(count >= 0 && startIndex - count + 1 >= 0, "count");
			equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
			if (equalityComparer == EqualityComparer<T>.Default)
			{
				return Array.LastIndexOf(_elements, item, startIndex, count);
			}
			for (int num = startIndex; num >= startIndex - count + 1; num--)
			{
				if (equalityComparer.Equals(item, _elements[num]))
				{
					return num;
				}
			}
			return -1;
		}

		public void Reverse()
		{
			int num = 0;
			int num2 = _count - 1;
			T[] elements = _elements;
			while (num < num2)
			{
				T val = elements[num];
				elements[num] = elements[num2];
				elements[num2] = val;
				num++;
				num2--;
			}
		}

		public void Sort()
		{
			if (Count > 1)
			{
				Array.Sort(_elements, 0, Count, Comparer<T>.Default);
			}
		}

		public void Sort(Comparison<T> comparison)
		{
			Requires.NotNull(comparison, "comparison");
			if (Count > 1)
			{
				Array.Sort(_elements, 0, _count, Comparer<T>.Create(comparison));
			}
		}

		public void Sort(IComparer<T>? comparer)
		{
			if (Count > 1)
			{
				Array.Sort(_elements, 0, _count, comparer);
			}
		}

		public void Sort(int index, int count, IComparer<T>? comparer)
		{
			Requires.Range(index >= 0, "index");
			Requires.Range(count >= 0 && index + count <= Count, "count");
			if (count > 1)
			{
				Array.Sort(_elements, index, count, comparer);
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < Count; i++)
			{
				yield return this[i];
			}
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private void AddRange<TDerived>(TDerived[] items, int length) where TDerived : T
		{
			EnsureCapacity(Count + length);
			int count = Count;
			Count += length;
			T[] elements = _elements;
			for (int i = 0; i < length; i++)
			{
				elements[count + i] = (T)(object)items[i];
			}
		}
	}

	public struct Enumerator
	{
		private readonly T[] _array;

		private int _index;

		public T Current => _array[_index];

		internal Enumerator(T[] array)
		{
			_array = array;
			_index = -1;
		}

		public bool MoveNext()
		{
			return ++_index < _array.Length;
		}
	}

	private sealed class EnumeratorObject : IEnumerator<T>, IEnumerator, IDisposable
	{
		private static readonly IEnumerator<T> s_EmptyEnumerator = new EnumeratorObject(ImmutableArray<T>.Empty.array);

		private readonly T[] _array;

		private int _index;

		public T Current
		{
			get
			{
				if ((uint)_index < (uint)_array.Length)
				{
					return _array[_index];
				}
				throw new InvalidOperationException();
			}
		}

		object IEnumerator.Current => Current;

		private EnumeratorObject(T[] array)
		{
			_index = -1;
			_array = array;
		}

		public bool MoveNext()
		{
			int num = _index + 1;
			int num2 = _array.Length;
			if ((uint)num <= (uint)num2)
			{
				_index = num;
				return (uint)num < (uint)num2;
			}
			return false;
		}

		void IEnumerator.Reset()
		{
			_index = -1;
		}

		public void Dispose()
		{
		}

		internal static IEnumerator<T> Create(T[] array)
		{
			if (array.Length != 0)
			{
				return new EnumeratorObject(array);
			}
			return s_EmptyEnumerator;
		}
	}

	public static readonly ImmutableArray<T> Empty = new ImmutableArray<T>(new T[0]);

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	internal readonly T[]? array;

	T IList<T>.this[int index]
	{
		get
		{
			ImmutableArray<T> immutableArray = this;
			immutableArray.ThrowInvalidOperationIfNotInitialized();
			return immutableArray[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	bool ICollection<T>.IsReadOnly => true;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	int ICollection<T>.Count
	{
		get
		{
			ImmutableArray<T> immutableArray = this;
			immutableArray.ThrowInvalidOperationIfNotInitialized();
			return immutableArray.Length;
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	int IReadOnlyCollection<T>.Count
	{
		get
		{
			ImmutableArray<T> immutableArray = this;
			immutableArray.ThrowInvalidOperationIfNotInitialized();
			return immutableArray.Length;
		}
	}

	T IReadOnlyList<T>.this[int index]
	{
		get
		{
			ImmutableArray<T> immutableArray = this;
			immutableArray.ThrowInvalidOperationIfNotInitialized();
			return immutableArray[index];
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	bool IList.IsFixedSize => true;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	bool IList.IsReadOnly => true;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	int ICollection.Count
	{
		get
		{
			ImmutableArray<T> immutableArray = this;
			immutableArray.ThrowInvalidOperationIfNotInitialized();
			return immutableArray.Length;
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	bool ICollection.IsSynchronized => true;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	object ICollection.SyncRoot
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	object? IList.this[int index]
	{
		get
		{
			ImmutableArray<T> immutableArray = this;
			immutableArray.ThrowInvalidOperationIfNotInitialized();
			return immutableArray[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public T this[int index]
	{
		[System.Runtime.Versioning.NonVersionable]
		get
		{
			return array[index];
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public bool IsEmpty
	{
		[System.Runtime.Versioning.NonVersionable]
		get
		{
			return array.Length == 0;
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public int Length
	{
		[System.Runtime.Versioning.NonVersionable]
		get
		{
			return array.Length;
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public bool IsDefault => array == null;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public bool IsDefaultOrEmpty
	{
		get
		{
			ImmutableArray<T> immutableArray = this;
			if (immutableArray.array != null)
			{
				return immutableArray.array.Length == 0;
			}
			return true;
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	Array? IImmutableArray.Array => array;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private string DebuggerDisplay
	{
		get
		{
			ImmutableArray<T> immutableArray = this;
			if (!immutableArray.IsDefault)
			{
				return $"Length = {immutableArray.Length}";
			}
			return "Uninitialized";
		}
	}

	public ReadOnlySpan<T> AsSpan()
	{
		return new ReadOnlySpan<T>(array);
	}

	public ReadOnlyMemory<T> AsMemory()
	{
		return new ReadOnlyMemory<T>(array);
	}

	public int IndexOf(T item)
	{
		ImmutableArray<T> immutableArray = this;
		return immutableArray.IndexOf(item, 0, immutableArray.Length, EqualityComparer<T>.Default);
	}

	public int IndexOf(T item, int startIndex, IEqualityComparer<T>? equalityComparer)
	{
		ImmutableArray<T> immutableArray = this;
		return immutableArray.IndexOf(item, startIndex, immutableArray.Length - startIndex, equalityComparer);
	}

	public int IndexOf(T item, int startIndex)
	{
		ImmutableArray<T> immutableArray = this;
		return immutableArray.IndexOf(item, startIndex, immutableArray.Length - startIndex, EqualityComparer<T>.Default);
	}

	public int IndexOf(T item, int startIndex, int count)
	{
		return IndexOf(item, startIndex, count, EqualityComparer<T>.Default);
	}

	public int IndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		if (count == 0 && startIndex == 0)
		{
			return -1;
		}
		Requires.Range(startIndex >= 0 && startIndex < immutableArray.Length, "startIndex");
		Requires.Range(count >= 0 && startIndex + count <= immutableArray.Length, "count");
		equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
		if (equalityComparer == EqualityComparer<T>.Default)
		{
			return Array.IndexOf(immutableArray.array, item, startIndex, count);
		}
		for (int i = startIndex; i < startIndex + count; i++)
		{
			if (equalityComparer.Equals(immutableArray.array[i], item))
			{
				return i;
			}
		}
		return -1;
	}

	public int LastIndexOf(T item)
	{
		ImmutableArray<T> immutableArray = this;
		if (immutableArray.Length == 0)
		{
			return -1;
		}
		return immutableArray.LastIndexOf(item, immutableArray.Length - 1, immutableArray.Length, EqualityComparer<T>.Default);
	}

	public int LastIndexOf(T item, int startIndex)
	{
		ImmutableArray<T> immutableArray = this;
		if (immutableArray.Length == 0 && startIndex == 0)
		{
			return -1;
		}
		return immutableArray.LastIndexOf(item, startIndex, startIndex + 1, EqualityComparer<T>.Default);
	}

	public int LastIndexOf(T item, int startIndex, int count)
	{
		return LastIndexOf(item, startIndex, count, EqualityComparer<T>.Default);
	}

	public int LastIndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		if (startIndex == 0 && count == 0)
		{
			return -1;
		}
		Requires.Range(startIndex >= 0 && startIndex < immutableArray.Length, "startIndex");
		Requires.Range(count >= 0 && startIndex - count + 1 >= 0, "count");
		equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
		if (equalityComparer == EqualityComparer<T>.Default)
		{
			return Array.LastIndexOf(immutableArray.array, item, startIndex, count);
		}
		for (int num = startIndex; num >= startIndex - count + 1; num--)
		{
			if (equalityComparer.Equals(item, immutableArray.array[num]))
			{
				return num;
			}
		}
		return -1;
	}

	public bool Contains(T item)
	{
		return IndexOf(item) >= 0;
	}

	public ImmutableArray<T> Insert(int index, T item)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		Requires.Range(index >= 0 && index <= immutableArray.Length, "index");
		if (immutableArray.Length == 0)
		{
			return ImmutableArray.Create(item);
		}
		T[] array = new T[immutableArray.Length + 1];
		array[index] = item;
		if (index != 0)
		{
			Array.Copy(immutableArray.array, array, index);
		}
		if (index != immutableArray.Length)
		{
			Array.Copy(immutableArray.array, index, array, index + 1, immutableArray.Length - index);
		}
		return new ImmutableArray<T>(array);
	}

	public ImmutableArray<T> InsertRange(int index, IEnumerable<T> items)
	{
		ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		Requires.Range(index >= 0 && index <= result.Length, "index");
		Requires.NotNull(items, "items");
		if (result.Length == 0)
		{
			return ImmutableArray.CreateRange(items);
		}
		int count = ImmutableExtensions.GetCount(ref items);
		if (count == 0)
		{
			return result;
		}
		T[] array = new T[result.Length + count];
		if (index != 0)
		{
			Array.Copy(result.array, array, index);
		}
		if (index != result.Length)
		{
			Array.Copy(result.array, index, array, index + count, result.Length - index);
		}
		if (!items.TryCopyTo(array, index))
		{
			int num = index;
			foreach (T item in items)
			{
				array[num++] = item;
			}
		}
		return new ImmutableArray<T>(array);
	}

	public ImmutableArray<T> InsertRange(int index, ImmutableArray<T> items)
	{
		ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		items.ThrowNullRefIfNotInitialized();
		Requires.Range(index >= 0 && index <= result.Length, "index");
		if (result.IsEmpty)
		{
			return items;
		}
		if (items.IsEmpty)
		{
			return result;
		}
		T[] array = new T[result.Length + items.Length];
		if (index != 0)
		{
			Array.Copy(result.array, array, index);
		}
		if (index != result.Length)
		{
			Array.Copy(result.array, index, array, index + items.Length, result.Length - index);
		}
		Array.Copy(items.array, 0, array, index, items.Length);
		return new ImmutableArray<T>(array);
	}

	public ImmutableArray<T> Add(T item)
	{
		ImmutableArray<T> immutableArray = this;
		if (immutableArray.Length == 0)
		{
			return ImmutableArray.Create(item);
		}
		return immutableArray.Insert(immutableArray.Length, item);
	}

	public ImmutableArray<T> AddRange(IEnumerable<T> items)
	{
		ImmutableArray<T> immutableArray = this;
		return immutableArray.InsertRange(immutableArray.Length, items);
	}

	public ImmutableArray<T> AddRange(ImmutableArray<T> items)
	{
		ImmutableArray<T> immutableArray = this;
		return immutableArray.InsertRange(immutableArray.Length, items);
	}

	public ImmutableArray<T> SetItem(int index, T item)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		Requires.Range(index >= 0 && index < immutableArray.Length, "index");
		T[] array = new T[immutableArray.Length];
		Array.Copy(immutableArray.array, array, immutableArray.Length);
		array[index] = item;
		return new ImmutableArray<T>(array);
	}

	public ImmutableArray<T> Replace(T oldValue, T newValue)
	{
		return Replace(oldValue, newValue, EqualityComparer<T>.Default);
	}

	public ImmutableArray<T> Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer)
	{
		ImmutableArray<T> immutableArray = this;
		int num = immutableArray.IndexOf(oldValue, 0, immutableArray.Length, equalityComparer);
		if (num < 0)
		{
			throw new ArgumentException(System.SR.CannotFindOldValue, "oldValue");
		}
		return immutableArray.SetItem(num, newValue);
	}

	public ImmutableArray<T> Remove(T item)
	{
		return Remove(item, EqualityComparer<T>.Default);
	}

	public ImmutableArray<T> Remove(T item, IEqualityComparer<T>? equalityComparer)
	{
		ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		int num = result.IndexOf(item, 0, result.Length, equalityComparer);
		if (num >= 0)
		{
			return result.RemoveAt(num);
		}
		return result;
	}

	public ImmutableArray<T> RemoveAt(int index)
	{
		return RemoveRange(index, 1);
	}

	public ImmutableArray<T> RemoveRange(int index, int length)
	{
		ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		Requires.Range(index >= 0 && index <= result.Length, "index");
		Requires.Range(length >= 0 && index + length <= result.Length, "length");
		if (length == 0)
		{
			return result;
		}
		T[] array = new T[result.Length - length];
		Array.Copy(result.array, array, index);
		Array.Copy(result.array, index + length, array, index, result.Length - index - length);
		return new ImmutableArray<T>(array);
	}

	public ImmutableArray<T> RemoveRange(IEnumerable<T> items)
	{
		return RemoveRange(items, EqualityComparer<T>.Default);
	}

	public ImmutableArray<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		Requires.NotNull(items, "items");
		SortedSet<int> sortedSet = new SortedSet<int>();
		foreach (T item in items)
		{
			int num = immutableArray.IndexOf(item, 0, immutableArray.Length, equalityComparer);
			while (num >= 0 && !sortedSet.Add(num) && num + 1 < immutableArray.Length)
			{
				num = immutableArray.IndexOf(item, num + 1, equalityComparer);
			}
		}
		return immutableArray.RemoveAtRange(sortedSet);
	}

	public ImmutableArray<T> RemoveRange(ImmutableArray<T> items)
	{
		return RemoveRange(items, EqualityComparer<T>.Default);
	}

	public ImmutableArray<T> RemoveRange(ImmutableArray<T> items, IEqualityComparer<T>? equalityComparer)
	{
		ImmutableArray<T> result = this;
		Requires.NotNull(items.array, "items");
		if (items.IsEmpty)
		{
			result.ThrowNullRefIfNotInitialized();
			return result;
		}
		if (items.Length == 1)
		{
			return result.Remove(items[0], equalityComparer);
		}
		return result.RemoveRange(items.array, equalityComparer);
	}

	public ImmutableArray<T> RemoveAll(Predicate<T> match)
	{
		ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		Requires.NotNull(match, "match");
		if (result.IsEmpty)
		{
			return result;
		}
		List<int> list = null;
		for (int i = 0; i < result.array.Length; i++)
		{
			if (match(result.array[i]))
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(i);
			}
		}
		if (list == null)
		{
			return result;
		}
		return result.RemoveAtRange(list);
	}

	public ImmutableArray<T> Clear()
	{
		return Empty;
	}

	public ImmutableArray<T> Sort()
	{
		ImmutableArray<T> immutableArray = this;
		return immutableArray.Sort(0, immutableArray.Length, Comparer<T>.Default);
	}

	public ImmutableArray<T> Sort(Comparison<T> comparison)
	{
		Requires.NotNull(comparison, "comparison");
		ImmutableArray<T> immutableArray = this;
		return immutableArray.Sort(Comparer<T>.Create(comparison));
	}

	public ImmutableArray<T> Sort(IComparer<T>? comparer)
	{
		ImmutableArray<T> immutableArray = this;
		return immutableArray.Sort(0, immutableArray.Length, comparer);
	}

	public ImmutableArray<T> Sort(int index, int count, IComparer<T>? comparer)
	{
		ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		Requires.Range(index >= 0, "index");
		Requires.Range(count >= 0 && index + count <= result.Length, "count");
		if (count > 1)
		{
			if (comparer == null)
			{
				comparer = Comparer<T>.Default;
			}
			bool flag = false;
			for (int i = index + 1; i < index + count; i++)
			{
				if (comparer.Compare(result.array[i - 1], result.array[i]) > 0)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				T[] array = new T[result.Length];
				Array.Copy(result.array, array, result.Length);
				Array.Sort(array, index, count, comparer);
				return new ImmutableArray<T>(array);
			}
		}
		return result;
	}

	public IEnumerable<TResult> OfType<TResult>()
	{
		ImmutableArray<T> immutableArray = this;
		if (immutableArray.array == null || immutableArray.array.Length == 0)
		{
			return Enumerable.Empty<TResult>();
		}
		return immutableArray.array.OfType<TResult>();
	}

	void IList<T>.Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	void IList<T>.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	void ICollection<T>.Add(T item)
	{
		throw new NotSupportedException();
	}

	void ICollection<T>.Clear()
	{
		throw new NotSupportedException();
	}

	bool ICollection<T>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	IImmutableList<T> IImmutableList<T>.Clear()
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.Clear();
	}

	IImmutableList<T> IImmutableList<T>.Add(T value)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.Add(value);
	}

	IImmutableList<T> IImmutableList<T>.AddRange(IEnumerable<T> items)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.AddRange(items);
	}

	IImmutableList<T> IImmutableList<T>.Insert(int index, T element)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.Insert(index, element);
	}

	IImmutableList<T> IImmutableList<T>.InsertRange(int index, IEnumerable<T> items)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.InsertRange(index, items);
	}

	IImmutableList<T> IImmutableList<T>.Remove(T value, IEqualityComparer<T> equalityComparer)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.Remove(value, equalityComparer);
	}

	IImmutableList<T> IImmutableList<T>.RemoveAll(Predicate<T> match)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.RemoveAll(match);
	}

	IImmutableList<T> IImmutableList<T>.RemoveRange(IEnumerable<T> items, IEqualityComparer<T> equalityComparer)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.RemoveRange(items, equalityComparer);
	}

	IImmutableList<T> IImmutableList<T>.RemoveRange(int index, int count)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.RemoveRange(index, count);
	}

	IImmutableList<T> IImmutableList<T>.RemoveAt(int index)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.RemoveAt(index);
	}

	IImmutableList<T> IImmutableList<T>.SetItem(int index, T value)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.SetItem(index, value);
	}

	IImmutableList<T> IImmutableList<T>.Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.Replace(oldValue, newValue, equalityComparer);
	}

	int IList.Add(object value)
	{
		throw new NotSupportedException();
	}

	void IList.Clear()
	{
		throw new NotSupportedException();
	}

	bool IList.Contains(object value)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.Contains((T)value);
	}

	int IList.IndexOf(object value)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.IndexOf((T)value);
	}

	void IList.Insert(int index, object value)
	{
		throw new NotSupportedException();
	}

	void IList.Remove(object value)
	{
		throw new NotSupportedException();
	}

	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	void ICollection.CopyTo(Array array, int index)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		Array.Copy(immutableArray.array, 0, array, index, immutableArray.Length);
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		ImmutableArray<T> immutableArray = this;
		Array array = other as Array;
		if (array == null && other is IImmutableArray immutableArray2)
		{
			array = immutableArray2.Array;
			if (immutableArray.array == null && array == null)
			{
				return true;
			}
			if (immutableArray.array == null)
			{
				return false;
			}
		}
		IStructuralEquatable structuralEquatable = immutableArray.array;
		return structuralEquatable.Equals(array, comparer);
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		ImmutableArray<T> immutableArray = this;
		return ((IStructuralEquatable)immutableArray.array)?.GetHashCode(comparer) ?? immutableArray.GetHashCode();
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		ImmutableArray<T> immutableArray = this;
		Array array = other as Array;
		if (array == null && other is IImmutableArray immutableArray2)
		{
			array = immutableArray2.Array;
			if (immutableArray.array == null && array == null)
			{
				return 0;
			}
			if ((immutableArray.array == null) ^ (array == null))
			{
				throw new ArgumentException(System.SR.ArrayInitializedStateNotEqual, "other");
			}
		}
		if (array != null)
		{
			IStructuralComparable structuralComparable = immutableArray.array;
			if (structuralComparable == null)
			{
				throw new ArgumentException(System.SR.ArrayInitializedStateNotEqual, "other");
			}
			return structuralComparable.CompareTo(array, comparer);
		}
		throw new ArgumentException(System.SR.ArrayLengthsNotEqual, "other");
	}

	private ImmutableArray<T> RemoveAtRange(ICollection<int> indicesToRemove)
	{
		ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		Requires.NotNull(indicesToRemove, "indicesToRemove");
		if (indicesToRemove.Count == 0)
		{
			return result;
		}
		T[] array = new T[result.Length - indicesToRemove.Count];
		int num = 0;
		int num2 = 0;
		int num3 = -1;
		foreach (int item in indicesToRemove)
		{
			int num4 = ((num3 == -1) ? item : (item - num3 - 1));
			Array.Copy(result.array, num + num2, array, num, num4);
			num2++;
			num += num4;
			num3 = item;
		}
		Array.Copy(result.array, num + num2, array, num, result.Length - (num + num2));
		return new ImmutableArray<T>(array);
	}

	internal ImmutableArray(T[]? items)
	{
		array = items;
	}

	[System.Runtime.Versioning.NonVersionable]
	public static bool operator ==(ImmutableArray<T> left, ImmutableArray<T> right)
	{
		return left.Equals(right);
	}

	[System.Runtime.Versioning.NonVersionable]
	public static bool operator !=(ImmutableArray<T> left, ImmutableArray<T> right)
	{
		return !left.Equals(right);
	}

	public static bool operator ==(ImmutableArray<T>? left, ImmutableArray<T>? right)
	{
		return left.GetValueOrDefault().Equals(right.GetValueOrDefault());
	}

	public static bool operator !=(ImmutableArray<T>? left, ImmutableArray<T>? right)
	{
		return !left.GetValueOrDefault().Equals(right.GetValueOrDefault());
	}

	public ref readonly T ItemRef(int index)
	{
		return ref array[index];
	}

	public void CopyTo(T[] destination)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		Array.Copy(immutableArray.array, destination, immutableArray.Length);
	}

	public void CopyTo(T[] destination, int destinationIndex)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		Array.Copy(immutableArray.array, 0, destination, destinationIndex, immutableArray.Length);
	}

	public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int length)
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		Array.Copy(immutableArray.array, sourceIndex, destination, destinationIndex, length);
	}

	public ImmutableArray<T>.Builder ToBuilder()
	{
		ImmutableArray<T> items = this;
		if (items.Length == 0)
		{
			return new Builder();
		}
		Builder builder = new Builder(items.Length);
		builder.AddRange(items);
		return builder;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Enumerator GetEnumerator()
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		return new Enumerator(immutableArray.array);
	}

	public override int GetHashCode()
	{
		ImmutableArray<T> immutableArray = this;
		if (immutableArray.array != null)
		{
			return immutableArray.array.GetHashCode();
		}
		return 0;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is IImmutableArray immutableArray)
		{
			return array == immutableArray.Array;
		}
		return false;
	}

	[System.Runtime.Versioning.NonVersionable]
	public bool Equals(ImmutableArray<T> other)
	{
		return array == other.array;
	}

	public static ImmutableArray<T> CastUp<TDerived>(ImmutableArray<TDerived> items) where TDerived : class?, T
	{
		T[] items2 = (T[])(object)items.array;
		return new ImmutableArray<T>(items2);
	}

	public ImmutableArray<TOther> CastArray<TOther>() where TOther : class?
	{
		return new ImmutableArray<TOther>((TOther[])(object)array);
	}

	public ImmutableArray<TOther> As<TOther>() where TOther : class?
	{
		return new ImmutableArray<TOther>(array as TOther[]);
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return EnumeratorObject.Create(immutableArray.array);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return EnumeratorObject.Create(immutableArray.array);
	}

	internal void ThrowNullRefIfNotInitialized()
	{
		_ = array.Length;
	}

	private void ThrowInvalidOperationIfNotInitialized()
	{
		if (IsDefault)
		{
			throw new InvalidOperationException(System.SR.InvalidOperationOnDefaultArray);
		}
	}
}
