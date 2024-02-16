namespace System.Xml;

internal sealed class HWStack : ICloneable
{
	private object[] _stack;

	private readonly int _growthRate;

	private int _used;

	private int _size;

	private readonly int _limit;

	internal object this[int index]
	{
		get
		{
			if (index >= 0 && index < _used)
			{
				return _stack[index];
			}
			throw new IndexOutOfRangeException();
		}
		set
		{
			if (index >= 0 && index < _used)
			{
				_stack[index] = value;
				return;
			}
			throw new IndexOutOfRangeException();
		}
	}

	internal int Length => _used;

	internal HWStack(int GrowthRate)
		: this(GrowthRate, int.MaxValue)
	{
	}

	internal HWStack(int GrowthRate, int limit)
	{
		_growthRate = GrowthRate;
		_used = 0;
		_stack = new object[GrowthRate];
		_size = GrowthRate;
		_limit = limit;
	}

	internal object Push()
	{
		if (_used == _size)
		{
			if (_limit <= _used)
			{
				throw new XmlException(System.SR.Xml_StackOverflow, string.Empty);
			}
			object[] array = new object[_size + _growthRate];
			if (_used > 0)
			{
				Array.Copy(_stack, array, _used);
			}
			_stack = array;
			_size += _growthRate;
		}
		return _stack[_used++];
	}

	internal object Pop()
	{
		if (0 < _used)
		{
			_used--;
			return _stack[_used];
		}
		return null;
	}

	internal object Peek()
	{
		if (_used <= 0)
		{
			return null;
		}
		return _stack[_used - 1];
	}

	internal void AddToTop(object o)
	{
		if (_used > 0)
		{
			_stack[_used - 1] = o;
		}
	}

	private HWStack(object[] stack, int growthRate, int used, int size)
	{
		_stack = stack;
		_growthRate = growthRate;
		_used = used;
		_size = size;
	}

	public object Clone()
	{
		return new HWStack((object[])_stack.Clone(), _growthRate, _used, _size);
	}
}
