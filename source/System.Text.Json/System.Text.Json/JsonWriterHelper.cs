using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;

namespace System.Text.Json;

internal static class JsonWriterHelper
{
	private static readonly StandardFormat s_dateTimeStandardFormat = new StandardFormat('O');

	private static readonly StandardFormat s_hexStandardFormat = new StandardFormat('X', 4);

	private static ReadOnlySpan<byte> AllowList => new byte[256]
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 1, 1, 0, 1, 1, 1, 0, 0,
		1, 1, 1, 0, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		0, 1, 0, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 0, 1, 1, 1, 0, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0
	};

	public static void WriteIndentation(Span<byte> buffer, int indent)
	{
		if (indent < 8)
		{
			int num = 0;
			while (num < indent)
			{
				buffer[num++] = 32;
				buffer[num++] = 32;
			}
		}
		else
		{
			buffer.Slice(0, indent).Fill(32);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidateProperty(ReadOnlySpan<byte> propertyName)
	{
		if (propertyName.Length > 166666666)
		{
			ThrowHelper.ThrowArgumentException_PropertyNameTooLarge(propertyName.Length);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidateValue(ReadOnlySpan<byte> value)
	{
		if (value.Length > 166666666)
		{
			ThrowHelper.ThrowArgumentException_ValueTooLarge(value.Length);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidateBytes(ReadOnlySpan<byte> bytes)
	{
		if (bytes.Length > 125000000)
		{
			ThrowHelper.ThrowArgumentException_ValueTooLarge(bytes.Length);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidateDouble(double value)
	{
		if (!JsonHelpers.IsFinite(value))
		{
			ThrowHelper.ThrowArgumentException_ValueNotSupported();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidateSingle(float value)
	{
		if (!JsonHelpers.IsFinite(value))
		{
			ThrowHelper.ThrowArgumentException_ValueNotSupported();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidateProperty(ReadOnlySpan<char> propertyName)
	{
		if (propertyName.Length > 166666666)
		{
			ThrowHelper.ThrowArgumentException_PropertyNameTooLarge(propertyName.Length);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidateValue(ReadOnlySpan<char> value)
	{
		if (value.Length > 166666666)
		{
			ThrowHelper.ThrowArgumentException_ValueTooLarge(value.Length);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidatePropertyAndValue(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> value)
	{
		if (propertyName.Length > 166666666 || value.Length > 166666666)
		{
			ThrowHelper.ThrowArgumentException(propertyName, value);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidatePropertyAndValue(ReadOnlySpan<byte> propertyName, ReadOnlySpan<char> value)
	{
		if (propertyName.Length > 166666666 || value.Length > 166666666)
		{
			ThrowHelper.ThrowArgumentException(propertyName, value);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidatePropertyAndValue(ReadOnlySpan<byte> propertyName, ReadOnlySpan<byte> value)
	{
		if (propertyName.Length > 166666666 || value.Length > 166666666)
		{
			ThrowHelper.ThrowArgumentException(propertyName, value);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidatePropertyAndValue(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> value)
	{
		if (propertyName.Length > 166666666 || value.Length > 166666666)
		{
			ThrowHelper.ThrowArgumentException(propertyName, value);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidatePropertyAndBytes(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> bytes)
	{
		if (propertyName.Length > 166666666 || bytes.Length > 125000000)
		{
			ThrowHelper.ThrowArgumentException(propertyName, bytes);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidatePropertyAndBytes(ReadOnlySpan<byte> propertyName, ReadOnlySpan<byte> bytes)
	{
		if (propertyName.Length > 166666666 || bytes.Length > 125000000)
		{
			ThrowHelper.ThrowArgumentException(propertyName, bytes);
		}
	}

	internal static void ValidateNumber(ReadOnlySpan<byte> utf8FormattedNumber)
	{
		int i = 0;
		if (utf8FormattedNumber[i] == 45)
		{
			i++;
			if (utf8FormattedNumber.Length <= i)
			{
				throw new ArgumentException(System.SR.RequiredDigitNotFoundEndOfData, "utf8FormattedNumber");
			}
		}
		if (utf8FormattedNumber[i] == 48)
		{
			i++;
		}
		else
		{
			for (; i < utf8FormattedNumber.Length && JsonHelpers.IsDigit(utf8FormattedNumber[i]); i++)
			{
			}
		}
		if (i == utf8FormattedNumber.Length)
		{
			return;
		}
		byte b = utf8FormattedNumber[i];
		if (b == 46)
		{
			i++;
			if (utf8FormattedNumber.Length <= i)
			{
				throw new ArgumentException(System.SR.RequiredDigitNotFoundEndOfData, "utf8FormattedNumber");
			}
			for (; i < utf8FormattedNumber.Length && JsonHelpers.IsDigit(utf8FormattedNumber[i]); i++)
			{
			}
			if (i == utf8FormattedNumber.Length)
			{
				return;
			}
			b = utf8FormattedNumber[i];
		}
		if (b == 101 || b == 69)
		{
			i++;
			if (utf8FormattedNumber.Length <= i)
			{
				throw new ArgumentException(System.SR.RequiredDigitNotFoundEndOfData, "utf8FormattedNumber");
			}
			b = utf8FormattedNumber[i];
			if (b == 43 || b == 45)
			{
				i++;
			}
			if (utf8FormattedNumber.Length <= i)
			{
				throw new ArgumentException(System.SR.RequiredDigitNotFoundEndOfData, "utf8FormattedNumber");
			}
			for (; i < utf8FormattedNumber.Length && JsonHelpers.IsDigit(utf8FormattedNumber[i]); i++)
			{
			}
			if (i == utf8FormattedNumber.Length)
			{
				return;
			}
			throw new ArgumentException(System.SR.Format(System.SR.ExpectedEndOfDigitNotFound, ThrowHelper.GetPrintableString(utf8FormattedNumber[i])), "utf8FormattedNumber");
		}
		throw new ArgumentException(System.SR.Format(System.SR.ExpectedEndOfDigitNotFound, ThrowHelper.GetPrintableString(b)), "utf8FormattedNumber");
	}

	public static void WriteDateTimeTrimmed(Span<byte> buffer, DateTime value, out int bytesWritten)
	{
		Span<byte> destination = stackalloc byte[33];
		bool flag = Utf8Formatter.TryFormat(value, destination, out bytesWritten, s_dateTimeStandardFormat);
		TrimDateTimeOffset(destination.Slice(0, bytesWritten), out bytesWritten);
		destination.Slice(0, bytesWritten).CopyTo(buffer);
	}

	public static void WriteDateTimeOffsetTrimmed(Span<byte> buffer, DateTimeOffset value, out int bytesWritten)
	{
		Span<byte> destination = stackalloc byte[33];
		bool flag = Utf8Formatter.TryFormat(value, destination, out bytesWritten, s_dateTimeStandardFormat);
		TrimDateTimeOffset(destination.Slice(0, bytesWritten), out bytesWritten);
		destination.Slice(0, bytesWritten).CopyTo(buffer);
	}

	public static void TrimDateTimeOffset(Span<byte> buffer, out int bytesWritten)
	{
		if (buffer[26] != 48)
		{
			bytesWritten = buffer.Length;
			return;
		}
		int num = ((buffer[25] != 48) ? 26 : ((buffer[24] != 48) ? 25 : ((buffer[23] != 48) ? 24 : ((buffer[22] != 48) ? 23 : ((buffer[21] != 48) ? 22 : ((buffer[20] != 48) ? 21 : 19))))));
		if (buffer.Length == 27)
		{
			bytesWritten = num;
		}
		else if (buffer.Length == 33)
		{
			buffer[num] = buffer[27];
			buffer[num + 1] = buffer[28];
			buffer[num + 2] = buffer[29];
			buffer[num + 3] = buffer[30];
			buffer[num + 4] = buffer[31];
			buffer[num + 5] = buffer[32];
			bytesWritten = num + 6;
		}
		else
		{
			buffer[num] = 90;
			bytesWritten = num + 1;
		}
	}

	private static bool NeedsEscaping(byte value)
	{
		return AllowList[value] == 0;
	}

	private static bool NeedsEscapingNoBoundsCheck(char value)
	{
		return AllowList[value] == 0;
	}

	public static int NeedsEscaping(ReadOnlySpan<byte> value, JavaScriptEncoder encoder)
	{
		return (encoder ?? JavaScriptEncoder.Default).FindFirstCharacterToEncodeUtf8(value);
	}

	public unsafe static int NeedsEscaping(ReadOnlySpan<char> value, JavaScriptEncoder encoder)
	{
		if (value.IsEmpty)
		{
			return -1;
		}
		fixed (char* text = value)
		{
			return (encoder ?? JavaScriptEncoder.Default).FindFirstCharacterToEncode(text, value.Length);
		}
	}

	public static int GetMaxEscapedLength(int textLength, int firstIndexToEscape)
	{
		return firstIndexToEscape + 6 * (textLength - firstIndexToEscape);
	}

	private static void EscapeString(ReadOnlySpan<byte> value, Span<byte> destination, JavaScriptEncoder encoder, ref int written)
	{
		if (encoder.EncodeUtf8(value, destination, out var _, out var bytesWritten) != 0)
		{
			ThrowHelper.ThrowArgumentException_InvalidUTF8(value.Slice(bytesWritten));
		}
		written += bytesWritten;
	}

	public static void EscapeString(ReadOnlySpan<byte> value, Span<byte> destination, int indexOfFirstByteToEscape, JavaScriptEncoder encoder, out int written)
	{
		value.Slice(0, indexOfFirstByteToEscape).CopyTo(destination);
		written = indexOfFirstByteToEscape;
		if (encoder != null)
		{
			destination = destination.Slice(indexOfFirstByteToEscape);
			value = value.Slice(indexOfFirstByteToEscape);
			EscapeString(value, destination, encoder, ref written);
			return;
		}
		while (indexOfFirstByteToEscape < value.Length)
		{
			byte b = value[indexOfFirstByteToEscape];
			if (IsAsciiValue(b))
			{
				if (NeedsEscaping(b))
				{
					EscapeNextBytes(b, destination, ref written);
					indexOfFirstByteToEscape++;
				}
				else
				{
					destination[written] = b;
					written++;
					indexOfFirstByteToEscape++;
				}
				continue;
			}
			destination = destination.Slice(written);
			value = value.Slice(indexOfFirstByteToEscape);
			EscapeString(value, destination, JavaScriptEncoder.Default, ref written);
			break;
		}
	}

	private static void EscapeNextBytes(byte value, Span<byte> destination, ref int written)
	{
		destination[written++] = 92;
		switch (value)
		{
		case 34:
			destination[written++] = 117;
			destination[written++] = 48;
			destination[written++] = 48;
			destination[written++] = 50;
			destination[written++] = 50;
			break;
		case 10:
			destination[written++] = 110;
			break;
		case 13:
			destination[written++] = 114;
			break;
		case 9:
			destination[written++] = 116;
			break;
		case 92:
			destination[written++] = 92;
			break;
		case 8:
			destination[written++] = 98;
			break;
		case 12:
			destination[written++] = 102;
			break;
		default:
		{
			destination[written++] = 117;
			int bytesWritten;
			bool flag = Utf8Formatter.TryFormat(value, destination.Slice(written), out bytesWritten, s_hexStandardFormat);
			written += bytesWritten;
			break;
		}
		}
	}

	private static bool IsAsciiValue(byte value)
	{
		return value <= 127;
	}

	private static bool IsAsciiValue(char value)
	{
		return value <= '\u007f';
	}

	private static void EscapeString(ReadOnlySpan<char> value, Span<char> destination, JavaScriptEncoder encoder, ref int written)
	{
		if (encoder.Encode(value, destination, out var _, out var charsWritten) != 0)
		{
			ThrowHelper.ThrowArgumentException_InvalidUTF16(value[charsWritten]);
		}
		written += charsWritten;
	}

	public static void EscapeString(ReadOnlySpan<char> value, Span<char> destination, int indexOfFirstByteToEscape, JavaScriptEncoder encoder, out int written)
	{
		value.Slice(0, indexOfFirstByteToEscape).CopyTo(destination);
		written = indexOfFirstByteToEscape;
		if (encoder != null)
		{
			destination = destination.Slice(indexOfFirstByteToEscape);
			value = value.Slice(indexOfFirstByteToEscape);
			EscapeString(value, destination, encoder, ref written);
			return;
		}
		while (indexOfFirstByteToEscape < value.Length)
		{
			char c = value[indexOfFirstByteToEscape];
			if (IsAsciiValue(c))
			{
				if (NeedsEscapingNoBoundsCheck(c))
				{
					EscapeNextChars(c, destination, ref written);
					indexOfFirstByteToEscape++;
				}
				else
				{
					destination[written] = c;
					written++;
					indexOfFirstByteToEscape++;
				}
				continue;
			}
			destination = destination.Slice(written);
			value = value.Slice(indexOfFirstByteToEscape);
			EscapeString(value, destination, JavaScriptEncoder.Default, ref written);
			break;
		}
	}

	private static void EscapeNextChars(char value, Span<char> destination, ref int written)
	{
		destination[written++] = '\\';
		switch ((byte)value)
		{
		case 34:
			destination[written++] = 'u';
			destination[written++] = '0';
			destination[written++] = '0';
			destination[written++] = '2';
			destination[written++] = '2';
			break;
		case 10:
			destination[written++] = 'n';
			break;
		case 13:
			destination[written++] = 'r';
			break;
		case 9:
			destination[written++] = 't';
			break;
		case 92:
			destination[written++] = '\\';
			break;
		case 8:
			destination[written++] = 'b';
			break;
		case 12:
			destination[written++] = 'f';
			break;
		default:
		{
			destination[written++] = 'u';
			int num = value;
			num.TryFormat(destination.Slice(written), out var charsWritten, "X4");
			written += charsWritten;
			break;
		}
		}
	}

	public unsafe static OperationStatus ToUtf8(ReadOnlySpan<byte> utf16Source, Span<byte> utf8Destination, out int bytesConsumed, out int bytesWritten)
	{
		fixed (byte* ptr = &MemoryMarshal.GetReference(utf16Source))
		{
			fixed (byte* ptr3 = &MemoryMarshal.GetReference(utf8Destination))
			{
				char* ptr2 = (char*)ptr;
				byte* ptr4 = ptr3;
				char* ptr5 = (char*)(ptr + utf16Source.Length);
				byte* ptr6 = ptr4 + utf8Destination.Length;
				while (true)
				{
					IL_025a:
					if (ptr5 - ptr2 > 13)
					{
						int num = Math.Min(PtrDiff(ptr5, ptr2), PtrDiff(ptr6, ptr4));
						char* ptr7 = ptr2 + num - 5;
						if (ptr2 < ptr7)
						{
							while (true)
							{
								int num2 = *ptr2;
								ptr2++;
								if (num2 > 127)
								{
									goto IL_0181;
								}
								*ptr4 = (byte)num2;
								ptr4++;
								if (((uint)(int)ptr2 & 2u) != 0)
								{
									num2 = *ptr2;
									ptr2++;
									if (num2 > 127)
									{
										goto IL_0181;
									}
									*ptr4 = (byte)num2;
									ptr4++;
								}
								while (ptr2 < ptr7)
								{
									num2 = *(int*)ptr2;
									int num3 = *(int*)(ptr2 + 2);
									if (((num2 | num3) & -8323200) == 0)
									{
										if (!BitConverter.IsLittleEndian)
										{
											*ptr4 = (byte)(num2 >> 16);
											ptr4[1] = (byte)num2;
											ptr2 += 4;
											ptr4[2] = (byte)(num3 >> 16);
											ptr4[3] = (byte)num3;
											ptr4 += 4;
										}
										else
										{
											*ptr4 = (byte)num2;
											ptr4[1] = (byte)(num2 >> 16);
											ptr2 += 4;
											ptr4[2] = (byte)num3;
											ptr4[3] = (byte)(num3 >> 16);
											ptr4 += 4;
										}
										continue;
									}
									goto IL_014f;
								}
								goto IL_0251;
								IL_014f:
								num2 = (BitConverter.IsLittleEndian ? ((ushort)num2) : (num2 >>> 16));
								ptr2++;
								if (num2 > 127)
								{
									goto IL_0181;
								}
								*ptr4 = (byte)num2;
								ptr4++;
								goto IL_0251;
								IL_0251:
								if (ptr2 < ptr7)
								{
									continue;
								}
								goto IL_025a;
								IL_0181:
								int num4;
								if (num2 <= 2047)
								{
									num4 = -64 | (num2 >> 6);
								}
								else
								{
									if (!JsonHelpers.IsInRangeInclusive(num2, 55296, 57343))
									{
										num4 = -32 | (num2 >> 12);
									}
									else
									{
										if (num2 > 56319)
										{
											break;
										}
										num4 = *ptr2;
										if (!JsonHelpers.IsInRangeInclusive(num4, 56320, 57343))
										{
											break;
										}
										ptr2++;
										num2 = num4 + (num2 << 10) + -56613888;
										*ptr4 = (byte)(0xFFFFFFF0u | (uint)(num2 >> 18));
										ptr4++;
										num4 = -128 | ((num2 >> 12) & 0x3F);
									}
									*ptr4 = (byte)num4;
									ptr7--;
									ptr4++;
									num4 = -128 | ((num2 >> 6) & 0x3F);
								}
								*ptr4 = (byte)num4;
								ptr7--;
								ptr4[1] = (byte)(0xFFFFFF80u | ((uint)num2 & 0x3Fu));
								ptr4 += 2;
								goto IL_0251;
							}
							break;
						}
					}
					while (true)
					{
						int num5;
						int num6;
						if (ptr2 < ptr5)
						{
							num5 = *ptr2;
							ptr2++;
							if (num5 <= 127)
							{
								if (ptr6 - ptr4 > 0)
								{
									*ptr4 = (byte)num5;
									ptr4++;
									continue;
								}
							}
							else if (num5 <= 2047)
							{
								if (ptr6 - ptr4 > 1)
								{
									num6 = -64 | (num5 >> 6);
									goto IL_0380;
								}
							}
							else if (!JsonHelpers.IsInRangeInclusive(num5, 55296, 57343))
							{
								if (ptr6 - ptr4 > 2)
								{
									num6 = -32 | (num5 >> 12);
									goto IL_0368;
								}
							}
							else if (ptr6 - ptr4 > 3)
							{
								if (num5 > 56319)
								{
									break;
								}
								if (ptr2 < ptr5)
								{
									num6 = *ptr2;
									if (!JsonHelpers.IsInRangeInclusive(num6, 56320, 57343))
									{
										break;
									}
									ptr2++;
									num5 = num6 + (num5 << 10) + -56613888;
									*ptr4 = (byte)(0xFFFFFFF0u | (uint)(num5 >> 18));
									ptr4++;
									num6 = -128 | ((num5 >> 12) & 0x3F);
									goto IL_0368;
								}
								bytesConsumed = (int)((byte*)(ptr2 - 1) - ptr);
								bytesWritten = (int)(ptr4 - ptr3);
								return OperationStatus.NeedMoreData;
							}
							bytesConsumed = (int)((byte*)(ptr2 - 1) - ptr);
							bytesWritten = (int)(ptr4 - ptr3);
							return OperationStatus.DestinationTooSmall;
						}
						bytesConsumed = (int)((byte*)ptr2 - ptr);
						bytesWritten = (int)(ptr4 - ptr3);
						return OperationStatus.Done;
						IL_0368:
						*ptr4 = (byte)num6;
						ptr4++;
						num6 = -128 | ((num5 >> 6) & 0x3F);
						goto IL_0380;
						IL_0380:
						*ptr4 = (byte)num6;
						ptr4[1] = (byte)(0xFFFFFF80u | ((uint)num5 & 0x3Fu));
						ptr4 += 2;
					}
					break;
				}
				bytesConsumed = (int)((byte*)(ptr2 - 1) - ptr);
				bytesWritten = (int)(ptr4 - ptr3);
				return OperationStatus.InvalidData;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static int PtrDiff(char* a, char* b)
	{
		return (int)((uint)((byte*)a - (byte*)b) >> 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static int PtrDiff(byte* a, byte* b)
	{
		return (int)(a - b);
	}
}
