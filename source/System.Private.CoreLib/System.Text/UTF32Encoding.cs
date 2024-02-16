using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Text;

public sealed class UTF32Encoding : Encoding
{
	private sealed class UTF32Decoder : DecoderNLS
	{
		internal int iChar;

		internal int readByteCount;

		internal override bool HasState => readByteCount != 0;

		public UTF32Decoder(UTF32Encoding encoding)
			: base(encoding)
		{
		}

		public override void Reset()
		{
			iChar = 0;
			readByteCount = 0;
			if (_fallbackBuffer != null)
			{
				_fallbackBuffer.Reset();
			}
		}
	}

	internal static readonly UTF32Encoding s_default = new UTF32Encoding(bigEndian: false, byteOrderMark: true);

	internal static readonly UTF32Encoding s_bigEndianDefault = new UTF32Encoding(bigEndian: true, byteOrderMark: true);

	private readonly bool _emitUTF32ByteOrderMark;

	private readonly bool _isThrowException;

	private readonly bool _bigEndian;

	public override ReadOnlySpan<byte> Preamble
	{
		get
		{
			if (!(GetType() != typeof(UTF32Encoding)))
			{
				if (_emitUTF32ByteOrderMark)
				{
					if (_bigEndian)
					{
						return new byte[4] { 0, 0, 254, 255 };
					}
					return new byte[4] { 255, 254, 0, 0 };
				}
				return default(ReadOnlySpan<byte>);
			}
			return new ReadOnlySpan<byte>(GetPreamble());
		}
	}

	public UTF32Encoding()
		: this(bigEndian: false, byteOrderMark: true)
	{
	}

	public UTF32Encoding(bool bigEndian, bool byteOrderMark)
		: base(bigEndian ? 12001 : 12000)
	{
		_bigEndian = bigEndian;
		_emitUTF32ByteOrderMark = byteOrderMark;
	}

	public UTF32Encoding(bool bigEndian, bool byteOrderMark, bool throwOnInvalidCharacters)
		: this(bigEndian, byteOrderMark)
	{
		_isThrowException = throwOnInvalidCharacters;
		if (_isThrowException)
		{
			SetDefaultFallbacks();
		}
	}

	internal override void SetDefaultFallbacks()
	{
		if (_isThrowException)
		{
			encoderFallback = System.Text.EncoderFallback.ExceptionFallback;
			decoderFallback = System.Text.DecoderFallback.ExceptionFallback;
		}
		else
		{
			encoderFallback = new EncoderReplacementFallback("\ufffd");
			decoderFallback = new DecoderReplacementFallback("\ufffd");
		}
	}

	public unsafe override int GetByteCount(char[] chars, int index, int count)
	{
		if (chars == null)
		{
			throw new ArgumentNullException("chars", SR.ArgumentNull_Array);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (chars.Length - index < count)
		{
			throw new ArgumentOutOfRangeException("chars", SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (count == 0)
		{
			return 0;
		}
		fixed (char* ptr = chars)
		{
			return GetByteCount(ptr + index, count, null);
		}
	}

	public unsafe override int GetByteCount(string s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		fixed (char* pChars = s)
		{
			return GetByteCount(pChars, s.Length, null);
		}
	}

	[CLSCompliant(false)]
	public unsafe override int GetByteCount(char* chars, int count)
	{
		if (chars == null)
		{
			throw new ArgumentNullException("chars", SR.ArgumentNull_Array);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		return GetByteCount(chars, count, null);
	}

	public unsafe override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
	{
		if (s == null || bytes == null)
		{
			throw new ArgumentNullException((s == null) ? "s" : "bytes", SR.ArgumentNull_Array);
		}
		if (charIndex < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((charIndex < 0) ? "charIndex" : "charCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (s.Length - charIndex < charCount)
		{
			throw new ArgumentOutOfRangeException("s", SR.ArgumentOutOfRange_IndexCount);
		}
		if (byteIndex < 0 || byteIndex > bytes.Length)
		{
			throw new ArgumentOutOfRangeException("byteIndex", SR.ArgumentOutOfRange_Index);
		}
		int byteCount = bytes.Length - byteIndex;
		fixed (char* ptr = s)
		{
			fixed (byte* ptr2 = &MemoryMarshal.GetReference<byte>(bytes))
			{
				return GetBytes(ptr + charIndex, charCount, ptr2 + byteIndex, byteCount, null);
			}
		}
	}

	public unsafe override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
	{
		if (chars == null || bytes == null)
		{
			throw new ArgumentNullException((chars == null) ? "chars" : "bytes", SR.ArgumentNull_Array);
		}
		if (charIndex < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((charIndex < 0) ? "charIndex" : "charCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (chars.Length - charIndex < charCount)
		{
			throw new ArgumentOutOfRangeException("chars", SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (byteIndex < 0 || byteIndex > bytes.Length)
		{
			throw new ArgumentOutOfRangeException("byteIndex", SR.ArgumentOutOfRange_Index);
		}
		if (charCount == 0)
		{
			return 0;
		}
		int byteCount = bytes.Length - byteIndex;
		fixed (char* ptr = chars)
		{
			fixed (byte* ptr2 = &MemoryMarshal.GetReference<byte>(bytes))
			{
				return GetBytes(ptr + charIndex, charCount, ptr2 + byteIndex, byteCount, null);
			}
		}
	}

	[CLSCompliant(false)]
	public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
	{
		if (bytes == null || chars == null)
		{
			throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", SR.ArgumentNull_Array);
		}
		if (charCount < 0 || byteCount < 0)
		{
			throw new ArgumentOutOfRangeException((charCount < 0) ? "charCount" : "byteCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		return GetBytes(chars, charCount, bytes, byteCount, null);
	}

	public unsafe override int GetCharCount(byte[] bytes, int index, int count)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", SR.ArgumentNull_Array);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (bytes.Length - index < count)
		{
			throw new ArgumentOutOfRangeException("bytes", SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (count == 0)
		{
			return 0;
		}
		fixed (byte* ptr = bytes)
		{
			return GetCharCount(ptr + index, count, null);
		}
	}

	[CLSCompliant(false)]
	public unsafe override int GetCharCount(byte* bytes, int count)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", SR.ArgumentNull_Array);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		return GetCharCount(bytes, count, null);
	}

	public unsafe override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
	{
		if (bytes == null || chars == null)
		{
			throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", SR.ArgumentNull_Array);
		}
		if (byteIndex < 0 || byteCount < 0)
		{
			throw new ArgumentOutOfRangeException((byteIndex < 0) ? "byteIndex" : "byteCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (bytes.Length - byteIndex < byteCount)
		{
			throw new ArgumentOutOfRangeException("bytes", SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (charIndex < 0 || charIndex > chars.Length)
		{
			throw new ArgumentOutOfRangeException("charIndex", SR.ArgumentOutOfRange_Index);
		}
		if (byteCount == 0)
		{
			return 0;
		}
		int charCount = chars.Length - charIndex;
		fixed (byte* ptr = bytes)
		{
			fixed (char* ptr2 = &MemoryMarshal.GetReference<char>(chars))
			{
				return GetChars(ptr + byteIndex, byteCount, ptr2 + charIndex, charCount, null);
			}
		}
	}

	[CLSCompliant(false)]
	public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
	{
		if (bytes == null || chars == null)
		{
			throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", SR.ArgumentNull_Array);
		}
		if (charCount < 0 || byteCount < 0)
		{
			throw new ArgumentOutOfRangeException((charCount < 0) ? "charCount" : "byteCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		return GetChars(bytes, byteCount, chars, charCount, null);
	}

	public unsafe override string GetString(byte[] bytes, int index, int count)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", SR.ArgumentNull_Array);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (bytes.Length - index < count)
		{
			throw new ArgumentOutOfRangeException("bytes", SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (count == 0)
		{
			return string.Empty;
		}
		fixed (byte* ptr = bytes)
		{
			return string.CreateStringFromEncoding(ptr + index, count, this);
		}
	}

	internal unsafe override int GetByteCount(char* chars, int count, EncoderNLS encoder)
	{
		char* ptr = chars + count;
		char* charStart = chars;
		int num = 0;
		char c = '\0';
		EncoderFallbackBuffer encoderFallbackBuffer = null;
		if (encoder != null)
		{
			c = encoder._charLeftOver;
			encoderFallbackBuffer = encoder.FallbackBuffer;
			if (encoderFallbackBuffer.Remaining > 0)
			{
				throw new ArgumentException(SR.Format(SR.Argument_EncoderFallbackNotEmpty, EncodingName, encoder.Fallback?.GetType().ToString() ?? string.Empty));
			}
		}
		else
		{
			encoderFallbackBuffer = encoderFallback.CreateFallbackBuffer();
		}
		encoderFallbackBuffer.InternalInitialize(charStart, ptr, encoder, setEncoder: false);
		while (true)
		{
			char c2;
			if ((c2 = encoderFallbackBuffer.InternalGetNextChar()) != 0 || chars < ptr)
			{
				if (c2 == '\0')
				{
					c2 = *chars;
					chars++;
				}
				if (c != 0)
				{
					if (char.IsLowSurrogate(c2))
					{
						c = '\0';
						num += 4;
						continue;
					}
					chars--;
					char* chars2 = chars;
					encoderFallbackBuffer.InternalFallback(c, ref chars2);
					chars = chars2;
					c = '\0';
				}
				else if (char.IsHighSurrogate(c2))
				{
					c = c2;
				}
				else if (char.IsLowSurrogate(c2))
				{
					char* chars2 = chars;
					encoderFallbackBuffer.InternalFallback(c2, ref chars2);
					chars = chars2;
				}
				else
				{
					num += 4;
				}
			}
			else
			{
				if ((encoder != null && !encoder.MustFlush) || c <= '\0')
				{
					break;
				}
				char* chars2 = chars;
				encoderFallbackBuffer.InternalFallback(c, ref chars2);
				chars = chars2;
				c = '\0';
			}
		}
		if (num < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_GetByteCountOverflow);
		}
		return num;
	}

	internal unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, EncoderNLS encoder)
	{
		char* ptr = chars;
		char* ptr2 = chars + charCount;
		byte* ptr3 = bytes;
		byte* ptr4 = bytes + byteCount;
		char c = '\0';
		EncoderFallbackBuffer encoderFallbackBuffer = null;
		if (encoder != null)
		{
			c = encoder._charLeftOver;
			encoderFallbackBuffer = encoder.FallbackBuffer;
			if (encoder._throwOnOverflow && encoderFallbackBuffer.Remaining > 0)
			{
				throw new ArgumentException(SR.Format(SR.Argument_EncoderFallbackNotEmpty, EncodingName, encoder.Fallback?.GetType()));
			}
		}
		else
		{
			encoderFallbackBuffer = encoderFallback.CreateFallbackBuffer();
		}
		encoderFallbackBuffer.InternalInitialize(ptr, ptr2, encoder, setEncoder: true);
		while (true)
		{
			char c2;
			char* chars2;
			if ((c2 = encoderFallbackBuffer.InternalGetNextChar()) != 0 || chars < ptr2)
			{
				if (c2 == '\0')
				{
					c2 = *chars;
					chars++;
				}
				if (c != 0)
				{
					if (!char.IsLowSurrogate(c2))
					{
						chars--;
						chars2 = chars;
						encoderFallbackBuffer.InternalFallback(c, ref chars2);
						chars = chars2;
						c = '\0';
						continue;
					}
					uint surrogate = GetSurrogate(c, c2);
					c = '\0';
					if (bytes + 3 < ptr4)
					{
						if (_bigEndian)
						{
							*(bytes++) = 0;
							*(bytes++) = (byte)(surrogate >> 16);
							*(bytes++) = (byte)(surrogate >> 8);
							*(bytes++) = (byte)surrogate;
						}
						else
						{
							*(bytes++) = (byte)surrogate;
							*(bytes++) = (byte)(surrogate >> 8);
							*(bytes++) = (byte)(surrogate >> 16);
							*(bytes++) = 0;
						}
						continue;
					}
					if (encoderFallbackBuffer.bFallingBack)
					{
						encoderFallbackBuffer.MovePrevious();
						encoderFallbackBuffer.MovePrevious();
					}
					else
					{
						chars -= 2;
					}
					ThrowBytesOverflow(encoder, bytes == ptr3);
					c = '\0';
				}
				else
				{
					if (char.IsHighSurrogate(c2))
					{
						c = c2;
						continue;
					}
					if (char.IsLowSurrogate(c2))
					{
						chars2 = chars;
						encoderFallbackBuffer.InternalFallback(c2, ref chars2);
						chars = chars2;
						continue;
					}
					if (bytes + 3 < ptr4)
					{
						if (_bigEndian)
						{
							*(bytes++) = 0;
							*(bytes++) = 0;
							*(bytes++) = (byte)((uint)c2 >> 8);
							*(bytes++) = (byte)c2;
						}
						else
						{
							*(bytes++) = (byte)c2;
							*(bytes++) = (byte)((uint)c2 >> 8);
							*(bytes++) = 0;
							*(bytes++) = 0;
						}
						continue;
					}
					if (encoderFallbackBuffer.bFallingBack)
					{
						encoderFallbackBuffer.MovePrevious();
					}
					else
					{
						chars--;
					}
					ThrowBytesOverflow(encoder, bytes == ptr3);
				}
			}
			if ((encoder != null && !encoder.MustFlush) || c <= '\0')
			{
				break;
			}
			chars2 = chars;
			encoderFallbackBuffer.InternalFallback(c, ref chars2);
			chars = chars2;
			c = '\0';
		}
		if (encoder != null)
		{
			encoder._charLeftOver = c;
			encoder._charsUsed = (int)(chars - ptr);
		}
		return (int)(bytes - ptr3);
	}

	internal unsafe override int GetCharCount(byte* bytes, int count, DecoderNLS baseDecoder)
	{
		UTF32Decoder uTF32Decoder = (UTF32Decoder)baseDecoder;
		int num = 0;
		byte* ptr = bytes + count;
		byte* byteStart = bytes;
		int num2 = 0;
		uint num3 = 0u;
		DecoderFallbackBuffer decoderFallbackBuffer = null;
		if (uTF32Decoder != null)
		{
			num2 = uTF32Decoder.readByteCount;
			num3 = (uint)uTF32Decoder.iChar;
			decoderFallbackBuffer = uTF32Decoder.FallbackBuffer;
		}
		else
		{
			decoderFallbackBuffer = decoderFallback.CreateFallbackBuffer();
		}
		decoderFallbackBuffer.InternalInitialize(byteStart, null);
		while (bytes < ptr && num >= 0)
		{
			if (_bigEndian)
			{
				num3 <<= 8;
				num3 += *(bytes++);
			}
			else
			{
				num3 >>= 8;
				num3 += (uint)(*(bytes++) << 24);
			}
			num2++;
			if (num2 < 4)
			{
				continue;
			}
			num2 = 0;
			if (num3 > 1114111 || (num3 >= 55296 && num3 <= 57343))
			{
				byte[] bytes2 = ((!_bigEndian) ? new byte[4]
				{
					(byte)num3,
					(byte)(num3 >> 8),
					(byte)(num3 >> 16),
					(byte)(num3 >> 24)
				} : new byte[4]
				{
					(byte)(num3 >> 24),
					(byte)(num3 >> 16),
					(byte)(num3 >> 8),
					(byte)num3
				});
				num += decoderFallbackBuffer.InternalFallback(bytes2, bytes);
				num3 = 0u;
			}
			else
			{
				if (num3 >= 65536)
				{
					num++;
				}
				num++;
				num3 = 0u;
			}
		}
		if (num2 > 0 && (uTF32Decoder == null || uTF32Decoder.MustFlush))
		{
			byte[] array = new byte[num2];
			if (_bigEndian)
			{
				while (num2 > 0)
				{
					array[--num2] = (byte)num3;
					num3 >>= 8;
				}
			}
			else
			{
				while (num2 > 0)
				{
					array[--num2] = (byte)(num3 >> 24);
					num3 <<= 8;
				}
			}
			num += decoderFallbackBuffer.InternalFallback(array, bytes);
		}
		if (num < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_GetByteCountOverflow);
		}
		return num;
	}

	internal unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount, DecoderNLS baseDecoder)
	{
		UTF32Decoder uTF32Decoder = (UTF32Decoder)baseDecoder;
		char* ptr = chars;
		char* ptr2 = chars + charCount;
		byte* ptr3 = bytes;
		byte* ptr4 = bytes + byteCount;
		int num = 0;
		uint num2 = 0u;
		DecoderFallbackBuffer decoderFallbackBuffer = null;
		if (uTF32Decoder != null)
		{
			num = uTF32Decoder.readByteCount;
			num2 = (uint)uTF32Decoder.iChar;
			decoderFallbackBuffer = baseDecoder.FallbackBuffer;
		}
		else
		{
			decoderFallbackBuffer = decoderFallback.CreateFallbackBuffer();
		}
		decoderFallbackBuffer.InternalInitialize(bytes, chars + charCount);
		while (bytes < ptr4)
		{
			if (_bigEndian)
			{
				num2 <<= 8;
				num2 += *(bytes++);
			}
			else
			{
				num2 >>= 8;
				num2 += (uint)(*(bytes++) << 24);
			}
			num++;
			if (num < 4)
			{
				continue;
			}
			num = 0;
			if (num2 > 1114111 || (num2 >= 55296 && num2 <= 57343))
			{
				byte[] bytes2 = ((!_bigEndian) ? new byte[4]
				{
					(byte)num2,
					(byte)(num2 >> 8),
					(byte)(num2 >> 16),
					(byte)(num2 >> 24)
				} : new byte[4]
				{
					(byte)(num2 >> 24),
					(byte)(num2 >> 16),
					(byte)(num2 >> 8),
					(byte)num2
				});
				char* chars2 = chars;
				bool flag = decoderFallbackBuffer.InternalFallback(bytes2, bytes, ref chars2);
				chars = chars2;
				if (!flag)
				{
					bytes -= 4;
					num2 = 0u;
					decoderFallbackBuffer.InternalReset();
					ThrowCharsOverflow(uTF32Decoder, chars == ptr);
					break;
				}
				num2 = 0u;
				continue;
			}
			if (num2 >= 65536)
			{
				if (chars >= ptr2 - 1)
				{
					bytes -= 4;
					num2 = 0u;
					ThrowCharsOverflow(uTF32Decoder, chars == ptr);
					break;
				}
				*(chars++) = GetHighSurrogate(num2);
				num2 = GetLowSurrogate(num2);
			}
			else if (chars >= ptr2)
			{
				bytes -= 4;
				num2 = 0u;
				ThrowCharsOverflow(uTF32Decoder, chars == ptr);
				break;
			}
			*(chars++) = (char)num2;
			num2 = 0u;
		}
		if (num > 0 && (uTF32Decoder == null || uTF32Decoder.MustFlush))
		{
			byte[] array = new byte[num];
			int num3 = num;
			if (_bigEndian)
			{
				while (num3 > 0)
				{
					array[--num3] = (byte)num2;
					num2 >>= 8;
				}
			}
			else
			{
				while (num3 > 0)
				{
					array[--num3] = (byte)(num2 >> 24);
					num2 <<= 8;
				}
			}
			char* chars2 = chars;
			bool flag2 = decoderFallbackBuffer.InternalFallback(array, bytes, ref chars2);
			chars = chars2;
			if (!flag2)
			{
				decoderFallbackBuffer.InternalReset();
				ThrowCharsOverflow(uTF32Decoder, chars == ptr);
			}
			else
			{
				num = 0;
				num2 = 0u;
			}
		}
		if (uTF32Decoder != null)
		{
			uTF32Decoder.iChar = (int)num2;
			uTF32Decoder.readByteCount = num;
			uTF32Decoder._bytesUsed = (int)(bytes - ptr3);
		}
		return (int)(chars - ptr);
	}

	private static uint GetSurrogate(char cHigh, char cLow)
	{
		return (uint)((cHigh - 55296) * 1024 + (cLow - 56320) + 65536);
	}

	private static char GetHighSurrogate(uint iChar)
	{
		return (char)((iChar - 65536) / 1024 + 55296);
	}

	private static char GetLowSurrogate(uint iChar)
	{
		return (char)((iChar - 65536) % 1024 + 56320);
	}

	public override Decoder GetDecoder()
	{
		return new UTF32Decoder(this);
	}

	public override Encoder GetEncoder()
	{
		return new EncoderNLS(this);
	}

	public override int GetMaxByteCount(int charCount)
	{
		if (charCount < 0)
		{
			throw new ArgumentOutOfRangeException("charCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		long num = (long)charCount + 1L;
		if (base.EncoderFallback.MaxCharCount > 1)
		{
			num *= base.EncoderFallback.MaxCharCount;
		}
		num *= 4;
		if (num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("charCount", SR.ArgumentOutOfRange_GetByteCountOverflow);
		}
		return (int)num;
	}

	public override int GetMaxCharCount(int byteCount)
	{
		if (byteCount < 0)
		{
			throw new ArgumentOutOfRangeException("byteCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		int num = byteCount / 2 + 2;
		if (base.DecoderFallback.MaxCharCount > 2)
		{
			num *= base.DecoderFallback.MaxCharCount;
			num /= 2;
		}
		if (num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("byteCount", SR.ArgumentOutOfRange_GetCharCountOverflow);
		}
		return num;
	}

	public override byte[] GetPreamble()
	{
		if (_emitUTF32ByteOrderMark)
		{
			if (!_bigEndian)
			{
				return new byte[4] { 255, 254, 0, 0 };
			}
			return new byte[4] { 0, 0, 254, 255 };
		}
		return Array.Empty<byte>();
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is UTF32Encoding uTF32Encoding)
		{
			if (_emitUTF32ByteOrderMark == uTF32Encoding._emitUTF32ByteOrderMark && _bigEndian == uTF32Encoding._bigEndian && base.EncoderFallback.Equals(uTF32Encoding.EncoderFallback))
			{
				return base.DecoderFallback.Equals(uTF32Encoding.DecoderFallback);
			}
			return false;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.EncoderFallback.GetHashCode() + base.DecoderFallback.GetHashCode() + CodePage + (_emitUTF32ByteOrderMark ? 4 : 0) + (_bigEndian ? 8 : 0);
	}
}
