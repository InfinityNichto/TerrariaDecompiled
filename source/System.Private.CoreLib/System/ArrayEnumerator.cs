using System.Collections;

namespace System;

internal sealed class ArrayEnumerator : IEnumerator, ICloneable
{
	private readonly Array _array;

	private nint _index;

	public object Current
	{
		get
		{
			nint index = _index;
			Array array = _array;
			if ((nuint)index >= array.NativeLength)
			{
				if (index < 0)
				{
					ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumNotStarted();
				}
				else
				{
					ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumEnded();
				}
			}
			return array.InternalGetValue(index);
		}
	}

	internal ArrayEnumerator(Array array)
	{
		_array = array;
		_index = -1;
	}

	public object Clone()
	{
		return MemberwiseClone();
	}

	public bool MoveNext()
	{
		nint num = _index + 1;
		if ((nuint)num >= _array.NativeLength)
		{
			_index = (nint)_array.NativeLength;
			return false;
		}
		_index = num;
		return true;
	}

	public void Reset()
	{
		_index = -1;
	}
}
