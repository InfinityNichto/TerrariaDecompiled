namespace System.Runtime.Serialization;

internal sealed class ObjectHolderList
{
	internal ObjectHolder[] _values;

	internal int _count;

	internal int Version => _count;

	internal int Count => _count;

	internal ObjectHolderList()
		: this(8)
	{
	}

	internal ObjectHolderList(int startingSize)
	{
		_count = 0;
		_values = new ObjectHolder[startingSize];
	}

	internal void Add(ObjectHolder value)
	{
		if (_count == _values.Length)
		{
			EnlargeArray();
		}
		_values[_count++] = value;
	}

	internal ObjectHolderListEnumerator GetFixupEnumerator()
	{
		return new ObjectHolderListEnumerator(this, isFixupEnumerator: true);
	}

	private void EnlargeArray()
	{
		int num = _values.Length * 2;
		if (num < 0)
		{
			num = int.MaxValue;
		}
		ObjectHolder[] array = new ObjectHolder[num];
		Array.Copy(_values, array, _count);
		_values = array;
	}
}
