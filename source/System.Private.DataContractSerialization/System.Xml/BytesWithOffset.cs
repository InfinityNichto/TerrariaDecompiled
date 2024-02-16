namespace System.Xml;

internal readonly struct BytesWithOffset
{
	private readonly byte[] _bytes;

	private readonly int _offset;

	public byte[] Bytes => _bytes;

	public int Offset => _offset;

	public BytesWithOffset(byte[] bytes, int offset)
	{
		_bytes = bytes;
		_offset = offset;
	}
}
