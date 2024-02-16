using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System.Text;

public class UnicodeEncoding : Encoding
{
	private sealed class Decoder : DecoderNLS
	{
		internal int lastByte = -1;

		internal char lastChar;

		internal override bool HasState
		{
			get
			{
				if (lastByte == -1)
				{
					return lastChar != '\0';
				}
				return true;
			}
		}

		public Decoder(UnicodeEncoding encoding)
			: base(encoding)
		{
		}

		public override void Reset()
		{
			lastByte = -1;
			lastChar = '\0';
			if (_fallbackBuffer != null)
			{
				_fallbackBuffer.Reset();
			}
		}
	}

	internal static readonly UnicodeEncoding s_bigEndianDefault = new UnicodeEncoding(bigEndian: true, byteOrderMark: true);

	internal static readonly UnicodeEncoding s_littleEndianDefault = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);

	private readonly bool isThrowException;

	private readonly bool bigEndian;

	private readonly bool byteOrderMark;

	public const int CharSize = 2;

	public override ReadOnlySpan<byte> Preamble
	{
		get
		{
			if (!(GetType() != typeof(UnicodeEncoding)))
			{
				if (byteOrderMark)
				{
					if (bigEndian)
					{
						return new byte[2] { 254, 255 };
					}
					return new byte[2] { 255, 254 };
				}
				return default(ReadOnlySpan<byte>);
			}
			return new ReadOnlySpan<byte>(GetPreamble());
		}
	}

	public UnicodeEncoding()
		: this(bigEndian: false, byteOrderMark: true)
	{
	}

	public UnicodeEncoding(bool bigEndian, bool byteOrderMark)
		: base(bigEndian ? 1201 : 1200)
	{
		this.bigEndian = bigEndian;
		this.byteOrderMark = byteOrderMark;
	}

	public UnicodeEncoding(bool bigEndian, bool byteOrderMark, bool throwOnInvalidBytes)
		: this(bigEndian, byteOrderMark)
	{
		isThrowException = throwOnInvalidBytes;
		if (isThrowException)
		{
			SetDefaultFallbacks();
		}
	}

	internal sealed override void SetDefaultFallbacks()
	{
		if (isThrowException)
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

	internal unsafe sealed override int GetByteCount(char* chars, int count, EncoderNLS encoder)
	{
		int num = count << 1;
		if (num < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_GetByteCountOverflow);
		}
		char* charStart = chars;
		char* ptr = chars + count;
		char c = '\0';
		bool flag = false;
		EncoderFallbackBuffer encoderFallbackBuffer = null;
		if (encoder != null)
		{
			c = encoder._charLeftOver;
			if (c > '\0')
			{
				num += 2;
			}
			if (encoder.InternalHasFallbackBuffer)
			{
				encoderFallbackBuffer = encoder.FallbackBuffer;
				if (encoderFallbackBuffer.Remaining > 0)
				{
					throw new ArgumentException(SR.Format(SR.Argument_EncoderFallbackNotEmpty, EncodingName, encoder.Fallback?.GetType()));
				}
				encoderFallbackBuffer.InternalInitialize(charStart, ptr, encoder, setEncoder: false);
			}
		}
		while (true)
		{
			char num2 = encoderFallbackBuffer?.InternalGetNextChar() ?? '\0';
			char c2 = num2;
			char* chars2;
			if (num2 != 0 || chars < ptr)
			{
				if (c2 == '\0')
				{
					if ((bigEndian ^ BitConverter.IsLittleEndian) && ((ulong)chars & 7uL) == 0L && c == '\0')
					{
						ulong* ptr2 = (ulong*)(ptr - 3);
						ulong* ptr3;
						for (ptr3 = (ulong*)chars; ptr3 < ptr2; ptr3++)
						{
							if ((0x8000800080008000uL & *ptr3) == 0L)
							{
								continue;
							}
							ulong num3 = (0xF800F800F800F800uL & *ptr3) ^ 0xD800D800D800D800uL;
							if ((num3 & 0xFFFF000000000000uL) == 0L || (num3 & 0xFFFF00000000L) == 0L || (num3 & 0xFFFF0000u) == 0L || (num3 & 0xFFFF) == 0L)
							{
								long num4 = -287953294993589248L & (long)(*ptr3);
								if (!BitConverter.IsLittleEndian)
								{
								}
								if (num4 != -2593835887162763264L)
								{
									break;
								}
							}
						}
						chars = (char*)ptr3;
						if (chars >= ptr)
						{
							goto IL_0295;
						}
					}
					c2 = *chars;
					chars++;
				}
				else
				{
					num += 2;
				}
				if (c2 >= '\ud800' && c2 <= '\udfff')
				{
					if (c2 <= '\udbff')
					{
						if (c > '\0')
						{
							chars--;
							num -= 2;
							if (encoderFallbackBuffer == null)
							{
								encoderFallbackBuffer = ((encoder != null) ? encoder.FallbackBuffer : encoderFallback.CreateFallbackBuffer());
								encoderFallbackBuffer.InternalInitialize(charStart, ptr, encoder, setEncoder: false);
							}
							chars2 = chars;
							encoderFallbackBuffer.InternalFallback(c, ref chars2);
							chars = chars2;
							c = '\0';
						}
						else
						{
							c = c2;
						}
					}
					else if (c == '\0')
					{
						num -= 2;
						if (encoderFallbackBuffer == null)
						{
							encoderFallbackBuffer = ((encoder != null) ? encoder.FallbackBuffer : encoderFallback.CreateFallbackBuffer());
							encoderFallbackBuffer.InternalInitialize(charStart, ptr, encoder, setEncoder: false);
						}
						chars2 = chars;
						encoderFallbackBuffer.InternalFallback(c2, ref chars2);
						chars = chars2;
					}
					else
					{
						c = '\0';
					}
				}
				else if (c > '\0')
				{
					chars--;
					if (encoderFallbackBuffer == null)
					{
						encoderFallbackBuffer = ((encoder != null) ? encoder.FallbackBuffer : encoderFallback.CreateFallbackBuffer());
						encoderFallbackBuffer.InternalInitialize(charStart, ptr, encoder, setEncoder: false);
					}
					chars2 = chars;
					encoderFallbackBuffer.InternalFallback(c, ref chars2);
					chars = chars2;
					num -= 2;
					c = '\0';
				}
				continue;
			}
			goto IL_0295;
			IL_0295:
			if (c <= '\0')
			{
				break;
			}
			num -= 2;
			if (encoder != null && !encoder.MustFlush)
			{
				break;
			}
			if (flag)
			{
				throw new ArgumentException(SR.Format(SR.Argument_RecursiveFallback, c), "chars");
			}
			if (encoderFallbackBuffer == null)
			{
				encoderFallbackBuffer = ((encoder != null) ? encoder.FallbackBuffer : encoderFallback.CreateFallbackBuffer());
				encoderFallbackBuffer.InternalInitialize(charStart, ptr, encoder, setEncoder: false);
			}
			chars2 = chars;
			encoderFallbackBuffer.InternalFallback(c, ref chars2);
			chars = chars2;
			c = '\0';
			flag = true;
		}
		return num;
	}

	internal unsafe sealed override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, EncoderNLS encoder)
	{
		char c = '\0';
		bool flag = false;
		byte* ptr = bytes + byteCount;
		char* ptr2 = chars + charCount;
		byte* ptr3 = bytes;
		char* ptr4 = chars;
		EncoderFallbackBuffer encoderFallbackBuffer = null;
		if (encoder != null)
		{
			c = encoder._charLeftOver;
			if (encoder.InternalHasFallbackBuffer)
			{
				encoderFallbackBuffer = encoder.FallbackBuffer;
				if (encoderFallbackBuffer.Remaining > 0 && encoder._throwOnOverflow)
				{
					throw new ArgumentException(SR.Format(SR.Argument_EncoderFallbackNotEmpty, EncodingName, encoder.Fallback?.GetType()));
				}
				encoderFallbackBuffer.InternalInitialize(ptr4, ptr2, encoder, setEncoder: false);
			}
		}
		while (true)
		{
			char num = encoderFallbackBuffer?.InternalGetNextChar() ?? '\0';
			char c2 = num;
			char* chars2;
			if (num != 0 || chars < ptr2)
			{
				if (c2 == '\0')
				{
					if ((bigEndian ^ BitConverter.IsLittleEndian) && ((ulong)chars & 7uL) == 0L && c == '\0')
					{
						ulong* ptr5 = (ulong*)(chars - 3 + ((ptr - bytes >> 1 < ptr2 - chars) ? (ptr - bytes >> 1) : (ptr2 - chars)));
						ulong* ptr6 = (ulong*)chars;
						ulong* ptr7 = (ulong*)bytes;
						while (ptr6 < ptr5)
						{
							if ((0x8000800080008000uL & *ptr6) != 0L)
							{
								ulong num2 = (0xF800F800F800F800uL & *ptr6) ^ 0xD800D800D800D800uL;
								if ((num2 & 0xFFFF000000000000uL) == 0L || (num2 & 0xFFFF00000000L) == 0L || (num2 & 0xFFFF0000u) == 0L || (num2 & 0xFFFF) == 0L)
								{
									long num3 = -287953294993589248L & (long)(*ptr6);
									if (!BitConverter.IsLittleEndian)
									{
									}
									if (num3 != -2593835887162763264L)
									{
										break;
									}
								}
							}
							Unsafe.WriteUnaligned(ptr7, *ptr6);
							ptr6++;
							ptr7++;
						}
						chars = (char*)ptr6;
						bytes = (byte*)ptr7;
						if (chars >= ptr2)
						{
							goto IL_039f;
						}
					}
					c2 = *chars;
					chars++;
				}
				if (c2 >= '\ud800' && c2 <= '\udfff')
				{
					if (c2 <= '\udbff')
					{
						if (c > '\0')
						{
							chars--;
							if (encoderFallbackBuffer == null)
							{
								encoderFallbackBuffer = ((encoder != null) ? encoder.FallbackBuffer : encoderFallback.CreateFallbackBuffer());
								encoderFallbackBuffer.InternalInitialize(ptr4, ptr2, encoder, setEncoder: true);
							}
							chars2 = chars;
							encoderFallbackBuffer.InternalFallback(c, ref chars2);
							chars = chars2;
							c = '\0';
						}
						else
						{
							c = c2;
						}
						continue;
					}
					if (c == '\0')
					{
						if (encoderFallbackBuffer == null)
						{
							encoderFallbackBuffer = ((encoder != null) ? encoder.FallbackBuffer : encoderFallback.CreateFallbackBuffer());
							encoderFallbackBuffer.InternalInitialize(ptr4, ptr2, encoder, setEncoder: true);
						}
						chars2 = chars;
						encoderFallbackBuffer.InternalFallback(c2, ref chars2);
						chars = chars2;
						continue;
					}
					if (bytes + 3 >= ptr)
					{
						if (encoderFallbackBuffer != null && encoderFallbackBuffer.bFallingBack)
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
						goto IL_039f;
					}
					if (bigEndian)
					{
						*(bytes++) = (byte)((int)c >> 8);
						*(bytes++) = (byte)c;
					}
					else
					{
						*(bytes++) = (byte)c;
						*(bytes++) = (byte)((int)c >> 8);
					}
					c = '\0';
				}
				else if (c > '\0')
				{
					chars--;
					if (encoderFallbackBuffer == null)
					{
						encoderFallbackBuffer = ((encoder != null) ? encoder.FallbackBuffer : encoderFallback.CreateFallbackBuffer());
						encoderFallbackBuffer.InternalInitialize(ptr4, ptr2, encoder, setEncoder: true);
					}
					chars2 = chars;
					encoderFallbackBuffer.InternalFallback(c, ref chars2);
					chars = chars2;
					c = '\0';
					continue;
				}
				if (bytes + 1 < ptr)
				{
					if (bigEndian)
					{
						*(bytes++) = (byte)((int)c2 >> 8);
						*(bytes++) = (byte)c2;
					}
					else
					{
						*(bytes++) = (byte)c2;
						*(bytes++) = (byte)((int)c2 >> 8);
					}
					continue;
				}
				if (encoderFallbackBuffer != null && encoderFallbackBuffer.bFallingBack)
				{
					encoderFallbackBuffer.MovePrevious();
				}
				else
				{
					chars--;
				}
				ThrowBytesOverflow(encoder, bytes == ptr3);
			}
			goto IL_039f;
			IL_039f:
			if (c <= '\0' || (encoder != null && !encoder.MustFlush))
			{
				break;
			}
			if (flag)
			{
				throw new ArgumentException(SR.Format(SR.Argument_RecursiveFallback, c), "chars");
			}
			if (encoderFallbackBuffer == null)
			{
				encoderFallbackBuffer = ((encoder != null) ? encoder.FallbackBuffer : encoderFallback.CreateFallbackBuffer());
				encoderFallbackBuffer.InternalInitialize(ptr4, ptr2, encoder, setEncoder: true);
			}
			chars2 = chars;
			encoderFallbackBuffer.InternalFallback(c, ref chars2);
			chars = chars2;
			c = '\0';
			flag = true;
		}
		if (encoder != null)
		{
			encoder._charLeftOver = c;
			encoder._charsUsed = (int)(chars - ptr4);
		}
		return (int)(bytes - ptr3);
	}

	internal unsafe sealed override int GetCharCount(byte* bytes, int count, DecoderNLS baseDecoder)
	{
		Decoder decoder = (Decoder)baseDecoder;
		byte* ptr = bytes + count;
		byte* byteStart = bytes;
		int num = -1;
		char c = '\0';
		int num2 = count >> 1;
		DecoderFallbackBuffer decoderFallbackBuffer = null;
		if (decoder != null)
		{
			num = decoder.lastByte;
			c = decoder.lastChar;
			if (c > '\0')
			{
				num2++;
			}
			if (num >= 0 && (count & 1) == 1)
			{
				num2++;
			}
		}
		while (bytes < ptr)
		{
			if ((bigEndian ^ BitConverter.IsLittleEndian) && ((ulong)bytes & 7uL) == 0L && num == -1 && c == '\0')
			{
				ulong* ptr2 = (ulong*)(ptr - 7);
				ulong* ptr3;
				for (ptr3 = (ulong*)bytes; ptr3 < ptr2; ptr3++)
				{
					if ((0x8000800080008000uL & *ptr3) == 0L)
					{
						continue;
					}
					ulong num3 = (0xF800F800F800F800uL & *ptr3) ^ 0xD800D800D800D800uL;
					if ((num3 & 0xFFFF000000000000uL) == 0L || (num3 & 0xFFFF00000000L) == 0L || (num3 & 0xFFFF0000u) == 0L || (num3 & 0xFFFF) == 0L)
					{
						long num4 = -287953294993589248L & (long)(*ptr3);
						if (!BitConverter.IsLittleEndian)
						{
						}
						if (num4 != -2593835887162763264L)
						{
							break;
						}
					}
				}
				bytes = (byte*)ptr3;
				if (bytes >= ptr)
				{
					break;
				}
			}
			if (num < 0)
			{
				num = *(bytes++);
				if (bytes >= ptr)
				{
					break;
				}
			}
			char c2 = ((!bigEndian) ? ((char)((*(bytes++) << 8) | num)) : ((char)((num << 8) | *(bytes++))));
			num = -1;
			if (c2 >= '\ud800' && c2 <= '\udfff')
			{
				if (c2 <= '\udbff')
				{
					if (c > '\0')
					{
						num2--;
						byte[] array = null;
						array = ((!bigEndian) ? new byte[2]
						{
							(byte)c,
							(byte)((int)c >> 8)
						} : new byte[2]
						{
							(byte)((int)c >> 8),
							(byte)c
						});
						if (decoderFallbackBuffer == null)
						{
							decoderFallbackBuffer = ((decoder != null) ? decoder.FallbackBuffer : decoderFallback.CreateFallbackBuffer());
							decoderFallbackBuffer.InternalInitialize(byteStart, null);
						}
						num2 += decoderFallbackBuffer.InternalFallback(array, bytes);
					}
					c = c2;
				}
				else if (c == '\0')
				{
					num2--;
					byte[] array2 = null;
					array2 = ((!bigEndian) ? new byte[2]
					{
						(byte)c2,
						(byte)((int)c2 >> 8)
					} : new byte[2]
					{
						(byte)((int)c2 >> 8),
						(byte)c2
					});
					if (decoderFallbackBuffer == null)
					{
						decoderFallbackBuffer = ((decoder != null) ? decoder.FallbackBuffer : decoderFallback.CreateFallbackBuffer());
						decoderFallbackBuffer.InternalInitialize(byteStart, null);
					}
					num2 += decoderFallbackBuffer.InternalFallback(array2, bytes);
				}
				else
				{
					c = '\0';
				}
			}
			else if (c > '\0')
			{
				num2--;
				byte[] array3 = null;
				array3 = ((!bigEndian) ? new byte[2]
				{
					(byte)c,
					(byte)((int)c >> 8)
				} : new byte[2]
				{
					(byte)((int)c >> 8),
					(byte)c
				});
				if (decoderFallbackBuffer == null)
				{
					decoderFallbackBuffer = ((decoder != null) ? decoder.FallbackBuffer : decoderFallback.CreateFallbackBuffer());
					decoderFallbackBuffer.InternalInitialize(byteStart, null);
				}
				num2 += decoderFallbackBuffer.InternalFallback(array3, bytes);
				c = '\0';
			}
		}
		if (decoder == null || decoder.MustFlush)
		{
			if (c > '\0')
			{
				num2--;
				byte[] array4 = null;
				array4 = ((!bigEndian) ? new byte[2]
				{
					(byte)c,
					(byte)((int)c >> 8)
				} : new byte[2]
				{
					(byte)((int)c >> 8),
					(byte)c
				});
				if (decoderFallbackBuffer == null)
				{
					decoderFallbackBuffer = ((decoder != null) ? decoder.FallbackBuffer : decoderFallback.CreateFallbackBuffer());
					decoderFallbackBuffer.InternalInitialize(byteStart, null);
				}
				num2 += decoderFallbackBuffer.InternalFallback(array4, bytes);
				c = '\0';
			}
			if (num >= 0)
			{
				if (decoderFallbackBuffer == null)
				{
					decoderFallbackBuffer = ((decoder != null) ? decoder.FallbackBuffer : decoderFallback.CreateFallbackBuffer());
					decoderFallbackBuffer.InternalInitialize(byteStart, null);
				}
				num2 += decoderFallbackBuffer.InternalFallback(new byte[1] { (byte)num }, bytes);
				num = -1;
			}
		}
		if (c > '\0')
		{
			num2--;
		}
		return num2;
	}

	internal unsafe sealed override int GetChars(byte* bytes, int byteCount, char* chars, int charCount, DecoderNLS baseDecoder)
	{
		Decoder decoder = (Decoder)baseDecoder;
		int num = -1;
		char c = '\0';
		if (decoder != null)
		{
			num = decoder.lastByte;
			c = decoder.lastChar;
		}
		DecoderFallbackBuffer decoderFallbackBuffer = null;
		byte* ptr = bytes + byteCount;
		char* ptr2 = chars + charCount;
		byte* ptr3 = bytes;
		char* ptr4 = chars;
		while (bytes < ptr)
		{
			if ((bigEndian ^ BitConverter.IsLittleEndian) && ((ulong)chars & 7uL) == 0L && num == -1 && c == '\0')
			{
				ulong* ptr5 = (ulong*)(bytes - 7 + ((ptr - bytes >> 1 < ptr2 - chars) ? (ptr - bytes) : (ptr2 - chars << 1)));
				ulong* ptr6 = (ulong*)bytes;
				ulong* ptr7 = (ulong*)chars;
				while (ptr6 < ptr5)
				{
					if ((0x8000800080008000uL & *ptr6) != 0L)
					{
						ulong num2 = (0xF800F800F800F800uL & *ptr6) ^ 0xD800D800D800D800uL;
						if ((num2 & 0xFFFF000000000000uL) == 0L || (num2 & 0xFFFF00000000L) == 0L || (num2 & 0xFFFF0000u) == 0L || (num2 & 0xFFFF) == 0L)
						{
							long num3 = -287953294993589248L & (long)(*ptr6);
							if (!BitConverter.IsLittleEndian)
							{
							}
							if (num3 != -2593835887162763264L)
							{
								break;
							}
						}
					}
					Unsafe.WriteUnaligned(ptr7, *ptr6);
					ptr6++;
					ptr7++;
				}
				chars = (char*)ptr7;
				bytes = (byte*)ptr6;
				if (bytes >= ptr)
				{
					break;
				}
			}
			if (num < 0)
			{
				num = *(bytes++);
				continue;
			}
			char c2 = ((!bigEndian) ? ((char)((*(bytes++) << 8) | num)) : ((char)((num << 8) | *(bytes++))));
			num = -1;
			if (c2 >= '\ud800' && c2 <= '\udfff')
			{
				if (c2 <= '\udbff')
				{
					if (c > '\0')
					{
						byte[] array = null;
						array = ((!bigEndian) ? new byte[2]
						{
							(byte)c,
							(byte)((int)c >> 8)
						} : new byte[2]
						{
							(byte)((int)c >> 8),
							(byte)c
						});
						if (decoderFallbackBuffer == null)
						{
							decoderFallbackBuffer = ((decoder != null) ? decoder.FallbackBuffer : decoderFallback.CreateFallbackBuffer());
							decoderFallbackBuffer.InternalInitialize(ptr3, ptr2);
						}
						char* chars2 = chars;
						bool flag = decoderFallbackBuffer.InternalFallback(array, bytes, ref chars2);
						chars = chars2;
						if (!flag)
						{
							bytes -= 2;
							decoderFallbackBuffer.InternalReset();
							ThrowCharsOverflow(decoder, chars == ptr4);
							break;
						}
					}
					c = c2;
					continue;
				}
				if (c == '\0')
				{
					byte[] array2 = null;
					array2 = ((!bigEndian) ? new byte[2]
					{
						(byte)c2,
						(byte)((int)c2 >> 8)
					} : new byte[2]
					{
						(byte)((int)c2 >> 8),
						(byte)c2
					});
					if (decoderFallbackBuffer == null)
					{
						decoderFallbackBuffer = ((decoder != null) ? decoder.FallbackBuffer : decoderFallback.CreateFallbackBuffer());
						decoderFallbackBuffer.InternalInitialize(ptr3, ptr2);
					}
					char* chars2 = chars;
					bool flag2 = decoderFallbackBuffer.InternalFallback(array2, bytes, ref chars2);
					chars = chars2;
					if (!flag2)
					{
						bytes -= 2;
						decoderFallbackBuffer.InternalReset();
						ThrowCharsOverflow(decoder, chars == ptr4);
						break;
					}
					continue;
				}
				if (chars >= ptr2 - 1)
				{
					bytes -= 2;
					ThrowCharsOverflow(decoder, chars == ptr4);
					break;
				}
				*(chars++) = c;
				c = '\0';
			}
			else if (c > '\0')
			{
				byte[] array3 = null;
				array3 = ((!bigEndian) ? new byte[2]
				{
					(byte)c,
					(byte)((int)c >> 8)
				} : new byte[2]
				{
					(byte)((int)c >> 8),
					(byte)c
				});
				if (decoderFallbackBuffer == null)
				{
					decoderFallbackBuffer = ((decoder != null) ? decoder.FallbackBuffer : decoderFallback.CreateFallbackBuffer());
					decoderFallbackBuffer.InternalInitialize(ptr3, ptr2);
				}
				char* chars2 = chars;
				bool flag3 = decoderFallbackBuffer.InternalFallback(array3, bytes, ref chars2);
				chars = chars2;
				if (!flag3)
				{
					bytes -= 2;
					decoderFallbackBuffer.InternalReset();
					ThrowCharsOverflow(decoder, chars == ptr4);
					break;
				}
				c = '\0';
			}
			if (chars >= ptr2)
			{
				bytes -= 2;
				ThrowCharsOverflow(decoder, chars == ptr4);
				break;
			}
			*(chars++) = c2;
		}
		if (decoder == null || decoder.MustFlush)
		{
			if (c > '\0')
			{
				byte[] array4 = null;
				array4 = ((!bigEndian) ? new byte[2]
				{
					(byte)c,
					(byte)((int)c >> 8)
				} : new byte[2]
				{
					(byte)((int)c >> 8),
					(byte)c
				});
				if (decoderFallbackBuffer == null)
				{
					decoderFallbackBuffer = ((decoder != null) ? decoder.FallbackBuffer : decoderFallback.CreateFallbackBuffer());
					decoderFallbackBuffer.InternalInitialize(ptr3, ptr2);
				}
				char* chars2 = chars;
				bool flag4 = decoderFallbackBuffer.InternalFallback(array4, bytes, ref chars2);
				chars = chars2;
				if (!flag4)
				{
					bytes -= 2;
					if (num >= 0)
					{
						bytes--;
					}
					decoderFallbackBuffer.InternalReset();
					ThrowCharsOverflow(decoder, chars == ptr4);
					bytes += 2;
					if (num >= 0)
					{
						bytes++;
					}
					goto IL_04df;
				}
				c = '\0';
			}
			if (num >= 0)
			{
				if (decoderFallbackBuffer == null)
				{
					decoderFallbackBuffer = ((decoder != null) ? decoder.FallbackBuffer : decoderFallback.CreateFallbackBuffer());
					decoderFallbackBuffer.InternalInitialize(ptr3, ptr2);
				}
				char* chars2 = chars;
				bool flag5 = decoderFallbackBuffer.InternalFallback(new byte[1] { (byte)num }, bytes, ref chars2);
				chars = chars2;
				if (!flag5)
				{
					bytes--;
					decoderFallbackBuffer.InternalReset();
					ThrowCharsOverflow(decoder, chars == ptr4);
					bytes++;
				}
				else
				{
					num = -1;
				}
			}
		}
		goto IL_04df;
		IL_04df:
		if (decoder != null)
		{
			decoder._bytesUsed = (int)(bytes - ptr3);
			decoder.lastChar = c;
			decoder.lastByte = num;
		}
		return (int)(chars - ptr4);
	}

	public override Encoder GetEncoder()
	{
		return new EncoderNLS(this);
	}

	public override System.Text.Decoder GetDecoder()
	{
		return new Decoder(this);
	}

	public override byte[] GetPreamble()
	{
		if (byteOrderMark)
		{
			if (!bigEndian)
			{
				return new byte[2] { 255, 254 };
			}
			return new byte[2] { 254, 255 };
		}
		return Array.Empty<byte>();
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
		num <<= 1;
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
		long num = (long)(byteCount >> 1) + (long)(byteCount & 1) + 1;
		if (base.DecoderFallback.MaxCharCount > 1)
		{
			num *= base.DecoderFallback.MaxCharCount;
		}
		if (num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("byteCount", SR.ArgumentOutOfRange_GetCharCountOverflow);
		}
		return (int)num;
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is UnicodeEncoding unicodeEncoding)
		{
			if (CodePage == unicodeEncoding.CodePage && byteOrderMark == unicodeEncoding.byteOrderMark && bigEndian == unicodeEncoding.bigEndian && base.EncoderFallback.Equals(unicodeEncoding.EncoderFallback))
			{
				return base.DecoderFallback.Equals(unicodeEncoding.DecoderFallback);
			}
			return false;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return CodePage + base.EncoderFallback.GetHashCode() + base.DecoderFallback.GetHashCode() + (byteOrderMark ? 4 : 0) + (bigEndian ? 8 : 0);
	}
}
