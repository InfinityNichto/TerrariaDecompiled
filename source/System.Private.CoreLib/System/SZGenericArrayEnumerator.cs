using System.Collections;
using System.Collections.Generic;

namespace System;

internal sealed class SZGenericArrayEnumerator<T> : IEnumerator<T>, IDisposable, IEnumerator
{
	private readonly T[] _array;

	private int _index;

	internal static readonly SZGenericArrayEnumerator<T> Empty = new SZGenericArrayEnumerator<T>(new T[0]);

	public T Current
	{
		get
		{
			int index = _index;
			T[] array = _array;
			if ((uint)index >= (uint)array.Length)
			{
				ThrowHelper.ThrowInvalidOperationException_EnumCurrent(index);
			}
			return array[index];
		}
	}

	object IEnumerator.Current => Current;

	internal SZGenericArrayEnumerator(T[] array)
	{
		_array = array;
		_index = -1;
	}

	public bool MoveNext()
	{
		int num = _index + 1;
		if ((uint)num >= (uint)_array.Length)
		{
			_index = _array.Length;
			return false;
		}
		_index = num;
		return true;
	}

	void IEnumerator.Reset()
	{
		_index = -1;
	}

	public void Dispose()
	{
	}
}
