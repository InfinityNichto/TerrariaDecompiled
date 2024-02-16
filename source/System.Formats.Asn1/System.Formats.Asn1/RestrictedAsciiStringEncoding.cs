using System.Collections.Generic;

namespace System.Formats.Asn1;

internal abstract class RestrictedAsciiStringEncoding : SpanBasedEncoding
{
	private readonly bool[] _isAllowed;

	protected RestrictedAsciiStringEncoding(byte minCharAllowed, byte maxCharAllowed)
	{
		bool[] array = new bool[128];
		for (byte b = minCharAllowed; b <= maxCharAllowed; b++)
		{
			array[b] = true;
		}
		_isAllowed = array;
	}

	protected RestrictedAsciiStringEncoding(IEnumerable<char> allowedChars)
	{
		bool[] array = new bool[127];
		foreach (char allowedChar in allowedChars)
		{
			if (allowedChar >= array.Length)
			{
				throw new ArgumentOutOfRangeException("allowedChars");
			}
			array[(uint)allowedChar] = true;
		}
		_isAllowed = array;
	}

	public override int GetMaxByteCount(int charCount)
	{
		return charCount;
	}

	public override int GetMaxCharCount(int byteCount)
	{
		return byteCount;
	}

	protected override int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, bool write)
	{
		if (chars.IsEmpty)
		{
			return 0;
		}
		for (int i = 0; i < chars.Length; i++)
		{
			char c = chars[i];
			if ((uint)c >= (uint)_isAllowed.Length || !_isAllowed[(uint)c])
			{
				base.EncoderFallback.CreateFallbackBuffer().Fallback(c, i);
				throw new InvalidOperationException();
			}
			if (write)
			{
				bytes[i] = (byte)c;
			}
		}
		return chars.Length;
	}

	protected override int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars, bool write)
	{
		if (bytes.IsEmpty)
		{
			return 0;
		}
		for (int i = 0; i < bytes.Length; i++)
		{
			byte b = bytes[i];
			if ((uint)b >= (uint)_isAllowed.Length || !_isAllowed[b])
			{
				base.DecoderFallback.CreateFallbackBuffer().Fallback(new byte[1] { b }, i);
				throw new InvalidOperationException();
			}
			if (write)
			{
				chars[i] = (char)b;
			}
		}
		return bytes.Length;
	}
}
