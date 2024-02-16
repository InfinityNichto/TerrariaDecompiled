namespace System.Xml;

internal sealed class IncrementalReadCharsDecoder : IncrementalReadDecoder
{
	private char[] _buffer;

	private int _startIndex;

	private int _curIndex;

	private int _endIndex;

	internal override int DecodedCount => _curIndex - _startIndex;

	internal override bool IsFull => _curIndex == _endIndex;

	internal override int Decode(char[] chars, int startPos, int len)
	{
		int num = _endIndex - _curIndex;
		if (num > len)
		{
			num = len;
		}
		Buffer.BlockCopy(chars, startPos * 2, _buffer, _curIndex * 2, num * 2);
		_curIndex += num;
		return num;
	}

	internal override int Decode(string str, int startPos, int len)
	{
		int num = _endIndex - _curIndex;
		if (num > len)
		{
			num = len;
		}
		str.CopyTo(startPos, _buffer, _curIndex, num);
		_curIndex += num;
		return num;
	}

	internal override void Reset()
	{
	}

	internal override void SetNextOutputBuffer(Array buffer, int index, int count)
	{
		_buffer = (char[])buffer;
		_startIndex = index;
		_curIndex = index;
		_endIndex = index + count;
	}
}
