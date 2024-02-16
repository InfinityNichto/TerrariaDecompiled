using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.Http.Headers;

[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(ObjectCollection<>.DebugView))]
internal sealed class ObjectCollection<T> : ICollection<T>, IEnumerable<T>, IEnumerable where T : class
{
	public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
	{
		private readonly ObjectCollection<T> _list;

		private int _index;

		private T _current;

		public T Current => _current;

		object IEnumerator.Current => _current;

		internal Enumerator(ObjectCollection<T> list)
		{
			_list = list;
			_index = 0;
			_current = null;
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			ObjectCollection<T> list = _list;
			if ((uint)_index < (uint)list._size)
			{
				_current = ((list._items is T[] array) ? array[_index] : ((T)list._items));
				_index++;
				return true;
			}
			_index = _list._size + 1;
			_current = null;
			return false;
		}

		void IEnumerator.Reset()
		{
			_index = 0;
			_current = null;
		}
	}

	internal sealed class DebugView
	{
		private readonly ObjectCollection<T> _collection;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get
			{
				T[] array = new T[_collection.Count];
				_collection.CopyTo(array, 0);
				return array;
			}
		}

		public DebugView(ObjectCollection<T> collection)
		{
			_collection = collection ?? throw new ArgumentNullException("collection");
		}
	}

	private readonly Action<T> _validator;

	internal object _items;

	internal int _size;

	public int Count => _size;

	public bool IsReadOnly => false;

	public ObjectCollection()
	{
	}

	public ObjectCollection(Action<T> validator)
	{
		_validator = validator;
	}

	public void Add(T item)
	{
		if (_validator == null)
		{
			if (item == null)
			{
				throw new ArgumentNullException("item");
			}
		}
		else
		{
			_validator(item);
		}
		if (_items == null)
		{
			_items = item;
			_size = 1;
			return;
		}
		if (_items is T val)
		{
			_items = new T[4] { val, item, null, null };
			_size = 2;
			return;
		}
		T[] array = (T[])_items;
		int size = _size;
		if ((uint)size < (uint)array.Length)
		{
			array[size] = item;
		}
		else
		{
			T[] array2 = new T[array.Length * 2];
			Array.Copy(array, array2, size);
			_items = array2;
			array2[size] = item;
		}
		_size = size + 1;
	}

	public void Clear()
	{
		_items = null;
		_size = 0;
	}

	public bool Contains(T item)
	{
		if (_size > 0)
		{
			if (!(_items is T val))
			{
				if (_items is T[] array)
				{
					return Array.IndexOf(array, item, 0, _size) != -1;
				}
				return false;
			}
			return val.Equals(item);
		}
		return false;
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		if (_items is T[] sourceArray)
		{
			Array.Copy(sourceArray, 0, array, arrayIndex, _size);
		}
		else if (array == null || _size > array.Length - arrayIndex)
		{
			new T[1] { (T)_items }.CopyTo(array, arrayIndex);
		}
		else if (_size == 1)
		{
			array[arrayIndex] = (T)_items;
		}
	}

	public bool Remove(T item)
	{
		if (_items is T val)
		{
			if (val.Equals(item))
			{
				_items = null;
				_size = 0;
				return true;
			}
		}
		else if (_items is T[] array)
		{
			int num = Array.IndexOf(array, item, 0, _size);
			if (num != -1)
			{
				_size--;
				if (num < _size)
				{
					Array.Copy(array, num + 1, array, num, _size - num);
				}
				array[_size] = null;
				return true;
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
}
