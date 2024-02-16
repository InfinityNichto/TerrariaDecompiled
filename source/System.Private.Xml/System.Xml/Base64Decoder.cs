namespace System.Xml;

internal sealed class Base64Decoder : IncrementalReadDecoder
{
	private byte[] _buffer;

	private int _startIndex;

	private int _curIndex;

	private int _endIndex;

	private int _bits;

	private int _bitsFilled;

	private static readonly byte[] s_mapBase64 = ConstructMapBase64();

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
		Decode(chars.AsSpan(startPos, len), _buffer.AsSpan(_curIndex, _endIndex - _curIndex), out var charsDecoded, out var bytesDecoded);
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
		Decode(str.AsSpan(startPos, len), _buffer.AsSpan(_curIndex, _endIndex - _curIndex), out var charsDecoded, out var bytesDecoded);
		_curIndex += bytesDecoded;
		return charsDecoded;
	}

	internal override void Reset()
	{
		_bitsFilled = 0;
		_bits = 0;
	}

	internal override void SetNextOutputBuffer(Array buffer, int index, int count)
	{
		_buffer = (byte[])buffer;
		_startIndex = index;
		_curIndex = index;
		_endIndex = index + count;
	}

	private static byte[] ConstructMapBase64()
	{
		byte[] array = new byte[123];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = byte.MaxValue;
		}
		for (int j = 0; j < "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/".Length; j++)
		{
			array[(uint)"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"[j]] = (byte)j;
		}
		return array;
	}

	private void Decode(ReadOnlySpan<char> chars, Span<byte> bytes, out int charsDecoded, out int bytesDecoded)
	{
		int num = 0;
		int num2 = 0;
		int num3 = _bits;
		int num4 = _bitsFilled;
		while (true)
		{
			if ((uint)num2 < (uint)chars.Length && (uint)num < (uint)bytes.Length)
			{
				char c = chars[num2];
				if (c != '=')
				{
					num2++;
					if (XmlCharType.IsWhiteSpace(c))
					{
						continue;
					}
					int num5;
					if (c > 'z' || (num5 = s_mapBase64[(uint)c]) == 255)
					{
						throw new XmlException(System.SR.Xml_InvalidBase64Value, chars.ToString());
					}
					num3 = (num3 << 6) | num5;
					num4 += 6;
					if (num4 >= 8)
					{
						bytes[num++] = (byte)((uint)(num3 >> num4 - 8) & 0xFFu);
						num4 -= 8;
						if (num == bytes.Length)
						{
							break;
						}
					}
					continue;
				}
			}
			if ((uint)num2 >= (uint)chars.Length || chars[num2] != '=')
			{
				break;
			}
			num4 = 0;
			do
			{
				num2++;
			}
			while ((uint)num2 < (uint)chars.Length && chars[num2] == '=');
			while ((uint)num2 < (uint)chars.Length)
			{
				if (!XmlCharType.IsWhiteSpace(chars[num2++]))
				{
					throw new XmlException(System.SR.Xml_InvalidBase64Value, chars.ToString());
				}
			}
			break;
		}
		_bits = num3;
		_bitsFilled = num4;
		bytesDecoded = num;
		charsDecoded = num2;
	}
}
