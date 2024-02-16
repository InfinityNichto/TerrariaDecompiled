namespace System.Collections.Generic;

internal ref struct BitHelper
{
	private readonly Span<int> _span;

	internal BitHelper(Span<int> span, bool clear)
	{
		if (clear)
		{
			span.Clear();
		}
		_span = span;
	}

	internal void MarkBit(int bitPosition)
	{
		int num = bitPosition / 32;
		if ((uint)num < (uint)_span.Length)
		{
			_span[num] |= 1 << bitPosition % 32;
		}
	}

	internal bool IsMarked(int bitPosition)
	{
		int num = bitPosition / 32;
		if ((uint)num < (uint)_span.Length)
		{
			return (_span[num] & (1 << bitPosition % 32)) != 0;
		}
		return false;
	}

	internal static int ToIntArrayLength(int n)
	{
		if (n <= 0)
		{
			return 0;
		}
		return (n - 1) / 32 + 1;
	}
}
