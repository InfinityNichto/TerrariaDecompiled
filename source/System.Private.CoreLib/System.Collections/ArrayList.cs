using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections;

[Serializable]
[DebuggerTypeProxy(typeof(ArrayListDebugView))]
[DebuggerDisplay("Count = {Count}")]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ArrayList : IList, ICollection, IEnumerable, ICloneable
{
	private sealed class IListWrapper : ArrayList
	{
		private sealed class IListWrapperEnumWrapper : IEnumerator, ICloneable
		{
			private IEnumerator _en;

			private int _remaining;

			private int _initialStartIndex;

			private int _initialCount;

			private bool _firstCall;

			public object Current
			{
				get
				{
					if (_firstCall)
					{
						throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
					}
					if (_remaining < 0)
					{
						throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
					}
					return _en.Current;
				}
			}

			internal IListWrapperEnumWrapper(IListWrapper listWrapper, int startIndex, int count)
			{
				_en = listWrapper.GetEnumerator();
				_initialStartIndex = startIndex;
				_initialCount = count;
				while (startIndex-- > 0 && _en.MoveNext())
				{
				}
				_remaining = count;
				_firstCall = true;
			}

			private IListWrapperEnumWrapper()
			{
			}

			public object Clone()
			{
				IListWrapperEnumWrapper listWrapperEnumWrapper = new IListWrapperEnumWrapper();
				listWrapperEnumWrapper._en = (IEnumerator)((ICloneable)_en).Clone();
				listWrapperEnumWrapper._initialStartIndex = _initialStartIndex;
				listWrapperEnumWrapper._initialCount = _initialCount;
				listWrapperEnumWrapper._remaining = _remaining;
				listWrapperEnumWrapper._firstCall = _firstCall;
				return listWrapperEnumWrapper;
			}

			public bool MoveNext()
			{
				if (_firstCall)
				{
					_firstCall = false;
					if (_remaining-- > 0)
					{
						return _en.MoveNext();
					}
					return false;
				}
				if (_remaining < 0)
				{
					return false;
				}
				if (_en.MoveNext())
				{
					return _remaining-- > 0;
				}
				return false;
			}

			public void Reset()
			{
				_en.Reset();
				int initialStartIndex = _initialStartIndex;
				while (initialStartIndex-- > 0 && _en.MoveNext())
				{
				}
				_remaining = _initialCount;
				_firstCall = true;
			}
		}

		private readonly IList _list;

		public override int Capacity
		{
			get
			{
				return _list.Count;
			}
			set
			{
				if (value < Count)
				{
					throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_SmallCapacity);
				}
			}
		}

		public override int Count => _list.Count;

		public override bool IsReadOnly => _list.IsReadOnly;

		public override bool IsFixedSize => _list.IsFixedSize;

		public override bool IsSynchronized => _list.IsSynchronized;

		public override object this[int index]
		{
			get
			{
				return _list[index];
			}
			set
			{
				_list[index] = value;
				_version++;
			}
		}

		public override object SyncRoot => _list.SyncRoot;

		internal IListWrapper(IList list)
		{
			_list = list;
			_version = 0;
		}

		public override int Add(object obj)
		{
			int result = _list.Add(obj);
			_version++;
			return result;
		}

		public override void AddRange(ICollection c)
		{
			InsertRange(Count, c);
		}

		public override int BinarySearch(int index, int count, object value, IComparer comparer)
		{
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (Count - index < count)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			if (comparer == null)
			{
				comparer = Comparer.Default;
			}
			int num = index;
			int num2 = index + count - 1;
			while (num <= num2)
			{
				int num3 = (num + num2) / 2;
				int num4 = comparer.Compare(value, _list[num3]);
				if (num4 == 0)
				{
					return num3;
				}
				if (num4 < 0)
				{
					num2 = num3 - 1;
				}
				else
				{
					num = num3 + 1;
				}
			}
			return ~num;
		}

		public override void Clear()
		{
			if (_list.IsFixedSize)
			{
				throw new NotSupportedException(SR.NotSupported_FixedSizeCollection);
			}
			_list.Clear();
			_version++;
		}

		public override object Clone()
		{
			return new IListWrapper(_list);
		}

		public override bool Contains(object obj)
		{
			return _list.Contains(obj);
		}

		public override void CopyTo(Array array, int index)
		{
			_list.CopyTo(array, index);
		}

		public override void CopyTo(int index, Array array, int arrayIndex, int count)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (index < 0 || arrayIndex < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "arrayIndex", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (array.Length - arrayIndex < count)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			if (array.Rank != 1)
			{
				throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, "array");
			}
			if (_list.Count - index < count)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			for (int i = index; i < index + count; i++)
			{
				array.SetValue(_list[i], arrayIndex++);
			}
		}

		public override IEnumerator GetEnumerator()
		{
			return _list.GetEnumerator();
		}

		public override IEnumerator GetEnumerator(int index, int count)
		{
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (_list.Count - index < count)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			return new IListWrapperEnumWrapper(this, index, count);
		}

		public override int IndexOf(object value)
		{
			return _list.IndexOf(value);
		}

		public override int IndexOf(object value, int startIndex)
		{
			return IndexOf(value, startIndex, _list.Count - startIndex);
		}

		public override int IndexOf(object value, int startIndex, int count)
		{
			if (startIndex < 0 || startIndex > Count)
			{
				throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_Index);
			}
			if (count < 0 || startIndex > Count - count)
			{
				throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_Count);
			}
			int num = startIndex + count;
			if (value == null)
			{
				for (int i = startIndex; i < num; i++)
				{
					if (_list[i] == null)
					{
						return i;
					}
				}
				return -1;
			}
			for (int j = startIndex; j < num; j++)
			{
				object obj = _list[j];
				if (obj != null && obj.Equals(value))
				{
					return j;
				}
			}
			return -1;
		}

		public override void Insert(int index, object obj)
		{
			_list.Insert(index, obj);
			_version++;
		}

		public override void InsertRange(int index, ICollection c)
		{
			if (c == null)
			{
				throw new ArgumentNullException("c", SR.ArgumentNull_Collection);
			}
			if (index < 0 || index > Count)
			{
				throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
			}
			if (c.Count <= 0)
			{
				return;
			}
			if (_list is ArrayList arrayList)
			{
				arrayList.InsertRange(index, c);
			}
			else
			{
				IEnumerator enumerator = c.GetEnumerator();
				while (enumerator.MoveNext())
				{
					_list.Insert(index++, enumerator.Current);
				}
			}
			_version++;
		}

		public override int LastIndexOf(object value)
		{
			return LastIndexOf(value, _list.Count - 1, _list.Count);
		}

		public override int LastIndexOf(object value, int startIndex)
		{
			return LastIndexOf(value, startIndex, startIndex + 1);
		}

		public override int LastIndexOf(object value, int startIndex, int count)
		{
			if (_list.Count == 0)
			{
				return -1;
			}
			if (startIndex < 0 || startIndex >= _list.Count)
			{
				throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_Index);
			}
			if (count < 0 || count > startIndex + 1)
			{
				throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_Count);
			}
			int num = startIndex - count + 1;
			if (value == null)
			{
				for (int num2 = startIndex; num2 >= num; num2--)
				{
					if (_list[num2] == null)
					{
						return num2;
					}
				}
				return -1;
			}
			for (int num3 = startIndex; num3 >= num; num3--)
			{
				object obj = _list[num3];
				if (obj != null && obj.Equals(value))
				{
					return num3;
				}
			}
			return -1;
		}

		public override void Remove(object value)
		{
			int num = IndexOf(value);
			if (num >= 0)
			{
				RemoveAt(num);
			}
		}

		public override void RemoveAt(int index)
		{
			_list.RemoveAt(index);
			_version++;
		}

		public override void RemoveRange(int index, int count)
		{
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (_list.Count - index < count)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			if (count > 0)
			{
				_version++;
			}
			while (count > 0)
			{
				_list.RemoveAt(index);
				count--;
			}
		}

		public override void Reverse(int index, int count)
		{
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (_list.Count - index < count)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			int num = index;
			int num2 = index + count - 1;
			while (num < num2)
			{
				object value = _list[num];
				_list[num++] = _list[num2];
				_list[num2--] = value;
			}
			_version++;
		}

		public override void SetRange(int index, ICollection c)
		{
			if (c == null)
			{
				throw new ArgumentNullException("c", SR.ArgumentNull_Collection);
			}
			if (index < 0 || index > _list.Count - c.Count)
			{
				throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
			}
			if (c.Count > 0)
			{
				IEnumerator enumerator = c.GetEnumerator();
				while (enumerator.MoveNext())
				{
					_list[index++] = enumerator.Current;
				}
				_version++;
			}
		}

		public override ArrayList GetRange(int index, int count)
		{
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (_list.Count - index < count)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			return new Range(this, index, count);
		}

		public override void Sort(int index, int count, IComparer comparer)
		{
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (_list.Count - index < count)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			object[] array = new object[count];
			CopyTo(index, array, 0, count);
			Array.Sort(array, 0, count, comparer);
			for (int i = 0; i < count; i++)
			{
				_list[i + index] = array[i];
			}
			_version++;
		}

		public override object[] ToArray()
		{
			if (Count == 0)
			{
				return Array.Empty<object>();
			}
			object[] array = new object[Count];
			_list.CopyTo(array, 0);
			return array;
		}

		public override Array ToArray(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			Array array = Array.CreateInstance(type, _list.Count);
			_list.CopyTo(array, 0);
			return array;
		}

		public override void TrimToSize()
		{
		}
	}

	private sealed class SyncArrayList : ArrayList
	{
		private readonly ArrayList _list;

		private readonly object _root;

		public override int Capacity
		{
			get
			{
				lock (_root)
				{
					return _list.Capacity;
				}
			}
			set
			{
				lock (_root)
				{
					_list.Capacity = value;
				}
			}
		}

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

		public override bool IsReadOnly => _list.IsReadOnly;

		public override bool IsFixedSize => _list.IsFixedSize;

		public override bool IsSynchronized => true;

		public override object this[int index]
		{
			get
			{
				lock (_root)
				{
					return _list[index];
				}
			}
			set
			{
				lock (_root)
				{
					_list[index] = value;
				}
			}
		}

		public override object SyncRoot => _root;

		internal SyncArrayList(ArrayList list)
		{
			_list = list;
			_root = list.SyncRoot;
		}

		public override int Add(object value)
		{
			lock (_root)
			{
				return _list.Add(value);
			}
		}

		public override void AddRange(ICollection c)
		{
			lock (_root)
			{
				_list.AddRange(c);
			}
		}

		public override int BinarySearch(object value)
		{
			lock (_root)
			{
				return _list.BinarySearch(value);
			}
		}

		public override int BinarySearch(object value, IComparer comparer)
		{
			lock (_root)
			{
				return _list.BinarySearch(value, comparer);
			}
		}

		public override int BinarySearch(int index, int count, object value, IComparer comparer)
		{
			lock (_root)
			{
				return _list.BinarySearch(index, count, value, comparer);
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
				return new SyncArrayList((ArrayList)_list.Clone());
			}
		}

		public override bool Contains(object item)
		{
			lock (_root)
			{
				return _list.Contains(item);
			}
		}

		public override void CopyTo(Array array)
		{
			lock (_root)
			{
				_list.CopyTo(array);
			}
		}

		public override void CopyTo(Array array, int index)
		{
			lock (_root)
			{
				_list.CopyTo(array, index);
			}
		}

		public override void CopyTo(int index, Array array, int arrayIndex, int count)
		{
			lock (_root)
			{
				_list.CopyTo(index, array, arrayIndex, count);
			}
		}

		public override IEnumerator GetEnumerator()
		{
			lock (_root)
			{
				return _list.GetEnumerator();
			}
		}

		public override IEnumerator GetEnumerator(int index, int count)
		{
			lock (_root)
			{
				return _list.GetEnumerator(index, count);
			}
		}

		public override int IndexOf(object value)
		{
			lock (_root)
			{
				return _list.IndexOf(value);
			}
		}

		public override int IndexOf(object value, int startIndex)
		{
			lock (_root)
			{
				return _list.IndexOf(value, startIndex);
			}
		}

		public override int IndexOf(object value, int startIndex, int count)
		{
			lock (_root)
			{
				return _list.IndexOf(value, startIndex, count);
			}
		}

		public override void Insert(int index, object value)
		{
			lock (_root)
			{
				_list.Insert(index, value);
			}
		}

		public override void InsertRange(int index, ICollection c)
		{
			lock (_root)
			{
				_list.InsertRange(index, c);
			}
		}

		public override int LastIndexOf(object value)
		{
			lock (_root)
			{
				return _list.LastIndexOf(value);
			}
		}

		public override int LastIndexOf(object value, int startIndex)
		{
			lock (_root)
			{
				return _list.LastIndexOf(value, startIndex);
			}
		}

		public override int LastIndexOf(object value, int startIndex, int count)
		{
			lock (_root)
			{
				return _list.LastIndexOf(value, startIndex, count);
			}
		}

		public override void Remove(object value)
		{
			lock (_root)
			{
				_list.Remove(value);
			}
		}

		public override void RemoveAt(int index)
		{
			lock (_root)
			{
				_list.RemoveAt(index);
			}
		}

		public override void RemoveRange(int index, int count)
		{
			lock (_root)
			{
				_list.RemoveRange(index, count);
			}
		}

		public override void Reverse(int index, int count)
		{
			lock (_root)
			{
				_list.Reverse(index, count);
			}
		}

		public override void SetRange(int index, ICollection c)
		{
			lock (_root)
			{
				_list.SetRange(index, c);
			}
		}

		public override ArrayList GetRange(int index, int count)
		{
			lock (_root)
			{
				return _list.GetRange(index, count);
			}
		}

		public override void Sort()
		{
			lock (_root)
			{
				_list.Sort();
			}
		}

		public override void Sort(IComparer comparer)
		{
			lock (_root)
			{
				_list.Sort(comparer);
			}
		}

		public override void Sort(int index, int count, IComparer comparer)
		{
			lock (_root)
			{
				_list.Sort(index, count, comparer);
			}
		}

		public override object[] ToArray()
		{
			lock (_root)
			{
				return _list.ToArray();
			}
		}

		public override Array ToArray(Type type)
		{
			lock (_root)
			{
				return _list.ToArray(type);
			}
		}

		public override void TrimToSize()
		{
			lock (_root)
			{
				_list.TrimToSize();
			}
		}
	}

	private sealed class SyncIList : IList, ICollection, IEnumerable
	{
		private readonly IList _list;

		private readonly object _root;

		public int Count
		{
			get
			{
				lock (_root)
				{
					return _list.Count;
				}
			}
		}

		public bool IsReadOnly => _list.IsReadOnly;

		public bool IsFixedSize => _list.IsFixedSize;

		public bool IsSynchronized => true;

		public object this[int index]
		{
			get
			{
				lock (_root)
				{
					return _list[index];
				}
			}
			set
			{
				lock (_root)
				{
					_list[index] = value;
				}
			}
		}

		public object SyncRoot => _root;

		internal SyncIList(IList list)
		{
			_list = list;
			_root = list.SyncRoot;
		}

		public int Add(object value)
		{
			lock (_root)
			{
				return _list.Add(value);
			}
		}

		public void Clear()
		{
			lock (_root)
			{
				_list.Clear();
			}
		}

		public bool Contains(object item)
		{
			lock (_root)
			{
				return _list.Contains(item);
			}
		}

		public void CopyTo(Array array, int index)
		{
			lock (_root)
			{
				_list.CopyTo(array, index);
			}
		}

		public IEnumerator GetEnumerator()
		{
			lock (_root)
			{
				return _list.GetEnumerator();
			}
		}

		public int IndexOf(object value)
		{
			lock (_root)
			{
				return _list.IndexOf(value);
			}
		}

		public void Insert(int index, object value)
		{
			lock (_root)
			{
				_list.Insert(index, value);
			}
		}

		public void Remove(object value)
		{
			lock (_root)
			{
				_list.Remove(value);
			}
		}

		public void RemoveAt(int index)
		{
			lock (_root)
			{
				_list.RemoveAt(index);
			}
		}
	}

	private sealed class FixedSizeList : IList, ICollection, IEnumerable
	{
		private readonly IList _list;

		public int Count => _list.Count;

		public bool IsReadOnly => _list.IsReadOnly;

		public bool IsFixedSize => true;

		public bool IsSynchronized => _list.IsSynchronized;

		public object this[int index]
		{
			get
			{
				return _list[index];
			}
			set
			{
				_list[index] = value;
			}
		}

		public object SyncRoot => _list.SyncRoot;

		internal FixedSizeList(IList l)
		{
			_list = l;
		}

		public int Add(object obj)
		{
			throw new NotSupportedException(SR.NotSupported_FixedSizeCollection);
		}

		public void Clear()
		{
			throw new NotSupportedException(SR.NotSupported_FixedSizeCollection);
		}

		public bool Contains(object obj)
		{
			return _list.Contains(obj);
		}

		public void CopyTo(Array array, int index)
		{
			_list.CopyTo(array, index);
		}

		public IEnumerator GetEnumerator()
		{
			return _list.GetEnumerator();
		}

		public int IndexOf(object value)
		{
			return _list.IndexOf(value);
		}

		public void Insert(int index, object obj)
		{
			throw new NotSupportedException(SR.NotSupported_FixedSizeCollection);
		}

		public void Remove(object value)
		{
			throw new NotSupportedException(SR.NotSupported_FixedSizeCollection);
		}

		public void RemoveAt(int index)
		{
			throw new NotSupportedException(SR.NotSupported_FixedSizeCollection);
		}
	}

	private sealed class FixedSizeArrayList : ArrayList
	{
		private ArrayList _list;

		public override int Count => _list.Count;

		public override bool IsReadOnly => _list.IsReadOnly;

		public override bool IsFixedSize => true;

		public override bool IsSynchronized => _list.IsSynchronized;

		public override object this[int index]
		{
			get
			{
				return _list[index];
			}
			set
			{
				_list[index] = value;
				_version = _list._version;
			}
		}

		public override object SyncRoot => _list.SyncRoot;

		public override int Capacity
		{
			get
			{
				return _list.Capacity;
			}
			set
			{
				throw new NotSupportedException(SR.NotSupported_FixedSizeCollection);
			}
		}

		internal FixedSizeArrayList(ArrayList l)
		{
			_list = l;
			_version = _list._version;
		}

		public override int Add(object obj)
		{
			throw new NotSupportedException(SR.NotSupported_FixedSizeCollection);
		}

		public override void AddRange(ICollection c)
		{
			throw new NotSupportedException(SR.NotSupported_FixedSizeCollection);
		}

		public override int BinarySearch(int index, int count, object value, IComparer comparer)
		{
			return _list.BinarySearch(index, count, value, comparer);
		}

		public override void Clear()
		{
			throw new NotSupportedException(SR.NotSupported_FixedSizeCollection);
		}

		public override object Clone()
		{
			FixedSizeArrayList fixedSizeArrayList = new FixedSizeArrayList(_list);
			fixedSizeArrayList._list = (ArrayList)_list.Clone();
			return fixedSizeArrayList;
		}

		public override bool Contains(object obj)
		{
			return _list.Contains(obj);
		}

		public override void CopyTo(Array array, int index)
		{
			_list.CopyTo(array, index);
		}

		public override void CopyTo(int index, Array array, int arrayIndex, int count)
		{
			_list.CopyTo(index, array, arrayIndex, count);
		}

		public override IEnumerator GetEnumerator()
		{
			return _list.GetEnumerator();
		}

		public override IEnumerator GetEnumerator(int index, int count)
		{
			return _list.GetEnumerator(index, count);
		}

		public override int IndexOf(object value)
		{
			return _list.IndexOf(value);
		}

		public override int IndexOf(object value, int startIndex)
		{
			return _list.IndexOf(value, startIndex);
		}

		public override int IndexOf(object value, int startIndex, int count)
		{
			return _list.IndexOf(value, startIndex, count);
		}

		public override void Insert(int index, object obj)
		{
			throw new NotSupportedException(SR.NotSupported_FixedSizeCollection);
		}

		public override void InsertRange(int index, ICollection c)
		{
			throw new NotSupportedException(SR.NotSupported_FixedSizeCollection);
		}

		public override int LastIndexOf(object value)
		{
			return _list.LastIndexOf(value);
		}

		public override int LastIndexOf(object value, int startIndex)
		{
			return _list.LastIndexOf(value, startIndex);
		}

		public override int LastIndexOf(object value, int startIndex, int count)
		{
			return _list.LastIndexOf(value, startIndex, count);
		}

		public override void Remove(object value)
		{
			throw new NotSupportedException(SR.NotSupported_FixedSizeCollection);
		}

		public override void RemoveAt(int index)
		{
			throw new NotSupportedException(SR.NotSupported_FixedSizeCollection);
		}

		public override void RemoveRange(int index, int count)
		{
			throw new NotSupportedException(SR.NotSupported_FixedSizeCollection);
		}

		public override void SetRange(int index, ICollection c)
		{
			_list.SetRange(index, c);
			_version = _list._version;
		}

		public override ArrayList GetRange(int index, int count)
		{
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (Count - index < count)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			return new Range(this, index, count);
		}

		public override void Reverse(int index, int count)
		{
			_list.Reverse(index, count);
			_version = _list._version;
		}

		public override void Sort(int index, int count, IComparer comparer)
		{
			_list.Sort(index, count, comparer);
			_version = _list._version;
		}

		public override object[] ToArray()
		{
			return _list.ToArray();
		}

		public override Array ToArray(Type type)
		{
			return _list.ToArray(type);
		}

		public override void TrimToSize()
		{
			throw new NotSupportedException(SR.NotSupported_FixedSizeCollection);
		}
	}

	private sealed class ReadOnlyList : IList, ICollection, IEnumerable
	{
		private readonly IList _list;

		public int Count => _list.Count;

		public bool IsReadOnly => true;

		public bool IsFixedSize => true;

		public bool IsSynchronized => _list.IsSynchronized;

		public object this[int index]
		{
			get
			{
				return _list[index];
			}
			set
			{
				throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
			}
		}

		public object SyncRoot => _list.SyncRoot;

		internal ReadOnlyList(IList l)
		{
			_list = l;
		}

		public int Add(object obj)
		{
			throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
		}

		public void Clear()
		{
			throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
		}

		public bool Contains(object obj)
		{
			return _list.Contains(obj);
		}

		public void CopyTo(Array array, int index)
		{
			_list.CopyTo(array, index);
		}

		public IEnumerator GetEnumerator()
		{
			return _list.GetEnumerator();
		}

		public int IndexOf(object value)
		{
			return _list.IndexOf(value);
		}

		public void Insert(int index, object obj)
		{
			throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
		}

		public void Remove(object value)
		{
			throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
		}

		public void RemoveAt(int index)
		{
			throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
		}
	}

	private sealed class ReadOnlyArrayList : ArrayList
	{
		private ArrayList _list;

		public override int Count => _list.Count;

		public override bool IsReadOnly => true;

		public override bool IsFixedSize => true;

		public override bool IsSynchronized => _list.IsSynchronized;

		public override object this[int index]
		{
			get
			{
				return _list[index];
			}
			set
			{
				throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
			}
		}

		public override object SyncRoot => _list.SyncRoot;

		public override int Capacity
		{
			get
			{
				return _list.Capacity;
			}
			set
			{
				throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
			}
		}

		internal ReadOnlyArrayList(ArrayList l)
		{
			_list = l;
		}

		public override int Add(object obj)
		{
			throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
		}

		public override void AddRange(ICollection c)
		{
			throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
		}

		public override int BinarySearch(int index, int count, object value, IComparer comparer)
		{
			return _list.BinarySearch(index, count, value, comparer);
		}

		public override void Clear()
		{
			throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
		}

		public override object Clone()
		{
			ReadOnlyArrayList readOnlyArrayList = new ReadOnlyArrayList(_list);
			readOnlyArrayList._list = (ArrayList)_list.Clone();
			return readOnlyArrayList;
		}

		public override bool Contains(object obj)
		{
			return _list.Contains(obj);
		}

		public override void CopyTo(Array array, int index)
		{
			_list.CopyTo(array, index);
		}

		public override void CopyTo(int index, Array array, int arrayIndex, int count)
		{
			_list.CopyTo(index, array, arrayIndex, count);
		}

		public override IEnumerator GetEnumerator()
		{
			return _list.GetEnumerator();
		}

		public override IEnumerator GetEnumerator(int index, int count)
		{
			return _list.GetEnumerator(index, count);
		}

		public override int IndexOf(object value)
		{
			return _list.IndexOf(value);
		}

		public override int IndexOf(object value, int startIndex)
		{
			return _list.IndexOf(value, startIndex);
		}

		public override int IndexOf(object value, int startIndex, int count)
		{
			return _list.IndexOf(value, startIndex, count);
		}

		public override void Insert(int index, object obj)
		{
			throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
		}

		public override void InsertRange(int index, ICollection c)
		{
			throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
		}

		public override int LastIndexOf(object value)
		{
			return _list.LastIndexOf(value);
		}

		public override int LastIndexOf(object value, int startIndex)
		{
			return _list.LastIndexOf(value, startIndex);
		}

		public override int LastIndexOf(object value, int startIndex, int count)
		{
			return _list.LastIndexOf(value, startIndex, count);
		}

		public override void Remove(object value)
		{
			throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
		}

		public override void RemoveAt(int index)
		{
			throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
		}

		public override void RemoveRange(int index, int count)
		{
			throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
		}

		public override void SetRange(int index, ICollection c)
		{
			throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
		}

		public override ArrayList GetRange(int index, int count)
		{
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (Count - index < count)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			return new Range(this, index, count);
		}

		public override void Reverse(int index, int count)
		{
			throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
		}

		public override void Sort(int index, int count, IComparer comparer)
		{
			throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
		}

		public override object[] ToArray()
		{
			return _list.ToArray();
		}

		public override Array ToArray(Type type)
		{
			return _list.ToArray(type);
		}

		public override void TrimToSize()
		{
			throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
		}
	}

	private sealed class ArrayListEnumerator : IEnumerator, ICloneable
	{
		private readonly ArrayList _list;

		private int _index;

		private readonly int _endIndex;

		private readonly int _version;

		private object _currentElement;

		private readonly int _startIndex;

		public object Current
		{
			get
			{
				if (_index < _startIndex)
				{
					throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
				}
				if (_index > _endIndex)
				{
					throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
				}
				return _currentElement;
			}
		}

		internal ArrayListEnumerator(ArrayList list, int index, int count)
		{
			_list = list;
			_startIndex = index;
			_index = index - 1;
			_endIndex = _index + count;
			_version = list._version;
			_currentElement = null;
		}

		public object Clone()
		{
			return MemberwiseClone();
		}

		public bool MoveNext()
		{
			if (_version != _list._version)
			{
				throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
			}
			if (_index < _endIndex)
			{
				_currentElement = _list[++_index];
				return true;
			}
			_index = _endIndex + 1;
			return false;
		}

		public void Reset()
		{
			if (_version != _list._version)
			{
				throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
			}
			_index = _startIndex - 1;
		}
	}

	private sealed class Range : ArrayList
	{
		private ArrayList _baseList;

		private readonly int _baseIndex;

		private int _baseSize;

		private int _baseVersion;

		public override int Capacity
		{
			get
			{
				return _baseList.Capacity;
			}
			set
			{
				if (value < Count)
				{
					throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_SmallCapacity);
				}
			}
		}

		public override int Count
		{
			get
			{
				InternalUpdateRange();
				return _baseSize;
			}
		}

		public override bool IsReadOnly => _baseList.IsReadOnly;

		public override bool IsFixedSize => _baseList.IsFixedSize;

		public override bool IsSynchronized => _baseList.IsSynchronized;

		public override object SyncRoot => _baseList.SyncRoot;

		public override object this[int index]
		{
			get
			{
				InternalUpdateRange();
				if (index < 0 || index >= _baseSize)
				{
					throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
				}
				return _baseList[_baseIndex + index];
			}
			set
			{
				InternalUpdateRange();
				if (index < 0 || index >= _baseSize)
				{
					throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
				}
				_baseList[_baseIndex + index] = value;
				InternalUpdateVersion();
			}
		}

		internal Range(ArrayList list, int index, int count)
		{
			_baseList = list;
			_baseIndex = index;
			_baseSize = count;
			_baseVersion = list._version;
			_version = list._version;
		}

		private void InternalUpdateRange()
		{
			if (_baseVersion != _baseList._version)
			{
				throw new InvalidOperationException(SR.InvalidOperation_UnderlyingArrayListChanged);
			}
		}

		private void InternalUpdateVersion()
		{
			_baseVersion++;
			_version++;
		}

		public override int Add(object value)
		{
			InternalUpdateRange();
			_baseList.Insert(_baseIndex + _baseSize, value);
			InternalUpdateVersion();
			return _baseSize++;
		}

		public override void AddRange(ICollection c)
		{
			if (c == null)
			{
				throw new ArgumentNullException("c");
			}
			InternalUpdateRange();
			int count = c.Count;
			if (count > 0)
			{
				_baseList.InsertRange(_baseIndex + _baseSize, c);
				InternalUpdateVersion();
				_baseSize += count;
			}
		}

		public override int BinarySearch(int index, int count, object value, IComparer comparer)
		{
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (_baseSize - index < count)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			InternalUpdateRange();
			int num = _baseList.BinarySearch(_baseIndex + index, count, value, comparer);
			if (num >= 0)
			{
				return num - _baseIndex;
			}
			return num + _baseIndex;
		}

		public override void Clear()
		{
			InternalUpdateRange();
			if (_baseSize != 0)
			{
				_baseList.RemoveRange(_baseIndex, _baseSize);
				InternalUpdateVersion();
				_baseSize = 0;
			}
		}

		public override object Clone()
		{
			InternalUpdateRange();
			Range range = new Range(_baseList, _baseIndex, _baseSize);
			range._baseList = (ArrayList)_baseList.Clone();
			return range;
		}

		public override bool Contains(object item)
		{
			InternalUpdateRange();
			if (item == null)
			{
				for (int i = 0; i < _baseSize; i++)
				{
					if (_baseList[_baseIndex + i] == null)
					{
						return true;
					}
				}
				return false;
			}
			for (int j = 0; j < _baseSize; j++)
			{
				object obj = _baseList[_baseIndex + j];
				if (obj != null && obj.Equals(item))
				{
					return true;
				}
			}
			return false;
		}

		public override void CopyTo(Array array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (array.Rank != 1)
			{
				throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, "array");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (array.Length - index < _baseSize)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			InternalUpdateRange();
			_baseList.CopyTo(_baseIndex, array, index, _baseSize);
		}

		public override void CopyTo(int index, Array array, int arrayIndex, int count)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (array.Rank != 1)
			{
				throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, "array");
			}
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (array.Length - arrayIndex < count)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			if (_baseSize - index < count)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			InternalUpdateRange();
			_baseList.CopyTo(_baseIndex + index, array, arrayIndex, count);
		}

		public override IEnumerator GetEnumerator()
		{
			return GetEnumerator(0, _baseSize);
		}

		public override IEnumerator GetEnumerator(int index, int count)
		{
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (_baseSize - index < count)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			InternalUpdateRange();
			return _baseList.GetEnumerator(_baseIndex + index, count);
		}

		public override ArrayList GetRange(int index, int count)
		{
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (_baseSize - index < count)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			InternalUpdateRange();
			return new Range(this, index, count);
		}

		public override int IndexOf(object value)
		{
			InternalUpdateRange();
			int num = _baseList.IndexOf(value, _baseIndex, _baseSize);
			if (num >= 0)
			{
				return num - _baseIndex;
			}
			return -1;
		}

		public override int IndexOf(object value, int startIndex)
		{
			if (startIndex < 0)
			{
				throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (startIndex > _baseSize)
			{
				throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_Index);
			}
			InternalUpdateRange();
			int num = _baseList.IndexOf(value, _baseIndex + startIndex, _baseSize - startIndex);
			if (num >= 0)
			{
				return num - _baseIndex;
			}
			return -1;
		}

		public override int IndexOf(object value, int startIndex, int count)
		{
			if (startIndex < 0 || startIndex > _baseSize)
			{
				throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_Index);
			}
			if (count < 0 || startIndex > _baseSize - count)
			{
				throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_Count);
			}
			InternalUpdateRange();
			int num = _baseList.IndexOf(value, _baseIndex + startIndex, count);
			if (num >= 0)
			{
				return num - _baseIndex;
			}
			return -1;
		}

		public override void Insert(int index, object value)
		{
			if (index < 0 || index > _baseSize)
			{
				throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
			}
			InternalUpdateRange();
			_baseList.Insert(_baseIndex + index, value);
			InternalUpdateVersion();
			_baseSize++;
		}

		public override void InsertRange(int index, ICollection c)
		{
			if (index < 0 || index > _baseSize)
			{
				throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
			}
			if (c == null)
			{
				throw new ArgumentNullException("c");
			}
			InternalUpdateRange();
			int count = c.Count;
			if (count > 0)
			{
				_baseList.InsertRange(_baseIndex + index, c);
				_baseSize += count;
				InternalUpdateVersion();
			}
		}

		public override int LastIndexOf(object value)
		{
			InternalUpdateRange();
			int num = _baseList.LastIndexOf(value, _baseIndex + _baseSize - 1, _baseSize);
			if (num >= 0)
			{
				return num - _baseIndex;
			}
			return -1;
		}

		public override int LastIndexOf(object value, int startIndex)
		{
			return LastIndexOf(value, startIndex, startIndex + 1);
		}

		public override int LastIndexOf(object value, int startIndex, int count)
		{
			InternalUpdateRange();
			if (_baseSize == 0)
			{
				return -1;
			}
			if (startIndex >= _baseSize)
			{
				throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_Index);
			}
			if (startIndex < 0)
			{
				throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			int num = _baseList.LastIndexOf(value, _baseIndex + startIndex, count);
			if (num >= 0)
			{
				return num - _baseIndex;
			}
			return -1;
		}

		public override void RemoveAt(int index)
		{
			if (index < 0 || index >= _baseSize)
			{
				throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
			}
			InternalUpdateRange();
			_baseList.RemoveAt(_baseIndex + index);
			InternalUpdateVersion();
			_baseSize--;
		}

		public override void RemoveRange(int index, int count)
		{
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (_baseSize - index < count)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			InternalUpdateRange();
			if (count > 0)
			{
				_baseList.RemoveRange(_baseIndex + index, count);
				InternalUpdateVersion();
				_baseSize -= count;
			}
		}

		public override void Reverse(int index, int count)
		{
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (_baseSize - index < count)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			InternalUpdateRange();
			_baseList.Reverse(_baseIndex + index, count);
			InternalUpdateVersion();
		}

		public override void SetRange(int index, ICollection c)
		{
			InternalUpdateRange();
			if (index < 0 || index >= _baseSize)
			{
				throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
			}
			_baseList.SetRange(_baseIndex + index, c);
			if (c.Count > 0)
			{
				InternalUpdateVersion();
			}
		}

		public override void Sort(int index, int count, IComparer comparer)
		{
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (_baseSize - index < count)
			{
				throw new ArgumentException(SR.Argument_InvalidOffLen);
			}
			InternalUpdateRange();
			_baseList.Sort(_baseIndex + index, count, comparer);
			InternalUpdateVersion();
		}

		public override object[] ToArray()
		{
			InternalUpdateRange();
			if (_baseSize == 0)
			{
				return Array.Empty<object>();
			}
			object[] array = new object[_baseSize];
			_baseList.CopyTo(_baseIndex, array, 0, _baseSize);
			return array;
		}

		public override Array ToArray(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			InternalUpdateRange();
			Array array = Array.CreateInstance(type, _baseSize);
			_baseList.CopyTo(_baseIndex, array, 0, _baseSize);
			return array;
		}

		public override void TrimToSize()
		{
			throw new NotSupportedException(SR.NotSupported_RangeCollection);
		}
	}

	private sealed class ArrayListEnumeratorSimple : IEnumerator, ICloneable
	{
		private readonly ArrayList _list;

		private int _index;

		private readonly int _version;

		private object _currentElement;

		private readonly bool _isArrayList;

		private static readonly object s_dummyObject = new object();

		public object Current
		{
			get
			{
				object currentElement = _currentElement;
				if (s_dummyObject == currentElement)
				{
					if (_index == -1)
					{
						throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
					}
					throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
				}
				return currentElement;
			}
		}

		internal ArrayListEnumeratorSimple(ArrayList list)
		{
			_list = list;
			_index = -1;
			_version = list._version;
			_isArrayList = list.GetType() == typeof(ArrayList);
			_currentElement = s_dummyObject;
		}

		public object Clone()
		{
			return MemberwiseClone();
		}

		public bool MoveNext()
		{
			if (_version != _list._version)
			{
				throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
			}
			if (_isArrayList)
			{
				if (_index < _list._size - 1)
				{
					_currentElement = _list._items[++_index];
					return true;
				}
				_currentElement = s_dummyObject;
				_index = _list._size;
				return false;
			}
			if (_index < _list.Count - 1)
			{
				_currentElement = _list[++_index];
				return true;
			}
			_index = _list.Count;
			_currentElement = s_dummyObject;
			return false;
		}

		public void Reset()
		{
			if (_version != _list._version)
			{
				throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
			}
			_currentElement = s_dummyObject;
			_index = -1;
		}
	}

	internal sealed class ArrayListDebugView
	{
		private readonly ArrayList _arrayList;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public object[] Items => _arrayList.ToArray();

		public ArrayListDebugView(ArrayList arrayList)
		{
			if (arrayList == null)
			{
				throw new ArgumentNullException("arrayList");
			}
			_arrayList = arrayList;
		}
	}

	private object[] _items;

	private int _size;

	private int _version;

	public virtual int Capacity
	{
		get
		{
			return _items.Length;
		}
		set
		{
			if (value < _size)
			{
				throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_SmallCapacity);
			}
			if (value == _items.Length)
			{
				return;
			}
			if (value > 0)
			{
				object[] array = new object[value];
				if (_size > 0)
				{
					Array.Copy(_items, array, _size);
				}
				_items = array;
			}
			else
			{
				_items = new object[4];
			}
		}
	}

	public virtual int Count => _size;

	public virtual bool IsFixedSize => false;

	public virtual bool IsReadOnly => false;

	public virtual bool IsSynchronized => false;

	public virtual object SyncRoot => this;

	public virtual object? this[int index]
	{
		get
		{
			if (index < 0 || index >= _size)
			{
				throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
			}
			return _items[index];
		}
		set
		{
			if (index < 0 || index >= _size)
			{
				throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
			}
			_items[index] = value;
			_version++;
		}
	}

	public ArrayList()
	{
		_items = Array.Empty<object>();
	}

	public ArrayList(int capacity)
	{
		if (capacity < 0)
		{
			throw new ArgumentOutOfRangeException("capacity", SR.Format(SR.ArgumentOutOfRange_MustBeNonNegNum, "capacity"));
		}
		if (capacity == 0)
		{
			_items = Array.Empty<object>();
		}
		else
		{
			_items = new object[capacity];
		}
	}

	public ArrayList(ICollection c)
	{
		if (c == null)
		{
			throw new ArgumentNullException("c", SR.ArgumentNull_Collection);
		}
		int count = c.Count;
		if (count == 0)
		{
			_items = Array.Empty<object>();
			return;
		}
		_items = new object[count];
		AddRange(c);
	}

	public static ArrayList Adapter(IList list)
	{
		if (list == null)
		{
			throw new ArgumentNullException("list");
		}
		return new IListWrapper(list);
	}

	public virtual int Add(object? value)
	{
		if (_size == _items.Length)
		{
			EnsureCapacity(_size + 1);
		}
		_items[_size] = value;
		_version++;
		return _size++;
	}

	public virtual void AddRange(ICollection c)
	{
		InsertRange(_size, c);
	}

	public virtual int BinarySearch(int index, int count, object? value, IComparer? comparer)
	{
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (_size - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		return Array.BinarySearch(_items, index, count, value, comparer);
	}

	public virtual int BinarySearch(object? value)
	{
		return BinarySearch(0, Count, value, null);
	}

	public virtual int BinarySearch(object? value, IComparer? comparer)
	{
		return BinarySearch(0, Count, value, comparer);
	}

	public virtual void Clear()
	{
		if (_size > 0)
		{
			Array.Clear(_items, 0, _size);
			_size = 0;
		}
		_version++;
	}

	public virtual object Clone()
	{
		ArrayList arrayList = new ArrayList(_size);
		arrayList._size = _size;
		arrayList._version = _version;
		Array.Copy(_items, arrayList._items, _size);
		return arrayList;
	}

	public virtual bool Contains(object? item)
	{
		if (item == null)
		{
			for (int i = 0; i < _size; i++)
			{
				if (_items[i] == null)
				{
					return true;
				}
			}
			return false;
		}
		for (int j = 0; j < _size; j++)
		{
			object obj = _items[j];
			if (obj != null && obj.Equals(item))
			{
				return true;
			}
		}
		return false;
	}

	public virtual void CopyTo(Array array)
	{
		CopyTo(array, 0);
	}

	public virtual void CopyTo(Array array, int arrayIndex)
	{
		if (array != null && array.Rank != 1)
		{
			throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, "array");
		}
		Array.Copy(_items, 0, array, arrayIndex, _size);
	}

	public virtual void CopyTo(int index, Array array, int arrayIndex, int count)
	{
		if (_size - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		if (array != null && array.Rank != 1)
		{
			throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, "array");
		}
		Array.Copy(_items, index, array, arrayIndex, count);
	}

	private void EnsureCapacity(int min)
	{
		if (_items.Length < min)
		{
			int num = ((_items.Length == 0) ? 4 : (_items.Length * 2));
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
	}

	public static IList FixedSize(IList list)
	{
		if (list == null)
		{
			throw new ArgumentNullException("list");
		}
		return new FixedSizeList(list);
	}

	public static ArrayList FixedSize(ArrayList list)
	{
		if (list == null)
		{
			throw new ArgumentNullException("list");
		}
		return new FixedSizeArrayList(list);
	}

	public virtual IEnumerator GetEnumerator()
	{
		return new ArrayListEnumeratorSimple(this);
	}

	public virtual IEnumerator GetEnumerator(int index, int count)
	{
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (_size - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		return new ArrayListEnumerator(this, index, count);
	}

	public virtual int IndexOf(object? value)
	{
		return Array.IndexOf(_items, value, 0, _size);
	}

	public virtual int IndexOf(object? value, int startIndex)
	{
		if (startIndex > _size)
		{
			throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_Index);
		}
		return Array.IndexOf(_items, value, startIndex, _size - startIndex);
	}

	public virtual int IndexOf(object? value, int startIndex, int count)
	{
		if (startIndex > _size)
		{
			throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_Index);
		}
		if (count < 0 || startIndex > _size - count)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_Count);
		}
		return Array.IndexOf(_items, value, startIndex, count);
	}

	public virtual void Insert(int index, object? value)
	{
		if (index < 0 || index > _size)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
		}
		if (_size == _items.Length)
		{
			EnsureCapacity(_size + 1);
		}
		if (index < _size)
		{
			Array.Copy(_items, index, _items, index + 1, _size - index);
		}
		_items[index] = value;
		_size++;
		_version++;
	}

	public virtual void InsertRange(int index, ICollection c)
	{
		if (c == null)
		{
			throw new ArgumentNullException("c", SR.ArgumentNull_Collection);
		}
		if (index < 0 || index > _size)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
		}
		int count = c.Count;
		if (count > 0)
		{
			EnsureCapacity(_size + count);
			if (index < _size)
			{
				Array.Copy(_items, index, _items, index + count, _size - index);
			}
			object[] array = new object[count];
			c.CopyTo(array, 0);
			array.CopyTo(_items, index);
			_size += count;
			_version++;
		}
	}

	public virtual int LastIndexOf(object? value)
	{
		return LastIndexOf(value, _size - 1, _size);
	}

	public virtual int LastIndexOf(object? value, int startIndex)
	{
		if (startIndex >= _size)
		{
			throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_Index);
		}
		return LastIndexOf(value, startIndex, startIndex + 1);
	}

	public virtual int LastIndexOf(object? value, int startIndex, int count)
	{
		if (Count != 0 && (startIndex < 0 || count < 0))
		{
			throw new ArgumentOutOfRangeException((startIndex < 0) ? "startIndex" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (_size == 0)
		{
			return -1;
		}
		if (startIndex >= _size || count > startIndex + 1)
		{
			throw new ArgumentOutOfRangeException((startIndex >= _size) ? "startIndex" : "count", SR.ArgumentOutOfRange_BiggerThanCollection);
		}
		return Array.LastIndexOf(_items, value, startIndex, count);
	}

	public static IList ReadOnly(IList list)
	{
		if (list == null)
		{
			throw new ArgumentNullException("list");
		}
		return new ReadOnlyList(list);
	}

	public static ArrayList ReadOnly(ArrayList list)
	{
		if (list == null)
		{
			throw new ArgumentNullException("list");
		}
		return new ReadOnlyArrayList(list);
	}

	public virtual void Remove(object? obj)
	{
		int num = IndexOf(obj);
		if (num >= 0)
		{
			RemoveAt(num);
		}
	}

	public virtual void RemoveAt(int index)
	{
		if (index < 0 || index >= _size)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
		}
		_size--;
		if (index < _size)
		{
			Array.Copy(_items, index + 1, _items, index, _size - index);
		}
		_items[_size] = null;
		_version++;
	}

	public virtual void RemoveRange(int index, int count)
	{
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (_size - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		if (count > 0)
		{
			int num = _size;
			_size -= count;
			if (index < _size)
			{
				Array.Copy(_items, index + count, _items, index, _size - index);
			}
			while (num > _size)
			{
				_items[--num] = null;
			}
			_version++;
		}
	}

	public static ArrayList Repeat(object? value, int count)
	{
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		ArrayList arrayList = new ArrayList((count > 4) ? count : 4);
		for (int i = 0; i < count; i++)
		{
			arrayList.Add(value);
		}
		return arrayList;
	}

	public virtual void Reverse()
	{
		Reverse(0, Count);
	}

	public virtual void Reverse(int index, int count)
	{
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (_size - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		Array.Reverse(_items, index, count);
		_version++;
	}

	public virtual void SetRange(int index, ICollection c)
	{
		if (c == null)
		{
			throw new ArgumentNullException("c", SR.ArgumentNull_Collection);
		}
		int count = c.Count;
		if (index < 0 || index > _size - count)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
		}
		if (count > 0)
		{
			c.CopyTo(_items, index);
			_version++;
		}
	}

	public virtual ArrayList GetRange(int index, int count)
	{
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (_size - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		return new Range(this, index, count);
	}

	public virtual void Sort()
	{
		Sort(0, Count, Comparer.Default);
	}

	public virtual void Sort(IComparer? comparer)
	{
		Sort(0, Count, comparer);
	}

	public virtual void Sort(int index, int count, IComparer? comparer)
	{
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (_size - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		Array.Sort(_items, index, count, comparer);
		_version++;
	}

	public static IList Synchronized(IList list)
	{
		if (list == null)
		{
			throw new ArgumentNullException("list");
		}
		return new SyncIList(list);
	}

	public static ArrayList Synchronized(ArrayList list)
	{
		if (list == null)
		{
			throw new ArgumentNullException("list");
		}
		return new SyncArrayList(list);
	}

	public virtual object?[] ToArray()
	{
		if (_size == 0)
		{
			return Array.Empty<object>();
		}
		object[] array = new object[_size];
		Array.Copy(_items, array, _size);
		return array;
	}

	public virtual Array ToArray(Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		Array array = Array.CreateInstance(type, _size);
		Array.Copy(_items, array, _size);
		return array;
	}

	public virtual void TrimToSize()
	{
		Capacity = _size;
	}
}
