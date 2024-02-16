using System.Text;

namespace System.Xml.Schema;

internal struct XsdDuration
{
	private enum Parts
	{
		HasNone = 0,
		HasYears = 1,
		HasMonths = 2,
		HasDays = 4,
		HasHours = 8,
		HasMinutes = 0x10,
		HasSeconds = 0x20
	}

	public enum DurationType
	{
		Duration,
		YearMonthDuration,
		DayTimeDuration
	}

	private int _years;

	private int _months;

	private int _days;

	private int _hours;

	private int _minutes;

	private int _seconds;

	private uint _nanoseconds;

	public bool IsNegative => (_nanoseconds & 0x80000000u) != 0;

	public int Years => _years;

	public int Months => _months;

	public int Days => _days;

	public int Hours => _hours;

	public int Minutes => _minutes;

	public int Seconds => _seconds;

	public int Nanoseconds => (int)(_nanoseconds & 0x7FFFFFFF);

	public XsdDuration(bool isNegative, int years, int months, int days, int hours, int minutes, int seconds, int nanoseconds)
	{
		if (years < 0)
		{
			throw new ArgumentOutOfRangeException("years");
		}
		if (months < 0)
		{
			throw new ArgumentOutOfRangeException("months");
		}
		if (days < 0)
		{
			throw new ArgumentOutOfRangeException("days");
		}
		if (hours < 0)
		{
			throw new ArgumentOutOfRangeException("hours");
		}
		if (minutes < 0)
		{
			throw new ArgumentOutOfRangeException("minutes");
		}
		if (seconds < 0)
		{
			throw new ArgumentOutOfRangeException("seconds");
		}
		if (nanoseconds < 0 || nanoseconds > 999999999)
		{
			throw new ArgumentOutOfRangeException("nanoseconds");
		}
		_years = years;
		_months = months;
		_days = days;
		_hours = hours;
		_minutes = minutes;
		_seconds = seconds;
		_nanoseconds = (uint)nanoseconds;
		if (isNegative)
		{
			_nanoseconds |= 2147483648u;
		}
	}

	public XsdDuration(TimeSpan timeSpan)
		: this(timeSpan, DurationType.Duration)
	{
	}

	public XsdDuration(TimeSpan timeSpan, DurationType durationType)
	{
		long ticks = timeSpan.Ticks;
		bool flag;
		ulong num;
		if (ticks < 0)
		{
			flag = true;
			num = (ulong)(-ticks);
		}
		else
		{
			flag = false;
			num = (ulong)ticks;
		}
		if (durationType == DurationType.YearMonthDuration)
		{
			int num2 = (int)(num / 315360000000000L);
			int num3 = (int)(num % 315360000000000L / 25920000000000L);
			if (num3 == 12)
			{
				num2++;
				num3 = 0;
			}
			this = new XsdDuration(flag, num2, num3, 0, 0, 0, 0, 0);
			return;
		}
		_nanoseconds = (uint)((int)(num % 10000000) * 100);
		if (flag)
		{
			_nanoseconds |= 2147483648u;
		}
		_years = 0;
		_months = 0;
		_days = (int)(num / 864000000000L);
		_hours = (int)(num / 36000000000L % 24);
		_minutes = (int)(num / 600000000 % 60);
		_seconds = (int)(num / 10000000 % 60);
	}

	public XsdDuration(string s)
		: this(s, DurationType.Duration)
	{
	}

	public XsdDuration(string s, DurationType durationType)
	{
		XsdDuration result;
		Exception ex = TryParse(s, durationType, out result);
		if (ex != null)
		{
			throw ex;
		}
		_years = result.Years;
		_months = result.Months;
		_days = result.Days;
		_hours = result.Hours;
		_minutes = result.Minutes;
		_seconds = result.Seconds;
		_nanoseconds = (uint)result.Nanoseconds;
		if (result.IsNegative)
		{
			_nanoseconds |= 2147483648u;
		}
	}

	public TimeSpan ToTimeSpan()
	{
		return ToTimeSpan(DurationType.Duration);
	}

	public TimeSpan ToTimeSpan(DurationType durationType)
	{
		TimeSpan result;
		Exception ex = TryToTimeSpan(durationType, out result);
		if (ex != null)
		{
			throw ex;
		}
		return result;
	}

	internal Exception TryToTimeSpan(out TimeSpan result)
	{
		return TryToTimeSpan(DurationType.Duration, out result);
	}

	internal Exception TryToTimeSpan(DurationType durationType, out TimeSpan result)
	{
		Exception ex = null;
		ulong num = 0uL;
		checked
		{
			try
			{
				if (durationType != DurationType.DayTimeDuration)
				{
					num += ((ulong)_years + unchecked(checked((ulong)_months) / 12)) * 365;
					num += unchecked(checked((ulong)_months) % 12) * 30;
				}
				if (durationType != DurationType.YearMonthDuration)
				{
					num += (ulong)_days;
					num *= 24;
					num += (ulong)_hours;
					num *= 60;
					num += (ulong)_minutes;
					num *= 60;
					num += (ulong)_seconds;
					num *= 10000000;
					num += unchecked(checked((ulong)Nanoseconds) / 100);
				}
				else
				{
					num *= 864000000000L;
				}
				if (IsNegative)
				{
					if (num == 9223372036854775808uL)
					{
						result = new TimeSpan(long.MinValue);
					}
					else
					{
						result = new TimeSpan(-(long)num);
					}
				}
				else
				{
					result = new TimeSpan((long)num);
				}
				return null;
			}
			catch (OverflowException)
			{
				result = TimeSpan.MinValue;
				return new OverflowException(System.SR.Format(System.SR.XmlConvert_Overflow, durationType, "TimeSpan"));
			}
		}
	}

	public override string ToString()
	{
		return ToString(DurationType.Duration);
	}

	internal string ToString(DurationType durationType)
	{
		StringBuilder stringBuilder = new StringBuilder(20);
		if (IsNegative)
		{
			stringBuilder.Append('-');
		}
		stringBuilder.Append('P');
		if (durationType != DurationType.DayTimeDuration)
		{
			if (_years != 0)
			{
				stringBuilder.Append(XmlConvert.ToString(_years));
				stringBuilder.Append('Y');
			}
			if (_months != 0)
			{
				stringBuilder.Append(XmlConvert.ToString(_months));
				stringBuilder.Append('M');
			}
		}
		if (durationType != DurationType.YearMonthDuration)
		{
			if (_days != 0)
			{
				stringBuilder.Append(XmlConvert.ToString(_days));
				stringBuilder.Append('D');
			}
			if (_hours != 0 || _minutes != 0 || _seconds != 0 || Nanoseconds != 0)
			{
				stringBuilder.Append('T');
				if (_hours != 0)
				{
					stringBuilder.Append(XmlConvert.ToString(_hours));
					stringBuilder.Append('H');
				}
				if (_minutes != 0)
				{
					stringBuilder.Append(XmlConvert.ToString(_minutes));
					stringBuilder.Append('M');
				}
				int num = Nanoseconds;
				if (_seconds != 0 || num != 0)
				{
					stringBuilder.Append(XmlConvert.ToString(_seconds));
					if (num != 0)
					{
						stringBuilder.Append('.');
						int length = stringBuilder.Length;
						stringBuilder.Length += 9;
						int num2 = stringBuilder.Length - 1;
						for (int num3 = num2; num3 >= length; num3--)
						{
							int num4 = num % 10;
							stringBuilder[num3] = (char)(num4 + 48);
							if (num2 == num3 && num4 == 0)
							{
								num2--;
							}
							num /= 10;
						}
						stringBuilder.Length = num2 + 1;
					}
					stringBuilder.Append('S');
				}
			}
			if (stringBuilder[stringBuilder.Length - 1] == 'P')
			{
				stringBuilder.Append("T0S");
			}
		}
		else if (stringBuilder[stringBuilder.Length - 1] == 'P')
		{
			stringBuilder.Append("0M");
		}
		return stringBuilder.ToString();
	}

	internal static Exception TryParse(string s, out XsdDuration result)
	{
		return TryParse(s, DurationType.Duration, out result);
	}

	internal static Exception TryParse(string s, DurationType durationType, out XsdDuration result)
	{
		Parts parts = Parts.HasNone;
		result = default(XsdDuration);
		s = s.Trim();
		int length = s.Length;
		int offset = 0;
		int i = 0;
		int result2;
		if (offset < length)
		{
			if (s[offset] == '-')
			{
				offset++;
				result._nanoseconds = 2147483648u;
			}
			else
			{
				result._nanoseconds = 0u;
			}
			if (offset < length && s[offset++] == 'P')
			{
				string text = TryParseDigits(s, ref offset, eatDigits: false, out result2, out i);
				if (text != null)
				{
					goto IL_02ec;
				}
				if (offset < length)
				{
					if (s[offset] != 'Y')
					{
						goto IL_00c1;
					}
					if (i != 0)
					{
						parts |= Parts.HasYears;
						result._years = result2;
						if (++offset == length)
						{
							goto IL_02b8;
						}
						text = TryParseDigits(s, ref offset, eatDigits: false, out result2, out i);
						if (text != null)
						{
							goto IL_02ec;
						}
						if (offset < length)
						{
							goto IL_00c1;
						}
					}
				}
			}
		}
		goto IL_02d5;
		IL_02ec:
		return new OverflowException(System.SR.Format(System.SR.XmlConvert_Overflow, s, durationType));
		IL_014b:
		if (s[offset] != 'T')
		{
			goto IL_02b0;
		}
		if (i == 0)
		{
			offset++;
			string text = TryParseDigits(s, ref offset, eatDigits: false, out result2, out i);
			if (text != null)
			{
				goto IL_02ec;
			}
			if (offset < length)
			{
				if (s[offset] != 'H')
				{
					goto IL_01c4;
				}
				if (i != 0)
				{
					parts |= Parts.HasHours;
					result._hours = result2;
					if (++offset == length)
					{
						goto IL_02b8;
					}
					text = TryParseDigits(s, ref offset, eatDigits: false, out result2, out i);
					if (text != null)
					{
						goto IL_02ec;
					}
					if (offset < length)
					{
						goto IL_01c4;
					}
				}
			}
		}
		goto IL_02d5;
		IL_00c1:
		if (s[offset] != 'M')
		{
			goto IL_0106;
		}
		if (i != 0)
		{
			parts |= Parts.HasMonths;
			result._months = result2;
			if (++offset == length)
			{
				goto IL_02b8;
			}
			string text = TryParseDigits(s, ref offset, eatDigits: false, out result2, out i);
			if (text != null)
			{
				goto IL_02ec;
			}
			if (offset < length)
			{
				goto IL_0106;
			}
		}
		goto IL_02d5;
		IL_020a:
		if (s[offset] == '.')
		{
			offset++;
			parts |= Parts.HasSeconds;
			result._seconds = result2;
			string text = TryParseDigits(s, ref offset, eatDigits: true, out result2, out i);
			if (text != null)
			{
				goto IL_02ec;
			}
			if (i == 0)
			{
				result2 = 0;
			}
			while (i > 9)
			{
				result2 /= 10;
				i--;
			}
			for (; i < 9; i++)
			{
				result2 *= 10;
			}
			result._nanoseconds |= (uint)result2;
			if (offset >= length || s[offset] != 'S')
			{
				goto IL_02d5;
			}
			if (++offset == length)
			{
				goto IL_02b8;
			}
		}
		else if (s[offset] == 'S')
		{
			if (i == 0)
			{
				goto IL_02d5;
			}
			parts |= Parts.HasSeconds;
			result._seconds = result2;
			if (++offset == length)
			{
				goto IL_02b8;
			}
		}
		goto IL_02b0;
		IL_02d5:
		return new FormatException(System.SR.Format(System.SR.XmlConvert_BadFormat, s, durationType));
		IL_01c4:
		if (s[offset] != 'M')
		{
			goto IL_020a;
		}
		if (i != 0)
		{
			parts |= Parts.HasMinutes;
			result._minutes = result2;
			if (++offset == length)
			{
				goto IL_02b8;
			}
			string text = TryParseDigits(s, ref offset, eatDigits: false, out result2, out i);
			if (text != null)
			{
				goto IL_02ec;
			}
			if (offset < length)
			{
				goto IL_020a;
			}
		}
		goto IL_02d5;
		IL_02b0:
		if (i == 0 && offset == length)
		{
			goto IL_02b8;
		}
		goto IL_02d5;
		IL_0106:
		if (s[offset] != 'D')
		{
			goto IL_014b;
		}
		if (i != 0)
		{
			parts |= Parts.HasDays;
			result._days = result2;
			if (++offset == length)
			{
				goto IL_02b8;
			}
			string text = TryParseDigits(s, ref offset, eatDigits: false, out result2, out i);
			if (text != null)
			{
				goto IL_02ec;
			}
			if (offset < length)
			{
				goto IL_014b;
			}
		}
		goto IL_02d5;
		IL_02b8:
		if (parts != 0)
		{
			if (durationType == DurationType.DayTimeDuration)
			{
				if ((parts & (Parts)3) == 0)
				{
					goto IL_02d3;
				}
			}
			else if (durationType != DurationType.YearMonthDuration || (parts & (Parts)(-4)) == 0)
			{
				goto IL_02d3;
			}
		}
		goto IL_02d5;
		IL_02d3:
		return null;
	}

	private static string TryParseDigits(string s, ref int offset, bool eatDigits, out int result, out int numDigits)
	{
		int num = offset;
		int length = s.Length;
		result = 0;
		numDigits = 0;
		while (offset < length && s[offset] >= '0' && s[offset] <= '9')
		{
			int num2 = s[offset] - 48;
			if (result > (int.MaxValue - num2) / 10)
			{
				if (!eatDigits)
				{
					return System.SR.XmlConvert_Overflow;
				}
				numDigits = offset - num;
				while (offset < length && s[offset] >= '0' && s[offset] <= '9')
				{
					offset++;
				}
				return null;
			}
			result = result * 10 + num2;
			offset++;
		}
		numDigits = offset - num;
		return null;
	}
}
