using System.Collections;
using System.Collections.Generic;

namespace System.Xml.Xsl;

internal abstract class ListBase<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IList, ICollection
{
	public abstract int Count { get; }

	public abstract T this[int index] { get; set; }

	public virtual bool IsFixedSize => true;

	public virtual bool IsReadOnly => true;

	bool ICollection.IsSynchronized => IsReadOnly;

	object ICollection.SyncRoot => this;

	object IList.this[int index]
	{
		get
		{
			return this[index];
		}
		set
		{
			if (!IsCompatibleType(value.GetType()))
			{
				throw new ArgumentException(System.SR.Arg_IncompatibleParamType, "value");
			}
			this[index] = (T)value;
		}
	}

	public virtual bool Contains(T value)
	{
		return IndexOf(value) != -1;
	}

	public virtual int IndexOf(T value)
	{
		for (int i = 0; i < Count; i++)
		{
			if (value.Equals(this[i]))
			{
				return i;
			}
		}
		return -1;
	}

	public virtual void CopyTo(T[] array, int index)
	{
		for (int i = 0; i < Count; i++)
		{
			array[index + i] = this[i];
		}
	}

	public virtual IListEnumerator<T> GetEnumerator()
	{
		return new IListEnumerator<T>(this);
	}

	public virtual void Add(T value)
	{
		Insert(Count, value);
	}

	public virtual void Insert(int index, T value)
	{
		throw new NotSupportedException();
	}

	public virtual bool Remove(T value)
	{
		int num = IndexOf(value);
		if (num >= 0)
		{
			RemoveAt(num);
			return true;
		}
		return false;
	}

	public virtual void RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	public virtual void Clear()
	{
		for (int num = Count - 1; num >= 0; num--)
		{
			RemoveAt(num);
		}
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return new IListEnumerator<T>(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new IListEnumerator<T>(this);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		for (int i = 0; i < Count; i++)
		{
			array.SetValue(this[i], index);
		}
	}

	int IList.Add(object value)
	{
		if (!IsCompatibleType(value.GetType()))
		{
			throw new ArgumentException(System.SR.Arg_IncompatibleParamType, "value");
		}
		Add((T)value);
		return Count - 1;
	}

	void IList.Clear()
	{
		Clear();
	}

	bool IList.Contains(object value)
	{
		if (!IsCompatibleType(value.GetType()))
		{
			return false;
		}
		return Contains((T)value);
	}

	int IList.IndexOf(object value)
	{
		if (!IsCompatibleType(value.GetType()))
		{
			return -1;
		}
		return IndexOf((T)value);
	}

	void IList.Insert(int index, object value)
	{
		if (!IsCompatibleType(value.GetType()))
		{
			throw new ArgumentException(System.SR.Arg_IncompatibleParamType, "value");
		}
		Insert(index, (T)value);
	}

	void IList.Remove(object value)
	{
		if (IsCompatibleType(value.GetType()))
		{
			Remove((T)value);
		}
	}

	private static bool IsCompatibleType(object value)
	{
		if ((value == null && !typeof(T).IsValueType) || value is T)
		{
			return true;
		}
		return false;
	}
}
