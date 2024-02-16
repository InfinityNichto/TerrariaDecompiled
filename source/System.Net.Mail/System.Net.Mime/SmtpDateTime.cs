using System.Collections.Generic;
using System.Globalization;

namespace System.Net.Mime;

internal sealed class SmtpDateTime
{
	internal static readonly string[] s_validDateTimeFormats = new string[4] { "ddd, dd MMM yyyy HH:mm:ss", "dd MMM yyyy HH:mm:ss", "ddd, dd MMM yyyy HH:mm", "dd MMM yyyy HH:mm" };

	internal static readonly char[] s_allowedWhiteSpaceChars = new char[2] { ' ', '\t' };

	internal static readonly Dictionary<string, TimeSpan> s_timeZoneOffsetLookup = InitializeShortHandLookups();

	private readonly DateTime _date;

	private readonly TimeSpan _timeZone;

	private readonly bool _unknownTimeZone;

	internal DateTime Date
	{
		get
		{
			if (_unknownTimeZone)
			{
				return DateTime.SpecifyKind(_date, DateTimeKind.Unspecified);
			}
			return new DateTimeOffset(_date, _timeZone).LocalDateTime;
		}
	}

	internal static Dictionary<string, TimeSpan> InitializeShortHandLookups()
	{
		Dictionary<string, TimeSpan> dictionary = new Dictionary<string, TimeSpan>();
		dictionary.Add("UT", TimeSpan.Zero);
		dictionary.Add("GMT", TimeSpan.Zero);
		dictionary.Add("EDT", new TimeSpan(-4, 0, 0));
		dictionary.Add("EST", new TimeSpan(-5, 0, 0));
		dictionary.Add("CDT", new TimeSpan(-5, 0, 0));
		dictionary.Add("CST", new TimeSpan(-6, 0, 0));
		dictionary.Add("MDT", new TimeSpan(-6, 0, 0));
		dictionary.Add("MST", new TimeSpan(-7, 0, 0));
		dictionary.Add("PDT", new TimeSpan(-7, 0, 0));
		dictionary.Add("PST", new TimeSpan(-8, 0, 0));
		return dictionary;
	}

	internal SmtpDateTime(DateTime value)
	{
		_date = value;
		switch (value.Kind)
		{
		case DateTimeKind.Local:
		{
			TimeSpan utcOffset = TimeZoneInfo.Local.GetUtcOffset(value);
			_timeZone = ValidateAndGetSanitizedTimeSpan(utcOffset);
			break;
		}
		case DateTimeKind.Unspecified:
			_unknownTimeZone = true;
			break;
		case DateTimeKind.Utc:
			_timeZone = TimeSpan.Zero;
			break;
		}
	}

	internal SmtpDateTime(string value)
	{
		_date = ParseValue(value, out var timeZone);
		if (!TryParseTimeZoneString(timeZone, out _timeZone))
		{
			_unknownTimeZone = true;
		}
	}

	public override string ToString()
	{
		return FormatDate(_date) + " " + (_unknownTimeZone ? "-0000" : TimeSpanToOffset(_timeZone));
	}

	internal void ValidateAndGetTimeZoneOffsetValues(string offset, out bool positive, out int hours, out int minutes)
	{
		if (offset.Length != 5)
		{
			throw new FormatException(System.SR.MailDateInvalidFormat);
		}
		positive = offset.StartsWith('+');
		if (!int.TryParse(offset.AsSpan(1, 2), NumberStyles.None, CultureInfo.InvariantCulture, out hours))
		{
			throw new FormatException(System.SR.MailDateInvalidFormat);
		}
		if (!int.TryParse(offset.AsSpan(3, 2), NumberStyles.None, CultureInfo.InvariantCulture, out minutes))
		{
			throw new FormatException(System.SR.MailDateInvalidFormat);
		}
		if (minutes > 59)
		{
			throw new FormatException(System.SR.MailDateInvalidFormat);
		}
	}

	internal void ValidateTimeZoneShortHandValue(string value)
	{
		for (int i = 0; i < value.Length; i++)
		{
			if (!char.IsLetter(value, i))
			{
				throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, value));
			}
		}
	}

	internal string FormatDate(DateTime value)
	{
		return value.ToString("ddd, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture);
	}

	internal DateTime ParseValue(string data, out string timeZone)
	{
		if (string.IsNullOrEmpty(data))
		{
			throw new FormatException(System.SR.MailDateInvalidFormat);
		}
		int num = data.IndexOf(':');
		if (num == -1)
		{
			throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, data));
		}
		int num2 = data.IndexOfAny(s_allowedWhiteSpaceChars, num);
		if (num2 == -1)
		{
			throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, data));
		}
		string s = data.AsSpan(0, num2).Trim().ToString();
		if (!DateTime.TryParseExact(s, s_validDateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var result))
		{
			throw new FormatException(System.SR.MailDateInvalidFormat);
		}
		string text = data.AsSpan(num2).Trim().ToString();
		int num3 = text.IndexOfAny(s_allowedWhiteSpaceChars);
		if (num3 != -1)
		{
			text = text.Substring(0, num3);
		}
		if (string.IsNullOrEmpty(text))
		{
			throw new FormatException(System.SR.MailDateInvalidFormat);
		}
		timeZone = text;
		return result;
	}

	internal bool TryParseTimeZoneString(string timeZoneString, out TimeSpan timeZone)
	{
		if (timeZoneString == "-0000")
		{
			timeZone = TimeSpan.Zero;
			return false;
		}
		if (timeZoneString[0] == '+' || timeZoneString[0] == '-')
		{
			ValidateAndGetTimeZoneOffsetValues(timeZoneString, out var positive, out var hours, out var minutes);
			if (!positive)
			{
				if (hours != 0)
				{
					hours *= -1;
				}
				else if (minutes != 0)
				{
					minutes *= -1;
				}
			}
			timeZone = new TimeSpan(hours, minutes, 0);
			return true;
		}
		ValidateTimeZoneShortHandValue(timeZoneString);
		return s_timeZoneOffsetLookup.TryGetValue(timeZoneString, out timeZone);
	}

	internal TimeSpan ValidateAndGetSanitizedTimeSpan(TimeSpan span)
	{
		TimeSpan result = new TimeSpan(span.Days, span.Hours, span.Minutes, 0, 0);
		if (Math.Abs(result.Ticks) > 3599400000000L)
		{
			throw new FormatException(System.SR.MailDateInvalidFormat);
		}
		return result;
	}

	internal string TimeSpanToOffset(TimeSpan span)
	{
		if (span.Ticks == 0L)
		{
			return "+0000";
		}
		uint num = (uint)Math.Abs(Math.Floor(span.TotalHours));
		uint num2 = (uint)Math.Abs(span.Minutes);
		string text = ((span.Ticks > 0) ? "+" : "-");
		if (num < 10)
		{
			text += "0";
		}
		text += num;
		if (num2 < 10)
		{
			text += "0";
		}
		return text + num2;
	}
}
