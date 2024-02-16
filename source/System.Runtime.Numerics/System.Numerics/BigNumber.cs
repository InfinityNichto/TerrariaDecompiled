using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Numerics;

internal static class BigNumber
{
	private struct BigNumberBuffer
	{
		public StringBuilder digits;

		public int precision;

		public int scale;

		public bool sign;

		public static BigNumberBuffer Create()
		{
			BigNumberBuffer result = default(BigNumberBuffer);
			result.digits = new StringBuilder();
			return result;
		}
	}

	private static readonly uint[] s_uint32PowersOfTen = new uint[10] { 1u, 10u, 100u, 1000u, 10000u, 100000u, 1000000u, 10000000u, 100000000u, 1000000000u };

	internal static bool TryValidateParseStyleInteger(NumberStyles style, [NotNullWhen(false)] out ArgumentException e)
	{
		if (((uint)style & 0xFFFFFC00u) != 0)
		{
			e = new ArgumentException(System.SR.Format(System.SR.Argument_InvalidNumberStyles, "style"));
			return false;
		}
		if ((style & NumberStyles.AllowHexSpecifier) != 0 && ((uint)style & 0xFFFFFDFCu) != 0)
		{
			e = new ArgumentException(System.SR.Argument_InvalidHexStyle);
			return false;
		}
		e = null;
		return true;
	}

	internal static bool TryParseBigInteger(string value, NumberStyles style, NumberFormatInfo info, out BigInteger result)
	{
		if (value == null)
		{
			result = default(BigInteger);
			return false;
		}
		return TryParseBigInteger(value.AsSpan(), style, info, out result);
	}

	internal static bool TryParseBigInteger(ReadOnlySpan<char> value, NumberStyles style, NumberFormatInfo info, out BigInteger result)
	{
		if (!TryValidateParseStyleInteger(style, out var e))
		{
			throw e;
		}
		BigNumberBuffer number = BigNumberBuffer.Create();
		if (!FormatProvider.TryStringToBigInteger(value, style, info, number.digits, out number.precision, out number.scale, out number.sign))
		{
			result = default(BigInteger);
			return false;
		}
		if ((style & NumberStyles.AllowHexSpecifier) != 0)
		{
			return HexNumberToBigInteger(ref number, out result);
		}
		return NumberToBigInteger(ref number, out result);
	}

	internal static BigInteger ParseBigInteger(string value, NumberStyles style, NumberFormatInfo info)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		return ParseBigInteger(value.AsSpan(), style, info);
	}

	internal static BigInteger ParseBigInteger(ReadOnlySpan<char> value, NumberStyles style, NumberFormatInfo info)
	{
		if (!TryValidateParseStyleInteger(style, out var e))
		{
			throw e;
		}
		if (!TryParseBigInteger(value, style, info, out var result))
		{
			throw new FormatException(System.SR.Overflow_ParseBigInteger);
		}
		return result;
	}

	private static bool HexNumberToBigInteger(ref BigNumberBuffer number, out BigInteger result)
	{
		if (number.digits == null || number.digits.Length == 0)
		{
			result = default(BigInteger);
			return false;
		}
		int a = number.digits.Length - 1;
		int result2;
		int num = Math.DivRem(a, 8, out result2);
		int num2;
		if (result2 == 0)
		{
			num2 = 0;
		}
		else
		{
			num++;
			num2 = 8 - result2;
		}
		bool flag = System.HexConverter.FromChar(number.digits[0]) >= 8;
		uint num3 = ((flag && num2 > 0) ? uint.MaxValue : 0u);
		int[] array = null;
		Span<uint> span = ((num > 64) ? MemoryMarshal.Cast<int, uint>((array = ArrayPool<int>.Shared.Rent(num)).AsSpan(0, num)) : stackalloc uint[num]);
		Span<uint> span2 = span;
		int num4 = num - 1;
		try
		{
			StringBuilder.ChunkEnumerator enumerator = number.digits.GetChunks().GetEnumerator();
			while (enumerator.MoveNext())
			{
				ReadOnlySpan<char> span3 = enumerator.Current.Span;
				for (int i = 0; i < span3.Length; i++)
				{
					char c = span3[i];
					if (c == '\0')
					{
						break;
					}
					int num5 = System.HexConverter.FromChar(c);
					num3 = (num3 << 4) | (uint)num5;
					num2++;
					if (num2 == 8)
					{
						span2[num4] = num3;
						num4--;
						num3 = 0u;
						num2 = 0;
					}
				}
			}
			span2 = span2.TrimEnd(0u);
			int num6;
			uint[] array2;
			if (span2.IsEmpty)
			{
				num6 = 0;
				array2 = null;
			}
			else if (span2.Length == 1)
			{
				num6 = (int)span2[0];
				array2 = null;
				if ((!flag && num6 < 0) || num6 == int.MinValue)
				{
					array2 = new uint[1] { (uint)num6 };
					num6 = ((!flag) ? 1 : (-1));
				}
			}
			else
			{
				num6 = ((!flag) ? 1 : (-1));
				array2 = span2.ToArray();
				if (flag)
				{
					NumericsHelpers.DangerousMakeTwosComplement(array2);
				}
			}
			result = new BigInteger(num6, array2);
			return true;
		}
		finally
		{
			if (array != null)
			{
				ArrayPool<int>.Shared.Return(array);
			}
		}
	}

	private static bool NumberToBigInteger(ref BigNumberBuffer number, out BigInteger result)
	{
		Span<uint> span = stackalloc uint[64];
		Span<uint> currentBuffer2 = span;
		int currentBufferSize = 0;
		int[] arrayFromPool = null;
		uint partialValue = 0u;
		int partialDigitCount = 0;
		int totalDigitCount = 0;
		int numberScale = number.scale;
		try
		{
			StringBuilder.ChunkEnumerator enumerator = number.digits.GetChunks().GetEnumerator();
			while (enumerator.MoveNext())
			{
				if (!ProcessChunk(enumerator.Current.Span, ref currentBuffer2))
				{
					result = default(BigInteger);
					return false;
				}
			}
			if (partialDigitCount > 0)
			{
				MultiplyAdd(ref currentBuffer2, s_uint32PowersOfTen[partialDigitCount], partialValue);
			}
			int num;
			for (num = numberScale - totalDigitCount; num >= 9; num -= 9)
			{
				MultiplyAdd(ref currentBuffer2, 1000000000u, 0u);
			}
			if (num > 0)
			{
				MultiplyAdd(ref currentBuffer2, s_uint32PowersOfTen[num], 0u);
			}
			int n;
			uint[] rgu;
			if (currentBufferSize == 0)
			{
				n = 0;
				rgu = null;
			}
			else if (currentBufferSize == 1 && currentBuffer2[0] <= int.MaxValue)
			{
				n = (int)(number.sign ? (0L - (long)currentBuffer2[0]) : currentBuffer2[0]);
				rgu = null;
			}
			else
			{
				n = ((!number.sign) ? 1 : (-1));
				rgu = currentBuffer2.Slice(0, currentBufferSize).ToArray();
			}
			result = new BigInteger(n, rgu);
			return true;
		}
		finally
		{
			if (arrayFromPool != null)
			{
				ArrayPool<int>.Shared.Return(arrayFromPool);
			}
		}
		void MultiplyAdd(ref Span<uint> currentBuffer, uint multiplier, uint addValue)
		{
			Span<uint> span2 = currentBuffer.Slice(0, currentBufferSize);
			uint num2 = addValue;
			for (int i = 0; i < span2.Length; i++)
			{
				ulong num3 = (ulong)((long)multiplier * (long)span2[i] + num2);
				span2[i] = (uint)num3;
				num2 = (uint)(num3 >> 32);
			}
			if (num2 != 0)
			{
				if (currentBufferSize == currentBuffer.Length)
				{
					int[] array = arrayFromPool;
					arrayFromPool = ArrayPool<int>.Shared.Rent(checked(currentBufferSize * 2));
					Span<uint> span3 = MemoryMarshal.Cast<int, uint>(arrayFromPool);
					currentBuffer.CopyTo(span3);
					currentBuffer = span3;
					if (array != null)
					{
						ArrayPool<int>.Shared.Return(array);
					}
				}
				currentBuffer[currentBufferSize] = num2;
				currentBufferSize++;
			}
		}
		bool ProcessChunk(ReadOnlySpan<char> chunkDigits, ref Span<uint> currentBuffer)
		{
			int val = Math.Max(numberScale - totalDigitCount, 0);
			ReadOnlySpan<char> readOnlySpan = chunkDigits.Slice(0, Math.Min(val, chunkDigits.Length));
			bool flag = false;
			uint num4 = partialValue;
			int num5 = partialDigitCount;
			int num6 = totalDigitCount;
			for (int j = 0; j < readOnlySpan.Length; j++)
			{
				char c = chunkDigits[j];
				if (c == '\0')
				{
					flag = true;
					break;
				}
				num4 = num4 * 10 + (uint)(c - 48);
				num5++;
				num6++;
				if (num5 == 9)
				{
					MultiplyAdd(ref currentBuffer, 1000000000u, num4);
					num4 = 0u;
					num5 = 0;
				}
			}
			if (!flag)
			{
				ReadOnlySpan<char> readOnlySpan2 = chunkDigits.Slice(readOnlySpan.Length);
				for (int k = 0; k < readOnlySpan2.Length; k++)
				{
					switch (readOnlySpan2[k])
					{
					default:
						return false;
					case '0':
						continue;
					case '\0':
						break;
					}
					break;
				}
			}
			partialValue = num4;
			partialDigitCount = num5;
			totalDigitCount = num6;
			return true;
		}
	}

	internal static char ParseFormatSpecifier(ReadOnlySpan<char> format, out int digits)
	{
		digits = -1;
		if (format.Length == 0)
		{
			return 'R';
		}
		int num = 0;
		char c = format[num];
		if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
		{
			num++;
			int num2 = -1;
			if (num < format.Length && format[num] >= '0' && format[num] <= '9')
			{
				num2 = format[num++] - 48;
				while (num < format.Length && format[num] >= '0' && format[num] <= '9')
				{
					int num3 = num2 * 10 + (format[num++] - 48);
					if (num3 < num2)
					{
						throw new FormatException(System.SR.Argument_BadFormatSpecifier);
					}
					num2 = num3;
				}
			}
			if (num >= format.Length || format[num] == '\0')
			{
				digits = num2;
				return c;
			}
		}
		return '\0';
	}

	private static string FormatBigIntegerToHex(bool targetSpan, BigInteger value, char format, int digits, NumberFormatInfo info, Span<char> destination, out int charsWritten, out bool spanSuccess)
	{
		byte[] array = null;
		Span<byte> destination2 = stackalloc byte[64];
		if (!value.TryWriteOrCountBytes(destination2, out var bytesWritten))
		{
			destination2 = (array = ArrayPool<byte>.Shared.Rent(bytesWritten));
			bool flag = value.TryWriteBytes(destination2, out bytesWritten);
		}
		destination2 = destination2.Slice(0, bytesWritten);
		Span<char> initialBuffer = stackalloc char[128];
		System.Text.ValueStringBuilder valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		int num = destination2.Length - 1;
		if (num > -1)
		{
			bool flag2 = false;
			byte b = destination2[num];
			if (b > 247)
			{
				b -= 240;
				flag2 = true;
			}
			if (b < 8 || flag2)
			{
				valueStringBuilder.Append((b < 10) ? ((char)(b + 48)) : ((format == 'X') ? ((char)((b & 0xF) - 10 + 65)) : ((char)((b & 0xF) - 10 + 97))));
				num--;
			}
		}
		if (num > -1)
		{
			Span<char> span = valueStringBuilder.AppendSpan((num + 1) * 2);
			int num2 = 0;
			string text = ((format == 'x') ? "0123456789abcdef" : "0123456789ABCDEF");
			while (num > -1)
			{
				byte b2 = destination2[num--];
				span[num2++] = text[b2 >> 4];
				span[num2++] = text[b2 & 0xF];
			}
		}
		if (digits > valueStringBuilder.Length)
		{
			valueStringBuilder.Insert(0, (value._sign >= 0) ? '0' : ((format == 'x') ? 'f' : 'F'), digits - valueStringBuilder.Length);
		}
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
		if (targetSpan)
		{
			spanSuccess = valueStringBuilder.TryCopyTo(destination, out charsWritten);
			return null;
		}
		charsWritten = 0;
		spanSuccess = false;
		return valueStringBuilder.ToString();
	}

	internal static string FormatBigInteger(BigInteger value, string format, NumberFormatInfo info)
	{
		int charsWritten;
		bool spanSuccess;
		return FormatBigInteger(targetSpan: false, value, format, format, info, default(Span<char>), out charsWritten, out spanSuccess);
	}

	internal static bool TryFormatBigInteger(BigInteger value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<char> destination, out int charsWritten)
	{
		FormatBigInteger(targetSpan: true, value, null, format, info, destination, out charsWritten, out var spanSuccess);
		return spanSuccess;
	}

	private static string FormatBigInteger(bool targetSpan, BigInteger value, string formatString, ReadOnlySpan<char> formatSpan, NumberFormatInfo info, Span<char> destination, out int charsWritten, out bool spanSuccess)
	{
		int digits = 0;
		char c = ParseFormatSpecifier(formatSpan, out digits);
		if (c == 'x' || c == 'X')
		{
			return FormatBigIntegerToHex(targetSpan, value, c, digits, info, destination, out charsWritten, out spanSuccess);
		}
		if (value._bits == null)
		{
			if (c == 'g' || c == 'G' || c == 'r' || c == 'R')
			{
				formatSpan = (formatString = ((digits > 0) ? $"D{digits}" : "D"));
			}
			if (targetSpan)
			{
				spanSuccess = value._sign.TryFormat(destination, out charsWritten, formatSpan, info);
				return null;
			}
			charsWritten = 0;
			spanSuccess = false;
			return value._sign.ToString(formatString, info);
		}
		int num = value._bits.Length;
		uint[] array;
		int num3;
		int num4;
		checked
		{
			int num2;
			try
			{
				num2 = unchecked(checked(num * 10) / 9) + 2;
			}
			catch (OverflowException innerException)
			{
				throw new FormatException(System.SR.Format_TooLarge, innerException);
			}
			array = new uint[num2];
			num3 = 0;
			num4 = num;
		}
		while (--num4 >= 0)
		{
			uint num5 = value._bits[num4];
			for (int i = 0; i < num3; i++)
			{
				ulong num6 = NumericsHelpers.MakeUlong(array[i], num5);
				array[i] = (uint)(num6 % 1000000000);
				num5 = (uint)(num6 / 1000000000);
			}
			if (num5 != 0)
			{
				array[num3++] = num5 % 1000000000;
				num5 /= 1000000000;
				if (num5 != 0)
				{
					array[num3++] = num5;
				}
			}
		}
		int num7;
		bool flag;
		char[] array2;
		int num9;
		checked
		{
			try
			{
				num7 = num3 * 9;
			}
			catch (OverflowException innerException2)
			{
				throw new FormatException(System.SR.Format_TooLarge, innerException2);
			}
			flag = c == 'g' || c == 'G' || c == 'd' || c == 'D' || c == 'r' || c == 'R';
			if (flag)
			{
				if (digits > 0 && digits > num7)
				{
					num7 = digits;
				}
				if (value._sign < 0)
				{
					try
					{
						num7 += info.NegativeSign.Length;
					}
					catch (OverflowException innerException3)
					{
						throw new FormatException(System.SR.Format_TooLarge, innerException3);
					}
				}
			}
			int num8;
			try
			{
				num8 = num7 + 1;
			}
			catch (OverflowException innerException4)
			{
				throw new FormatException(System.SR.Format_TooLarge, innerException4);
			}
			array2 = new char[num8];
			num9 = num7;
		}
		for (int j = 0; j < num3 - 1; j++)
		{
			uint num10 = array[j];
			int num11 = 9;
			while (--num11 >= 0)
			{
				array2[--num9] = (char)(48 + num10 % 10);
				num10 /= 10;
			}
		}
		for (uint num12 = array[num3 - 1]; num12 != 0; num12 /= 10)
		{
			array2[--num9] = (char)(48 + num12 % 10);
		}
		if (!flag)
		{
			bool sign = value._sign < 0;
			int precision = 29;
			int scale = num7 - num9;
			Span<char> initialBuffer = stackalloc char[128];
			System.Text.ValueStringBuilder sb = new System.Text.ValueStringBuilder(initialBuffer);
			FormatProvider.FormatBigInteger(ref sb, precision, scale, sign, formatSpan, info, array2, num9);
			if (targetSpan)
			{
				spanSuccess = sb.TryCopyTo(destination, out charsWritten);
				return null;
			}
			charsWritten = 0;
			spanSuccess = false;
			return sb.ToString();
		}
		int num13 = num7 - num9;
		while (digits > 0 && digits > num13)
		{
			array2[--num9] = '0';
			digits--;
		}
		if (value._sign < 0)
		{
			string negativeSign = info.NegativeSign;
			for (int num14 = negativeSign.Length - 1; num14 > -1; num14--)
			{
				array2[--num9] = negativeSign[num14];
			}
		}
		int num15 = num7 - num9;
		if (!targetSpan)
		{
			charsWritten = 0;
			spanSuccess = false;
			return new string(array2, num9, num7 - num9);
		}
		if (new ReadOnlySpan<char>(array2, num9, num7 - num9).TryCopyTo(destination))
		{
			charsWritten = num15;
			spanSuccess = true;
			return null;
		}
		charsWritten = 0;
		spanSuccess = false;
		return null;
	}
}
