using System.Runtime.Serialization;

namespace System.Text;

internal sealed class Base64Encoding : Encoding
{
	private const string Val2Char = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

	private static ReadOnlySpan<byte> Char2val => new byte[128]
	{
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 62, 255, 255, 255, 63, 52, 53,
		54, 55, 56, 57, 58, 59, 60, 61, 255, 255,
		255, 64, 255, 255, 255, 0, 1, 2, 3, 4,
		5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
		15, 16, 17, 18, 19, 20, 21, 22, 23, 24,
		25, 255, 255, 255, 255, 255, 255, 26, 27, 28,
		29, 30, 31, 32, 33, 34, 35, 36, 37, 38,
		39, 40, 41, 42, 43, 44, 45, 46, 47, 48,
		49, 50, 51, 255, 255, 255, 255, 255
	};

	private static ReadOnlySpan<byte> Val2byte => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"u8;

	public override int GetMaxByteCount(int charCount)
	{
		if (charCount < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("charCount", System.SR.ValueMustBeNonNegative));
		}
		if (charCount % 4 != 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(System.SR.Format(System.SR.XmlInvalidBase64Length, charCount.ToString())));
		}
		return charCount / 4 * 3;
	}

	private bool IsValidLeadBytes(int v1, int v2, int v3, int v4)
	{
		if ((v1 | v2) < 64)
		{
			return (v3 | v4) != 255;
		}
		return false;
	}

	private bool IsValidTailBytes(int v3, int v4)
	{
		if (v3 == 64)
		{
			return v4 == 64;
		}
		return true;
	}

	public unsafe override int GetByteCount(char[] chars, int index, int count)
	{
		if (chars == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("chars"));
		}
		if (index < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("index", System.SR.ValueMustBeNonNegative));
		}
		if (index > chars.Length)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("index", System.SR.Format(System.SR.OffsetExceedsBufferSize, chars.Length)));
		}
		if (count < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.ValueMustBeNonNegative));
		}
		if (count > chars.Length - index)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("count", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, chars.Length - index)));
		}
		if (count == 0)
		{
			return 0;
		}
		if (count % 4 != 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(System.SR.Format(System.SR.XmlInvalidBase64Length, count.ToString())));
		}
		fixed (byte* ptr4 = &Char2val[0])
		{
			fixed (char* ptr = &chars[index])
			{
				int num = 0;
				char* ptr2 = ptr;
				for (char* ptr3 = ptr + count; ptr2 < ptr3; ptr2 += 4)
				{
					char c = *ptr2;
					char c2 = ptr2[1];
					char c3 = ptr2[2];
					char c4 = ptr2[3];
					if ((c | c2 | c3 | c4) >= 128)
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(System.SR.Format(System.SR.XmlInvalidBase64Sequence, new string(ptr2, 0, 4), index + (int)(ptr2 - ptr))));
					}
					int v = ptr4[(int)c];
					int v2 = ptr4[(int)c2];
					int num2 = ptr4[(int)c3];
					int num3 = ptr4[(int)c4];
					if (!IsValidLeadBytes(v, v2, num2, num3) || !IsValidTailBytes(num2, num3))
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(System.SR.Format(System.SR.XmlInvalidBase64Sequence, new string(ptr2, 0, 4), index + (int)(ptr2 - ptr))));
					}
					int num4 = ((num3 != 64) ? 3 : ((num2 == 64) ? 1 : 2));
					num += num4;
				}
				return num;
			}
		}
	}

	public unsafe override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
	{
		if (chars == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("chars"));
		}
		if (charIndex < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("charIndex", System.SR.ValueMustBeNonNegative));
		}
		if (charIndex > chars.Length)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("charIndex", System.SR.Format(System.SR.OffsetExceedsBufferSize, chars.Length)));
		}
		if (charCount < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("charCount", System.SR.ValueMustBeNonNegative));
		}
		if (charCount > chars.Length - charIndex)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("charCount", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, chars.Length - charIndex)));
		}
		if (bytes == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("bytes"));
		}
		if (byteIndex < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("byteIndex", System.SR.ValueMustBeNonNegative));
		}
		if (byteIndex > bytes.Length)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("byteIndex", System.SR.Format(System.SR.OffsetExceedsBufferSize, bytes.Length)));
		}
		if (charCount == 0)
		{
			return 0;
		}
		if (charCount % 4 != 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(System.SR.Format(System.SR.XmlInvalidBase64Length, charCount.ToString())));
		}
		fixed (byte* ptr7 = &Char2val[0])
		{
			fixed (char* ptr = &chars[charIndex])
			{
				fixed (byte* ptr4 = &bytes[byteIndex])
				{
					char* ptr2 = ptr;
					char* ptr3 = ptr + charCount;
					byte* ptr5 = ptr4;
					byte* ptr6 = ptr4 + bytes.Length - byteIndex;
					for (; ptr2 < ptr3; ptr2 += 4)
					{
						char c = *ptr2;
						char c2 = ptr2[1];
						char c3 = ptr2[2];
						char c4 = ptr2[3];
						if ((c | c2 | c3 | c4) >= 128)
						{
							throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(System.SR.Format(System.SR.XmlInvalidBase64Sequence, new string(ptr2, 0, 4), charIndex + (int)(ptr2 - ptr))));
						}
						int num = ptr7[(int)c];
						int num2 = ptr7[(int)c2];
						int num3 = ptr7[(int)c3];
						int num4 = ptr7[(int)c4];
						if (!IsValidLeadBytes(num, num2, num3, num4) || !IsValidTailBytes(num3, num4))
						{
							throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(System.SR.Format(System.SR.XmlInvalidBase64Sequence, new string(ptr2, 0, 4), charIndex + (int)(ptr2 - ptr))));
						}
						int num5 = ((num4 != 64) ? 3 : ((num3 == 64) ? 1 : 2));
						if (ptr5 + num5 > ptr6)
						{
							throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.XmlArrayTooSmall, "bytes"));
						}
						*ptr5 = (byte)((uint)(num << 2) | ((uint)(num2 >> 4) & 3u));
						if (num5 > 1)
						{
							ptr5[1] = (byte)((uint)(num2 << 4) | ((uint)(num3 >> 2) & 0xFu));
							if (num5 > 2)
							{
								ptr5[2] = (byte)((uint)(num3 << 6) | ((uint)num4 & 0x3Fu));
							}
						}
						ptr5 += num5;
					}
					return (int)(ptr5 - ptr4);
				}
			}
		}
	}

	public unsafe int GetBytes(byte[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
	{
		if (chars == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("chars"));
		}
		if (charIndex < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("charIndex", System.SR.ValueMustBeNonNegative));
		}
		if (charIndex > chars.Length)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("charIndex", System.SR.Format(System.SR.OffsetExceedsBufferSize, chars.Length)));
		}
		if (charCount < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("charCount", System.SR.ValueMustBeNonNegative));
		}
		if (charCount > chars.Length - charIndex)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("charCount", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, chars.Length - charIndex)));
		}
		if (bytes == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("bytes"));
		}
		if (byteIndex < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("byteIndex", System.SR.ValueMustBeNonNegative));
		}
		if (byteIndex > bytes.Length)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("byteIndex", System.SR.Format(System.SR.OffsetExceedsBufferSize, bytes.Length)));
		}
		if (charCount == 0)
		{
			return 0;
		}
		if (charCount % 4 != 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(System.SR.Format(System.SR.XmlInvalidBase64Length, charCount.ToString())));
		}
		fixed (byte* ptr7 = &Char2val[0])
		{
			fixed (byte* ptr = &chars[charIndex])
			{
				fixed (byte* ptr4 = &bytes[byteIndex])
				{
					byte* ptr2 = ptr;
					byte* ptr3 = ptr + charCount;
					byte* ptr5 = ptr4;
					byte* ptr6 = ptr4 + bytes.Length - byteIndex;
					for (; ptr2 < ptr3; ptr2 += 4)
					{
						byte b = *ptr2;
						byte b2 = ptr2[1];
						byte b3 = ptr2[2];
						byte b4 = ptr2[3];
						if ((b | b2 | b3 | b4) >= 128)
						{
							throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(System.SR.Format(System.SR.XmlInvalidBase64Sequence, "?", charIndex + (int)(ptr2 - ptr))));
						}
						int num = ptr7[(int)b];
						int num2 = ptr7[(int)b2];
						int num3 = ptr7[(int)b3];
						int num4 = ptr7[(int)b4];
						if (!IsValidLeadBytes(num, num2, num3, num4) || !IsValidTailBytes(num3, num4))
						{
							throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(System.SR.Format(System.SR.XmlInvalidBase64Sequence, "?", charIndex + (int)(ptr2 - ptr))));
						}
						int num5 = ((num4 != 64) ? 3 : ((num3 == 64) ? 1 : 2));
						if (ptr5 + num5 > ptr6)
						{
							throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.XmlArrayTooSmall, "bytes"));
						}
						*ptr5 = (byte)((uint)(num << 2) | ((uint)(num2 >> 4) & 3u));
						if (num5 > 1)
						{
							ptr5[1] = (byte)((uint)(num2 << 4) | ((uint)(num3 >> 2) & 0xFu));
							if (num5 > 2)
							{
								ptr5[2] = (byte)((uint)(num3 << 6) | ((uint)num4 & 0x3Fu));
							}
						}
						ptr5 += num5;
					}
					return (int)(ptr5 - ptr4);
				}
			}
		}
	}

	public override int GetMaxCharCount(int byteCount)
	{
		if (byteCount < 0 || byteCount > 1610612731)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("byteCount", System.SR.Format(System.SR.ValueMustBeInRange, 0, 1610612731)));
		}
		return (byteCount + 2) / 3 * 4;
	}

	public override int GetCharCount(byte[] bytes, int index, int count)
	{
		return GetMaxCharCount(count);
	}

	public unsafe override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
	{
		if (bytes == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("bytes"));
		}
		if (byteIndex < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("byteIndex", System.SR.ValueMustBeNonNegative));
		}
		if (byteIndex > bytes.Length)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("byteIndex", System.SR.Format(System.SR.OffsetExceedsBufferSize, bytes.Length)));
		}
		if (byteCount < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("byteCount", System.SR.ValueMustBeNonNegative));
		}
		if (byteCount > bytes.Length - byteIndex)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("byteCount", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, bytes.Length - byteIndex)));
		}
		int charCount = GetCharCount(bytes, byteIndex, byteCount);
		if (chars == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("chars"));
		}
		if (charIndex < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("charIndex", System.SR.ValueMustBeNonNegative));
		}
		if (charIndex > chars.Length)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("charIndex", System.SR.Format(System.SR.OffsetExceedsBufferSize, chars.Length)));
		}
		if (charCount < 0 || charCount > chars.Length - charIndex)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.XmlArrayTooSmall, "chars"));
		}
		if (byteCount > 0)
		{
			fixed (char* ptr6 = &"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/".GetPinnableReference())
			{
				fixed (byte* ptr = &bytes[byteIndex])
				{
					fixed (char* ptr4 = &chars[charIndex])
					{
						byte* ptr2 = ptr;
						byte* ptr3 = ptr2 + byteCount - 3;
						char* ptr5 = ptr4;
						while (ptr2 <= ptr3)
						{
							*ptr5 = ptr6[*ptr2 >> 2];
							ptr5[1] = ptr6[((*ptr2 & 3) << 4) | (ptr2[1] >> 4)];
							ptr5[2] = ptr6[((ptr2[1] & 0xF) << 2) | (ptr2[2] >> 6)];
							ptr5[3] = ptr6[ptr2[2] & 0x3F];
							ptr2 += 3;
							ptr5 += 4;
						}
						if (ptr2 - ptr3 == 2)
						{
							*ptr5 = ptr6[*ptr2 >> 2];
							ptr5[1] = ptr6[(*ptr2 & 3) << 4];
							ptr5[2] = '=';
							ptr5[3] = '=';
						}
						else if (ptr2 - ptr3 == 1)
						{
							*ptr5 = ptr6[*ptr2 >> 2];
							ptr5[1] = ptr6[((*ptr2 & 3) << 4) | (ptr2[1] >> 4)];
							ptr5[2] = ptr6[(ptr2[1] & 0xF) << 2];
							ptr5[3] = '=';
						}
					}
				}
			}
		}
		return charCount;
	}

	public unsafe int GetChars(byte[] bytes, int byteIndex, int byteCount, byte[] chars, int charIndex)
	{
		if (bytes == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("bytes"));
		}
		if (byteIndex < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("byteIndex", System.SR.ValueMustBeNonNegative));
		}
		if (byteIndex > bytes.Length)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("byteIndex", System.SR.Format(System.SR.OffsetExceedsBufferSize, bytes.Length)));
		}
		if (byteCount < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("byteCount", System.SR.ValueMustBeNonNegative));
		}
		if (byteCount > bytes.Length - byteIndex)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("byteCount", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, bytes.Length - byteIndex)));
		}
		int charCount = GetCharCount(bytes, byteIndex, byteCount);
		if (chars == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("chars"));
		}
		if (charIndex < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("charIndex", System.SR.ValueMustBeNonNegative));
		}
		if (charIndex > chars.Length)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("charIndex", System.SR.Format(System.SR.OffsetExceedsBufferSize, chars.Length)));
		}
		if (charCount < 0 || charCount > chars.Length - charIndex)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.XmlArrayTooSmall, "chars"));
		}
		if (byteCount > 0)
		{
			fixed (byte* ptr6 = &Val2byte[0])
			{
				fixed (byte* ptr = &bytes[byteIndex])
				{
					fixed (byte* ptr4 = &chars[charIndex])
					{
						byte* ptr2 = ptr;
						byte* ptr3 = ptr2 + byteCount - 3;
						byte* ptr5 = ptr4;
						while (ptr2 <= ptr3)
						{
							*ptr5 = ptr6[*ptr2 >> 2];
							ptr5[1] = ptr6[((*ptr2 & 3) << 4) | (ptr2[1] >> 4)];
							ptr5[2] = ptr6[((ptr2[1] & 0xF) << 2) | (ptr2[2] >> 6)];
							ptr5[3] = ptr6[ptr2[2] & 0x3F];
							ptr2 += 3;
							ptr5 += 4;
						}
						if (ptr2 - ptr3 == 2)
						{
							*ptr5 = ptr6[*ptr2 >> 2];
							ptr5[1] = ptr6[(*ptr2 & 3) << 4];
							ptr5[2] = 61;
							ptr5[3] = 61;
						}
						else if (ptr2 - ptr3 == 1)
						{
							*ptr5 = ptr6[*ptr2 >> 2];
							ptr5[1] = ptr6[((*ptr2 & 3) << 4) | (ptr2[1] >> 4)];
							ptr5[2] = ptr6[(ptr2[1] & 0xF) << 2];
							ptr5[3] = 61;
						}
					}
				}
			}
		}
		return charCount;
	}
}
