using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

internal struct LargeArrayBuilder<T>
{
	private readonly int _maxCapacity;

	private T[] _first;

	private System.Collections.Generic.ArrayBuilder<T[]> _buffers;

	private T[] _current;

	private int _index;

	private int _count;

	public int Count => _count;

	public LargeArrayBuilder(bool initialize)
		: this(int.MaxValue)
	{
	}

	public LargeArrayBuilder(int maxCapacity)
	{
		this = default(LargeArrayBuilder<T>);
		_first = (_current = Array.Empty<T>());
		_maxCapacity = maxCapacity;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(T item)
	{
		int index = _index;
		T[] current = _current;
		if ((uint)index >= (uint)current.Length)
		{
			AddWithBufferAllocation(item);
		}
		else
		{
			current[index] = item;
			_index = index + 1;
		}
		_count++;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void AddWithBufferAllocation(T item)
	{
		AllocateBuffer();
		_current[_index++] = item;
	}

	public void AddRange(IEnumerable<T> items)
	{
		using IEnumerator<T> enumerator = items.GetEnumerator();
		T[] destination = _current;
		int index = _index;
		while (enumerator.MoveNext())
		{
			T current = enumerator.Current;
			if ((uint)index >= (uint)destination.Length)
			{
				AddWithBufferAllocation(current, ref destination, ref index);
			}
			else
			{
				destination[index] = current;
			}
			index++;
		}
		_count += index - _index;
		_index = index;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void AddWithBufferAllocation(T item, ref T[] destination, ref int index)
	{
		_count += index - _index;
		_index = index;
		AllocateBuffer();
		destination = _current;
		index = _index;
		_current[index] = item;
	}

	public void CopyTo(T[] array, int arrayIndex, int count)
	{
		int num = 0;
		while (count > 0)
		{
			T[] buffer = GetBuffer(num);
			int num2 = Math.Min(count, buffer.Length);
			Array.Copy(buffer, 0, array, arrayIndex, num2);
			count -= num2;
			arrayIndex += num2;
			num++;
		}
	}

	public CopyPosition CopyTo(CopyPosition position, T[] array, int arrayIndex, int count)
	{
		int num = position.Row;
		int column = position.Column;
		T[] buffer = GetBuffer(num);
		int num2 = CopyToCore(buffer, column);
		if (count == 0)
		{
			return new CopyPosition(num, column + num2).Normalize(buffer.Length);
		}
		do
		{
			buffer = GetBuffer(++num);
			num2 = CopyToCore(buffer, 0);
		}
		while (count > 0);
		return new CopyPosition(num, num2).Normalize(buffer.Length);
		int CopyToCore(T[] sourceBuffer, int sourceIndex)
		{
			int num3 = Math.Min(sourceBuffer.Length - sourceIndex, count);
			Array.Copy(sourceBuffer, sourceIndex, array, arrayIndex, num3);
			arrayIndex += num3;
			count -= num3;
			return num3;
		}
	}

	public T[] GetBuffer(int index)
	{
		if (index != 0)
		{
			if (index > _buffers.Count)
			{
				return _current;
			}
			return _buffers[index - 1];
		}
		return _first;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public void SlowAdd(T item)
	{
		Add(item);
	}

	public T[] ToArray()
	{
		if (TryMove(out var array))
		{
			return array;
		}
		array = new T[_count];
		CopyTo(array, 0, _count);
		return array;
	}

	public bool TryMove(out T[] array)
	{
		array = _first;
		return _count == _first.Length;
	}

	private void AllocateBuffer()
	{
		if ((uint)_count < 8u)
		{
			int num = Math.Min((_count == 0) ? 4 : (_count * 2), _maxCapacity);
			_current = new T[num];
			Array.Copy(_first, _current, _count);
			_first = _current;
			return;
		}
		int num2;
		if (_count == 8)
		{
			num2 = 8;
		}
		else
		{
			_buffers.Add(_current);
			num2 = Math.Min(_count, _maxCapacity - _count);
		}
		_current = new T[num2];
		_index = 0;
	}
}
