using System.Runtime.CompilerServices;

namespace System.Text;

internal sealed class DecoderDBCS : Decoder
{
	private readonly Encoding _encoding;

	private readonly byte[] _leadByteRanges = new byte[10];

	private readonly int _rangesCount;

	private byte _leftOverLeadByte;

	internal DecoderDBCS(Encoding encoding)
	{
		_encoding = encoding;
		_rangesCount = global::Interop.Kernel32.GetLeadByteRanges(_encoding.CodePage, _leadByteRanges);
		Reset();
	}

	private bool IsLeadByte(byte b)
	{
		if (b < _leadByteRanges[0])
		{
			return false;
		}
		for (int i = 0; i < _rangesCount; i += 2)
		{
			if (b >= _leadByteRanges[i] && b <= _leadByteRanges[i + 1])
			{
				return true;
			}
		}
		return false;
	}

	public override void Reset()
	{
		_leftOverLeadByte = 0;
	}

	public override int GetCharCount(byte[] bytes, int index, int count)
	{
		return GetCharCount(bytes, index, count, flush: false);
	}

	public unsafe override int GetCharCount(byte[] bytes, int index, int count, bool flush)
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
		if (count == 0 && (_leftOverLeadByte == 0 || !flush))
		{
			return 0;
		}
		fixed (byte* ptr = bytes)
		{
			Unsafe.SkipInit(out byte b);
			byte* bytes2 = ((ptr == null) ? (&b) : (ptr + index));
			return GetCharCount(bytes2, count, flush);
		}
	}

	private unsafe int ConvertWithLeftOverByte(byte* bytes, int count, char* chars, int charCount)
	{
		byte* ptr = stackalloc byte[2];
		*ptr = _leftOverLeadByte;
		int num = 0;
		if (count > 0)
		{
			ptr[1] = *bytes;
			num++;
		}
		int num2 = OSEncoding.MultiByteToWideChar(_encoding.CodePage, ptr, num + 1, chars, charCount);
		if (count - num > 0)
		{
			num2 += OSEncoding.MultiByteToWideChar(_encoding.CodePage, bytes + num, count - num, (chars == null) ? null : (chars + num2), (chars != null) ? (charCount - num2) : 0);
		}
		return num2;
	}

	public unsafe override int GetCharCount(byte* bytes, int count, bool flush)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", System.SR.ArgumentNull_Array);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		bool flag = count > 0 && !flush && IsLastByteALeadByte(bytes, count);
		if (flag)
		{
			count--;
		}
		if (_leftOverLeadByte == 0)
		{
			if (count <= 0)
			{
				return 0;
			}
			return OSEncoding.MultiByteToWideChar(_encoding.CodePage, bytes, count, null, 0);
		}
		if (count == 0 && !flag && !flush)
		{
			return 0;
		}
		return ConvertWithLeftOverByte(bytes, count, null, 0);
	}

	public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
	{
		return GetChars(bytes, byteIndex, byteCount, chars, charIndex, flush: false);
	}

	public unsafe override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush)
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
		if (chars.Length == 0)
		{
			return 0;
		}
		if (byteCount == 0 && (_leftOverLeadByte == 0 || !flush))
		{
			return 0;
		}
		fixed (char* ptr2 = &chars[0])
		{
			fixed (byte* ptr = bytes)
			{
				Unsafe.SkipInit(out byte b);
				byte* bytes2 = ((ptr == null) ? (&b) : (ptr + byteIndex));
				return GetChars(bytes2, byteCount, ptr2 + charIndex, chars.Length - charIndex, flush);
			}
		}
	}

	public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount, bool flush)
	{
		if (chars == null || bytes == null)
		{
			throw new ArgumentNullException((chars == null) ? "chars" : "bytes", System.SR.ArgumentNull_Array);
		}
		if (byteCount < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((byteCount < 0) ? "byteCount" : "charCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (charCount == 0)
		{
			return 0;
		}
		byte b = (byte)((byteCount > 0 && !flush && IsLastByteALeadByte(bytes, byteCount)) ? bytes[byteCount - 1] : 0);
		if (b != 0)
		{
			byteCount--;
		}
		if (_leftOverLeadByte == 0)
		{
			if (byteCount <= 0)
			{
				_leftOverLeadByte = b;
				return 0;
			}
			int result = OSEncoding.MultiByteToWideChar(_encoding.CodePage, bytes, byteCount, chars, charCount);
			_leftOverLeadByte = b;
			return result;
		}
		if (byteCount == 0 && b == 0 && !flush)
		{
			return 0;
		}
		int result2 = ConvertWithLeftOverByte(bytes, byteCount, chars, charCount);
		_leftOverLeadByte = b;
		return result2;
	}

	public unsafe override void Convert(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
	{
		if (bytes == null || chars == null)
		{
			throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", System.SR.ArgumentNull_Array);
		}
		if (byteIndex < 0 || byteCount < 0)
		{
			throw new ArgumentOutOfRangeException((byteIndex < 0) ? "byteIndex" : "byteCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (charIndex < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((charIndex < 0) ? "charIndex" : "charCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (bytes.Length - byteIndex < byteCount)
		{
			throw new ArgumentOutOfRangeException("bytes", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (chars.Length - charIndex < charCount)
		{
			throw new ArgumentOutOfRangeException("chars", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (charCount == 0 || (bytes.Length == 0 && (_leftOverLeadByte == 0 || !flush)))
		{
			bytesUsed = 0;
			charsUsed = 0;
			completed = false;
			return;
		}
		fixed (char* ptr2 = &chars[0])
		{
			fixed (byte* ptr = bytes)
			{
				Unsafe.SkipInit(out byte b);
				byte* bytes2 = ((ptr == null) ? (&b) : (ptr + byteIndex));
				Convert(bytes2, byteCount, ptr2 + charIndex, charCount, flush, out bytesUsed, out charsUsed, out completed);
			}
		}
	}

	public unsafe override void Convert(byte* bytes, int byteCount, char* chars, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
	{
		if (chars == null || bytes == null)
		{
			throw new ArgumentNullException((chars == null) ? "chars" : "bytes", System.SR.ArgumentNull_Array);
		}
		if (byteCount < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((byteCount < 0) ? "byteCount" : "charCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		int num;
		for (num = byteCount; num > 0; num /= 2)
		{
			int charCount2 = GetCharCount(bytes, num, flush);
			if (charCount2 <= charCount)
			{
				break;
			}
		}
		if (num > 0)
		{
			charsUsed = GetChars(bytes, num, chars, charCount, flush);
			bytesUsed = num;
			completed = _leftOverLeadByte == 0 && byteCount == num;
		}
		else
		{
			bytesUsed = 0;
			charsUsed = 0;
			completed = false;
		}
	}

	private unsafe bool IsLastByteALeadByte(byte* bytes, int count)
	{
		if (!IsLeadByte(bytes[count - 1]))
		{
			return false;
		}
		int i = 0;
		if (_leftOverLeadByte != 0)
		{
			i++;
		}
		for (; i < count; i++)
		{
			if (IsLeadByte(bytes[i]))
			{
				i++;
				if (i >= count)
				{
					return true;
				}
			}
		}
		return false;
	}
}
