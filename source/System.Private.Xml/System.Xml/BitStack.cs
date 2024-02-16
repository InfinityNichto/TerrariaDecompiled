namespace System.Xml;

internal sealed class BitStack
{
	private uint[] _bitStack;

	private int _stackPos;

	private uint _curr;

	public BitStack()
	{
		_curr = 1u;
	}

	public void PushBit(bool bit)
	{
		if ((_curr & 0x80000000u) != 0)
		{
			PushCurr();
		}
		_curr = (_curr << 1) | (bit ? 1u : 0u);
	}

	public bool PopBit()
	{
		bool result = (_curr & 1) != 0;
		_curr >>= 1;
		if (_curr == 1)
		{
			PopCurr();
		}
		return result;
	}

	public bool PeekBit()
	{
		return (_curr & 1) != 0;
	}

	private void PushCurr()
	{
		if (_bitStack == null)
		{
			_bitStack = new uint[16];
		}
		_bitStack[_stackPos++] = _curr;
		_curr = 1u;
		int num = _bitStack.Length;
		if (_stackPos >= num)
		{
			uint[] array = new uint[2 * num];
			Array.Copy(_bitStack, array, num);
			_bitStack = array;
		}
	}

	private void PopCurr()
	{
		if (_stackPos > 0)
		{
			_curr = _bitStack[--_stackPos];
		}
	}
}
