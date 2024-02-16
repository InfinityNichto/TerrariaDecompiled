namespace System.Collections.Generic;

internal struct ArrayBuilder<T>
{
	private T[] _array;

	private int _count;

	public int Capacity
	{
		get
		{
			T[] array = _array;
			if (array == null)
			{
				return 0;
			}
			return array.Length;
		}
	}

	public int Count => _count;

	public T this[int index] => _array[index];

	public void Add(T item)
	{
		if (_count == Capacity)
		{
			EnsureCapacity(_count + 1);
		}
		UncheckedAdd(item);
	}

	public T First()
	{
		return _array[0];
	}

	public T Last()
	{
		return _array[_count - 1];
	}

	public void UncheckedAdd(T item)
	{
		_array[_count++] = item;
	}

	private void EnsureCapacity(int minimum)
	{
		int capacity = Capacity;
		int num = ((capacity == 0) ? 4 : (2 * capacity));
		if ((uint)num > (uint)Array.MaxLength)
		{
			num = Math.Max(capacity + 1, Array.MaxLength);
		}
		num = Math.Max(num, minimum);
		T[] array = new T[num];
		if (_count > 0)
		{
			Array.Copy(_array, array, _count);
		}
		_array = array;
	}
}
