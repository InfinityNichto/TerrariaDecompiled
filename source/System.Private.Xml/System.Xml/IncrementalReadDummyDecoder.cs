namespace System.Xml;

internal sealed class IncrementalReadDummyDecoder : IncrementalReadDecoder
{
	internal override int DecodedCount => -1;

	internal override bool IsFull => false;

	internal override void SetNextOutputBuffer(Array array, int offset, int len)
	{
	}

	internal override int Decode(char[] chars, int startPos, int len)
	{
		return len;
	}

	internal override int Decode(string str, int startPos, int len)
	{
		return len;
	}

	internal override void Reset()
	{
	}
}
