using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Json;

internal static class JsonReaderHelper
{
	public static readonly UTF8Encoding s_utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	public static (int, int) CountNewLines(ReadOnlySpan<byte> data)
	{
		int item = -1;
		int num = 0;
		for (int i = 0; i < data.Length; i++)
		{
			if (data[i] == 10)
			{
				item = i;
				num++;
			}
		}
		return (num, item);
	}

	internal static JsonValueKind ToValueKind(this JsonTokenType tokenType)
	{
		switch (tokenType)
		{
		case JsonTokenType.None:
			return JsonValueKind.Undefined;
		case JsonTokenType.StartArray:
			return JsonValueKind.Array;
		case JsonTokenType.StartObject:
			return JsonValueKind.Object;
		case JsonTokenType.String:
		case JsonTokenType.Number:
		case JsonTokenType.True:
		case JsonTokenType.False:
		case JsonTokenType.Null:
			return (JsonValueKind)(tokenType - 4);
		default:
			return JsonValueKind.Undefined;
		}
	}

	public static bool IsTokenTypePrimitive(JsonTokenType tokenType)
	{
		return (int)(tokenType - 7) <= 4;
	}

	public static bool IsHexDigit(byte nextByte)
	{
		return System.HexConverter.IsHexChar(nextByte);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexOfQuoteOrAnyControlOrBackSlash(this ReadOnlySpan<byte> span)
	{
		return IndexOfOrLessThan(ref MemoryMarshal.GetReference(span), 34, 92, 32, span.Length);
	}

	private unsafe static int IndexOfOrLessThan(ref byte searchSpace, byte value0, byte value1, byte lessThan, int length)
	{
		IntPtr intPtr = (IntPtr)0;
		IntPtr intPtr2 = (IntPtr)length;
		if (Vector.IsHardwareAccelerated && length >= Vector<byte>.Count * 2)
		{
			int num = (int)Unsafe.AsPointer(ref searchSpace) & (Vector<byte>.Count - 1);
			intPtr2 = (IntPtr)((Vector<byte>.Count - num) & (Vector<byte>.Count - 1));
		}
		while (true)
		{
			if ((nuint)(void*)intPtr2 >= (nuint)8u)
			{
				intPtr2 -= 8;
				uint num2 = Unsafe.AddByteOffset(ref searchSpace, intPtr);
				if (value0 == num2 || value1 == num2 || lessThan > num2)
				{
					goto IL_0393;
				}
				num2 = Unsafe.AddByteOffset(ref searchSpace, intPtr + 1);
				if (value0 == num2 || value1 == num2 || lessThan > num2)
				{
					goto IL_039b;
				}
				num2 = Unsafe.AddByteOffset(ref searchSpace, intPtr + 2);
				if (value0 == num2 || value1 == num2 || lessThan > num2)
				{
					goto IL_03a9;
				}
				num2 = Unsafe.AddByteOffset(ref searchSpace, intPtr + 3);
				if (value0 != num2 && value1 != num2 && lessThan <= num2)
				{
					num2 = Unsafe.AddByteOffset(ref searchSpace, intPtr + 4);
					if (value0 != num2 && value1 != num2 && lessThan <= num2)
					{
						num2 = Unsafe.AddByteOffset(ref searchSpace, intPtr + 5);
						if (value0 != num2 && value1 != num2 && lessThan <= num2)
						{
							num2 = Unsafe.AddByteOffset(ref searchSpace, intPtr + 6);
							if (value0 != num2 && value1 != num2 && lessThan <= num2)
							{
								num2 = Unsafe.AddByteOffset(ref searchSpace, intPtr + 7);
								if (value0 == num2 || value1 == num2 || lessThan > num2)
								{
									break;
								}
								intPtr += 8;
								continue;
							}
							return (int)(void*)(intPtr + 6);
						}
						return (int)(void*)(intPtr + 5);
					}
					return (int)(void*)(intPtr + 4);
				}
				goto IL_03b7;
			}
			if ((nuint)(void*)intPtr2 >= (nuint)4u)
			{
				intPtr2 -= 4;
				uint num2 = Unsafe.AddByteOffset(ref searchSpace, intPtr);
				if (value0 == num2 || value1 == num2 || lessThan > num2)
				{
					goto IL_0393;
				}
				num2 = Unsafe.AddByteOffset(ref searchSpace, intPtr + 1);
				if (value0 == num2 || value1 == num2 || lessThan > num2)
				{
					goto IL_039b;
				}
				num2 = Unsafe.AddByteOffset(ref searchSpace, intPtr + 2);
				if (value0 == num2 || value1 == num2 || lessThan > num2)
				{
					goto IL_03a9;
				}
				num2 = Unsafe.AddByteOffset(ref searchSpace, intPtr + 3);
				if (value0 == num2 || value1 == num2 || lessThan > num2)
				{
					goto IL_03b7;
				}
				intPtr += 4;
			}
			while ((void*)intPtr2 != null)
			{
				intPtr2 -= 1;
				uint num2 = Unsafe.AddByteOffset(ref searchSpace, intPtr);
				if (value0 != num2 && value1 != num2 && lessThan <= num2)
				{
					intPtr += 1;
					continue;
				}
				goto IL_0393;
			}
			if (Vector.IsHardwareAccelerated && (int)(void*)intPtr < length)
			{
				intPtr2 = (IntPtr)((length - (int)(void*)intPtr) & ~(Vector<byte>.Count - 1));
				Vector<byte> right = new Vector<byte>(value0);
				Vector<byte> right2 = new Vector<byte>(value1);
				Vector<byte> right3 = new Vector<byte>(lessThan);
				for (; (void*)intPtr2 > (void*)intPtr; intPtr += Vector<byte>.Count)
				{
					Vector<byte> left = Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.AddByteOffset(ref searchSpace, intPtr));
					Vector<byte> vector = Vector.BitwiseOr(Vector.BitwiseOr(Vector.Equals(left, right), Vector.Equals(left, right2)), Vector.LessThan(left, right3));
					if (!Vector<byte>.Zero.Equals(vector))
					{
						return (int)(void*)intPtr + LocateFirstFoundByte(vector);
					}
				}
				if ((int)(void*)intPtr < length)
				{
					intPtr2 = (IntPtr)(length - (int)(void*)intPtr);
					continue;
				}
			}
			return -1;
			IL_0393:
			return (int)(void*)intPtr;
			IL_039b:
			return (int)(void*)(intPtr + 1);
			IL_03b7:
			return (int)(void*)(intPtr + 3);
			IL_03a9:
			return (int)(void*)(intPtr + 2);
		}
		return (int)(void*)(intPtr + 7);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int LocateFirstFoundByte(Vector<byte> match)
	{
		Vector<ulong> vector = Vector.AsVectorUInt64(match);
		ulong num = 0uL;
		int i;
		for (i = 0; i < Vector<ulong>.Count; i++)
		{
			num = vector[i];
			if (num != 0L)
			{
				break;
			}
		}
		return i * 8 + LocateFirstFoundByte(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int LocateFirstFoundByte(ulong match)
	{
		ulong num = match ^ (match - 1);
		return (int)(num * 283686952306184L >> 57);
	}

	public static bool TryGetEscapedDateTime(ReadOnlySpan<byte> source, out DateTime value)
	{
		int idx = source.IndexOf<byte>(92);
		Span<byte> span = stackalloc byte[252];
		Unescape(source, span, idx, out var written);
		span = span.Slice(0, written);
		if (span.Length <= 42 && JsonHelpers.TryParseAsISO((ReadOnlySpan<byte>)span, out DateTime value2))
		{
			value = value2;
			return true;
		}
		value = default(DateTime);
		return false;
	}

	public static bool TryGetEscapedDateTimeOffset(ReadOnlySpan<byte> source, out DateTimeOffset value)
	{
		int idx = source.IndexOf<byte>(92);
		Span<byte> span = stackalloc byte[252];
		Unescape(source, span, idx, out var written);
		span = span.Slice(0, written);
		if (span.Length <= 42 && JsonHelpers.TryParseAsISO((ReadOnlySpan<byte>)span, out DateTimeOffset value2))
		{
			value = value2;
			return true;
		}
		value = default(DateTimeOffset);
		return false;
	}

	public static bool TryGetEscapedGuid(ReadOnlySpan<byte> source, out Guid value)
	{
		int idx = source.IndexOf<byte>(92);
		Span<byte> span = stackalloc byte[216];
		Unescape(source, span, idx, out var written);
		span = span.Slice(0, written);
		if (span.Length == 36 && Utf8Parser.TryParse((ReadOnlySpan<byte>)span, out Guid value2, out int _, 'D'))
		{
			value = value2;
			return true;
		}
		value = default(Guid);
		return false;
	}

	public static bool TryGetFloatingPointConstant(ReadOnlySpan<byte> span, out float value)
	{
		if (span.Length == 3)
		{
			if (span.SequenceEqual(JsonConstants.NaNValue))
			{
				value = float.NaN;
				return true;
			}
		}
		else if (span.Length == 8)
		{
			if (span.SequenceEqual(JsonConstants.PositiveInfinityValue))
			{
				value = float.PositiveInfinity;
				return true;
			}
		}
		else if (span.Length == 9 && span.SequenceEqual(JsonConstants.NegativeInfinityValue))
		{
			value = float.NegativeInfinity;
			return true;
		}
		value = 0f;
		return false;
	}

	public static bool TryGetFloatingPointConstant(ReadOnlySpan<byte> span, out double value)
	{
		if (span.Length == 3)
		{
			if (span.SequenceEqual(JsonConstants.NaNValue))
			{
				value = double.NaN;
				return true;
			}
		}
		else if (span.Length == 8)
		{
			if (span.SequenceEqual(JsonConstants.PositiveInfinityValue))
			{
				value = double.PositiveInfinity;
				return true;
			}
		}
		else if (span.Length == 9 && span.SequenceEqual(JsonConstants.NegativeInfinityValue))
		{
			value = double.NegativeInfinity;
			return true;
		}
		value = 0.0;
		return false;
	}

	public static bool TryGetUnescapedBase64Bytes(ReadOnlySpan<byte> utf8Source, int idx, [NotNullWhen(true)] out byte[] bytes)
	{
		byte[] array = null;
		Span<byte> span = ((utf8Source.Length > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(utf8Source.Length))) : stackalloc byte[256]);
		Span<byte> span2 = span;
		Unescape(utf8Source, span2, idx, out var written);
		span2 = span2.Slice(0, written);
		bool result = TryDecodeBase64InPlace(span2, out bytes);
		if (array != null)
		{
			span2.Clear();
			ArrayPool<byte>.Shared.Return(array);
		}
		return result;
	}

	public static string GetUnescapedString(ReadOnlySpan<byte> utf8Source, int idx)
	{
		int length = utf8Source.Length;
		byte[] array = null;
		Span<byte> span = ((length > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(length))) : stackalloc byte[256]);
		Span<byte> span2 = span;
		Unescape(utf8Source, span2, idx, out var written);
		span2 = span2.Slice(0, written);
		string result = TranscodeHelper(span2);
		if (array != null)
		{
			span2.Clear();
			ArrayPool<byte>.Shared.Return(array);
		}
		return result;
	}

	public static ReadOnlySpan<byte> GetUnescapedSpan(ReadOnlySpan<byte> utf8Source, int idx)
	{
		int length = utf8Source.Length;
		byte[] array = null;
		Span<byte> span = ((length > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(length))) : stackalloc byte[256]);
		Span<byte> destination = span;
		Unescape(utf8Source, destination, idx, out var written);
		ReadOnlySpan<byte> result = destination.Slice(0, written).ToArray();
		if (array != null)
		{
			new Span<byte>(array, 0, written).Clear();
			ArrayPool<byte>.Shared.Return(array);
		}
		return result;
	}

	public static bool UnescapeAndCompare(ReadOnlySpan<byte> utf8Source, ReadOnlySpan<byte> other)
	{
		byte[] array = null;
		Span<byte> span = ((utf8Source.Length > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(utf8Source.Length))) : stackalloc byte[256]);
		Span<byte> span2 = span;
		Unescape(utf8Source, span2, 0, out var written);
		span2 = span2.Slice(0, written);
		bool result = other.SequenceEqual(span2);
		if (array != null)
		{
			span2.Clear();
			ArrayPool<byte>.Shared.Return(array);
		}
		return result;
	}

	public static bool UnescapeAndCompare(ReadOnlySequence<byte> utf8Source, ReadOnlySpan<byte> other)
	{
		byte[] array = null;
		byte[] array2 = null;
		int num = checked((int)utf8Source.Length);
		Span<byte> span = ((num > 256) ? ((Span<byte>)(array2 = ArrayPool<byte>.Shared.Rent(num))) : stackalloc byte[256]);
		Span<byte> span2 = span;
		Span<byte> span3 = ((num > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(num))) : stackalloc byte[256]);
		Span<byte> span4 = span3;
		utf8Source.CopyTo(span4);
		span4 = span4.Slice(0, num);
		Unescape(span4, span2, 0, out var written);
		span2 = span2.Slice(0, written);
		bool result = other.SequenceEqual(span2);
		if (array2 != null)
		{
			span2.Clear();
			ArrayPool<byte>.Shared.Return(array2);
			span4.Clear();
			ArrayPool<byte>.Shared.Return(array);
		}
		return result;
	}

	public static bool TryDecodeBase64InPlace(Span<byte> utf8Unescaped, [NotNullWhen(true)] out byte[] bytes)
	{
		if (Base64.DecodeFromUtf8InPlace(utf8Unescaped, out var bytesWritten) != 0)
		{
			bytes = null;
			return false;
		}
		bytes = utf8Unescaped.Slice(0, bytesWritten).ToArray();
		return true;
	}

	public static bool TryDecodeBase64(ReadOnlySpan<byte> utf8Unescaped, [NotNullWhen(true)] out byte[] bytes)
	{
		byte[] array = null;
		Span<byte> span = ((utf8Unescaped.Length > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(utf8Unescaped.Length))) : stackalloc byte[256]);
		Span<byte> bytes2 = span;
		if (Base64.DecodeFromUtf8(utf8Unescaped, bytes2, out var _, out var bytesWritten) != 0)
		{
			bytes = null;
			if (array != null)
			{
				bytes2.Clear();
				ArrayPool<byte>.Shared.Return(array);
			}
			return false;
		}
		bytes = bytes2.Slice(0, bytesWritten).ToArray();
		if (array != null)
		{
			bytes2.Clear();
			ArrayPool<byte>.Shared.Return(array);
		}
		return true;
	}

	public static string TranscodeHelper(ReadOnlySpan<byte> utf8Unescaped)
	{
		try
		{
			return s_utf8Encoding.GetString(utf8Unescaped);
		}
		catch (DecoderFallbackException innerException)
		{
			throw ThrowHelper.GetInvalidOperationException_ReadInvalidUTF8(innerException);
		}
	}

	internal static int GetUtf8ByteCount(ReadOnlySpan<char> text)
	{
		try
		{
			return s_utf8Encoding.GetByteCount(text);
		}
		catch (EncoderFallbackException innerException)
		{
			throw ThrowHelper.GetArgumentException_ReadInvalidUTF16(innerException);
		}
	}

	internal static int GetUtf8FromText(ReadOnlySpan<char> text, Span<byte> dest)
	{
		try
		{
			return s_utf8Encoding.GetBytes(text, dest);
		}
		catch (EncoderFallbackException innerException)
		{
			throw ThrowHelper.GetArgumentException_ReadInvalidUTF16(innerException);
		}
	}

	internal static string GetTextFromUtf8(ReadOnlySpan<byte> utf8Text)
	{
		return s_utf8Encoding.GetString(utf8Text);
	}

	internal static void Unescape(ReadOnlySpan<byte> source, Span<byte> destination, int idx, out int written)
	{
		source.Slice(0, idx).CopyTo(destination);
		written = idx;
		while (idx < source.Length)
		{
			byte b = source[idx];
			if (b == 92)
			{
				idx++;
				switch (source[idx])
				{
				case 34:
					destination[written++] = 34;
					break;
				case 110:
					destination[written++] = 10;
					break;
				case 114:
					destination[written++] = 13;
					break;
				case 92:
					destination[written++] = 92;
					break;
				case 47:
					destination[written++] = 47;
					break;
				case 116:
					destination[written++] = 9;
					break;
				case 98:
					destination[written++] = 8;
					break;
				case 102:
					destination[written++] = 12;
					break;
				case 117:
				{
					bool flag = Utf8Parser.TryParse(source.Slice(idx + 1, 4), out int value, out int bytesConsumed, 'x');
					idx += bytesConsumed;
					if (JsonHelpers.IsInRangeInclusive((uint)value, 55296u, 57343u))
					{
						if (value >= 56320)
						{
							ThrowHelper.ThrowInvalidOperationException_ReadInvalidUTF16(value);
						}
						idx += 3;
						if (source.Length < idx + 4 || source[idx - 2] != 92 || source[idx - 1] != 117)
						{
							ThrowHelper.ThrowInvalidOperationException_ReadInvalidUTF16();
						}
						flag = Utf8Parser.TryParse(source.Slice(idx, 4), out int value2, out bytesConsumed, 'x');
						if (!JsonHelpers.IsInRangeInclusive((uint)value2, 56320u, 57343u))
						{
							ThrowHelper.ThrowInvalidOperationException_ReadInvalidUTF16(value2);
						}
						idx += bytesConsumed - 1;
						value = 1024 * (value - 55296) + (value2 - 56320) + 65536;
					}
					int num = new Rune(value).EncodeToUtf8(destination.Slice(written));
					written += num;
					break;
				}
				}
			}
			else
			{
				destination[written++] = b;
			}
			idx++;
		}
	}
}
