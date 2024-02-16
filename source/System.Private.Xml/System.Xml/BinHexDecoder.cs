namespace System.Xml;

internal sealed class BinHexDecoder : IncrementalReadDecoder
{
	private byte[] _buffer;

	private int _startIndex;

	private int _curIndex;

	private int _endIndex;

	private bool _hasHalfByteCached;

	private byte _cachedHalfByte;

	internal override int DecodedCount => _curIndex - _startIndex;

	internal override bool IsFull => _curIndex == _endIndex;

	internal override int Decode(char[] chars, int startPos, int len)
	{
		if (chars == null)
		{
			throw new ArgumentNullException("chars");
		}
		if (len < 0)
		{
			throw new ArgumentOutOfRangeException("len");
		}
		if (startPos < 0)
		{
			throw new ArgumentOutOfRangeException("startPos");
		}
		if (chars.Length - startPos < len)
		{
			throw new ArgumentOutOfRangeException("len");
		}
		if (len == 0)
		{
			return 0;
		}
		Decode(chars.AsSpan(startPos, len), _buffer.AsSpan(_curIndex, _endIndex - _curIndex), ref _hasHalfByteCached, ref _cachedHalfByte, out var charsDecoded, out var bytesDecoded);
		_curIndex += bytesDecoded;
		return charsDecoded;
	}

	internal override int Decode(string str, int startPos, int len)
	{
		if (str == null)
		{
			throw new ArgumentNullException("str");
		}
		if (len < 0)
		{
			throw new ArgumentOutOfRangeException("len");
		}
		if (startPos < 0)
		{
			throw new ArgumentOutOfRangeException("startPos");
		}
		if (str.Length - startPos < len)
		{
			throw new ArgumentOutOfRangeException("len");
		}
		if (len == 0)
		{
			return 0;
		}
		Decode(str.AsSpan(startPos, len), _buffer.AsSpan(_curIndex, _endIndex - _curIndex), ref _hasHalfByteCached, ref _cachedHalfByte, out var charsDecoded, out var bytesDecoded);
		_curIndex += bytesDecoded;
		return charsDecoded;
	}

	internal override void Reset()
	{
		_hasHalfByteCached = false;
		_cachedHalfByte = 0;
	}

	internal override void SetNextOutputBuffer(Array buffer, int index, int count)
	{
		_buffer = (byte[])buffer;
		_startIndex = index;
		_curIndex = index;
		_endIndex = index + count;
	}

	public static byte[] Decode(char[] chars, bool allowOddChars)
	{
		if (chars == null)
		{
			throw new ArgumentNullException("chars");
		}
		int num = chars.Length;
		if (num == 0)
		{
			return Array.Empty<byte>();
		}
		byte[] array = new byte[(num + 1) / 2];
		bool hasHalfByteCached = false;
		byte cachedHalfByte = 0;
		Decode(chars, array, ref hasHalfByteCached, ref cachedHalfByte, out var _, out var bytesDecoded);
		if (hasHalfByteCached && !allowOddChars)
		{
			throw new XmlException(System.SR.Xml_InvalidBinHexValueOddCount, new string(chars));
		}
		if (bytesDecoded < array.Length)
		{
			Array.Resize(ref array, bytesDecoded);
		}
		return array;
	}

	private static void Decode(ReadOnlySpan<char> chars, Span<byte> bytes, ref bool hasHalfByteCached, ref byte cachedHalfByte, out int charsDecoded, out int bytesDecoded)
	{
		int num = 0;
		int i;
		for (i = 0; i < chars.Length; i++)
		{
			if ((uint)num >= (uint)bytes.Length)
			{
				break;
			}
			char c = chars[i];
			int num2 = System.HexConverter.FromChar(c);
			if (num2 != 255)
			{
				byte b = (byte)num2;
				if (hasHalfByteCached)
				{
					bytes[num++] = (byte)((cachedHalfByte << 4) + b);
					hasHalfByteCached = false;
				}
				else
				{
					cachedHalfByte = b;
					hasHalfByteCached = true;
				}
			}
			else if (!XmlCharType.IsWhiteSpace(c))
			{
				throw new XmlException(System.SR.Xml_InvalidBinHexValue, chars.ToString());
			}
		}
		bytesDecoded = num;
		charsDecoded = i;
	}
}
