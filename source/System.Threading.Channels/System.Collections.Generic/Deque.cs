using System.Diagnostics;

namespace System.Collections.Generic;

[DebuggerDisplay("Count = {_size}")]
internal sealed class Deque<T>
{
	private T[] _array = Array.Empty<T>();

	private int _head;

	private int _tail;

	private int _size;

	public int Count => _size;

	public bool IsEmpty => _size == 0;

	public void EnqueueTail(T item)
	{
		if (_size == _array.Length)
		{
			Grow();
		}
		_array[_tail] = item;
		if (++_tail == _array.Length)
		{
			_tail = 0;
		}
		_size++;
	}

	public T DequeueHead()
	{
		T result = _array[_head];
		_array[_head] = default(T);
		if (++_head == _array.Length)
		{
			_head = 0;
		}
		_size--;
		return result;
	}

	public T PeekHead()
	{
		return _array[_head];
	}

	public T DequeueTail()
	{
		if (--_tail == -1)
		{
			_tail = _array.Length - 1;
		}
		T result = _array[_tail];
		_array[_tail] = default(T);
		_size--;
		return result;
	}

	public IEnumerator<T> GetEnumerator()
	{
		int pos = _head;
		int count = _size;
		while (count-- > 0)
		{
			yield return _array[pos];
			pos = (pos + 1) % _array.Length;
		}
	}

	private void Grow()
	{
		int num = (int)((long)_array.Length * 2L);
		if (num < _array.Length + 4)
		{
			num = _array.Length + 4;
		}
		T[] array = new T[num];
		if (_head == 0)
		{
			Array.Copy(_array, array, _size);
		}
		else
		{
			Array.Copy(_array, _head, array, 0, _array.Length - _head);
			Array.Copy(_array, 0, array, _array.Length - _head, _tail);
		}
		_array = array;
		_head = 0;
		_tail = _size;
	}
}
