using System.Text;

namespace System.Xml;

internal abstract class Ucs4Decoder : Decoder
{
	internal byte[] lastBytes = new byte[4];

	internal int lastBytesCount;

	public override int GetCharCount(byte[] bytes, int index, int count)
	{
		return (count + lastBytesCount) / 4;
	}

	internal abstract int GetFullChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex);

	public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
	{
		int num = lastBytesCount;
		if (lastBytesCount > 0)
		{
			while (lastBytesCount < 4 && byteCount > 0)
			{
				lastBytes[lastBytesCount] = bytes[byteIndex];
				byteIndex++;
				byteCount--;
				lastBytesCount++;
			}
			if (lastBytesCount < 4)
			{
				return 0;
			}
			num = GetFullChars(lastBytes, 0, 4, chars, charIndex);
			charIndex += num;
			lastBytesCount = 0;
		}
		else
		{
			num = 0;
		}
		num = GetFullChars(bytes, byteIndex, byteCount, chars, charIndex) + num;
		int num2 = byteCount & 3;
		if (num2 >= 0)
		{
			for (int i = 0; i < num2; i++)
			{
				lastBytes[i] = bytes[byteIndex + byteCount - num2 + i];
			}
			lastBytesCount = num2;
		}
		return num;
	}

	public override void Convert(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
	{
		bytesUsed = 0;
		charsUsed = 0;
		int num = 0;
		int i = lastBytesCount;
		if (i > 0)
		{
			for (; i < 4; i++)
			{
				if (byteCount <= 0)
				{
					break;
				}
				lastBytes[i] = bytes[byteIndex];
				byteIndex++;
				byteCount--;
				bytesUsed++;
			}
			if (i < 4)
			{
				lastBytesCount = i;
				completed = true;
				return;
			}
			num = GetFullChars(lastBytes, 0, 4, chars, charIndex);
			charIndex += num;
			charCount -= num;
			charsUsed = num;
			lastBytesCount = 0;
		}
		else
		{
			num = 0;
		}
		if (charCount * 4 < byteCount)
		{
			byteCount = charCount * 4;
			completed = false;
		}
		else
		{
			completed = true;
		}
		bytesUsed += byteCount;
		charsUsed = GetFullChars(bytes, byteIndex, byteCount, chars, charIndex) + num;
		int num2 = byteCount & 3;
		if (num2 >= 0)
		{
			for (int j = 0; j < num2; j++)
			{
				lastBytes[j] = bytes[byteIndex + byteCount - num2 + j];
			}
			lastBytesCount = num2;
		}
	}

	internal void Ucs4ToUTF16(uint code, char[] chars, int charIndex)
	{
		chars[charIndex] = (char)(55296 + (ushort)((code >> 16) - 1) + (ushort)((code >> 10) & 0x3F));
		chars[charIndex + 1] = (char)(56320 + (ushort)(code & 0x3FF));
	}
}
