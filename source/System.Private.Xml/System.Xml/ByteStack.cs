namespace System.Xml;

internal sealed class ByteStack
{
	private byte[] _stack;

	private readonly int _growthRate;

	private int _top;

	private int _size;

	public ByteStack(int growthRate)
	{
		_growthRate = growthRate;
		_top = 0;
		_stack = new byte[growthRate];
		_size = growthRate;
	}

	public void Push(byte data)
	{
		if (_size == _top)
		{
			byte[] array = new byte[_size + _growthRate];
			if (_top > 0)
			{
				Buffer.BlockCopy(_stack, 0, array, 0, _top);
			}
			_stack = array;
			_size += _growthRate;
		}
		_stack[_top++] = data;
	}

	public byte Pop()
	{
		if (_top > 0)
		{
			return _stack[--_top];
		}
		return 0;
	}
}
