using System.Globalization;
using System.Text;

namespace System.Xml;

internal abstract class BinXmlDateTime
{
	internal static int[] KatmaiTimeScaleMultiplicator = new int[8] { 10000000, 1000000, 100000, 10000, 1000, 100, 10, 1 };

	private static void Write2Dig(StringBuilder sb, int val)
	{
		sb.Append((char)(48 + val / 10));
		sb.Append((char)(48 + val % 10));
	}

	private static void Write4DigNeg(StringBuilder sb, int val)
	{
		if (val < 0)
		{
			val = -val;
			sb.Append('-');
		}
		Write2Dig(sb, val / 100);
		Write2Dig(sb, val % 100);
	}

	private static void Write3Dec(StringBuilder sb, int val)
	{
		int num = val % 10;
		val /= 10;
		int num2 = val % 10;
		val /= 10;
		int num3 = val;
		sb.Append('.');
		sb.Append((char)(48 + num3));
		sb.Append((char)(48 + num2));
		sb.Append((char)(48 + num));
	}

	private static void WriteDate(StringBuilder sb, int yr, int mnth, int day)
	{
		Write4DigNeg(sb, yr);
		sb.Append('-');
		Write2Dig(sb, mnth);
		sb.Append('-');
		Write2Dig(sb, day);
	}

	private static void WriteTime(StringBuilder sb, int hr, int min, int sec, int ms)
	{
		Write2Dig(sb, hr);
		sb.Append(':');
		Write2Dig(sb, min);
		sb.Append(':');
		Write2Dig(sb, sec);
		if (ms != 0)
		{
			Write3Dec(sb, ms);
		}
	}

	private static void WriteTimeFullPrecision(StringBuilder sb, int hr, int min, int sec, int fraction)
	{
		Write2Dig(sb, hr);
		sb.Append(':');
		Write2Dig(sb, min);
		sb.Append(':');
		Write2Dig(sb, sec);
		if (fraction != 0)
		{
			int num = 7;
			while (fraction % 10 == 0)
			{
				num--;
				fraction /= 10;
			}
			char[] array = new char[num];
			while (num > 0)
			{
				num--;
				array[num] = (char)(fraction % 10 + 48);
				fraction /= 10;
			}
			sb.Append('.');
			sb.Append(array);
		}
	}

	private static void WriteTimeZone(StringBuilder sb, TimeSpan zone)
	{
		bool negTimeZone = true;
		if (zone.Ticks < 0)
		{
			negTimeZone = false;
			zone = zone.Negate();
		}
		WriteTimeZone(sb, negTimeZone, zone.Hours, zone.Minutes);
	}

	private static void WriteTimeZone(StringBuilder sb, bool negTimeZone, int hr, int min)
	{
		if (hr == 0 && min == 0)
		{
			sb.Append('Z');
			return;
		}
		sb.Append(negTimeZone ? '+' : '-');
		Write2Dig(sb, hr);
		sb.Append(':');
		Write2Dig(sb, min);
	}

	private static void BreakDownXsdDateTime(long val, out int yr, out int mnth, out int day, out int hr, out int min, out int sec, out int ms)
	{
		if (val >= 0)
		{
			long num = val / 4;
			ms = (int)(num % 1000);
			num /= 1000;
			sec = (int)(num % 60);
			num /= 60;
			min = (int)(num % 60);
			num /= 60;
			hr = (int)(num % 24);
			num /= 24;
			day = (int)(num % 31) + 1;
			num /= 31;
			mnth = (int)(num % 12) + 1;
			num /= 12;
			yr = (int)(num - 9999);
			if (yr >= -9999 && yr <= 9999)
			{
				return;
			}
		}
		throw new XmlException(System.SR.SqlTypes_ArithOverflow, (string)null);
	}

	private static void BreakDownXsdDate(long val, out int yr, out int mnth, out int day, out bool negTimeZone, out int hr, out int min)
	{
		if (val >= 0)
		{
			val /= 4;
			int num = (int)(val % 1740) - 840;
			long num2 = val / 1740;
			if (negTimeZone = num < 0)
			{
				num = -num;
			}
			min = num % 60;
			hr = num / 60;
			day = (int)(num2 % 31) + 1;
			num2 /= 31;
			mnth = (int)(num2 % 12) + 1;
			yr = (int)(num2 / 12) - 9999;
			if (yr >= -9999 && yr <= 9999)
			{
				return;
			}
		}
		throw new XmlException(System.SR.SqlTypes_ArithOverflow, (string)null);
	}

	private static void BreakDownXsdTime(long val, out int hr, out int min, out int sec, out int ms)
	{
		if (val >= 0)
		{
			val /= 4;
			ms = (int)(val % 1000);
			val /= 1000;
			sec = (int)(val % 60);
			val /= 60;
			min = (int)(val % 60);
			hr = (int)(val / 60);
			if (0 <= hr && hr <= 23)
			{
				return;
			}
		}
		throw new XmlException(System.SR.SqlTypes_ArithOverflow, (string)null);
	}

	public static string XsdDateTimeToString(long val)
	{
		BreakDownXsdDateTime(val, out var yr, out var mnth, out var day, out var hr, out var min, out var sec, out var ms);
		StringBuilder stringBuilder = new StringBuilder(20);
		WriteDate(stringBuilder, yr, mnth, day);
		stringBuilder.Append('T');
		WriteTime(stringBuilder, hr, min, sec, ms);
		stringBuilder.Append('Z');
		return stringBuilder.ToString();
	}

	public static DateTime XsdDateTimeToDateTime(long val)
	{
		BreakDownXsdDateTime(val, out var yr, out var mnth, out var day, out var hr, out var min, out var sec, out var ms);
		return new DateTime(yr, mnth, day, hr, min, sec, ms, DateTimeKind.Utc);
	}

	public static string XsdDateToString(long val)
	{
		BreakDownXsdDate(val, out var yr, out var mnth, out var day, out var negTimeZone, out var hr, out var min);
		StringBuilder stringBuilder = new StringBuilder(20);
		WriteDate(stringBuilder, yr, mnth, day);
		WriteTimeZone(stringBuilder, negTimeZone, hr, min);
		return stringBuilder.ToString();
	}

	public static DateTime XsdDateToDateTime(long val)
	{
		BreakDownXsdDate(val, out var yr, out var mnth, out var day, out var negTimeZone, out var hr, out var min);
		DateTime dateTime = new DateTime(yr, mnth, day, 0, 0, 0, DateTimeKind.Utc);
		int num = ((!negTimeZone) ? 1 : (-1)) * (hr * 60 + min);
		return TimeZoneInfo.ConvertTime(dateTime.AddMinutes(num), TimeZoneInfo.Local);
	}

	public static string XsdTimeToString(long val)
	{
		BreakDownXsdTime(val, out var hr, out var min, out var sec, out var ms);
		StringBuilder stringBuilder = new StringBuilder(16);
		WriteTime(stringBuilder, hr, min, sec, ms);
		stringBuilder.Append('Z');
		return stringBuilder.ToString();
	}

	public static DateTime XsdTimeToDateTime(long val)
	{
		BreakDownXsdTime(val, out var hr, out var min, out var sec, out var ms);
		return new DateTime(1, 1, 1, hr, min, sec, ms, DateTimeKind.Utc);
	}

	public static string SqlDateTimeToString(int dateticks, uint timeticks)
	{
		DateTime dateTime = SqlDateTimeToDateTime(dateticks, timeticks);
		string text = ((dateTime.Millisecond != 0) ? "yyyy/MM/dd\\THH:mm:ss.ffff" : "yyyy/MM/dd\\THH:mm:ss");
		return dateTime.ToString(text, CultureInfo.InvariantCulture);
	}

	public static DateTime SqlDateTimeToDateTime(int dateticks, uint timeticks)
	{
		DateTime dateTime = new DateTime(1900, 1, 1);
		long num = (long)((double)timeticks / 0.3 + 0.5);
		return dateTime.Add(new TimeSpan(dateticks * 864000000000L + num * 10000));
	}

	public static string SqlSmallDateTimeToString(short dateticks, ushort timeticks)
	{
		return SqlSmallDateTimeToDateTime(dateticks, timeticks).ToString("yyyy/MM/dd\\THH:mm:ss", CultureInfo.InvariantCulture);
	}

	public static DateTime SqlSmallDateTimeToDateTime(short dateticks, ushort timeticks)
	{
		return SqlDateTimeToDateTime(dateticks, (uint)(timeticks * 18000));
	}

	public static DateTime XsdKatmaiDateToDateTime(byte[] data, int offset)
	{
		long katmaiDateTicks = GetKatmaiDateTicks(data, ref offset);
		return new DateTime(katmaiDateTicks);
	}

	public static DateTime XsdKatmaiDateTimeToDateTime(byte[] data, int offset)
	{
		long katmaiTimeTicks = GetKatmaiTimeTicks(data, ref offset);
		long katmaiDateTicks = GetKatmaiDateTicks(data, ref offset);
		return new DateTime(katmaiDateTicks + katmaiTimeTicks);
	}

	public static DateTime XsdKatmaiTimeToDateTime(byte[] data, int offset)
	{
		return XsdKatmaiDateTimeToDateTime(data, offset);
	}

	public static DateTime XsdKatmaiDateOffsetToDateTime(byte[] data, int offset)
	{
		return XsdKatmaiDateOffsetToDateTimeOffset(data, offset).LocalDateTime;
	}

	public static DateTime XsdKatmaiDateTimeOffsetToDateTime(byte[] data, int offset)
	{
		return XsdKatmaiDateTimeOffsetToDateTimeOffset(data, offset).LocalDateTime;
	}

	public static DateTime XsdKatmaiTimeOffsetToDateTime(byte[] data, int offset)
	{
		return XsdKatmaiTimeOffsetToDateTimeOffset(data, offset).LocalDateTime;
	}

	public static DateTimeOffset XsdKatmaiDateOffsetToDateTimeOffset(byte[] data, int offset)
	{
		return XsdKatmaiDateTimeOffsetToDateTimeOffset(data, offset);
	}

	public static DateTimeOffset XsdKatmaiDateTimeOffsetToDateTimeOffset(byte[] data, int offset)
	{
		long katmaiTimeTicks = GetKatmaiTimeTicks(data, ref offset);
		long katmaiDateTicks = GetKatmaiDateTicks(data, ref offset);
		long katmaiTimeZoneTicks = GetKatmaiTimeZoneTicks(data, offset);
		return new DateTimeOffset(katmaiDateTicks + katmaiTimeTicks + katmaiTimeZoneTicks, new TimeSpan(katmaiTimeZoneTicks));
	}

	public static DateTimeOffset XsdKatmaiTimeOffsetToDateTimeOffset(byte[] data, int offset)
	{
		return XsdKatmaiDateTimeOffsetToDateTimeOffset(data, offset);
	}

	public static string XsdKatmaiDateToString(byte[] data, int offset)
	{
		DateTime dateTime = XsdKatmaiDateToDateTime(data, offset);
		StringBuilder stringBuilder = new StringBuilder(10);
		WriteDate(stringBuilder, dateTime.Year, dateTime.Month, dateTime.Day);
		return stringBuilder.ToString();
	}

	public static string XsdKatmaiDateTimeToString(byte[] data, int offset)
	{
		DateTime dt = XsdKatmaiDateTimeToDateTime(data, offset);
		StringBuilder stringBuilder = new StringBuilder(33);
		WriteDate(stringBuilder, dt.Year, dt.Month, dt.Day);
		stringBuilder.Append('T');
		WriteTimeFullPrecision(stringBuilder, dt.Hour, dt.Minute, dt.Second, GetFractions(dt));
		return stringBuilder.ToString();
	}

	public static string XsdKatmaiTimeToString(byte[] data, int offset)
	{
		DateTime dt = XsdKatmaiTimeToDateTime(data, offset);
		StringBuilder stringBuilder = new StringBuilder(16);
		WriteTimeFullPrecision(stringBuilder, dt.Hour, dt.Minute, dt.Second, GetFractions(dt));
		return stringBuilder.ToString();
	}

	public static string XsdKatmaiDateOffsetToString(byte[] data, int offset)
	{
		DateTimeOffset dateTimeOffset = XsdKatmaiDateOffsetToDateTimeOffset(data, offset);
		StringBuilder stringBuilder = new StringBuilder(16);
		WriteDate(stringBuilder, dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day);
		WriteTimeZone(stringBuilder, dateTimeOffset.Offset);
		return stringBuilder.ToString();
	}

	public static string XsdKatmaiDateTimeOffsetToString(byte[] data, int offset)
	{
		DateTimeOffset dt = XsdKatmaiDateTimeOffsetToDateTimeOffset(data, offset);
		StringBuilder stringBuilder = new StringBuilder(39);
		WriteDate(stringBuilder, dt.Year, dt.Month, dt.Day);
		stringBuilder.Append('T');
		WriteTimeFullPrecision(stringBuilder, dt.Hour, dt.Minute, dt.Second, GetFractions(dt));
		WriteTimeZone(stringBuilder, dt.Offset);
		return stringBuilder.ToString();
	}

	public static string XsdKatmaiTimeOffsetToString(byte[] data, int offset)
	{
		DateTimeOffset dt = XsdKatmaiTimeOffsetToDateTimeOffset(data, offset);
		StringBuilder stringBuilder = new StringBuilder(22);
		WriteTimeFullPrecision(stringBuilder, dt.Hour, dt.Minute, dt.Second, GetFractions(dt));
		WriteTimeZone(stringBuilder, dt.Offset);
		return stringBuilder.ToString();
	}

	private static long GetKatmaiDateTicks(byte[] data, ref int pos)
	{
		int num = pos;
		pos = num + 3;
		return (data[num] | (data[num + 1] << 8) | (data[num + 2] << 16)) * 864000000000L;
	}

	private static long GetKatmaiTimeTicks(byte[] data, ref int pos)
	{
		int num = pos;
		byte b = data[num];
		num++;
		long num2;
		if (b <= 2)
		{
			num2 = data[num] | (data[num + 1] << 8) | (data[num + 2] << 16);
			pos = num + 3;
		}
		else if (b <= 4)
		{
			num2 = data[num] | (data[num + 1] << 8) | (data[num + 2] << 16);
			num2 |= (long)((ulong)data[num + 3] << 24);
			pos = num + 4;
		}
		else
		{
			if (b > 7)
			{
				throw new XmlException(System.SR.SqlTypes_ArithOverflow, (string)null);
			}
			num2 = data[num] | (data[num + 1] << 8) | (data[num + 2] << 16);
			num2 |= (long)(((ulong)data[num + 3] << 24) | ((ulong)data[num + 4] << 32));
			pos = num + 5;
		}
		return num2 * KatmaiTimeScaleMultiplicator[b];
	}

	private static long GetKatmaiTimeZoneTicks(byte[] data, int pos)
	{
		return (long)(short)(data[pos] | (data[pos + 1] << 8)) * 600000000L;
	}

	private static int GetFractions(DateTime dt)
	{
		return (int)(dt.Ticks - new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second).Ticks);
	}

	private static int GetFractions(DateTimeOffset dt)
	{
		return (int)(dt.Ticks - new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second).Ticks);
	}
}
