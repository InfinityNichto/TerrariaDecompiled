using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

[Serializable]
[DebuggerTypeProxy(typeof(StackDebugView<>))]
[DebuggerDisplay("Count = {Count}")]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class Stack<T> : IEnumerable<T>, IEnumerable, ICollection, IReadOnlyCollection<T>
{
	public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
	{
		private readonly Stack<T> _stack;

		private readonly int _version;

		private int _index;

		private T _currentElement;

		public T Current
		{
			get
			{
				if (_index < 0)
				{
					ThrowEnumerationNotStartedOrEnded();
				}
				return _currentElement;
			}
		}

		object? IEnumerator.Current => Current;

		internal Enumerator(Stack<T> stack)
		{
			_stack = stack;
			_version = stack._version;
			_index = -2;
			_currentElement = default(T);
		}

		public void Dispose()
		{
			_index = -1;
		}

		public bool MoveNext()
		{
			if (_version != _stack._version)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
			}
			bool flag;
			if (_index == -2)
			{
				_index = _stack._size - 1;
				flag = _index >= 0;
				if (flag)
				{
					_currentElement = _stack._array[_index];
				}
				return flag;
			}
			if (_index == -1)
			{
				return false;
			}
			flag = --_index >= 0;
			if (flag)
			{
				_currentElement = _stack._array[_index];
			}
			else
			{
				_currentElement = default(T);
			}
			return flag;
		}

		private void ThrowEnumerationNotStartedOrEnded()
		{
			throw new InvalidOperationException((_index == -2) ? System.SR.InvalidOperation_EnumNotStarted : System.SR.InvalidOperation_EnumEnded);
		}

		void IEnumerator.Reset()
		{
			if (_version != _stack._version)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
			}
			_index = -2;
			_currentElement = default(T);
		}
	}

	private T[] _array;

	private int _size;

	private int _version;

	public int Count => _size;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	public Stack()
	{
		_array = Array.Empty<T>();
	}

	public Stack(int capacity)
	{
		if (capacity < 0)
		{
			throw new ArgumentOutOfRangeException("capacity", capacity, System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		_array = new T[capacity];
	}

	public Stack(IEnumerable<T> collection)
	{
		if (collection == null)
		{
			throw new ArgumentNullException("collection");
		}
		_array = System.Collections.Generic.EnumerableHelpers.ToArray(collection, out _size);
	}

	public void Clear()
	{
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			Array.Clear(_array, 0, _size);
		}
		_size = 0;
		_version++;
	}

	public bool Contains(T item)
	{
		if (_size != 0)
		{
			return Array.LastIndexOf(_array, item, _size - 1) != -1;
		}
		return false;
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (arrayIndex < 0 || arrayIndex > array.Length)
		{
			throw new ArgumentOutOfRangeException("arrayIndex", arrayIndex, System.SR.ArgumentOutOfRange_Index);
		}
		if (array.Length - arrayIndex < _size)
		{
			throw new ArgumentException(System.SR.Argument_InvalidOffLen);
		}
		int num = 0;
		int num2 = arrayIndex + _size;
		while (num < _size)
		{
			array[--num2] = _array[num++];
		}
	}

	void ICollection.CopyTo(Array array, int arrayIndex)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (array.Rank != 1)
		{
			throw new ArgumentException(System.SR.Arg_RankMultiDimNotSupported, "array");
		}
		if (array.GetLowerBound(0) != 0)
		{
			throw new ArgumentException(System.SR.Arg_NonZeroLowerBound, "array");
		}
		if (arrayIndex < 0 || arrayIndex > array.Length)
		{
			throw new ArgumentOutOfRangeException("arrayIndex", arrayIndex, System.SR.ArgumentOutOfRange_Index);
		}
		if (array.Length - arrayIndex < _size)
		{
			throw new ArgumentException(System.SR.Argument_InvalidOffLen);
		}
		try
		{
			Array.Copy(_array, 0, array, arrayIndex, _size);
			Array.Reverse(array, arrayIndex, _size);
		}
		catch (ArrayTypeMismatchException)
		{
			throw new ArgumentException(System.SR.Argument_InvalidArrayType, "array");
		}
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(this);
	}

	public void TrimExcess()
	{
		int num = (int)((double)_array.Length * 0.9);
		if (_size < num)
		{
			Array.Resize(ref _array, _size);
			_version++;
		}
	}

	public T Peek()
	{
		int num = _size - 1;
		T[] array = _array;
		if ((uint)num >= (uint)array.Length)
		{
			ThrowForEmptyStack();
		}
		return array[num];
	}

	public bool TryPeek([MaybeNullWhen(false)] out T result)
	{
		int num = _size - 1;
		T[] array = _array;
		if ((uint)num >= (uint)array.Length)
		{
			result = default(T);
			return false;
		}
		result = array[num];
		return true;
	}

	public T Pop()
	{
		int num = _size - 1;
		T[] array = _array;
		if ((uint)num >= (uint)array.Length)
		{
			ThrowForEmptyStack();
		}
		_version++;
		_size = num;
		T result = array[num];
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			array[num] = default(T);
		}
		return result;
	}

	public bool TryPop([MaybeNullWhen(false)] out T result)
	{
		int num = _size - 1;
		T[] array = _array;
		if ((uint)num >= (uint)array.Length)
		{
			result = default(T);
			return false;
		}
		_version++;
		_size = num;
		result = array[num];
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			array[num] = default(T);
		}
		return true;
	}

	public void Push(T item)
	{
		int size = _size;
		T[] array = _array;
		if ((uint)size < (uint)array.Length)
		{
			array[size] = item;
			_version++;
			_size = size + 1;
		}
		else
		{
			PushWithResize(item);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void PushWithResize(T item)
	{
		Grow(_size + 1);
		_array[_size] = item;
		_version++;
		_size++;
	}

	public int EnsureCapacity(int capacity)
	{
		if (capacity < 0)
		{
			throw new ArgumentOutOfRangeException("capacity", capacity, System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (_array.Length < capacity)
		{
			Grow(capacity);
			_version++;
		}
		return _array.Length;
	}

	private void Grow(int capacity)
	{
		int num = ((_array.Length == 0) ? 4 : (2 * _array.Length));
		if ((uint)num > Array.MaxLength)
		{
			num = Array.MaxLength;
		}
		if (num < capacity)
		{
			num = capacity;
		}
		Array.Resize(ref _array, num);
	}

	public T[] ToArray()
	{
		if (_size == 0)
		{
			return Array.Empty<T>();
		}
		T[] array = new T[_size];
		for (int i = 0; i < _size; i++)
		{
			array[i] = _array[_size - i - 1];
		}
		return array;
	}

	private void ThrowForEmptyStack()
	{
		throw new InvalidOperationException(System.SR.InvalidOperation_EmptyStack);
	}
}
