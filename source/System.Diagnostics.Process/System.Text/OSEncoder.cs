using System.Runtime.CompilerServices;

namespace System.Text;

internal sealed class OSEncoder : Encoder
{
	private char _charLeftOver;

	private readonly Encoding _encoding;

	internal OSEncoder(Encoding encoding)
	{
		_encoding = encoding;
		Reset();
	}

	public override void Reset()
	{
		_charLeftOver = '\0';
	}

	public unsafe override int GetByteCount(char[] chars, int index, int count, bool flush)
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
		if (chars.Length == 0 && (_charLeftOver == '\0' || !flush))
		{
			return 0;
		}
		fixed (char* ptr = chars)
		{
			Unsafe.SkipInit(out char c);
			char* chars2 = ((ptr == null) ? (&c) : (ptr + index));
			return GetByteCount(chars2, count, flush);
		}
	}

	private unsafe int ConvertWithLeftOverChar(char* chars, int count, byte* bytes, int byteCount)
	{
		char* ptr = stackalloc char[2];
		*ptr = _charLeftOver;
		int num = 0;
		if (count > 0 && char.IsLowSurrogate(*chars))
		{
			ptr[1] = *chars;
			num++;
		}
		int num2 = OSEncoding.WideCharToMultiByte(_encoding.CodePage, ptr, num + 1, bytes, byteCount);
		if (count - num > 0)
		{
			num2 += OSEncoding.WideCharToMultiByte(_encoding.CodePage, chars + num, count - num, (bytes == null) ? null : (bytes + num2), (bytes != null) ? (byteCount - num2) : 0);
		}
		return num2;
	}

	public unsafe override int GetByteCount(char* chars, int count, bool flush)
	{
		if (chars == null)
		{
			throw new ArgumentNullException("chars", System.SR.ArgumentNull_Array);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		bool flag = count > 0 && !flush && char.IsHighSurrogate(chars[count - 1]);
		if (flag)
		{
			count--;
		}
		if (_charLeftOver == '\0')
		{
			if (count <= 0)
			{
				return 0;
			}
			return OSEncoding.WideCharToMultiByte(_encoding.CodePage, chars, count, null, 0);
		}
		if (count == 0 && !flag && !flush)
		{
			return 0;
		}
		return ConvertWithLeftOverChar(chars, count, null, 0);
	}

	public unsafe override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush)
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
		if (bytes.Length == 0)
		{
			return 0;
		}
		if (charCount == 0 && (_charLeftOver == '\0' || !flush))
		{
			return 0;
		}
		fixed (char* ptr = chars)
		{
			fixed (byte* ptr2 = &bytes[0])
			{
				Unsafe.SkipInit(out char c);
				char* chars2 = ((ptr == null) ? (&c) : (ptr + charIndex));
				return GetBytes(chars2, charCount, ptr2 + byteIndex, bytes.Length - byteIndex, flush);
			}
		}
	}

	public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, bool flush)
	{
		if (chars == null || bytes == null)
		{
			throw new ArgumentNullException((chars == null) ? "chars" : "bytes", System.SR.ArgumentNull_Array);
		}
		if (byteCount < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((byteCount < 0) ? "byteCount" : "charCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (byteCount == 0)
		{
			return 0;
		}
		char c = ((charCount > 0 && !flush && char.IsHighSurrogate(chars[charCount - 1])) ? chars[charCount - 1] : '\0');
		if (c != 0)
		{
			charCount--;
		}
		if (_charLeftOver == '\0')
		{
			if (charCount <= 0)
			{
				_charLeftOver = c;
				return 0;
			}
			int result = OSEncoding.WideCharToMultiByte(_encoding.CodePage, chars, charCount, bytes, byteCount);
			_charLeftOver = c;
			return result;
		}
		if (charCount == 0 && c == '\0' && !flush)
		{
			return 0;
		}
		int result2 = ConvertWithLeftOverChar(chars, charCount, bytes, byteCount);
		_charLeftOver = c;
		return result2;
	}

	public unsafe override void Convert(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
	{
		if (chars == null || bytes == null)
		{
			throw new ArgumentNullException((chars == null) ? "chars" : "bytes", System.SR.ArgumentNull_Array);
		}
		if (charIndex < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((charIndex < 0) ? "charIndex" : "charCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (byteIndex < 0 || byteCount < 0)
		{
			throw new ArgumentOutOfRangeException((byteIndex < 0) ? "byteIndex" : "byteCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (chars.Length - charIndex < charCount)
		{
			throw new ArgumentOutOfRangeException("chars", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (bytes.Length - byteIndex < byteCount)
		{
			throw new ArgumentOutOfRangeException("bytes", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (bytes.Length == 0 || (chars.Length == 0 && (_charLeftOver == '\0' || !flush)))
		{
			bytesUsed = 0;
			charsUsed = 0;
			completed = false;
			return;
		}
		fixed (char* ptr = chars)
		{
			fixed (byte* ptr2 = &bytes[0])
			{
				Unsafe.SkipInit(out char c);
				char* chars2 = ((ptr == null) ? (&c) : (ptr + charIndex));
				Convert(chars2, charCount, ptr2 + byteIndex, byteCount, flush, out charsUsed, out bytesUsed, out completed);
			}
		}
	}

	public unsafe override void Convert(char* chars, int charCount, byte* bytes, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
	{
		if (bytes == null || chars == null)
		{
			throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", System.SR.ArgumentNull_Array);
		}
		if (charCount < 0 || byteCount < 0)
		{
			throw new ArgumentOutOfRangeException((charCount < 0) ? "charCount" : "byteCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		int num;
		for (num = charCount; num > 0; num /= 2)
		{
			int byteCount2 = GetByteCount(chars, num, flush);
			if (byteCount2 <= byteCount)
			{
				break;
			}
		}
		if (num > 0)
		{
			bytesUsed = GetBytes(chars, num, bytes, byteCount, flush);
			charsUsed = num;
			completed = _charLeftOver == '\0' && charCount == num;
		}
		else
		{
			bytesUsed = 0;
			charsUsed = 0;
			completed = false;
		}
	}
}
