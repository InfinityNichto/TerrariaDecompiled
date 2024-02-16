using System.Runtime.CompilerServices;

namespace System.Text.Json;

internal struct BitStack
{
	private int[] _array;

	private ulong _allocationFreeContainer;

	private int _currentDepth;

	public int CurrentDepth => _currentDepth;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PushTrue()
	{
		if (_currentDepth < 64)
		{
			_allocationFreeContainer = (_allocationFreeContainer << 1) | 1;
		}
		else
		{
			PushToArray(value: true);
		}
		_currentDepth++;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PushFalse()
	{
		if (_currentDepth < 64)
		{
			_allocationFreeContainer <<= 1;
		}
		else
		{
			PushToArray(value: false);
		}
		_currentDepth++;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void PushToArray(bool value)
	{
		if (_array == null)
		{
			_array = new int[2];
		}
		int number = _currentDepth - 64;
		int remainder;
		int num = Div32Rem(number, out remainder);
		if (num >= _array.Length)
		{
			DoubleArray(num);
		}
		int num2 = _array[num];
		num2 = ((!value) ? (num2 & ~(1 << remainder)) : (num2 | (1 << remainder)));
		_array[num] = num2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Pop()
	{
		_currentDepth--;
		bool flag = false;
		if (_currentDepth < 64)
		{
			_allocationFreeContainer >>= 1;
			return (_allocationFreeContainer & 1) != 0;
		}
		if (_currentDepth == 64)
		{
			return (_allocationFreeContainer & 1) != 0;
		}
		return PopFromArray();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool PopFromArray()
	{
		int number = _currentDepth - 64 - 1;
		int remainder;
		int num = Div32Rem(number, out remainder);
		return (_array[num] & (1 << remainder)) != 0;
	}

	private void DoubleArray(int minSize)
	{
		int newSize = Math.Max(minSize + 1, _array.Length * 2);
		Array.Resize(ref _array, newSize);
	}

	public void SetFirstBit()
	{
		_currentDepth++;
		_allocationFreeContainer = 1uL;
	}

	public void ResetFirstBit()
	{
		_currentDepth++;
		_allocationFreeContainer = 0uL;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int Div32Rem(int number, out int remainder)
	{
		uint result = (uint)number / 32u;
		remainder = number & 0x1F;
		return (int)result;
	}
}
