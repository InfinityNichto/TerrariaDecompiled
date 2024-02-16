using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public class XmlQuerySequence<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IList, ICollection
{
	public static readonly XmlQuerySequence<T> Empty = new XmlQuerySequence<T>();

	private T[] _items;

	private int _size;

	public int Count => _size;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	bool ICollection<T>.IsReadOnly => true;

	bool IList.IsFixedSize => true;

	bool IList.IsReadOnly => true;

	object IList.this[int index]
	{
		get
		{
			if (index >= _size)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			return _items[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public T this[int index]
	{
		get
		{
			if (index >= _size)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			return _items[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public static XmlQuerySequence<T> CreateOrReuse(XmlQuerySequence<T> seq)
	{
		if (seq != null)
		{
			seq.Clear();
			return seq;
		}
		return new XmlQuerySequence<T>();
	}

	public static XmlQuerySequence<T> CreateOrReuse(XmlQuerySequence<T> seq, T item)
	{
		if (seq != null)
		{
			seq.Clear();
			seq.Add(item);
			return seq;
		}
		return new XmlQuerySequence<T>(item);
	}

	public XmlQuerySequence()
	{
		_items = new T[16];
	}

	public XmlQuerySequence(int capacity)
	{
		_items = new T[capacity];
	}

	public XmlQuerySequence(T[] array, int size)
	{
		_items = array;
		_size = size;
	}

	public XmlQuerySequence(T value)
	{
		_items = new T[1];
		_items[0] = value;
		_size = 1;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new IListEnumerator<T>(this);
	}

	public IEnumerator<T> GetEnumerator()
	{
		return new IListEnumerator<T>(this);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		if (_size != 0)
		{
			Array.Copy(_items, 0, array, index, _size);
		}
	}

	void ICollection<T>.Add(T value)
	{
		throw new NotSupportedException();
	}

	void ICollection<T>.Clear()
	{
		throw new NotSupportedException();
	}

	public bool Contains(T value)
	{
		return IndexOf(value) != -1;
	}

	public void CopyTo(T[] array, int index)
	{
		for (int i = 0; i < Count; i++)
		{
			array[index + i] = this[i];
		}
	}

	bool ICollection<T>.Remove(T value)
	{
		throw new NotSupportedException();
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
		return Contains((T)value);
	}

	int IList.IndexOf(object value)
	{
		return IndexOf((T)value);
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

	public int IndexOf(T value)
	{
		int num = Array.IndexOf(_items, value);
		if (num >= _size)
		{
			return -1;
		}
		return num;
	}

	void IList<T>.Insert(int index, T value)
	{
		throw new NotSupportedException();
	}

	void IList<T>.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	public void Clear()
	{
		_size = 0;
		OnItemsChanged();
	}

	public void Add(T value)
	{
		EnsureCache();
		_items[_size++] = value;
		OnItemsChanged();
	}

	public void SortByKeys(Array keys)
	{
		if (_size > 1)
		{
			Array.Sort(keys, _items, 0, _size);
			OnItemsChanged();
		}
	}

	private void EnsureCache()
	{
		if (_size >= _items.Length)
		{
			T[] array = new T[_size * 2];
			CopyTo(array, 0);
			_items = array;
		}
	}

	protected virtual void OnItemsChanged()
	{
	}
}
