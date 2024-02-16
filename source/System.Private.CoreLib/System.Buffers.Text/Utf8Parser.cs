using System.Buffers.Binary;
using Internal.Runtime.CompilerServices;

namespace System.Buffers.Text;

public static class Utf8Parser
{
	[Flags]
	private enum ParseNumberOptions
	{
		AllowExponent = 1
	}

	private enum ComponentParseResult : byte
	{
		NoMoreData,
		Colon,
		Period,
		ParseFailure
	}

	private struct TimeSpanSplitter
	{
		public uint V1;

		public uint V2;

		public uint V3;

		public uint V4;

		public uint V5;

		public bool IsNegative;

		public uint Separators;

		public bool TrySplitTimeSpan(ReadOnlySpan<byte> source, bool periodUsedToSeparateDay, out int bytesConsumed)
		{
			int i = 0;
			byte b = 0;
			for (; i != source.Length; i++)
			{
				b = source[i];
				if (b != 32 && b != 9)
				{
					break;
				}
			}
			if (i == source.Length)
			{
				bytesConsumed = 0;
				return false;
			}
			if (b == 45)
			{
				IsNegative = true;
				i++;
				if (i == source.Length)
				{
					bytesConsumed = 0;
					return false;
				}
			}
			if (!TryParseUInt32D(source.Slice(i), out V1, out var bytesConsumed2))
			{
				bytesConsumed = 0;
				return false;
			}
			i += bytesConsumed2;
			ComponentParseResult componentParseResult = ParseComponent(source, periodUsedToSeparateDay, ref i, out V2);
			switch (componentParseResult)
			{
			case ComponentParseResult.ParseFailure:
				bytesConsumed = 0;
				return false;
			case ComponentParseResult.NoMoreData:
				bytesConsumed = i;
				return true;
			default:
				Separators |= (uint)componentParseResult << 24;
				componentParseResult = ParseComponent(source, neverParseAsFraction: false, ref i, out V3);
				switch (componentParseResult)
				{
				case ComponentParseResult.ParseFailure:
					bytesConsumed = 0;
					return false;
				case ComponentParseResult.NoMoreData:
					bytesConsumed = i;
					return true;
				default:
					Separators |= (uint)componentParseResult << 16;
					componentParseResult = ParseComponent(source, neverParseAsFraction: false, ref i, out V4);
					switch (componentParseResult)
					{
					case ComponentParseResult.ParseFailure:
						bytesConsumed = 0;
						return false;
					case ComponentParseResult.NoMoreData:
						bytesConsumed = i;
						return true;
					default:
						Separators |= (uint)componentParseResult << 8;
						componentParseResult = ParseComponent(source, neverParseAsFraction: false, ref i, out V5);
						switch (componentParseResult)
						{
						case ComponentParseResult.ParseFailure:
							bytesConsumed = 0;
							return false;
						case ComponentParseResult.NoMoreData:
							bytesConsumed = i;
							return true;
						default:
							Separators |= (uint)componentParseResult;
							if (i != source.Length && (source[i] == 46 || source[i] == 58))
							{
								bytesConsumed = 0;
								return false;
							}
							bytesConsumed = i;
							return true;
						}
					}
				}
			}
		}

		private static ComponentParseResult ParseComponent(ReadOnlySpan<byte> source, bool neverParseAsFraction, ref int srcIndex, out uint value)
		{
			if (srcIndex == source.Length)
			{
				value = 0u;
				return ComponentParseResult.NoMoreData;
			}
			byte b = source[srcIndex];
			if (b == 58 || (b == 46 && neverParseAsFraction))
			{
				srcIndex++;
				if (!TryParseUInt32D(source.Slice(srcIndex), out value, out var bytesConsumed))
				{
					value = 0u;
					return ComponentParseResult.ParseFailure;
				}
				srcIndex += bytesConsumed;
				if (b != 58)
				{
					return ComponentParseResult.Period;
				}
				return ComponentParseResult.Colon;
			}
			if (b == 46)
			{
				srcIndex++;
				if (!TryParseTimeSpanFraction(source.Slice(srcIndex), out value, out var bytesConsumed2))
				{
					value = 0u;
					return ComponentParseResult.ParseFailure;
				}
				srcIndex += bytesConsumed2;
				return ComponentParseResult.Period;
			}
			value = 0u;
			return ComponentParseResult.NoMoreData;
		}
	}

	private static readonly int[] s_daysToMonth365 = new int[13]
	{
		0, 31, 59, 90, 120, 151, 181, 212, 243, 273,
		304, 334, 365
	};

	private static readonly int[] s_daysToMonth366 = new int[13]
	{
		0, 31, 60, 91, 121, 152, 182, 213, 244, 274,
		305, 335, 366
	};

	public static bool TryParse(ReadOnlySpan<byte> source, out bool value, out int bytesConsumed, char standardFormat = '\0')
	{
		if (standardFormat != 0 && standardFormat != 'G' && standardFormat != 'l')
		{
			ThrowHelper.ThrowFormatException_BadFormatSpecifier();
		}
		if (source.Length >= 4)
		{
			int num = BinaryPrimitives.ReadInt32LittleEndian(source) & -538976289;
			if (num == 1163219540)
			{
				bytesConsumed = 4;
				value = true;
				return true;
			}
			if (source.Length > 4 && num == 1397506374 && (source[4] & -33) == 69)
			{
				bytesConsumed = 5;
				value = false;
				return true;
			}
		}
		bytesConsumed = 0;
		value = false;
		return false;
	}

	public static bool TryParse(ReadOnlySpan<byte> source, out DateTime value, out int bytesConsumed, char standardFormat = '\0')
	{
		switch (standardFormat)
		{
		case 'R':
		{
			if (!TryParseDateTimeOffsetR(source, 0u, out var dateTimeOffset, out bytesConsumed))
			{
				value = default(DateTime);
				return false;
			}
			value = dateTimeOffset.DateTime;
			return true;
		}
		case 'l':
		{
			if (!TryParseDateTimeOffsetR(source, 32u, out var dateTimeOffset2, out bytesConsumed))
			{
				value = default(DateTime);
				return false;
			}
			value = dateTimeOffset2.DateTime;
			return true;
		}
		case 'O':
		{
			if (!TryParseDateTimeOffsetO(source, out var value2, out bytesConsumed, out var kind))
			{
				value = default(DateTime);
				bytesConsumed = 0;
				return false;
			}
			switch (kind)
			{
			case DateTimeKind.Local:
				value = value2.LocalDateTime;
				break;
			case DateTimeKind.Utc:
				value = value2.UtcDateTime;
				break;
			default:
				value = value2.DateTime;
				break;
			}
			return true;
		}
		case '\0':
		case 'G':
		{
			DateTimeOffset valueAsOffset;
			return TryParseDateTimeG(source, out value, out valueAsOffset, out bytesConsumed);
		}
		default:
			return ParserHelpers.TryParseThrowFormatException<DateTime>(out value, out bytesConsumed);
		}
	}

	public static bool TryParse(ReadOnlySpan<byte> source, out DateTimeOffset value, out int bytesConsumed, char standardFormat = '\0')
	{
		DateTimeKind kind;
		DateTime value2;
		return standardFormat switch
		{
			'R' => TryParseDateTimeOffsetR(source, 0u, out value, out bytesConsumed), 
			'l' => TryParseDateTimeOffsetR(source, 32u, out value, out bytesConsumed), 
			'O' => TryParseDateTimeOffsetO(source, out value, out bytesConsumed, out kind), 
			'\0' => TryParseDateTimeOffsetDefault(source, out value, out bytesConsumed), 
			'G' => TryParseDateTimeG(source, out value2, out value, out bytesConsumed), 
			_ => ParserHelpers.TryParseThrowFormatException<DateTimeOffset>(out value, out bytesConsumed), 
		};
	}

	private static bool TryParseDateTimeOffsetDefault(ReadOnlySpan<byte> source, out DateTimeOffset value, out int bytesConsumed)
	{
		if (source.Length < 26)
		{
			bytesConsumed = 0;
			value = default(DateTimeOffset);
			return false;
		}
		if (!TryParseDateTimeG(source, out var value2, out var _, out var _))
		{
			bytesConsumed = 0;
			value = default(DateTimeOffset);
			return false;
		}
		if (source[19] != 32)
		{
			bytesConsumed = 0;
			value = default(DateTimeOffset);
			return false;
		}
		byte b = source[20];
		if (b != 43 && b != 45)
		{
			bytesConsumed = 0;
			value = default(DateTimeOffset);
			return false;
		}
		uint num = (uint)(source[21] - 48);
		uint num2 = (uint)(source[22] - 48);
		if (num > 9 || num2 > 9)
		{
			bytesConsumed = 0;
			value = default(DateTimeOffset);
			return false;
		}
		int offsetHours = (int)(num * 10 + num2);
		if (source[23] != 58)
		{
			bytesConsumed = 0;
			value = default(DateTimeOffset);
			return false;
		}
		uint num3 = (uint)(source[24] - 48);
		uint num4 = (uint)(source[25] - 48);
		if (num3 > 9 || num4 > 9)
		{
			bytesConsumed = 0;
			value = default(DateTimeOffset);
			return false;
		}
		int offsetMinutes = (int)(num3 * 10 + num4);
		if (!TryCreateDateTimeOffset(value2, b == 45, offsetHours, offsetMinutes, out value))
		{
			bytesConsumed = 0;
			value = default(DateTimeOffset);
			return false;
		}
		bytesConsumed = 26;
		return true;
	}

	private static bool TryParseDateTimeG(ReadOnlySpan<byte> source, out DateTime value, out DateTimeOffset valueAsOffset, out int bytesConsumed)
	{
		if (source.Length < 19)
		{
			bytesConsumed = 0;
			value = default(DateTime);
			valueAsOffset = default(DateTimeOffset);
			return false;
		}
		uint num = (uint)(source[0] - 48);
		uint num2 = (uint)(source[1] - 48);
		if (num > 9 || num2 > 9)
		{
			bytesConsumed = 0;
			value = default(DateTime);
			valueAsOffset = default(DateTimeOffset);
			return false;
		}
		int month = (int)(num * 10 + num2);
		if (source[2] != 47)
		{
			bytesConsumed = 0;
			value = default(DateTime);
			valueAsOffset = default(DateTimeOffset);
			return false;
		}
		uint num3 = (uint)(source[3] - 48);
		uint num4 = (uint)(source[4] - 48);
		if (num3 > 9 || num4 > 9)
		{
			bytesConsumed = 0;
			value = default(DateTime);
			valueAsOffset = default(DateTimeOffset);
			return false;
		}
		int day = (int)(num3 * 10 + num4);
		if (source[5] != 47)
		{
			bytesConsumed = 0;
			value = default(DateTime);
			valueAsOffset = default(DateTimeOffset);
			return false;
		}
		uint num5 = (uint)(source[6] - 48);
		uint num6 = (uint)(source[7] - 48);
		uint num7 = (uint)(source[8] - 48);
		uint num8 = (uint)(source[9] - 48);
		if (num5 > 9 || num6 > 9 || num7 > 9 || num8 > 9)
		{
			bytesConsumed = 0;
			value = default(DateTime);
			valueAsOffset = default(DateTimeOffset);
			return false;
		}
		int year = (int)(num5 * 1000 + num6 * 100 + num7 * 10 + num8);
		if (source[10] != 32)
		{
			bytesConsumed = 0;
			value = default(DateTime);
			valueAsOffset = default(DateTimeOffset);
			return false;
		}
		uint num9 = (uint)(source[11] - 48);
		uint num10 = (uint)(source[12] - 48);
		if (num9 > 9 || num10 > 9)
		{
			bytesConsumed = 0;
			value = default(DateTime);
			valueAsOffset = default(DateTimeOffset);
			return false;
		}
		int hour = (int)(num9 * 10 + num10);
		if (source[13] != 58)
		{
			bytesConsumed = 0;
			value = default(DateTime);
			valueAsOffset = default(DateTimeOffset);
			return false;
		}
		uint num11 = (uint)(source[14] - 48);
		uint num12 = (uint)(source[15] - 48);
		if (num11 > 9 || num12 > 9)
		{
			bytesConsumed = 0;
			value = default(DateTime);
			valueAsOffset = default(DateTimeOffset);
			return false;
		}
		int minute = (int)(num11 * 10 + num12);
		if (source[16] != 58)
		{
			bytesConsumed = 0;
			value = default(DateTime);
			valueAsOffset = default(DateTimeOffset);
			return false;
		}
		uint num13 = (uint)(source[17] - 48);
		uint num14 = (uint)(source[18] - 48);
		if (num13 > 9 || num14 > 9)
		{
			bytesConsumed = 0;
			value = default(DateTime);
			valueAsOffset = default(DateTimeOffset);
			return false;
		}
		int second = (int)(num13 * 10 + num14);
		if (!TryCreateDateTimeOffsetInterpretingDataAsLocalTime(year, month, day, hour, minute, second, 0, out valueAsOffset))
		{
			bytesConsumed = 0;
			value = default(DateTime);
			valueAsOffset = default(DateTimeOffset);
			return false;
		}
		bytesConsumed = 19;
		value = valueAsOffset.DateTime;
		return true;
	}

	private static bool TryCreateDateTimeOffset(DateTime dateTime, bool offsetNegative, int offsetHours, int offsetMinutes, out DateTimeOffset value)
	{
		if ((uint)offsetHours > 14u)
		{
			value = default(DateTimeOffset);
			return false;
		}
		if ((uint)offsetMinutes > 59u)
		{
			value = default(DateTimeOffset);
			return false;
		}
		if (offsetHours == 14 && offsetMinutes != 0)
		{
			value = default(DateTimeOffset);
			return false;
		}
		long num = ((long)offsetHours * 3600L + (long)offsetMinutes * 60L) * 10000000;
		if (offsetNegative)
		{
			num = -num;
		}
		try
		{
			value = new DateTimeOffset(dateTime.Ticks, new TimeSpan(num));
		}
		catch (ArgumentOutOfRangeException)
		{
			value = default(DateTimeOffset);
			return false;
		}
		return true;
	}

	private static bool TryCreateDateTimeOffset(int year, int month, int day, int hour, int minute, int second, int fraction, bool offsetNegative, int offsetHours, int offsetMinutes, out DateTimeOffset value)
	{
		if (!TryCreateDateTime(year, month, day, hour, minute, second, fraction, DateTimeKind.Unspecified, out var value2))
		{
			value = default(DateTimeOffset);
			return false;
		}
		if (!TryCreateDateTimeOffset(value2, offsetNegative, offsetHours, offsetMinutes, out value))
		{
			value = default(DateTimeOffset);
			return false;
		}
		return true;
	}

	private static bool TryCreateDateTimeOffsetInterpretingDataAsLocalTime(int year, int month, int day, int hour, int minute, int second, int fraction, out DateTimeOffset value)
	{
		if (!TryCreateDateTime(year, month, day, hour, minute, second, fraction, DateTimeKind.Local, out var value2))
		{
			value = default(DateTimeOffset);
			return false;
		}
		try
		{
			value = new DateTimeOffset(value2);
		}
		catch (ArgumentOutOfRangeException)
		{
			value = default(DateTimeOffset);
			return false;
		}
		return true;
	}

	private static bool TryCreateDateTime(int year, int month, int day, int hour, int minute, int second, int fraction, DateTimeKind kind, out DateTime value)
	{
		if (year == 0)
		{
			value = default(DateTime);
			return false;
		}
		if ((uint)(month - 1) >= 12u)
		{
			value = default(DateTime);
			return false;
		}
		uint num = (uint)(day - 1);
		if (num >= 28 && num >= DateTime.DaysInMonth(year, month))
		{
			value = default(DateTime);
			return false;
		}
		if ((uint)hour > 23u)
		{
			value = default(DateTime);
			return false;
		}
		if ((uint)minute > 59u)
		{
			value = default(DateTime);
			return false;
		}
		if ((uint)second > 59u)
		{
			value = default(DateTime);
			return false;
		}
		int[] array = (DateTime.IsLeapYear(year) ? s_daysToMonth366 : s_daysToMonth365);
		int num2 = year - 1;
		int num3 = num2 * 365 + num2 / 4 - num2 / 100 + num2 / 400 + array[month - 1] + day - 1;
		long num4 = num3 * 864000000000L;
		int num5 = hour * 3600 + minute * 60 + second;
		num4 += (long)num5 * 10000000L;
		num4 += fraction;
		value = new DateTime(num4, kind);
		return true;
	}

	private static bool TryParseDateTimeOffsetO(ReadOnlySpan<byte> source, out DateTimeOffset value, out int bytesConsumed, out DateTimeKind kind)
	{
		if (source.Length < 27)
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		uint num = (uint)(source[0] - 48);
		uint num2 = (uint)(source[1] - 48);
		uint num3 = (uint)(source[2] - 48);
		uint num4 = (uint)(source[3] - 48);
		if (num > 9 || num2 > 9 || num3 > 9 || num4 > 9)
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		int year = (int)(num * 1000 + num2 * 100 + num3 * 10 + num4);
		if (source[4] != 45)
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		uint num5 = (uint)(source[5] - 48);
		uint num6 = (uint)(source[6] - 48);
		if (num5 > 9 || num6 > 9)
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		int month = (int)(num5 * 10 + num6);
		if (source[7] != 45)
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		uint num7 = (uint)(source[8] - 48);
		uint num8 = (uint)(source[9] - 48);
		if (num7 > 9 || num8 > 9)
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		int day = (int)(num7 * 10 + num8);
		if (source[10] != 84)
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		uint num9 = (uint)(source[11] - 48);
		uint num10 = (uint)(source[12] - 48);
		if (num9 > 9 || num10 > 9)
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		int hour = (int)(num9 * 10 + num10);
		if (source[13] != 58)
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		uint num11 = (uint)(source[14] - 48);
		uint num12 = (uint)(source[15] - 48);
		if (num11 > 9 || num12 > 9)
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		int minute = (int)(num11 * 10 + num12);
		if (source[16] != 58)
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		uint num13 = (uint)(source[17] - 48);
		uint num14 = (uint)(source[18] - 48);
		if (num13 > 9 || num14 > 9)
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		int second = (int)(num13 * 10 + num14);
		if (source[19] != 46)
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		uint num15 = (uint)(source[20] - 48);
		uint num16 = (uint)(source[21] - 48);
		uint num17 = (uint)(source[22] - 48);
		uint num18 = (uint)(source[23] - 48);
		uint num19 = (uint)(source[24] - 48);
		uint num20 = (uint)(source[25] - 48);
		uint num21 = (uint)(source[26] - 48);
		if (num15 > 9 || num16 > 9 || num17 > 9 || num18 > 9 || num19 > 9 || num20 > 9 || num21 > 9)
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		int fraction = (int)(num15 * 1000000 + num16 * 100000 + num17 * 10000 + num18 * 1000 + num19 * 100 + num20 * 10 + num21);
		byte b = (byte)((source.Length > 27) ? source[27] : 0);
		if (b != 90 && b != 43 && b != 45)
		{
			if (!TryCreateDateTimeOffsetInterpretingDataAsLocalTime(year, month, day, hour, minute, second, fraction, out value))
			{
				value = default(DateTimeOffset);
				bytesConsumed = 0;
				kind = DateTimeKind.Unspecified;
				return false;
			}
			bytesConsumed = 27;
			kind = DateTimeKind.Unspecified;
			return true;
		}
		if (b == 90)
		{
			if (!TryCreateDateTimeOffset(year, month, day, hour, minute, second, fraction, offsetNegative: false, 0, 0, out value))
			{
				value = default(DateTimeOffset);
				bytesConsumed = 0;
				kind = DateTimeKind.Unspecified;
				return false;
			}
			bytesConsumed = 28;
			kind = DateTimeKind.Utc;
			return true;
		}
		if (source.Length < 33)
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		uint num22 = (uint)(source[28] - 48);
		uint num23 = (uint)(source[29] - 48);
		if (num22 > 9 || num23 > 9)
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		int offsetHours = (int)(num22 * 10 + num23);
		if (source[30] != 58)
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		uint num24 = (uint)(source[31] - 48);
		uint num25 = (uint)(source[32] - 48);
		if (num24 > 9 || num25 > 9)
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		int offsetMinutes = (int)(num24 * 10 + num25);
		if (!TryCreateDateTimeOffset(year, month, day, hour, minute, second, fraction, b == 45, offsetHours, offsetMinutes, out value))
		{
			value = default(DateTimeOffset);
			bytesConsumed = 0;
			kind = DateTimeKind.Unspecified;
			return false;
		}
		bytesConsumed = 33;
		kind = DateTimeKind.Local;
		return true;
	}

	private static bool TryParseDateTimeOffsetR(ReadOnlySpan<byte> source, uint caseFlipXorMask, out DateTimeOffset dateTimeOffset, out int bytesConsumed)
	{
		if (source.Length < 29)
		{
			bytesConsumed = 0;
			dateTimeOffset = default(DateTimeOffset);
			return false;
		}
		uint num = source[0] ^ caseFlipXorMask;
		uint num2 = source[1];
		uint num3 = source[2];
		uint num4 = source[3];
		DayOfWeek dayOfWeek;
		switch ((num << 24) | (num2 << 16) | (num3 << 8) | num4)
		{
		case 1400204844u:
			dayOfWeek = DayOfWeek.Sunday;
			break;
		case 1299148332u:
			dayOfWeek = DayOfWeek.Monday;
			break;
		case 1416979756u:
			dayOfWeek = DayOfWeek.Tuesday;
			break;
		case 1466262572u:
			dayOfWeek = DayOfWeek.Wednesday;
			break;
		case 1416131884u:
			dayOfWeek = DayOfWeek.Thursday;
			break;
		case 1181903148u:
			dayOfWeek = DayOfWeek.Friday;
			break;
		case 1398895660u:
			dayOfWeek = DayOfWeek.Saturday;
			break;
		default:
			bytesConsumed = 0;
			dateTimeOffset = default(DateTimeOffset);
			return false;
		}
		if (source[4] != 32)
		{
			bytesConsumed = 0;
			dateTimeOffset = default(DateTimeOffset);
			return false;
		}
		uint num5 = (uint)(source[5] - 48);
		uint num6 = (uint)(source[6] - 48);
		if (num5 > 9 || num6 > 9)
		{
			bytesConsumed = 0;
			dateTimeOffset = default(DateTimeOffset);
			return false;
		}
		int day = (int)(num5 * 10 + num6);
		if (source[7] != 32)
		{
			bytesConsumed = 0;
			dateTimeOffset = default(DateTimeOffset);
			return false;
		}
		uint num7 = source[8] ^ caseFlipXorMask;
		uint num8 = source[9];
		uint num9 = source[10];
		uint num10 = source[11];
		int month;
		switch ((num7 << 24) | (num8 << 16) | (num9 << 8) | num10)
		{
		case 1247899168u:
			month = 1;
			break;
		case 1181049376u:
			month = 2;
			break;
		case 1298231840u:
			month = 3;
			break;
		case 1097888288u:
			month = 4;
			break;
		case 1298233632u:
			month = 5;
			break;
		case 1249209888u:
			month = 6;
			break;
		case 1249209376u:
			month = 7;
			break;
		case 1098213152u:
			month = 8;
			break;
		case 1399156768u:
			month = 9;
			break;
		case 1331917856u:
			month = 10;
			break;
		case 1315927584u:
			month = 11;
			break;
		case 1147495200u:
			month = 12;
			break;
		default:
			bytesConsumed = 0;
			dateTimeOffset = default(DateTimeOffset);
			return false;
		}
		uint num11 = (uint)(source[12] - 48);
		uint num12 = (uint)(source[13] - 48);
		uint num13 = (uint)(source[14] - 48);
		uint num14 = (uint)(source[15] - 48);
		if (num11 > 9 || num12 > 9 || num13 > 9 || num14 > 9)
		{
			bytesConsumed = 0;
			dateTimeOffset = default(DateTimeOffset);
			return false;
		}
		int year = (int)(num11 * 1000 + num12 * 100 + num13 * 10 + num14);
		if (source[16] != 32)
		{
			bytesConsumed = 0;
			dateTimeOffset = default(DateTimeOffset);
			return false;
		}
		uint num15 = (uint)(source[17] - 48);
		uint num16 = (uint)(source[18] - 48);
		if (num15 > 9 || num16 > 9)
		{
			bytesConsumed = 0;
			dateTimeOffset = default(DateTimeOffset);
			return false;
		}
		int hour = (int)(num15 * 10 + num16);
		if (source[19] != 58)
		{
			bytesConsumed = 0;
			dateTimeOffset = default(DateTimeOffset);
			return false;
		}
		uint num17 = (uint)(source[20] - 48);
		uint num18 = (uint)(source[21] - 48);
		if (num17 > 9 || num18 > 9)
		{
			bytesConsumed = 0;
			dateTimeOffset = default(DateTimeOffset);
			return false;
		}
		int minute = (int)(num17 * 10 + num18);
		if (source[22] != 58)
		{
			bytesConsumed = 0;
			dateTimeOffset = default(DateTimeOffset);
			return false;
		}
		uint num19 = (uint)(source[23] - 48);
		uint num20 = (uint)(source[24] - 48);
		if (num19 > 9 || num20 > 9)
		{
			bytesConsumed = 0;
			dateTimeOffset = default(DateTimeOffset);
			return false;
		}
		int second = (int)(num19 * 10 + num20);
		uint num21 = source[25];
		uint num22 = source[26] ^ caseFlipXorMask;
		uint num23 = source[27] ^ caseFlipXorMask;
		uint num24 = source[28] ^ caseFlipXorMask;
		uint num25 = (num21 << 24) | (num22 << 16) | (num23 << 8) | num24;
		if (num25 != 541543764)
		{
			bytesConsumed = 0;
			dateTimeOffset = default(DateTimeOffset);
			return false;
		}
		if (!TryCreateDateTimeOffset(year, month, day, hour, minute, second, 0, offsetNegative: false, 0, 0, out dateTimeOffset))
		{
			bytesConsumed = 0;
			dateTimeOffset = default(DateTimeOffset);
			return false;
		}
		if (dayOfWeek != dateTimeOffset.DayOfWeek)
		{
			bytesConsumed = 0;
			dateTimeOffset = default(DateTimeOffset);
			return false;
		}
		bytesConsumed = 29;
		return true;
	}

	public unsafe static bool TryParse(ReadOnlySpan<byte> source, out decimal value, out int bytesConsumed, char standardFormat = '\0')
	{
		ParseNumberOptions options;
		switch (standardFormat)
		{
		case '\0':
		case 'E':
		case 'G':
		case 'e':
		case 'g':
			options = ParseNumberOptions.AllowExponent;
			break;
		case 'F':
		case 'f':
			options = (ParseNumberOptions)0;
			break;
		default:
			return ParserHelpers.TryParseThrowFormatException<decimal>(out value, out bytesConsumed);
		}
		byte* digits = stackalloc byte[31];
		Number.NumberBuffer number = new Number.NumberBuffer(Number.NumberBufferKind.Decimal, digits, 31);
		if (!TryParseNumber(source, ref number, out bytesConsumed, options, out var textUsedExponentNotation))
		{
			value = default(decimal);
			return false;
		}
		if (!textUsedExponentNotation && (standardFormat == 'E' || standardFormat == 'e'))
		{
			value = default(decimal);
			bytesConsumed = 0;
			return false;
		}
		value = default(decimal);
		if (!Number.TryNumberToDecimal(ref number, ref value))
		{
			value = default(decimal);
			bytesConsumed = 0;
			return false;
		}
		return true;
	}

	public unsafe static bool TryParse(ReadOnlySpan<byte> source, out float value, out int bytesConsumed, char standardFormat = '\0')
	{
		byte* digits = stackalloc byte[114];
		Number.NumberBuffer number = new Number.NumberBuffer(Number.NumberBufferKind.FloatingPoint, digits, 114);
		if (TryParseNormalAsFloatingPoint(source, ref number, out bytesConsumed, standardFormat))
		{
			value = Number.NumberToSingle(ref number);
			return true;
		}
		return TryParseAsSpecialFloatingPoint(source, float.PositiveInfinity, float.NegativeInfinity, float.NaN, out value, out bytesConsumed);
	}

	public unsafe static bool TryParse(ReadOnlySpan<byte> source, out double value, out int bytesConsumed, char standardFormat = '\0')
	{
		byte* digits = stackalloc byte[769];
		Number.NumberBuffer number = new Number.NumberBuffer(Number.NumberBufferKind.FloatingPoint, digits, 769);
		if (TryParseNormalAsFloatingPoint(source, ref number, out bytesConsumed, standardFormat))
		{
			value = Number.NumberToDouble(ref number);
			return true;
		}
		return TryParseAsSpecialFloatingPoint(source, double.PositiveInfinity, double.NegativeInfinity, double.NaN, out value, out bytesConsumed);
	}

	private static bool TryParseNormalAsFloatingPoint(ReadOnlySpan<byte> source, ref Number.NumberBuffer number, out int bytesConsumed, char standardFormat)
	{
		ParseNumberOptions options;
		switch (standardFormat)
		{
		case '\0':
		case 'E':
		case 'G':
		case 'e':
		case 'g':
			options = ParseNumberOptions.AllowExponent;
			break;
		case 'F':
		case 'f':
			options = (ParseNumberOptions)0;
			break;
		default:
			return ParserHelpers.TryParseThrowFormatException(out bytesConsumed);
		}
		if (!TryParseNumber(source, ref number, out bytesConsumed, options, out var textUsedExponentNotation))
		{
			return false;
		}
		if (!textUsedExponentNotation && (standardFormat == 'E' || standardFormat == 'e'))
		{
			bytesConsumed = 0;
			return false;
		}
		return true;
	}

	private static bool TryParseAsSpecialFloatingPoint<T>(ReadOnlySpan<byte> source, T positiveInfinity, T negativeInfinity, T nan, out T value, out int bytesConsumed) where T : struct
	{
		int num = 0;
		int num2 = source.Length;
		bool flag = false;
		if (num2 >= 4)
		{
			byte b = source[num];
			if (b != 43)
			{
				if (b != 45)
				{
					goto IL_002e;
				}
				flag = true;
			}
			num++;
			num2--;
		}
		goto IL_002e;
		IL_002e:
		if (num2 >= 3)
		{
			if (((source[num] ^ 0x6E) & -33) == 0 && ((source[num + 1] ^ 0x61) & -33) == 0 && ((source[num + 2] ^ 0x6E) & -33) == 0)
			{
				value = nan;
				bytesConsumed = 3 + num;
				return true;
			}
			if (num2 >= 8)
			{
				int num3 = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(num)) ^ 0x69666E69;
				if ((num3 & -538976289) == 0)
				{
					num3 = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(num + 4)) ^ 0x7974696E;
					if ((num3 & -538976289) == 0)
					{
						value = (flag ? negativeInfinity : positiveInfinity);
						bytesConsumed = 8 + num;
						return true;
					}
				}
			}
		}
		value = default(T);
		bytesConsumed = 0;
		return false;
	}

	public static bool TryParse(ReadOnlySpan<byte> source, out Guid value, out int bytesConsumed, char standardFormat = '\0')
	{
		while (true)
		{
			switch (standardFormat)
			{
			case '\0':
				return TryParseGuidCore(source, out value, out bytesConsumed, 0);
			case 'D':
				break;
			case 'B':
				return TryParseGuidCore(source, out value, out bytesConsumed, 32123);
			case 'P':
				return TryParseGuidCore(source, out value, out bytesConsumed, 10536);
			case 'N':
				return TryParseGuidN(source, out value, out bytesConsumed);
			default:
				return ParserHelpers.TryParseThrowFormatException<Guid>(source, out value, out bytesConsumed);
			}
			standardFormat = '\0';
		}
	}

	private static bool TryParseGuidN(ReadOnlySpan<byte> text, out Guid value, out int bytesConsumed)
	{
		if (text.Length < 32)
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		if (!TryParseUInt32X(text.Slice(0, 8), out var value2, out var bytesConsumed2) || bytesConsumed2 != 8)
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		if (!TryParseUInt16X(text.Slice(8, 4), out var value3, out bytesConsumed2) || bytesConsumed2 != 4)
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		if (!TryParseUInt16X(text.Slice(12, 4), out var value4, out bytesConsumed2) || bytesConsumed2 != 4)
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		if (!TryParseUInt16X(text.Slice(16, 4), out var value5, out bytesConsumed2) || bytesConsumed2 != 4)
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		if (!TryParseUInt64X(text.Slice(20), out var value6, out bytesConsumed2) || bytesConsumed2 != 12)
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		bytesConsumed = 32;
		value = new Guid((int)value2, (short)value3, (short)value4, (byte)(value5 >> 8), (byte)value5, (byte)(value6 >> 40), (byte)(value6 >> 32), (byte)(value6 >> 24), (byte)(value6 >> 16), (byte)(value6 >> 8), (byte)value6);
		return true;
	}

	private static bool TryParseGuidCore(ReadOnlySpan<byte> source, out Guid value, out int bytesConsumed, int ends)
	{
		int num = 36 + ((ends != 0) ? 2 : 0);
		if (source.Length < num)
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		if (ends != 0)
		{
			if (source[0] != (byte)ends)
			{
				value = default(Guid);
				bytesConsumed = 0;
				return false;
			}
			source = source.Slice(1);
			ends >>= 8;
		}
		if (!TryParseUInt32X(source, out var value2, out var bytesConsumed2))
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		if (bytesConsumed2 != 8)
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		if (source[bytesConsumed2] != 45)
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		source = source.Slice(9);
		if (!TryParseUInt16X(source, out var value3, out bytesConsumed2))
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		if (bytesConsumed2 != 4)
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		if (source[bytesConsumed2] != 45)
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		source = source.Slice(5);
		if (!TryParseUInt16X(source, out var value4, out bytesConsumed2))
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		if (bytesConsumed2 != 4)
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		if (source[bytesConsumed2] != 45)
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		source = source.Slice(5);
		if (!TryParseUInt16X(source, out var value5, out bytesConsumed2))
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		if (bytesConsumed2 != 4)
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		if (source[bytesConsumed2] != 45)
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		source = source.Slice(5);
		if (!TryParseUInt64X(source, out var value6, out bytesConsumed2))
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		if (bytesConsumed2 != 12)
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		if (ends != 0 && source[bytesConsumed2] != (byte)ends)
		{
			value = default(Guid);
			bytesConsumed = 0;
			return false;
		}
		bytesConsumed = num;
		value = new Guid((int)value2, (short)value3, (short)value4, (byte)(value5 >> 8), (byte)value5, (byte)(value6 >> 40), (byte)(value6 >> 32), (byte)(value6 >> 24), (byte)(value6 >> 16), (byte)(value6 >> 8), (byte)value6);
		return true;
	}

	[CLSCompliant(false)]
	public static bool TryParse(ReadOnlySpan<byte> source, out sbyte value, out int bytesConsumed, char standardFormat = '\0')
	{
		while (standardFormat != 0)
		{
			switch (standardFormat | 0x20)
			{
			case 100:
			case 103:
				break;
			case 110:
				return TryParseSByteN(source, out value, out bytesConsumed);
			case 120:
				Unsafe.SkipInit<sbyte>(out value);
				return TryParseByteX(source, out Unsafe.As<sbyte, byte>(ref value), out bytesConsumed);
			default:
				return ParserHelpers.TryParseThrowFormatException<sbyte>(source, out value, out bytesConsumed);
			}
			standardFormat = '\0';
		}
		return TryParseSByteD(source, out value, out bytesConsumed);
	}

	public static bool TryParse(ReadOnlySpan<byte> source, out short value, out int bytesConsumed, char standardFormat = '\0')
	{
		while (standardFormat != 0)
		{
			switch (standardFormat | 0x20)
			{
			case 100:
			case 103:
				break;
			case 110:
				return TryParseInt16N(source, out value, out bytesConsumed);
			case 120:
				Unsafe.SkipInit<short>(out value);
				return TryParseUInt16X(source, out Unsafe.As<short, ushort>(ref value), out bytesConsumed);
			default:
				return ParserHelpers.TryParseThrowFormatException<short>(source, out value, out bytesConsumed);
			}
			standardFormat = '\0';
		}
		return TryParseInt16D(source, out value, out bytesConsumed);
	}

	public static bool TryParse(ReadOnlySpan<byte> source, out int value, out int bytesConsumed, char standardFormat = '\0')
	{
		while (standardFormat != 0)
		{
			switch (standardFormat | 0x20)
			{
			case 100:
			case 103:
				break;
			case 110:
				return TryParseInt32N(source, out value, out bytesConsumed);
			case 120:
				Unsafe.SkipInit<int>(out value);
				return TryParseUInt32X(source, out Unsafe.As<int, uint>(ref value), out bytesConsumed);
			default:
				return ParserHelpers.TryParseThrowFormatException<int>(source, out value, out bytesConsumed);
			}
			standardFormat = '\0';
		}
		return TryParseInt32D(source, out value, out bytesConsumed);
	}

	public static bool TryParse(ReadOnlySpan<byte> source, out long value, out int bytesConsumed, char standardFormat = '\0')
	{
		while (standardFormat != 0)
		{
			switch (standardFormat | 0x20)
			{
			case 100:
			case 103:
				break;
			case 110:
				return TryParseInt64N(source, out value, out bytesConsumed);
			case 120:
				Unsafe.SkipInit<long>(out value);
				return TryParseUInt64X(source, out Unsafe.As<long, ulong>(ref value), out bytesConsumed);
			default:
				return ParserHelpers.TryParseThrowFormatException<long>(source, out value, out bytesConsumed);
			}
			standardFormat = '\0';
		}
		return TryParseInt64D(source, out value, out bytesConsumed);
	}

	private static bool TryParseSByteD(ReadOnlySpan<byte> source, out sbyte value, out int bytesConsumed)
	{
		int num;
		int num2;
		int num4;
		int num3;
		if (source.Length >= 1)
		{
			num = 1;
			num2 = 0;
			num3 = source[num2];
			if (num3 == 45)
			{
				num = -1;
				num2++;
				if ((uint)num2 >= (uint)source.Length)
				{
					goto IL_0123;
				}
				num3 = source[num2];
			}
			else if (num3 == 43)
			{
				num2++;
				if ((uint)num2 >= (uint)source.Length)
				{
					goto IL_0123;
				}
				num3 = source[num2];
			}
			num4 = 0;
			if (ParserHelpers.IsDigit(num3))
			{
				if (num3 != 48)
				{
					goto IL_009c;
				}
				while (true)
				{
					num2++;
					if ((uint)num2 >= (uint)source.Length)
					{
						break;
					}
					num3 = source[num2];
					if (num3 == 48)
					{
						continue;
					}
					goto IL_0091;
				}
				goto IL_012b;
			}
		}
		goto IL_0123;
		IL_012b:
		bytesConsumed = num2;
		value = (sbyte)(num4 * num);
		return true;
		IL_009c:
		num4 = num3 - 48;
		num2++;
		if ((uint)num2 < (uint)source.Length)
		{
			num3 = source[num2];
			if (ParserHelpers.IsDigit(num3))
			{
				num2++;
				num4 = 10 * num4 + num3 - 48;
				if ((uint)num2 < (uint)source.Length)
				{
					num3 = source[num2];
					if (ParserHelpers.IsDigit(num3))
					{
						num2++;
						num4 = num4 * 10 + num3 - 48;
						if ((uint)num4 > 127L + (long)((-1 * num + 1) / 2) || ((uint)num2 < (uint)source.Length && ParserHelpers.IsDigit(source[num2])))
						{
							goto IL_0123;
						}
					}
				}
			}
		}
		goto IL_012b;
		IL_0123:
		bytesConsumed = 0;
		value = 0;
		return false;
		IL_0091:
		if (ParserHelpers.IsDigit(num3))
		{
			goto IL_009c;
		}
		goto IL_012b;
	}

	private static bool TryParseInt16D(ReadOnlySpan<byte> source, out short value, out int bytesConsumed)
	{
		int num;
		int num2;
		int num4;
		int num3;
		if (source.Length >= 1)
		{
			num = 1;
			num2 = 0;
			num3 = source[num2];
			if (num3 == 45)
			{
				num = -1;
				num2++;
				if ((uint)num2 >= (uint)source.Length)
				{
					goto IL_0186;
				}
				num3 = source[num2];
			}
			else if (num3 == 43)
			{
				num2++;
				if ((uint)num2 >= (uint)source.Length)
				{
					goto IL_0186;
				}
				num3 = source[num2];
			}
			num4 = 0;
			if (ParserHelpers.IsDigit(num3))
			{
				if (num3 != 48)
				{
					goto IL_009c;
				}
				while (true)
				{
					num2++;
					if ((uint)num2 >= (uint)source.Length)
					{
						break;
					}
					num3 = source[num2];
					if (num3 == 48)
					{
						continue;
					}
					goto IL_0091;
				}
				goto IL_018e;
			}
		}
		goto IL_0186;
		IL_018e:
		bytesConsumed = num2;
		value = (short)(num4 * num);
		return true;
		IL_009c:
		num4 = num3 - 48;
		num2++;
		if ((uint)num2 < (uint)source.Length)
		{
			num3 = source[num2];
			if (ParserHelpers.IsDigit(num3))
			{
				num2++;
				num4 = 10 * num4 + num3 - 48;
				if ((uint)num2 < (uint)source.Length)
				{
					num3 = source[num2];
					if (ParserHelpers.IsDigit(num3))
					{
						num2++;
						num4 = 10 * num4 + num3 - 48;
						if ((uint)num2 < (uint)source.Length)
						{
							num3 = source[num2];
							if (ParserHelpers.IsDigit(num3))
							{
								num2++;
								num4 = 10 * num4 + num3 - 48;
								if ((uint)num2 < (uint)source.Length)
								{
									num3 = source[num2];
									if (ParserHelpers.IsDigit(num3))
									{
										num2++;
										num4 = num4 * 10 + num3 - 48;
										if ((uint)num4 > 32767L + (long)((-1 * num + 1) / 2) || ((uint)num2 < (uint)source.Length && ParserHelpers.IsDigit(source[num2])))
										{
											goto IL_0186;
										}
									}
								}
							}
						}
					}
				}
			}
		}
		goto IL_018e;
		IL_0186:
		bytesConsumed = 0;
		value = 0;
		return false;
		IL_0091:
		if (ParserHelpers.IsDigit(num3))
		{
			goto IL_009c;
		}
		goto IL_018e;
	}

	private static bool TryParseInt32D(ReadOnlySpan<byte> source, out int value, out int bytesConsumed)
	{
		int num;
		int num2;
		int num4;
		int num3;
		if (source.Length >= 1)
		{
			num = 1;
			num2 = 0;
			num3 = source[num2];
			if (num3 == 45)
			{
				num = -1;
				num2++;
				if ((uint)num2 >= (uint)source.Length)
				{
					goto IL_0281;
				}
				num3 = source[num2];
			}
			else if (num3 == 43)
			{
				num2++;
				if ((uint)num2 >= (uint)source.Length)
				{
					goto IL_0281;
				}
				num3 = source[num2];
			}
			num4 = 0;
			if (ParserHelpers.IsDigit(num3))
			{
				if (num3 != 48)
				{
					goto IL_009c;
				}
				while (true)
				{
					num2++;
					if ((uint)num2 >= (uint)source.Length)
					{
						break;
					}
					num3 = source[num2];
					if (num3 == 48)
					{
						continue;
					}
					goto IL_0091;
				}
				goto IL_0289;
			}
		}
		goto IL_0281;
		IL_0289:
		bytesConsumed = num2;
		value = num4 * num;
		return true;
		IL_009c:
		num4 = num3 - 48;
		num2++;
		if ((uint)num2 < (uint)source.Length)
		{
			num3 = source[num2];
			if (ParserHelpers.IsDigit(num3))
			{
				num2++;
				num4 = 10 * num4 + num3 - 48;
				if ((uint)num2 < (uint)source.Length)
				{
					num3 = source[num2];
					if (ParserHelpers.IsDigit(num3))
					{
						num2++;
						num4 = 10 * num4 + num3 - 48;
						if ((uint)num2 < (uint)source.Length)
						{
							num3 = source[num2];
							if (ParserHelpers.IsDigit(num3))
							{
								num2++;
								num4 = 10 * num4 + num3 - 48;
								if ((uint)num2 < (uint)source.Length)
								{
									num3 = source[num2];
									if (ParserHelpers.IsDigit(num3))
									{
										num2++;
										num4 = 10 * num4 + num3 - 48;
										if ((uint)num2 < (uint)source.Length)
										{
											num3 = source[num2];
											if (ParserHelpers.IsDigit(num3))
											{
												num2++;
												num4 = 10 * num4 + num3 - 48;
												if ((uint)num2 < (uint)source.Length)
												{
													num3 = source[num2];
													if (ParserHelpers.IsDigit(num3))
													{
														num2++;
														num4 = 10 * num4 + num3 - 48;
														if ((uint)num2 < (uint)source.Length)
														{
															num3 = source[num2];
															if (ParserHelpers.IsDigit(num3))
															{
																num2++;
																num4 = 10 * num4 + num3 - 48;
																if ((uint)num2 < (uint)source.Length)
																{
																	num3 = source[num2];
																	if (ParserHelpers.IsDigit(num3))
																	{
																		num2++;
																		num4 = 10 * num4 + num3 - 48;
																		if ((uint)num2 < (uint)source.Length)
																		{
																			num3 = source[num2];
																			if (ParserHelpers.IsDigit(num3))
																			{
																				num2++;
																				if (num4 <= 214748364)
																				{
																					num4 = num4 * 10 + num3 - 48;
																					if ((uint)num4 <= 2147483647L + (long)((-1 * num + 1) / 2) && ((uint)num2 >= (uint)source.Length || !ParserHelpers.IsDigit(source[num2])))
																					{
																						goto IL_0289;
																					}
																				}
																				goto IL_0281;
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
		goto IL_0289;
		IL_0281:
		bytesConsumed = 0;
		value = 0;
		return false;
		IL_0091:
		if (ParserHelpers.IsDigit(num3))
		{
			goto IL_009c;
		}
		goto IL_0289;
	}

	private static bool TryParseInt64D(ReadOnlySpan<byte> source, out long value, out int bytesConsumed)
	{
		long num = 0L;
		int num2 = 0;
		while ((uint)num2 < (uint)source.Length)
		{
			nuint num3 = (uint)(source[num2] - 48);
			if ((uint)num3 > 9)
			{
				if (num2 != 0)
				{
					break;
				}
				num2++;
				if ((int)num3 == -3)
				{
					num--;
				}
				else if ((int)num3 != -5)
				{
					break;
				}
				continue;
			}
			ulong num4 = num3;
			int num5 = 19 + num2;
			num2++;
			if (source.Length < num5)
			{
				for (; (uint)num2 < (uint)source.Length; num2++)
				{
					nuint num6 = (uint)(source[num2] - 48);
					if ((uint)num6 > 9)
					{
						break;
					}
					num4 = num4 * 10 + num6;
				}
			}
			else
			{
				while ((uint)num2 < (uint)source.Length)
				{
					nuint num7 = (uint)(source[num2] - 48);
					if ((uint)num7 > 9)
					{
						break;
					}
					num2++;
					if (num4 < 922337203685477580L)
					{
						num4 = num4 * 10 + num7;
						continue;
					}
					if (num4 != 922337203685477580L || (int)num7 + (int)num > 7)
					{
						goto end_IL_0005;
					}
					num4 = 9223372036854775800uL + (ulong)num7;
				}
			}
			bytesConsumed = num2;
			value = (long)(num4 ^ (ulong)num) - num;
			return true;
			continue;
			end_IL_0005:
			break;
		}
		bytesConsumed = 0;
		value = 0L;
		return false;
	}

	private static bool TryParseSByteN(ReadOnlySpan<byte> source, out sbyte value, out int bytesConsumed)
	{
		int num;
		int num2;
		int num4;
		int num3;
		if (source.Length >= 1)
		{
			num = 1;
			num2 = 0;
			num3 = source[num2];
			if (num3 == 45)
			{
				num = -1;
				num2++;
				if ((uint)num2 >= (uint)source.Length)
				{
					goto IL_00f9;
				}
				num3 = source[num2];
			}
			else if (num3 == 43)
			{
				num2++;
				if ((uint)num2 >= (uint)source.Length)
				{
					goto IL_00f9;
				}
				num3 = source[num2];
			}
			if (num3 != 46)
			{
				if (ParserHelpers.IsDigit(num3))
				{
					num4 = num3 - 48;
					while (true)
					{
						num2++;
						if ((uint)num2 >= (uint)source.Length)
						{
							break;
						}
						num3 = source[num2];
						if (num3 == 44)
						{
							continue;
						}
						if (num3 == 46)
						{
							goto IL_00d4;
						}
						if (!ParserHelpers.IsDigit(num3))
						{
							break;
						}
						num4 = num4 * 10 + num3 - 48;
						if (num4 <= 127 + (-1 * num + 1) / 2)
						{
							continue;
						}
						goto IL_00f9;
					}
					goto IL_0101;
				}
			}
			else
			{
				num4 = 0;
				num2++;
				if ((uint)num2 < (uint)source.Length && source[num2] == 48)
				{
					goto IL_00d4;
				}
			}
		}
		goto IL_00f9;
		IL_00f9:
		bytesConsumed = 0;
		value = 0;
		return false;
		IL_00f1:
		if (ParserHelpers.IsDigit(num3))
		{
			goto IL_00f9;
		}
		goto IL_0101;
		IL_00d4:
		while (true)
		{
			num2++;
			if ((uint)num2 >= (uint)source.Length)
			{
				break;
			}
			num3 = source[num2];
			if (num3 == 48)
			{
				continue;
			}
			goto IL_00f1;
		}
		goto IL_0101;
		IL_0101:
		bytesConsumed = num2;
		value = (sbyte)(num4 * num);
		return true;
	}

	private static bool TryParseInt16N(ReadOnlySpan<byte> source, out short value, out int bytesConsumed)
	{
		int num;
		int num2;
		int num4;
		int num3;
		if (source.Length >= 1)
		{
			num = 1;
			num2 = 0;
			num3 = source[num2];
			if (num3 == 45)
			{
				num = -1;
				num2++;
				if ((uint)num2 >= (uint)source.Length)
				{
					goto IL_00ff;
				}
				num3 = source[num2];
			}
			else if (num3 == 43)
			{
				num2++;
				if ((uint)num2 >= (uint)source.Length)
				{
					goto IL_00ff;
				}
				num3 = source[num2];
			}
			if (num3 != 46)
			{
				if (ParserHelpers.IsDigit(num3))
				{
					num4 = num3 - 48;
					while (true)
					{
						num2++;
						if ((uint)num2 >= (uint)source.Length)
						{
							break;
						}
						num3 = source[num2];
						if (num3 == 44)
						{
							continue;
						}
						if (num3 == 46)
						{
							goto IL_00da;
						}
						if (!ParserHelpers.IsDigit(num3))
						{
							break;
						}
						num4 = num4 * 10 + num3 - 48;
						if (num4 <= 32767 + (-1 * num + 1) / 2)
						{
							continue;
						}
						goto IL_00ff;
					}
					goto IL_0107;
				}
			}
			else
			{
				num4 = 0;
				num2++;
				if ((uint)num2 < (uint)source.Length && source[num2] == 48)
				{
					goto IL_00da;
				}
			}
		}
		goto IL_00ff;
		IL_00ff:
		bytesConsumed = 0;
		value = 0;
		return false;
		IL_00f7:
		if (ParserHelpers.IsDigit(num3))
		{
			goto IL_00ff;
		}
		goto IL_0107;
		IL_00da:
		while (true)
		{
			num2++;
			if ((uint)num2 >= (uint)source.Length)
			{
				break;
			}
			num3 = source[num2];
			if (num3 == 48)
			{
				continue;
			}
			goto IL_00f7;
		}
		goto IL_0107;
		IL_0107:
		bytesConsumed = num2;
		value = (short)(num4 * num);
		return true;
	}

	private static bool TryParseInt32N(ReadOnlySpan<byte> source, out int value, out int bytesConsumed)
	{
		int num;
		int num2;
		int num4;
		int num3;
		if (source.Length >= 1)
		{
			num = 1;
			num2 = 0;
			num3 = source[num2];
			if (num3 == 45)
			{
				num = -1;
				num2++;
				if ((uint)num2 >= (uint)source.Length)
				{
					goto IL_010a;
				}
				num3 = source[num2];
			}
			else if (num3 == 43)
			{
				num2++;
				if ((uint)num2 >= (uint)source.Length)
				{
					goto IL_010a;
				}
				num3 = source[num2];
			}
			if (num3 != 46)
			{
				if (ParserHelpers.IsDigit(num3))
				{
					num4 = num3 - 48;
					while (true)
					{
						num2++;
						if ((uint)num2 >= (uint)source.Length)
						{
							break;
						}
						num3 = source[num2];
						if (num3 == 44)
						{
							continue;
						}
						if (num3 == 46)
						{
							goto IL_00e5;
						}
						if (!ParserHelpers.IsDigit(num3))
						{
							break;
						}
						if ((uint)num4 <= 214748364u)
						{
							num4 = num4 * 10 + num3 - 48;
							if ((uint)num4 <= 2147483647L + (long)((-1 * num + 1) / 2))
							{
								continue;
							}
						}
						goto IL_010a;
					}
					goto IL_0112;
				}
			}
			else
			{
				num4 = 0;
				num2++;
				if ((uint)num2 < (uint)source.Length && source[num2] == 48)
				{
					goto IL_00e5;
				}
			}
		}
		goto IL_010a;
		IL_010a:
		bytesConsumed = 0;
		value = 0;
		return false;
		IL_0102:
		if (ParserHelpers.IsDigit(num3))
		{
			goto IL_010a;
		}
		goto IL_0112;
		IL_00e5:
		while (true)
		{
			num2++;
			if ((uint)num2 >= (uint)source.Length)
			{
				break;
			}
			num3 = source[num2];
			if (num3 == 48)
			{
				continue;
			}
			goto IL_0102;
		}
		goto IL_0112;
		IL_0112:
		bytesConsumed = num2;
		value = num4 * num;
		return true;
	}

	private static bool TryParseInt64N(ReadOnlySpan<byte> source, out long value, out int bytesConsumed)
	{
		int num;
		int num2;
		long num4;
		int num3;
		if (source.Length >= 1)
		{
			num = 1;
			num2 = 0;
			num3 = source[num2];
			if (num3 == 45)
			{
				num = -1;
				num2++;
				if ((uint)num2 >= (uint)source.Length)
				{
					goto IL_0115;
				}
				num3 = source[num2];
			}
			else if (num3 == 43)
			{
				num2++;
				if ((uint)num2 >= (uint)source.Length)
				{
					goto IL_0115;
				}
				num3 = source[num2];
			}
			if (num3 != 46)
			{
				if (ParserHelpers.IsDigit(num3))
				{
					num4 = num3 - 48;
					while (true)
					{
						num2++;
						if ((uint)num2 >= (uint)source.Length)
						{
							break;
						}
						num3 = source[num2];
						if (num3 == 44)
						{
							continue;
						}
						if (num3 == 46)
						{
							goto IL_00f0;
						}
						if (!ParserHelpers.IsDigit(num3))
						{
							break;
						}
						if ((ulong)num4 <= 922337203685477580uL)
						{
							num4 = num4 * 10 + num3 - 48;
							if ((ulong)num4 <= (ulong)(long.MaxValue + (-1 * num + 1) / 2))
							{
								continue;
							}
						}
						goto IL_0115;
					}
					goto IL_011e;
				}
			}
			else
			{
				num4 = 0L;
				num2++;
				if ((uint)num2 < (uint)source.Length && source[num2] == 48)
				{
					goto IL_00f0;
				}
			}
		}
		goto IL_0115;
		IL_0115:
		bytesConsumed = 0;
		value = 0L;
		return false;
		IL_010d:
		if (ParserHelpers.IsDigit(num3))
		{
			goto IL_0115;
		}
		goto IL_011e;
		IL_00f0:
		while (true)
		{
			num2++;
			if ((uint)num2 >= (uint)source.Length)
			{
				break;
			}
			num3 = source[num2];
			if (num3 == 48)
			{
				continue;
			}
			goto IL_010d;
		}
		goto IL_011e;
		IL_011e:
		bytesConsumed = num2;
		value = num4 * num;
		return true;
	}

	public static bool TryParse(ReadOnlySpan<byte> source, out byte value, out int bytesConsumed, char standardFormat = '\0')
	{
		while (standardFormat != 0)
		{
			switch (standardFormat | 0x20)
			{
			case 100:
			case 103:
				break;
			case 110:
				return TryParseByteN(source, out value, out bytesConsumed);
			case 120:
				return TryParseByteX(source, out value, out bytesConsumed);
			default:
				return ParserHelpers.TryParseThrowFormatException<byte>(source, out value, out bytesConsumed);
			}
			standardFormat = '\0';
		}
		return TryParseByteD(source, out value, out bytesConsumed);
	}

	[CLSCompliant(false)]
	public static bool TryParse(ReadOnlySpan<byte> source, out ushort value, out int bytesConsumed, char standardFormat = '\0')
	{
		while (standardFormat != 0)
		{
			switch (standardFormat | 0x20)
			{
			case 100:
			case 103:
				break;
			case 110:
				return TryParseUInt16N(source, out value, out bytesConsumed);
			case 120:
				return TryParseUInt16X(source, out value, out bytesConsumed);
			default:
				return ParserHelpers.TryParseThrowFormatException<ushort>(source, out value, out bytesConsumed);
			}
			standardFormat = '\0';
		}
		return TryParseUInt16D(source, out value, out bytesConsumed);
	}

	[CLSCompliant(false)]
	public static bool TryParse(ReadOnlySpan<byte> source, out uint value, out int bytesConsumed, char standardFormat = '\0')
	{
		while (standardFormat != 0)
		{
			switch (standardFormat | 0x20)
			{
			case 100:
			case 103:
				break;
			case 110:
				return TryParseUInt32N(source, out value, out bytesConsumed);
			case 120:
				return TryParseUInt32X(source, out value, out bytesConsumed);
			default:
				return ParserHelpers.TryParseThrowFormatException<uint>(source, out value, out bytesConsumed);
			}
			standardFormat = '\0';
		}
		return TryParseUInt32D(source, out value, out bytesConsumed);
	}

	[CLSCompliant(false)]
	public static bool TryParse(ReadOnlySpan<byte> source, out ulong value, out int bytesConsumed, char standardFormat = '\0')
	{
		while (standardFormat != 0)
		{
			switch (standardFormat | 0x20)
			{
			case 100:
			case 103:
				break;
			case 110:
				return TryParseUInt64N(source, out value, out bytesConsumed);
			case 120:
				return TryParseUInt64X(source, out value, out bytesConsumed);
			default:
				return ParserHelpers.TryParseThrowFormatException<ulong>(source, out value, out bytesConsumed);
			}
			standardFormat = '\0';
		}
		return TryParseUInt64D(source, out value, out bytesConsumed);
	}

	private static bool TryParseByteD(ReadOnlySpan<byte> source, out byte value, out int bytesConsumed)
	{
		int num;
		int num3;
		int num2;
		if (source.Length >= 1)
		{
			num = 0;
			num2 = source[num];
			num3 = 0;
			if (ParserHelpers.IsDigit(num2))
			{
				if (num2 != 48)
				{
					goto IL_0056;
				}
				while (true)
				{
					num++;
					if ((uint)num >= (uint)source.Length)
					{
						break;
					}
					num2 = source[num];
					if (num2 == 48)
					{
						continue;
					}
					goto IL_004b;
				}
				goto IL_00dd;
			}
		}
		goto IL_00d5;
		IL_004b:
		if (ParserHelpers.IsDigit(num2))
		{
			goto IL_0056;
		}
		goto IL_00dd;
		IL_0056:
		num3 = num2 - 48;
		num++;
		if ((uint)num < (uint)source.Length)
		{
			num2 = source[num];
			if (ParserHelpers.IsDigit(num2))
			{
				num++;
				num3 = 10 * num3 + num2 - 48;
				if ((uint)num < (uint)source.Length)
				{
					num2 = source[num];
					if (ParserHelpers.IsDigit(num2))
					{
						num++;
						num3 = num3 * 10 + num2 - 48;
						if ((uint)num3 > 255u || ((uint)num < (uint)source.Length && ParserHelpers.IsDigit(source[num])))
						{
							goto IL_00d5;
						}
					}
				}
			}
		}
		goto IL_00dd;
		IL_00dd:
		bytesConsumed = num;
		value = (byte)num3;
		return true;
		IL_00d5:
		bytesConsumed = 0;
		value = 0;
		return false;
	}

	private static bool TryParseUInt16D(ReadOnlySpan<byte> source, out ushort value, out int bytesConsumed)
	{
		int num;
		int num3;
		int num2;
		if (source.Length >= 1)
		{
			num = 0;
			num2 = source[num];
			num3 = 0;
			if (ParserHelpers.IsDigit(num2))
			{
				if (num2 != 48)
				{
					goto IL_0056;
				}
				while (true)
				{
					num++;
					if ((uint)num >= (uint)source.Length)
					{
						break;
					}
					num2 = source[num];
					if (num2 == 48)
					{
						continue;
					}
					goto IL_004b;
				}
				goto IL_013d;
			}
		}
		goto IL_0135;
		IL_004b:
		if (ParserHelpers.IsDigit(num2))
		{
			goto IL_0056;
		}
		goto IL_013d;
		IL_0056:
		num3 = num2 - 48;
		num++;
		if ((uint)num < (uint)source.Length)
		{
			num2 = source[num];
			if (ParserHelpers.IsDigit(num2))
			{
				num++;
				num3 = 10 * num3 + num2 - 48;
				if ((uint)num < (uint)source.Length)
				{
					num2 = source[num];
					if (ParserHelpers.IsDigit(num2))
					{
						num++;
						num3 = 10 * num3 + num2 - 48;
						if ((uint)num < (uint)source.Length)
						{
							num2 = source[num];
							if (ParserHelpers.IsDigit(num2))
							{
								num++;
								num3 = 10 * num3 + num2 - 48;
								if ((uint)num < (uint)source.Length)
								{
									num2 = source[num];
									if (ParserHelpers.IsDigit(num2))
									{
										num++;
										num3 = num3 * 10 + num2 - 48;
										if ((uint)num3 > 65535u || ((uint)num < (uint)source.Length && ParserHelpers.IsDigit(source[num])))
										{
											goto IL_0135;
										}
									}
								}
							}
						}
					}
				}
			}
		}
		goto IL_013d;
		IL_013d:
		bytesConsumed = num;
		value = (ushort)num3;
		return true;
		IL_0135:
		bytesConsumed = 0;
		value = 0;
		return false;
	}

	private static bool TryParseUInt32D(ReadOnlySpan<byte> source, out uint value, out int bytesConsumed)
	{
		int num;
		int num3;
		int num2;
		if (source.Length >= 1)
		{
			num = 0;
			num2 = source[num];
			num3 = 0;
			if (ParserHelpers.IsDigit(num2))
			{
				if (num2 != 48)
				{
					goto IL_0056;
				}
				while (true)
				{
					num++;
					if ((uint)num >= (uint)source.Length)
					{
						break;
					}
					num2 = source[num];
					if (num2 == 48)
					{
						continue;
					}
					goto IL_004b;
				}
				goto IL_023d;
			}
		}
		goto IL_0235;
		IL_004b:
		if (ParserHelpers.IsDigit(num2))
		{
			goto IL_0056;
		}
		goto IL_023d;
		IL_0056:
		num3 = num2 - 48;
		num++;
		if ((uint)num < (uint)source.Length)
		{
			num2 = source[num];
			if (ParserHelpers.IsDigit(num2))
			{
				num++;
				num3 = 10 * num3 + num2 - 48;
				if ((uint)num < (uint)source.Length)
				{
					num2 = source[num];
					if (ParserHelpers.IsDigit(num2))
					{
						num++;
						num3 = 10 * num3 + num2 - 48;
						if ((uint)num < (uint)source.Length)
						{
							num2 = source[num];
							if (ParserHelpers.IsDigit(num2))
							{
								num++;
								num3 = 10 * num3 + num2 - 48;
								if ((uint)num < (uint)source.Length)
								{
									num2 = source[num];
									if (ParserHelpers.IsDigit(num2))
									{
										num++;
										num3 = 10 * num3 + num2 - 48;
										if ((uint)num < (uint)source.Length)
										{
											num2 = source[num];
											if (ParserHelpers.IsDigit(num2))
											{
												num++;
												num3 = 10 * num3 + num2 - 48;
												if ((uint)num < (uint)source.Length)
												{
													num2 = source[num];
													if (ParserHelpers.IsDigit(num2))
													{
														num++;
														num3 = 10 * num3 + num2 - 48;
														if ((uint)num < (uint)source.Length)
														{
															num2 = source[num];
															if (ParserHelpers.IsDigit(num2))
															{
																num++;
																num3 = 10 * num3 + num2 - 48;
																if ((uint)num < (uint)source.Length)
																{
																	num2 = source[num];
																	if (ParserHelpers.IsDigit(num2))
																	{
																		num++;
																		num3 = 10 * num3 + num2 - 48;
																		if ((uint)num < (uint)source.Length)
																		{
																			num2 = source[num];
																			if (ParserHelpers.IsDigit(num2))
																			{
																				num++;
																				if ((uint)num3 <= 429496729u && (num3 != 429496729 || num2 <= 53))
																				{
																					num3 = num3 * 10 + num2 - 48;
																					if ((uint)num >= (uint)source.Length || !ParserHelpers.IsDigit(source[num]))
																					{
																						goto IL_023d;
																					}
																				}
																				goto IL_0235;
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
		goto IL_023d;
		IL_023d:
		bytesConsumed = num;
		value = (uint)num3;
		return true;
		IL_0235:
		bytesConsumed = 0;
		value = 0u;
		return false;
	}

	private static bool TryParseUInt64D(ReadOnlySpan<byte> source, out ulong value, out int bytesConsumed)
	{
		if (!source.IsEmpty)
		{
			nuint num = (uint)(source[0] - 48);
			if ((uint)num <= 9)
			{
				ulong num2 = num;
				int i = 1;
				if (source.Length < 20)
				{
					for (; (uint)i < (uint)source.Length; i++)
					{
						nuint num3 = (uint)(source[i] - 48);
						if ((uint)num3 > 9)
						{
							break;
						}
						num2 = num2 * 10 + num3;
					}
				}
				else
				{
					while ((uint)i < (uint)source.Length)
					{
						nuint num4 = (uint)(source[i] - 48);
						if ((uint)num4 > 9)
						{
							break;
						}
						i++;
						if (num2 < 1844674407370955161L)
						{
							num2 = num2 * 10 + num4;
							continue;
						}
						if (num2 == 1844674407370955161L && (uint)num4 <= 5)
						{
							num2 = 18446744073709551610uL + (ulong)num4;
							continue;
						}
						goto IL_00c0;
					}
				}
				bytesConsumed = i;
				value = num2;
				return true;
			}
		}
		goto IL_00c0;
		IL_00c0:
		bytesConsumed = 0;
		value = 0uL;
		return false;
	}

	private static bool TryParseByteN(ReadOnlySpan<byte> source, out byte value, out int bytesConsumed)
	{
		int num;
		int num3;
		int num2;
		if (source.Length >= 1)
		{
			num = 0;
			num2 = source[num];
			if (num2 == 43)
			{
				num++;
				if ((uint)num >= (uint)source.Length)
				{
					goto IL_00ce;
				}
				num2 = source[num];
			}
			if (num2 != 46)
			{
				if (ParserHelpers.IsDigit(num2))
				{
					num3 = num2 - 48;
					while (true)
					{
						num++;
						if ((uint)num >= (uint)source.Length)
						{
							break;
						}
						num2 = source[num];
						if (num2 == 44)
						{
							continue;
						}
						if (num2 == 46)
						{
							goto IL_00a9;
						}
						if (!ParserHelpers.IsDigit(num2))
						{
							break;
						}
						num3 = num3 * 10 + num2 - 48;
						if (num3 <= 255)
						{
							continue;
						}
						goto IL_00ce;
					}
					goto IL_00d6;
				}
			}
			else
			{
				num3 = 0;
				num++;
				if ((uint)num < (uint)source.Length && source[num] == 48)
				{
					goto IL_00a9;
				}
			}
		}
		goto IL_00ce;
		IL_00c6:
		if (ParserHelpers.IsDigit(num2))
		{
			goto IL_00ce;
		}
		goto IL_00d6;
		IL_00a9:
		while (true)
		{
			num++;
			if ((uint)num >= (uint)source.Length)
			{
				break;
			}
			num2 = source[num];
			if (num2 == 48)
			{
				continue;
			}
			goto IL_00c6;
		}
		goto IL_00d6;
		IL_00d6:
		bytesConsumed = num;
		value = (byte)num3;
		return true;
		IL_00ce:
		bytesConsumed = 0;
		value = 0;
		return false;
	}

	private static bool TryParseUInt16N(ReadOnlySpan<byte> source, out ushort value, out int bytesConsumed)
	{
		int num;
		int num3;
		int num2;
		if (source.Length >= 1)
		{
			num = 0;
			num2 = source[num];
			if (num2 == 43)
			{
				num++;
				if ((uint)num >= (uint)source.Length)
				{
					goto IL_00ce;
				}
				num2 = source[num];
			}
			if (num2 != 46)
			{
				if (ParserHelpers.IsDigit(num2))
				{
					num3 = num2 - 48;
					while (true)
					{
						num++;
						if ((uint)num >= (uint)source.Length)
						{
							break;
						}
						num2 = source[num];
						if (num2 == 44)
						{
							continue;
						}
						if (num2 == 46)
						{
							goto IL_00a9;
						}
						if (!ParserHelpers.IsDigit(num2))
						{
							break;
						}
						num3 = num3 * 10 + num2 - 48;
						if (num3 <= 65535)
						{
							continue;
						}
						goto IL_00ce;
					}
					goto IL_00d6;
				}
			}
			else
			{
				num3 = 0;
				num++;
				if ((uint)num < (uint)source.Length && source[num] == 48)
				{
					goto IL_00a9;
				}
			}
		}
		goto IL_00ce;
		IL_00c6:
		if (ParserHelpers.IsDigit(num2))
		{
			goto IL_00ce;
		}
		goto IL_00d6;
		IL_00a9:
		while (true)
		{
			num++;
			if ((uint)num >= (uint)source.Length)
			{
				break;
			}
			num2 = source[num];
			if (num2 == 48)
			{
				continue;
			}
			goto IL_00c6;
		}
		goto IL_00d6;
		IL_00d6:
		bytesConsumed = num;
		value = (ushort)num3;
		return true;
		IL_00ce:
		bytesConsumed = 0;
		value = 0;
		return false;
	}

	private static bool TryParseUInt32N(ReadOnlySpan<byte> source, out uint value, out int bytesConsumed)
	{
		int num;
		int num3;
		int num2;
		if (source.Length >= 1)
		{
			num = 0;
			num2 = source[num];
			if (num2 == 43)
			{
				num++;
				if ((uint)num >= (uint)source.Length)
				{
					goto IL_00de;
				}
				num2 = source[num];
			}
			if (num2 != 46)
			{
				if (ParserHelpers.IsDigit(num2))
				{
					num3 = num2 - 48;
					while (true)
					{
						num++;
						if ((uint)num >= (uint)source.Length)
						{
							break;
						}
						num2 = source[num];
						if (num2 == 44)
						{
							continue;
						}
						if (num2 == 46)
						{
							goto IL_00b9;
						}
						if (!ParserHelpers.IsDigit(num2))
						{
							break;
						}
						if ((uint)num3 <= 429496729u && (num3 != 429496729 || num2 <= 53))
						{
							num3 = num3 * 10 + num2 - 48;
							continue;
						}
						goto IL_00de;
					}
					goto IL_00e6;
				}
			}
			else
			{
				num3 = 0;
				num++;
				if ((uint)num < (uint)source.Length && source[num] == 48)
				{
					goto IL_00b9;
				}
			}
		}
		goto IL_00de;
		IL_00de:
		bytesConsumed = 0;
		value = 0u;
		return false;
		IL_00b9:
		while (true)
		{
			num++;
			if ((uint)num >= (uint)source.Length)
			{
				break;
			}
			num2 = source[num];
			if (num2 == 48)
			{
				continue;
			}
			goto IL_00d6;
		}
		goto IL_00e6;
		IL_00e6:
		bytesConsumed = num;
		value = (uint)num3;
		return true;
		IL_00d6:
		if (ParserHelpers.IsDigit(num2))
		{
			goto IL_00de;
		}
		goto IL_00e6;
	}

	private static bool TryParseUInt64N(ReadOnlySpan<byte> source, out ulong value, out int bytesConsumed)
	{
		int num;
		long num3;
		int num2;
		if (source.Length >= 1)
		{
			num = 0;
			num2 = source[num];
			if (num2 == 43)
			{
				num++;
				if ((uint)num >= (uint)source.Length)
				{
					goto IL_00eb;
				}
				num2 = source[num];
			}
			if (num2 != 46)
			{
				if (ParserHelpers.IsDigit(num2))
				{
					num3 = num2 - 48;
					while (true)
					{
						num++;
						if ((uint)num >= (uint)source.Length)
						{
							break;
						}
						num2 = source[num];
						if (num2 == 44)
						{
							continue;
						}
						if (num2 == 46)
						{
							goto IL_00c6;
						}
						if (!ParserHelpers.IsDigit(num2))
						{
							break;
						}
						if ((ulong)num3 <= 1844674407370955161uL && (num3 != 1844674407370955161L || num2 <= 53))
						{
							num3 = num3 * 10 + num2 - 48;
							continue;
						}
						goto IL_00eb;
					}
					goto IL_00f4;
				}
			}
			else
			{
				num3 = 0L;
				num++;
				if ((uint)num < (uint)source.Length && source[num] == 48)
				{
					goto IL_00c6;
				}
			}
		}
		goto IL_00eb;
		IL_00eb:
		bytesConsumed = 0;
		value = 0uL;
		return false;
		IL_00c6:
		while (true)
		{
			num++;
			if ((uint)num >= (uint)source.Length)
			{
				break;
			}
			num2 = source[num];
			if (num2 == 48)
			{
				continue;
			}
			goto IL_00e3;
		}
		goto IL_00f4;
		IL_00f4:
		bytesConsumed = num;
		value = (ulong)num3;
		return true;
		IL_00e3:
		if (ParserHelpers.IsDigit(num2))
		{
			goto IL_00eb;
		}
		goto IL_00f4;
	}

	private static bool TryParseByteX(ReadOnlySpan<byte> source, out byte value, out int bytesConsumed)
	{
		if (source.Length < 1)
		{
			bytesConsumed = 0;
			value = 0;
			return false;
		}
		ReadOnlySpan<byte> charToHexLookup = HexConverter.CharToHexLookup;
		byte index = source[0];
		byte b = charToHexLookup[index];
		if (b == byte.MaxValue)
		{
			bytesConsumed = 0;
			value = 0;
			return false;
		}
		uint num = b;
		if (source.Length <= 2)
		{
			for (int i = 1; i < source.Length; i++)
			{
				index = source[i];
				b = charToHexLookup[index];
				if (b == byte.MaxValue)
				{
					bytesConsumed = i;
					value = (byte)num;
					return true;
				}
				num = (num << 4) + b;
			}
		}
		else
		{
			for (int j = 1; j < 2; j++)
			{
				index = source[j];
				b = charToHexLookup[index];
				if (b == byte.MaxValue)
				{
					bytesConsumed = j;
					value = (byte)num;
					return true;
				}
				num = (num << 4) + b;
			}
			for (int k = 2; k < source.Length; k++)
			{
				index = source[k];
				b = charToHexLookup[index];
				if (b == byte.MaxValue)
				{
					bytesConsumed = k;
					value = (byte)num;
					return true;
				}
				if (num > 15)
				{
					bytesConsumed = 0;
					value = 0;
					return false;
				}
				num = (num << 4) + b;
			}
		}
		bytesConsumed = source.Length;
		value = (byte)num;
		return true;
	}

	private static bool TryParseUInt16X(ReadOnlySpan<byte> source, out ushort value, out int bytesConsumed)
	{
		if (source.Length < 1)
		{
			bytesConsumed = 0;
			value = 0;
			return false;
		}
		ReadOnlySpan<byte> charToHexLookup = HexConverter.CharToHexLookup;
		byte index = source[0];
		byte b = charToHexLookup[index];
		if (b == byte.MaxValue)
		{
			bytesConsumed = 0;
			value = 0;
			return false;
		}
		uint num = b;
		if (source.Length <= 4)
		{
			for (int i = 1; i < source.Length; i++)
			{
				index = source[i];
				b = charToHexLookup[index];
				if (b == byte.MaxValue)
				{
					bytesConsumed = i;
					value = (ushort)num;
					return true;
				}
				num = (num << 4) + b;
			}
		}
		else
		{
			for (int j = 1; j < 4; j++)
			{
				index = source[j];
				b = charToHexLookup[index];
				if (b == byte.MaxValue)
				{
					bytesConsumed = j;
					value = (ushort)num;
					return true;
				}
				num = (num << 4) + b;
			}
			for (int k = 4; k < source.Length; k++)
			{
				index = source[k];
				b = charToHexLookup[index];
				if (b == byte.MaxValue)
				{
					bytesConsumed = k;
					value = (ushort)num;
					return true;
				}
				if (num > 4095)
				{
					bytesConsumed = 0;
					value = 0;
					return false;
				}
				num = (num << 4) + b;
			}
		}
		bytesConsumed = source.Length;
		value = (ushort)num;
		return true;
	}

	private static bool TryParseUInt32X(ReadOnlySpan<byte> source, out uint value, out int bytesConsumed)
	{
		if (source.Length < 1)
		{
			bytesConsumed = 0;
			value = 0u;
			return false;
		}
		ReadOnlySpan<byte> charToHexLookup = HexConverter.CharToHexLookup;
		byte index = source[0];
		byte b = charToHexLookup[index];
		if (b == byte.MaxValue)
		{
			bytesConsumed = 0;
			value = 0u;
			return false;
		}
		uint num = b;
		if (source.Length <= 8)
		{
			for (int i = 1; i < source.Length; i++)
			{
				index = source[i];
				b = charToHexLookup[index];
				if (b == byte.MaxValue)
				{
					bytesConsumed = i;
					value = num;
					return true;
				}
				num = (num << 4) + b;
			}
		}
		else
		{
			for (int j = 1; j < 8; j++)
			{
				index = source[j];
				b = charToHexLookup[index];
				if (b == byte.MaxValue)
				{
					bytesConsumed = j;
					value = num;
					return true;
				}
				num = (num << 4) + b;
			}
			for (int k = 8; k < source.Length; k++)
			{
				index = source[k];
				b = charToHexLookup[index];
				if (b == byte.MaxValue)
				{
					bytesConsumed = k;
					value = num;
					return true;
				}
				if (num > 268435455)
				{
					bytesConsumed = 0;
					value = 0u;
					return false;
				}
				num = (num << 4) + b;
			}
		}
		bytesConsumed = source.Length;
		value = num;
		return true;
	}

	private static bool TryParseUInt64X(ReadOnlySpan<byte> source, out ulong value, out int bytesConsumed)
	{
		if (source.Length < 1)
		{
			bytesConsumed = 0;
			value = 0uL;
			return false;
		}
		ReadOnlySpan<byte> charToHexLookup = HexConverter.CharToHexLookup;
		byte index = source[0];
		byte b = charToHexLookup[index];
		if (b == byte.MaxValue)
		{
			bytesConsumed = 0;
			value = 0uL;
			return false;
		}
		ulong num = b;
		if (source.Length <= 16)
		{
			for (int i = 1; i < source.Length; i++)
			{
				index = source[i];
				b = charToHexLookup[index];
				if (b == byte.MaxValue)
				{
					bytesConsumed = i;
					value = num;
					return true;
				}
				num = (num << 4) + b;
			}
		}
		else
		{
			for (int j = 1; j < 16; j++)
			{
				index = source[j];
				b = charToHexLookup[index];
				if (b == byte.MaxValue)
				{
					bytesConsumed = j;
					value = num;
					return true;
				}
				num = (num << 4) + b;
			}
			for (int k = 16; k < source.Length; k++)
			{
				index = source[k];
				b = charToHexLookup[index];
				if (b == byte.MaxValue)
				{
					bytesConsumed = k;
					value = num;
					return true;
				}
				if (num > 1152921504606846975L)
				{
					bytesConsumed = 0;
					value = 0uL;
					return false;
				}
				num = (num << 4) + b;
			}
		}
		bytesConsumed = source.Length;
		value = num;
		return true;
	}

	private static bool TryParseNumber(ReadOnlySpan<byte> source, ref Number.NumberBuffer number, out int bytesConsumed, ParseNumberOptions options, out bool textUsedExponentNotation)
	{
		textUsedExponentNotation = false;
		if (source.Length == 0)
		{
			bytesConsumed = 0;
			return false;
		}
		Span<byte> digits = number.Digits;
		int i = 0;
		int num = 0;
		byte b = source[i];
		if (b != 43)
		{
			if (b != 45)
			{
				goto IL_0055;
			}
			number.IsNegative = true;
		}
		i++;
		if (i == source.Length)
		{
			bytesConsumed = 0;
			return false;
		}
		b = source[i];
		goto IL_0055;
		IL_02a0:
		if ((byte)(b - 48) > 9)
		{
			bytesConsumed = 0;
			return false;
		}
		if (!TryParseUInt32D(source.Slice(i), out var value, out var bytesConsumed2))
		{
			value = uint.MaxValue;
			for (i += 10; i != source.Length; i++)
			{
				b = source[i];
				int num2 = (byte)(b - 48);
				if (num2 > 9)
				{
					break;
				}
			}
		}
		i += bytesConsumed2;
		bool flag;
		if (flag)
		{
			if (number.Scale < int.MinValue + value)
			{
				number.Scale = int.MinValue;
			}
			else
			{
				number.Scale -= (int)value;
			}
		}
		else if (number.Scale > 2147483647L - (long)value)
		{
			number.Scale = int.MaxValue;
		}
		else
		{
			number.Scale += (int)value;
		}
		digits[num] = 0;
		number.DigitsCount = num;
		bytesConsumed = i;
		return true;
		IL_0055:
		int num3 = i;
		int num4 = 0;
		int num5 = digits.Length - 1;
		for (; i != source.Length; i++)
		{
			b = source[i];
			if (b != 48)
			{
				break;
			}
		}
		if (i == source.Length)
		{
			bytesConsumed = i;
			return true;
		}
		int num6 = i;
		int num7 = 0;
		while (i != source.Length)
		{
			b = source[i];
			int num8 = (byte)(b - 48);
			if (num8 > 9)
			{
				break;
			}
			i++;
			num4++;
			if (num4 >= num5)
			{
				num7 |= num8;
			}
		}
		number.HasNonZeroTail = num7 != 0;
		int num9 = i - num3;
		int num10 = i - num6;
		int num11 = Math.Min(num10, num5);
		source.Slice(num6, num11).CopyTo(digits);
		num = num11;
		number.Scale = num10;
		if (i == source.Length)
		{
			digits[num] = 0;
			number.DigitsCount = num;
			bytesConsumed = i;
			return true;
		}
		int num12 = 0;
		if (b == 46)
		{
			i++;
			int num13 = i;
			while (i != source.Length)
			{
				b = source[i];
				int num14 = (byte)(b - 48);
				if (num14 > 9)
				{
					break;
				}
				i++;
				num4++;
				if (num4 >= num5)
				{
					num7 |= num14;
				}
			}
			number.HasNonZeroTail = num7 != 0;
			num12 = i - num13;
			int j = num13;
			if (num == 0)
			{
				for (; j < i && source[j] == 48; j++)
				{
					number.Scale--;
				}
			}
			int num15 = Math.Min(i - j, num5 - num);
			source.Slice(j, num15).CopyTo(digits.Slice(num));
			num += num15;
			if (i == source.Length)
			{
				if (num9 == 0 && num12 == 0)
				{
					bytesConsumed = 0;
					return false;
				}
				digits[num] = 0;
				number.DigitsCount = num;
				bytesConsumed = i;
				return true;
			}
		}
		if (num9 == 0 && num12 == 0)
		{
			bytesConsumed = 0;
			return false;
		}
		if ((b & -33) != 69)
		{
			digits[num] = 0;
			number.DigitsCount = num;
			bytesConsumed = i;
			return true;
		}
		textUsedExponentNotation = true;
		i++;
		if ((options & ParseNumberOptions.AllowExponent) == 0)
		{
			bytesConsumed = 0;
			return false;
		}
		if (i == source.Length)
		{
			bytesConsumed = 0;
			return false;
		}
		flag = false;
		b = source[i];
		if (b != 43)
		{
			if (b != 45)
			{
				goto IL_02a0;
			}
			flag = true;
		}
		i++;
		if (i == source.Length)
		{
			bytesConsumed = 0;
			return false;
		}
		b = source[i];
		goto IL_02a0;
	}

	private static bool TryParseTimeSpanBigG(ReadOnlySpan<byte> source, out TimeSpan value, out int bytesConsumed)
	{
		int i = 0;
		byte b = 0;
		for (; i != source.Length; i++)
		{
			b = source[i];
			if (b != 32 && b != 9)
			{
				break;
			}
		}
		if (i == source.Length)
		{
			value = default(TimeSpan);
			bytesConsumed = 0;
			return false;
		}
		bool isNegative = false;
		if (b == 45)
		{
			isNegative = true;
			i++;
			if (i == source.Length)
			{
				value = default(TimeSpan);
				bytesConsumed = 0;
				return false;
			}
		}
		if (!TryParseUInt32D(source.Slice(i), out var value2, out var bytesConsumed2))
		{
			value = default(TimeSpan);
			bytesConsumed = 0;
			return false;
		}
		i += bytesConsumed2;
		if (i == source.Length || source[i++] != 58)
		{
			value = default(TimeSpan);
			bytesConsumed = 0;
			return false;
		}
		if (!TryParseUInt32D(source.Slice(i), out var value3, out bytesConsumed2))
		{
			value = default(TimeSpan);
			bytesConsumed = 0;
			return false;
		}
		i += bytesConsumed2;
		if (i == source.Length || source[i++] != 58)
		{
			value = default(TimeSpan);
			bytesConsumed = 0;
			return false;
		}
		if (!TryParseUInt32D(source.Slice(i), out var value4, out bytesConsumed2))
		{
			value = default(TimeSpan);
			bytesConsumed = 0;
			return false;
		}
		i += bytesConsumed2;
		if (i == source.Length || source[i++] != 58)
		{
			value = default(TimeSpan);
			bytesConsumed = 0;
			return false;
		}
		if (!TryParseUInt32D(source.Slice(i), out var value5, out bytesConsumed2))
		{
			value = default(TimeSpan);
			bytesConsumed = 0;
			return false;
		}
		i += bytesConsumed2;
		if (i == source.Length || source[i++] != 46)
		{
			value = default(TimeSpan);
			bytesConsumed = 0;
			return false;
		}
		if (!TryParseTimeSpanFraction(source.Slice(i), out var value6, out bytesConsumed2))
		{
			value = default(TimeSpan);
			bytesConsumed = 0;
			return false;
		}
		i += bytesConsumed2;
		if (!TryCreateTimeSpan(isNegative, value2, value3, value4, value5, value6, out value))
		{
			value = default(TimeSpan);
			bytesConsumed = 0;
			return false;
		}
		if (i != source.Length && (source[i] == 46 || source[i] == 58))
		{
			value = default(TimeSpan);
			bytesConsumed = 0;
			return false;
		}
		bytesConsumed = i;
		return true;
	}

	private static bool TryParseTimeSpanC(ReadOnlySpan<byte> source, out TimeSpan value, out int bytesConsumed)
	{
		TimeSpanSplitter timeSpanSplitter = default(TimeSpanSplitter);
		if (!timeSpanSplitter.TrySplitTimeSpan(source, periodUsedToSeparateDay: true, out bytesConsumed))
		{
			value = default(TimeSpan);
			return false;
		}
		bool isNegative = timeSpanSplitter.IsNegative;
		bool flag;
		switch (timeSpanSplitter.Separators)
		{
		case 0u:
			flag = TryCreateTimeSpan(isNegative, timeSpanSplitter.V1, 0u, 0u, 0u, 0u, out value);
			break;
		case 16777216u:
			flag = TryCreateTimeSpan(isNegative, 0u, timeSpanSplitter.V1, timeSpanSplitter.V2, 0u, 0u, out value);
			break;
		case 33619968u:
			flag = TryCreateTimeSpan(isNegative, timeSpanSplitter.V1, timeSpanSplitter.V2, timeSpanSplitter.V3, 0u, 0u, out value);
			break;
		case 16842752u:
			flag = TryCreateTimeSpan(isNegative, 0u, timeSpanSplitter.V1, timeSpanSplitter.V2, timeSpanSplitter.V3, 0u, out value);
			break;
		case 33620224u:
			flag = TryCreateTimeSpan(isNegative, timeSpanSplitter.V1, timeSpanSplitter.V2, timeSpanSplitter.V3, timeSpanSplitter.V4, 0u, out value);
			break;
		case 16843264u:
			flag = TryCreateTimeSpan(isNegative, 0u, timeSpanSplitter.V1, timeSpanSplitter.V2, timeSpanSplitter.V3, timeSpanSplitter.V4, out value);
			break;
		case 33620226u:
			flag = TryCreateTimeSpan(isNegative, timeSpanSplitter.V1, timeSpanSplitter.V2, timeSpanSplitter.V3, timeSpanSplitter.V4, timeSpanSplitter.V5, out value);
			break;
		default:
			value = default(TimeSpan);
			flag = false;
			break;
		}
		if (!flag)
		{
			bytesConsumed = 0;
			return false;
		}
		return true;
	}

	public static bool TryParse(ReadOnlySpan<byte> source, out TimeSpan value, out int bytesConsumed, char standardFormat = '\0')
	{
		switch (standardFormat)
		{
		case '\0':
		case 'T':
		case 'c':
		case 't':
			return TryParseTimeSpanC(source, out value, out bytesConsumed);
		case 'G':
			return TryParseTimeSpanBigG(source, out value, out bytesConsumed);
		case 'g':
			return TryParseTimeSpanLittleG(source, out value, out bytesConsumed);
		default:
			return ParserHelpers.TryParseThrowFormatException<TimeSpan>(out value, out bytesConsumed);
		}
	}

	private static bool TryParseTimeSpanFraction(ReadOnlySpan<byte> source, out uint value, out int bytesConsumed)
	{
		int num = 0;
		if (num == source.Length)
		{
			value = 0u;
			bytesConsumed = 0;
			return false;
		}
		uint num2 = (uint)(source[num] - 48);
		if (num2 > 9)
		{
			value = 0u;
			bytesConsumed = 0;
			return false;
		}
		num++;
		uint num3 = num2;
		int num4 = 1;
		while (num != source.Length)
		{
			num2 = (uint)(source[num] - 48);
			if (num2 > 9)
			{
				break;
			}
			num++;
			num4++;
			if (num4 > 7)
			{
				value = 0u;
				bytesConsumed = 0;
				return false;
			}
			num3 = 10 * num3 + num2;
		}
		switch (num4)
		{
		case 6:
			num3 *= 10;
			break;
		case 5:
			num3 *= 100;
			break;
		case 4:
			num3 *= 1000;
			break;
		case 3:
			num3 *= 10000;
			break;
		case 2:
			num3 *= 100000;
			break;
		default:
			num3 *= 1000000;
			break;
		case 7:
			break;
		}
		value = num3;
		bytesConsumed = num;
		return true;
	}

	private static bool TryCreateTimeSpan(bool isNegative, uint days, uint hours, uint minutes, uint seconds, uint fraction, out TimeSpan timeSpan)
	{
		if (hours > 23 || minutes > 59 || seconds > 59)
		{
			timeSpan = default(TimeSpan);
			return false;
		}
		long num = ((long)days * 3600L * 24 + (long)hours * 3600L + (long)minutes * 60L + seconds) * 1000;
		long ticks;
		if (isNegative)
		{
			num = -num;
			if (num < -922337203685477L)
			{
				timeSpan = default(TimeSpan);
				return false;
			}
			long num2 = num * 10000;
			if (num2 < long.MinValue + fraction)
			{
				timeSpan = default(TimeSpan);
				return false;
			}
			ticks = num2 - fraction;
		}
		else
		{
			if (num > 922337203685477L)
			{
				timeSpan = default(TimeSpan);
				return false;
			}
			long num3 = num * 10000;
			if (num3 > long.MaxValue - (long)fraction)
			{
				timeSpan = default(TimeSpan);
				return false;
			}
			ticks = num3 + fraction;
		}
		timeSpan = new TimeSpan(ticks);
		return true;
	}

	private static bool TryParseTimeSpanLittleG(ReadOnlySpan<byte> source, out TimeSpan value, out int bytesConsumed)
	{
		TimeSpanSplitter timeSpanSplitter = default(TimeSpanSplitter);
		if (!timeSpanSplitter.TrySplitTimeSpan(source, periodUsedToSeparateDay: false, out bytesConsumed))
		{
			value = default(TimeSpan);
			return false;
		}
		bool isNegative = timeSpanSplitter.IsNegative;
		bool flag;
		switch (timeSpanSplitter.Separators)
		{
		case 0u:
			flag = TryCreateTimeSpan(isNegative, timeSpanSplitter.V1, 0u, 0u, 0u, 0u, out value);
			break;
		case 16777216u:
			flag = TryCreateTimeSpan(isNegative, 0u, timeSpanSplitter.V1, timeSpanSplitter.V2, 0u, 0u, out value);
			break;
		case 16842752u:
			flag = TryCreateTimeSpan(isNegative, 0u, timeSpanSplitter.V1, timeSpanSplitter.V2, timeSpanSplitter.V3, 0u, out value);
			break;
		case 16843008u:
			flag = TryCreateTimeSpan(isNegative, timeSpanSplitter.V1, timeSpanSplitter.V2, timeSpanSplitter.V3, timeSpanSplitter.V4, 0u, out value);
			break;
		case 16843264u:
			flag = TryCreateTimeSpan(isNegative, 0u, timeSpanSplitter.V1, timeSpanSplitter.V2, timeSpanSplitter.V3, timeSpanSplitter.V4, out value);
			break;
		case 16843010u:
			flag = TryCreateTimeSpan(isNegative, timeSpanSplitter.V1, timeSpanSplitter.V2, timeSpanSplitter.V3, timeSpanSplitter.V4, timeSpanSplitter.V5, out value);
			break;
		default:
			value = default(TimeSpan);
			flag = false;
			break;
		}
		if (!flag)
		{
			bytesConsumed = 0;
			return false;
		}
		return true;
	}
}
