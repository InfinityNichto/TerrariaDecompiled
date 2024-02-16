namespace System.Runtime.Serialization;

internal sealed class LongList
{
	private long[] _values;

	private int _count;

	private int _totalItems;

	private int _currentItem;

	internal int Count => _count;

	internal long Current => _values[_currentItem];

	internal LongList()
		: this(2)
	{
	}

	internal LongList(int startingSize)
	{
		_count = 0;
		_totalItems = 0;
		_values = new long[startingSize];
	}

	internal void Add(long value)
	{
		if (_totalItems == _values.Length)
		{
			EnlargeArray();
		}
		_values[_totalItems++] = value;
		_count++;
	}

	internal void StartEnumeration()
	{
		_currentItem = -1;
	}

	internal bool MoveNext()
	{
		while (++_currentItem < _totalItems && _values[_currentItem] == -1)
		{
		}
		return _currentItem != _totalItems;
	}

	internal bool RemoveElement(long value)
	{
		int i;
		for (i = 0; i < _totalItems && _values[i] != value; i++)
		{
		}
		if (i == _totalItems)
		{
			return false;
		}
		_values[i] = -1L;
		return true;
	}

	private void EnlargeArray()
	{
		int num = _values.Length * 2;
		if (num < 0)
		{
			num = int.MaxValue;
		}
		long[] array = new long[num];
		Array.Copy(_values, array, _count);
		_values = array;
	}
}
