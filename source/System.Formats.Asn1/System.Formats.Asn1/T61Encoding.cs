using System.Text;

namespace System.Formats.Asn1;

internal sealed class T61Encoding : Encoding
{
	private static readonly Encoding s_utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	private static readonly Encoding s_latin1Encoding = Encoding.GetEncoding("iso-8859-1");

	public override int GetByteCount(char[] chars, int index, int count)
	{
		return s_utf8Encoding.GetByteCount(chars, index, count);
	}

	public unsafe override int GetByteCount(char* chars, int count)
	{
		return s_utf8Encoding.GetByteCount(chars, count);
	}

	public override int GetByteCount(string s)
	{
		return s_utf8Encoding.GetByteCount(s);
	}

	public override int GetByteCount(ReadOnlySpan<char> chars)
	{
		return s_utf8Encoding.GetByteCount(chars);
	}

	public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
	{
		return s_utf8Encoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
	}

	public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
	{
		return s_utf8Encoding.GetBytes(chars, charCount, bytes, byteCount);
	}

	public override int GetCharCount(byte[] bytes, int index, int count)
	{
		try
		{
			return s_utf8Encoding.GetCharCount(bytes, index, count);
		}
		catch (DecoderFallbackException)
		{
			return s_latin1Encoding.GetCharCount(bytes, index, count);
		}
	}

	public unsafe override int GetCharCount(byte* bytes, int count)
	{
		try
		{
			return s_utf8Encoding.GetCharCount(bytes, count);
		}
		catch (DecoderFallbackException)
		{
			return s_latin1Encoding.GetCharCount(bytes, count);
		}
	}

	public override int GetCharCount(ReadOnlySpan<byte> bytes)
	{
		try
		{
			return s_utf8Encoding.GetCharCount(bytes);
		}
		catch (DecoderFallbackException)
		{
			return s_latin1Encoding.GetCharCount(bytes);
		}
	}

	public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
	{
		try
		{
			return s_utf8Encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
		}
		catch (DecoderFallbackException)
		{
			return s_latin1Encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
		}
	}

	public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
	{
		try
		{
			return s_utf8Encoding.GetChars(bytes, byteCount, chars, charCount);
		}
		catch (DecoderFallbackException)
		{
			return s_latin1Encoding.GetChars(bytes, byteCount, chars, charCount);
		}
	}

	public override int GetMaxByteCount(int charCount)
	{
		return s_utf8Encoding.GetMaxByteCount(charCount);
	}

	public override int GetMaxCharCount(int byteCount)
	{
		return byteCount;
	}
}
