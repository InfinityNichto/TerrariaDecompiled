namespace System.Runtime.Serialization;

internal sealed class FixupHolderList
{
	internal FixupHolder[] _values;

	internal int _count;

	internal FixupHolderList()
		: this(2)
	{
	}

	internal FixupHolderList(int startingSize)
	{
		_count = 0;
		_values = new FixupHolder[startingSize];
	}

	internal void Add(FixupHolder fixup)
	{
		if (_count == _values.Length)
		{
			EnlargeArray();
		}
		_values[_count++] = fixup;
	}

	private void EnlargeArray()
	{
		int num = _values.Length * 2;
		if (num < 0)
		{
			num = int.MaxValue;
		}
		FixupHolder[] array = new FixupHolder[num];
		Array.Copy(_values, array, _count);
		_values = array;
	}
}
