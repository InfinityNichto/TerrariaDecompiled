using System.Text;

namespace System.Xml;

internal class Ucs4Encoding : Encoding
{
	internal Ucs4Decoder ucs4Decoder;

	public override string WebName => EncodingName;

	public override int CodePage => 0;

	internal static Encoding UCS4_Littleendian => new Ucs4Encoding4321();

	internal static Encoding UCS4_Bigendian => new Ucs4Encoding1234();

	internal static Encoding UCS4_2143 => new Ucs4Encoding2143();

	internal static Encoding UCS4_3412 => new Ucs4Encoding3412();

	public override Decoder GetDecoder()
	{
		return ucs4Decoder;
	}

	public override int GetByteCount(char[] chars, int index, int count)
	{
		return checked(count * 4);
	}

	public override int GetByteCount(char[] chars)
	{
		return chars.Length * 4;
	}

	public override byte[] GetBytes(string s)
	{
		return null;
	}

	public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
	{
		return 0;
	}

	public override int GetMaxByteCount(int charCount)
	{
		return 0;
	}

	public override int GetCharCount(byte[] bytes, int index, int count)
	{
		return ucs4Decoder.GetCharCount(bytes, index, count);
	}

	public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
	{
		return ucs4Decoder.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
	}

	public override int GetMaxCharCount(int byteCount)
	{
		return (byteCount + 3) / 4;
	}

	public override int GetCharCount(byte[] bytes)
	{
		return bytes.Length / 4;
	}

	public override Encoder GetEncoder()
	{
		return null;
	}
}
