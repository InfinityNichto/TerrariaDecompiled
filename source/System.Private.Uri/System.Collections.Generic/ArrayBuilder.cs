using System.Reflection;

namespace System.Collections.Generic;

[DefaultMember("Item")]
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

	public void Add(T item)
	{
		if (_count == Capacity)
		{
			EnsureCapacity(_count + 1);
		}
		UncheckedAdd(item);
	}

	public T[] ToArray()
	{
		if (_count == 0)
		{
			return Array.Empty<T>();
		}
		T[] array = _array;
		if (_count < array.Length)
		{
			array = new T[_count];
			Array.Copy(_array, array, _count);
		}
		return array;
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
