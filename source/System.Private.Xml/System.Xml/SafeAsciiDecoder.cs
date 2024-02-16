using System.Text;

namespace System.Xml;

internal sealed class SafeAsciiDecoder : Decoder
{
	public override int GetCharCount(byte[] bytes, int index, int count)
	{
		return count;
	}

	public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
	{
		int num = byteIndex;
		int num2 = charIndex;
		while (num < byteIndex + byteCount)
		{
			chars[num2++] = (char)bytes[num++];
		}
		return byteCount;
	}

	public override void Convert(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
	{
		if (charCount < byteCount)
		{
			byteCount = charCount;
			completed = false;
		}
		else
		{
			completed = true;
		}
		int num = byteIndex;
		int num2 = charIndex;
		int num3 = byteIndex + byteCount;
		while (num < num3)
		{
			chars[num2++] = (char)bytes[num++];
		}
		charsUsed = byteCount;
		bytesUsed = byteCount;
	}
}
