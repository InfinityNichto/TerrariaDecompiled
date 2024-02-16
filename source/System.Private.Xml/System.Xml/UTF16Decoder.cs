using System.Text;

namespace System.Xml;

internal sealed class UTF16Decoder : Decoder
{
	private readonly bool _bigEndian;

	private int _lastByte;

	public UTF16Decoder(bool bigEndian)
	{
		_lastByte = -1;
		_bigEndian = bigEndian;
	}

	public override int GetCharCount(byte[] bytes, int index, int count)
	{
		return GetCharCount(bytes, index, count, flush: false);
	}

	public override int GetCharCount(byte[] bytes, int index, int count, bool flush)
	{
		int num = count + ((_lastByte >= 0) ? 1 : 0);
		if (flush && num % 2 != 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Enc_InvalidByteInEncoding, -1), (string?)null);
		}
		return num / 2;
	}

	public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
	{
		int charCount = GetCharCount(bytes, byteIndex, byteCount);
		if (_lastByte >= 0)
		{
			if (byteCount == 0)
			{
				return charCount;
			}
			int num = bytes[byteIndex++];
			byteCount--;
			chars[charIndex++] = (_bigEndian ? ((char)((_lastByte << 8) | num)) : ((char)((num << 8) | _lastByte)));
			_lastByte = -1;
		}
		if (((uint)byteCount & (true ? 1u : 0u)) != 0)
		{
			_lastByte = bytes[byteIndex + --byteCount];
		}
		if (_bigEndian == BitConverter.IsLittleEndian)
		{
			int num2 = byteIndex + byteCount;
			if (_bigEndian)
			{
				while (byteIndex < num2)
				{
					int num3 = bytes[byteIndex++];
					int num4 = bytes[byteIndex++];
					chars[charIndex++] = (char)((num3 << 8) | num4);
				}
			}
			else
			{
				while (byteIndex < num2)
				{
					int num5 = bytes[byteIndex++];
					int num6 = bytes[byteIndex++];
					chars[charIndex++] = (char)((num6 << 8) | num5);
				}
			}
		}
		else
		{
			Buffer.BlockCopy(bytes, byteIndex, chars, charIndex * 2, byteCount);
		}
		return charCount;
	}

	public override void Convert(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
	{
		charsUsed = 0;
		bytesUsed = 0;
		if (_lastByte >= 0)
		{
			if (byteCount == 0)
			{
				completed = true;
				return;
			}
			int num = bytes[byteIndex++];
			byteCount--;
			bytesUsed++;
			chars[charIndex++] = (_bigEndian ? ((char)((_lastByte << 8) | num)) : ((char)((num << 8) | _lastByte)));
			charCount--;
			charsUsed++;
			_lastByte = -1;
		}
		if (charCount * 2 < byteCount)
		{
			byteCount = charCount * 2;
			completed = false;
		}
		else
		{
			completed = true;
		}
		if (_bigEndian == BitConverter.IsLittleEndian)
		{
			int num2 = byteIndex;
			int num3 = num2 + (byteCount & -2);
			if (_bigEndian)
			{
				while (num2 < num3)
				{
					int num4 = bytes[num2++];
					int num5 = bytes[num2++];
					chars[charIndex++] = (char)((num4 << 8) | num5);
				}
			}
			else
			{
				while (num2 < num3)
				{
					int num6 = bytes[num2++];
					int num7 = bytes[num2++];
					chars[charIndex++] = (char)((num7 << 8) | num6);
				}
			}
		}
		else
		{
			Buffer.BlockCopy(bytes, byteIndex, chars, charIndex * 2, byteCount & -2);
		}
		charsUsed += byteCount / 2;
		bytesUsed += byteCount;
		if (((uint)byteCount & (true ? 1u : 0u)) != 0)
		{
			_lastByte = bytes[byteIndex + byteCount - 1];
		}
	}
}
