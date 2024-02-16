namespace System.Text;

internal sealed class OSEncoding : Encoding
{
	private readonly int _codePage;

	private string _encodingName;

	public override string EncodingName
	{
		get
		{
			if (_encodingName == null)
			{
				_encodingName = "Codepage - " + _codePage;
			}
			return _encodingName;
		}
	}

	public override string WebName => EncodingName;

	internal OSEncoding(int codePage)
		: base(codePage)
	{
		_codePage = codePage;
	}

	public unsafe override int GetByteCount(char[] chars, int index, int count)
	{
		if (chars == null)
		{
			throw new ArgumentNullException("chars", System.SR.ArgumentNull_Array);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (chars.Length - index < count)
		{
			throw new ArgumentOutOfRangeException("chars", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (count == 0)
		{
			return 0;
		}
		fixed (char* ptr = chars)
		{
			return WideCharToMultiByte(_codePage, ptr + index, count, null, 0);
		}
	}

	public unsafe override int GetByteCount(string s)
	{
		if (s == null)
		{
			throw new ArgumentNullException("s");
		}
		if (s.Length == 0)
		{
			return 0;
		}
		fixed (char* pChars = s)
		{
			return WideCharToMultiByte(_codePage, pChars, s.Length, null, 0);
		}
	}

	public unsafe override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
	{
		if (s == null || bytes == null)
		{
			throw new ArgumentNullException((s == null) ? "s" : "bytes", System.SR.ArgumentNull_Array);
		}
		if (charIndex < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((charIndex < 0) ? "charIndex" : "charCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (s.Length - charIndex < charCount)
		{
			throw new ArgumentOutOfRangeException("s", System.SR.ArgumentOutOfRange_IndexCount);
		}
		if (byteIndex < 0 || byteIndex > bytes.Length)
		{
			throw new ArgumentOutOfRangeException("byteIndex", System.SR.ArgumentOutOfRange_Index);
		}
		if (charCount == 0)
		{
			return 0;
		}
		if (bytes.Length == 0)
		{
			throw new ArgumentOutOfRangeException(System.SR.Argument_EncodingConversionOverflowBytes);
		}
		fixed (char* ptr = s)
		{
			fixed (byte* ptr2 = &bytes[0])
			{
				return WideCharToMultiByte(_codePage, ptr + charIndex, charCount, ptr2 + byteIndex, bytes.Length - byteIndex);
			}
		}
	}

	public unsafe override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
	{
		if (chars == null || bytes == null)
		{
			throw new ArgumentNullException((chars == null) ? "chars" : "bytes", System.SR.ArgumentNull_Array);
		}
		if (charIndex < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((charIndex < 0) ? "charIndex" : "charCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (chars.Length - charIndex < charCount)
		{
			throw new ArgumentOutOfRangeException("chars", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (byteIndex < 0 || byteIndex > bytes.Length)
		{
			throw new ArgumentOutOfRangeException("byteIndex", System.SR.ArgumentOutOfRange_Index);
		}
		if (charCount == 0)
		{
			return 0;
		}
		if (bytes.Length == 0)
		{
			throw new ArgumentOutOfRangeException(System.SR.Argument_EncodingConversionOverflowBytes);
		}
		fixed (char* ptr = chars)
		{
			fixed (byte* ptr2 = &bytes[0])
			{
				return WideCharToMultiByte(_codePage, ptr + charIndex, charCount, ptr2 + byteIndex, bytes.Length - byteIndex);
			}
		}
	}

	public unsafe override int GetCharCount(byte[] bytes, int index, int count)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", System.SR.ArgumentNull_Array);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (bytes.Length - index < count)
		{
			throw new ArgumentOutOfRangeException("bytes", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (count == 0)
		{
			return 0;
		}
		fixed (byte* ptr = bytes)
		{
			return MultiByteToWideChar(_codePage, ptr + index, count, null, 0);
		}
	}

	public unsafe override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
	{
		if (bytes == null || chars == null)
		{
			throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", System.SR.ArgumentNull_Array);
		}
		if (byteIndex < 0 || byteCount < 0)
		{
			throw new ArgumentOutOfRangeException((byteIndex < 0) ? "byteIndex" : "byteCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (bytes.Length - byteIndex < byteCount)
		{
			throw new ArgumentOutOfRangeException("bytes", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (charIndex < 0 || charIndex > chars.Length)
		{
			throw new ArgumentOutOfRangeException("charIndex", System.SR.ArgumentOutOfRange_Index);
		}
		if (byteCount == 0)
		{
			return 0;
		}
		if (chars.Length == 0)
		{
			throw new ArgumentOutOfRangeException(System.SR.Argument_EncodingConversionOverflowChars);
		}
		fixed (byte* ptr = bytes)
		{
			fixed (char* ptr2 = &chars[0])
			{
				return MultiByteToWideChar(_codePage, ptr + byteIndex, byteCount, ptr2 + charIndex, chars.Length - charIndex);
			}
		}
	}

	public override int GetMaxByteCount(int charCount)
	{
		if (charCount < 0)
		{
			throw new ArgumentOutOfRangeException("charCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		long num = (long)charCount * 14L;
		if (num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("charCount", System.SR.ArgumentOutOfRange_GetByteCountOverflow);
		}
		return (int)num;
	}

	public override int GetMaxCharCount(int byteCount)
	{
		if (byteCount < 0)
		{
			throw new ArgumentOutOfRangeException("byteCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		long num = byteCount * 4;
		if (num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("byteCount", System.SR.ArgumentOutOfRange_GetCharCountOverflow);
		}
		return (int)num;
	}

	public override Encoder GetEncoder()
	{
		return new OSEncoder(this);
	}

	public override Decoder GetDecoder()
	{
		switch (CodePage)
		{
		case 932:
		case 936:
		case 949:
		case 950:
		case 1361:
		case 10001:
		case 10002:
		case 10003:
		case 10008:
		case 20000:
		case 20001:
		case 20002:
		case 20003:
		case 20004:
		case 20005:
		case 20261:
		case 20932:
		case 20936:
		case 51949:
			return new DecoderDBCS(this);
		default:
			return base.GetDecoder();
		}
	}

	internal unsafe static int WideCharToMultiByte(int codePage, char* pChars, int count, byte* pBytes, int byteCount)
	{
		int num = global::Interop.Kernel32.WideCharToMultiByte((uint)codePage, 0u, pChars, count, pBytes, byteCount, IntPtr.Zero, IntPtr.Zero);
		if (num <= 0)
		{
			throw new ArgumentException(System.SR.Argument_InvalidCharSequenceNoIndex);
		}
		return num;
	}

	internal unsafe static int MultiByteToWideChar(int codePage, byte* pBytes, int byteCount, char* pChars, int count)
	{
		int num = global::Interop.Kernel32.MultiByteToWideChar((uint)codePage, 0u, pBytes, byteCount, pChars, count);
		if (num <= 0)
		{
			throw new ArgumentException(System.SR.Argument_InvalidCharSequenceNoIndex);
		}
		return num;
	}
}
