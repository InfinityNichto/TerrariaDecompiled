namespace System.Runtime.Serialization;

internal sealed class ObjectHolderListEnumerator
{
	private readonly bool _isFixupEnumerator;

	private readonly ObjectHolderList _list;

	private readonly int _startingVersion;

	private int _currPos;

	internal ObjectHolder Current => _list._values[_currPos];

	internal ObjectHolderListEnumerator(ObjectHolderList list, bool isFixupEnumerator)
	{
		_list = list;
		_startingVersion = _list.Version;
		_currPos = -1;
		_isFixupEnumerator = isFixupEnumerator;
	}

	internal bool MoveNext()
	{
		if (_isFixupEnumerator)
		{
			while (++_currPos < _list.Count && _list._values[_currPos].CompletelyFixed)
			{
			}
			return _currPos != _list.Count;
		}
		_currPos++;
		return _currPos != _list.Count;
	}
}
