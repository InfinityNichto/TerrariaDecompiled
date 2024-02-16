using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization;

internal static class TimeSpanFormat
{
	private enum StandardFormat
	{
		C,
		G,
		g
	}

	internal struct FormatLiterals
	{
		internal string AppCompatLiteral;

		internal int dd;

		internal int hh;

		internal int mm;

		internal int ss;

		internal int ff;

		private string[] _literals;

		internal string Start => _literals[0];

		internal string DayHourSep => _literals[1];

		internal string HourMinuteSep => _literals[2];

		internal string MinuteSecondSep => _literals[3];

		internal string SecondFractionSep => _literals[4];

		internal string End => _literals[5];

		internal static FormatLiterals InitInvariant(bool isNegative)
		{
			FormatLiterals result = default(FormatLiterals);
			result._literals = new string[6];
			result._literals[0] = (isNegative ? "-" : string.Empty);
			result._literals[1] = ".";
			result._literals[2] = ":";
			result._literals[3] = ":";
			result._literals[4] = ".";
			result._literals[5] = string.Empty;
			result.AppCompatLiteral = ":.";
			result.dd = 2;
			result.hh = 2;
			result.mm = 2;
			result.ss = 2;
			result.ff = 7;
			return result;
		}

		internal void Init(ReadOnlySpan<char> format, bool useInvariantFieldLengths)
		{
			dd = (hh = (mm = (ss = (ff = 0))));
			_literals = new string[6];
			for (int i = 0; i < _literals.Length; i++)
			{
				_literals[i] = string.Empty;
			}
			StringBuilder stringBuilder = StringBuilderCache.Acquire();
			bool flag = false;
			char c = '\'';
			int num = 0;
			for (int j = 0; j < format.Length; j++)
			{
				switch (format[j])
				{
				case '"':
				case '\'':
					if (flag && c == format[j])
					{
						if (num < 0 || num > 5)
						{
							return;
						}
						_literals[num] = stringBuilder.ToString();
						stringBuilder.Length = 0;
						flag = false;
					}
					else if (!flag)
					{
						c = format[j];
						flag = true;
					}
					continue;
				case '\\':
					if (!flag)
					{
						j++;
						continue;
					}
					break;
				case 'd':
					if (!flag)
					{
						num = 1;
						dd++;
					}
					continue;
				case 'h':
					if (!flag)
					{
						num = 2;
						hh++;
					}
					continue;
				case 'm':
					if (!flag)
					{
						num = 3;
						mm++;
					}
					continue;
				case 's':
					if (!flag)
					{
						num = 4;
						ss++;
					}
					continue;
				case 'F':
				case 'f':
					if (!flag)
					{
						num = 5;
						ff++;
					}
					continue;
				}
				stringBuilder.Append(format[j]);
			}
			AppCompatLiteral = MinuteSecondSep + SecondFractionSep;
			if (useInvariantFieldLengths)
			{
				dd = 2;
				hh = 2;
				mm = 2;
				ss = 2;
				ff = 7;
			}
			else
			{
				if (dd < 1 || dd > 2)
				{
					dd = 2;
				}
				if (hh < 1 || hh > 2)
				{
					hh = 2;
				}
				if (mm < 1 || mm > 2)
				{
					mm = 2;
				}
				if (ss < 1 || ss > 2)
				{
					ss = 2;
				}
				if (ff < 1 || ff > 7)
				{
					ff = 7;
				}
			}
			StringBuilderCache.Release(stringBuilder);
		}
	}

	internal static readonly FormatLiterals PositiveInvariantFormatLiterals = FormatLiterals.InitInvariant(isNegative: false);

	internal static readonly FormatLiterals NegativeInvariantFormatLiterals = FormatLiterals.InitInvariant(isNegative: true);

	internal static string Format(TimeSpan value, string format, IFormatProvider formatProvider)
	{
		if (string.IsNullOrEmpty(format))
		{
			return FormatC(value);
		}
		if (format.Length == 1)
		{
			char c = format[0];
			if (c == 'c' || (c | 0x20) == 116)
			{
				return FormatC(value);
			}
			if ((c | 0x20) == 103)
			{
				return FormatG(value, DateTimeFormatInfo.GetInstance(formatProvider), (c == 'G') ? StandardFormat.G : StandardFormat.g);
			}
			throw new FormatException(SR.Format_InvalidString);
		}
		return StringBuilderCache.GetStringAndRelease(FormatCustomized(value, format, DateTimeFormatInfo.GetInstance(formatProvider)));
	}

	internal static bool TryFormat(TimeSpan value, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider formatProvider)
	{
		if (format.Length == 0)
		{
			return TryFormatStandard(value, StandardFormat.C, null, destination, out charsWritten);
		}
		if (format.Length == 1)
		{
			char c = format[0];
			if (c == 'c' || (c | 0x20) == 116)
			{
				return TryFormatStandard(value, StandardFormat.C, null, destination, out charsWritten);
			}
			return TryFormatStandard(value, c switch
			{
				'G' => StandardFormat.G, 
				'g' => StandardFormat.g, 
				_ => throw new FormatException(SR.Format_InvalidString), 
			}, DateTimeFormatInfo.GetInstance(formatProvider).DecimalSeparator, destination, out charsWritten);
		}
		StringBuilder stringBuilder = FormatCustomized(value, format, DateTimeFormatInfo.GetInstance(formatProvider));
		if (stringBuilder.Length <= destination.Length)
		{
			stringBuilder.CopyTo(0, destination, stringBuilder.Length);
			charsWritten = stringBuilder.Length;
			StringBuilderCache.Release(stringBuilder);
			return true;
		}
		charsWritten = 0;
		StringBuilderCache.Release(stringBuilder);
		return false;
	}

	internal static string FormatC(TimeSpan value)
	{
		Span<char> destination = stackalloc char[26];
		TryFormatStandard(value, StandardFormat.C, null, destination, out var charsWritten);
		return new string(destination.Slice(0, charsWritten));
	}

	private static string FormatG(TimeSpan value, DateTimeFormatInfo dtfi, StandardFormat format)
	{
		string decimalSeparator = dtfi.DecimalSeparator;
		int num = 25 + decimalSeparator.Length;
		Span<char> span = ((num >= 128) ? ((Span<char>)new char[num]) : stackalloc char[num]);
		Span<char> destination = span;
		TryFormatStandard(value, format, decimalSeparator, destination, out var charsWritten);
		return new string(destination.Slice(0, charsWritten));
	}

	private static bool TryFormatStandard(TimeSpan value, StandardFormat format, string decimalSeparator, Span<char> destination, out int charsWritten)
	{
		int num = 8;
		long num2 = value.Ticks;
		uint valueWithoutTrailingZeros;
		ulong num3;
		if (num2 < 0)
		{
			num = 9;
			num2 = -num2;
			if (num2 < 0)
			{
				valueWithoutTrailingZeros = 4775808u;
				num3 = 922337203685uL;
				goto IL_0050;
			}
		}
		(ulong Quotient, ulong Remainder) tuple = Math.DivRem((ulong)num2, 10000000uL);
		num3 = tuple.Quotient;
		ulong item = tuple.Remainder;
		valueWithoutTrailingZeros = (uint)item;
		goto IL_0050;
		IL_0050:
		int num4 = 0;
		switch (format)
		{
		case StandardFormat.C:
			if (valueWithoutTrailingZeros != 0)
			{
				num4 = 7;
				num += num4 + 1;
			}
			break;
		case StandardFormat.G:
			num4 = 7;
			num += num4 + 1;
			break;
		default:
			if (valueWithoutTrailingZeros != 0)
			{
				num4 = 7 - FormattingHelpers.CountDecimalTrailingZeros(valueWithoutTrailingZeros, out valueWithoutTrailingZeros);
				num += num4 + 1;
			}
			break;
		}
		ulong num5 = 0uL;
		ulong num6 = 0uL;
		if (num3 != 0)
		{
			(num5, num6) = Math.DivRem(num3, 60uL);
		}
		ulong num7 = 0uL;
		ulong num8 = 0uL;
		if (num5 != 0)
		{
			(num7, num8) = Math.DivRem(num5, 60uL);
		}
		uint num9 = 0u;
		uint num10 = 0u;
		if (num7 != 0)
		{
			(num9, num10) = Math.DivRem((uint)num7, 24u);
		}
		int num11 = 2;
		if (format == StandardFormat.g && num10 < 10)
		{
			num11 = 1;
			num--;
		}
		int num12 = 0;
		if (num9 != 0)
		{
			num12 = FormattingHelpers.CountDigits(num9);
			num += num12 + 1;
		}
		else if (format == StandardFormat.G)
		{
			num += 2;
			num12 = 1;
		}
		if (destination.Length < num)
		{
			charsWritten = 0;
			return false;
		}
		int num13 = 0;
		if (value.Ticks < 0)
		{
			destination[num13++] = '-';
		}
		if (num12 != 0)
		{
			WriteDigits(num9, destination.Slice(num13, num12));
			num13 += num12;
			destination[num13++] = ((format == StandardFormat.C) ? '.' : ':');
		}
		if (num11 == 2)
		{
			WriteTwoDigits(num10, destination.Slice(num13));
			num13 += 2;
		}
		else
		{
			destination[num13++] = (char)(48 + num10);
		}
		destination[num13++] = ':';
		WriteTwoDigits((uint)num8, destination.Slice(num13));
		num13 += 2;
		destination[num13++] = ':';
		WriteTwoDigits((uint)num6, destination.Slice(num13));
		num13 += 2;
		if (num4 != 0)
		{
			if (format == StandardFormat.C)
			{
				destination[num13++] = '.';
			}
			else if (decimalSeparator.Length == 1)
			{
				destination[num13++] = decimalSeparator[0];
			}
			else
			{
				decimalSeparator.CopyTo(destination);
				num13 += decimalSeparator.Length;
			}
			WriteDigits(valueWithoutTrailingZeros, destination.Slice(num13, num4));
			num13 += num4;
		}
		charsWritten = num;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void WriteTwoDigits(uint value, Span<char> buffer)
	{
		uint num = 48 + value;
		value /= 10;
		buffer[1] = (char)(num - value * 10);
		buffer[0] = (char)(48 + value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void WriteDigits(uint value, Span<char> buffer)
	{
		for (int num = buffer.Length - 1; num >= 1; num--)
		{
			uint num2 = 48 + value;
			value /= 10;
			buffer[num] = (char)(num2 - value * 10);
		}
		buffer[0] = (char)(48 + value);
	}

	private static StringBuilder FormatCustomized(TimeSpan value, ReadOnlySpan<char> format, DateTimeFormatInfo dtfi, StringBuilder result = null)
	{
		bool flag = false;
		if (result == null)
		{
			result = StringBuilderCache.Acquire();
			flag = true;
		}
		int num = (int)(value.Ticks / 864000000000L);
		long num2 = value.Ticks % 864000000000L;
		if (value.Ticks < 0)
		{
			num = -num;
			num2 = -num2;
		}
		int value2 = (int)(num2 / 36000000000L % 24);
		int value3 = (int)(num2 / 600000000 % 60);
		int value4 = (int)(num2 / 10000000 % 60);
		int num3 = (int)(num2 % 10000000);
		long num4 = 0L;
		int num6;
		for (int i = 0; i < format.Length; i += num6)
		{
			char c = format[i];
			switch (c)
			{
			case 'h':
				num6 = DateTimeFormat.ParseRepeatPattern(format, i, c);
				if (num6 <= 2)
				{
					DateTimeFormat.FormatDigits(result, value2, num6);
					continue;
				}
				break;
			case 'm':
				num6 = DateTimeFormat.ParseRepeatPattern(format, i, c);
				if (num6 <= 2)
				{
					DateTimeFormat.FormatDigits(result, value3, num6);
					continue;
				}
				break;
			case 's':
				num6 = DateTimeFormat.ParseRepeatPattern(format, i, c);
				if (num6 <= 2)
				{
					DateTimeFormat.FormatDigits(result, value4, num6);
					continue;
				}
				break;
			case 'f':
				num6 = DateTimeFormat.ParseRepeatPattern(format, i, c);
				if (num6 <= 7)
				{
					num4 = num3;
					num4 /= TimeSpanParse.Pow10(7 - num6);
					result.AppendSpanFormattable(num4, DateTimeFormat.fixedNumberFormats[num6 - 1], CultureInfo.InvariantCulture);
					continue;
				}
				break;
			case 'F':
				num6 = DateTimeFormat.ParseRepeatPattern(format, i, c);
				if (num6 <= 7)
				{
					num4 = num3;
					num4 /= TimeSpanParse.Pow10(7 - num6);
					int num7 = num6;
					while (num7 > 0 && num4 % 10 == 0L)
					{
						num4 /= 10;
						num7--;
					}
					if (num7 > 0)
					{
						result.AppendSpanFormattable(num4, DateTimeFormat.fixedNumberFormats[num7 - 1], CultureInfo.InvariantCulture);
					}
					continue;
				}
				break;
			case 'd':
				num6 = DateTimeFormat.ParseRepeatPattern(format, i, c);
				if (num6 <= 8)
				{
					DateTimeFormat.FormatDigits(result, num, num6, overrideLengthLimit: true);
					continue;
				}
				break;
			case '"':
			case '\'':
				num6 = DateTimeFormat.ParseQuoteString(format, i, result);
				continue;
			case '%':
			{
				int num5 = DateTimeFormat.ParseNextChar(format, i);
				if (num5 >= 0 && num5 != 37)
				{
					char reference = (char)num5;
					StringBuilder stringBuilder = FormatCustomized(value, MemoryMarshal.CreateReadOnlySpan(ref reference, 1), dtfi, result);
					num6 = 2;
					continue;
				}
				break;
			}
			case '\\':
			{
				int num5 = DateTimeFormat.ParseNextChar(format, i);
				if (num5 >= 0)
				{
					result.Append((char)num5);
					num6 = 2;
					continue;
				}
				break;
			}
			}
			if (flag)
			{
				StringBuilderCache.Release(result);
			}
			throw new FormatException(SR.Format_InvalidString);
		}
		return result;
	}
}
