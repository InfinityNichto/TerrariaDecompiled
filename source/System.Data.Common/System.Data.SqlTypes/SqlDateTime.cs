using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data.SqlTypes;

[Serializable]
[XmlSchemaProvider("GetXsdType")]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public struct SqlDateTime : INullable, IComparable, IXmlSerializable
{
	private bool m_fNotNull;

	private int m_day;

	private int m_time;

	public static readonly int SQLTicksPerSecond = 300;

	public static readonly int SQLTicksPerMinute = SQLTicksPerSecond * 60;

	public static readonly int SQLTicksPerHour = SQLTicksPerMinute * 60;

	private static readonly int s_SQLTicksPerDay = SQLTicksPerHour * 24;

	private static readonly DateTime s_SQLBaseDate = new DateTime(1900, 1, 1);

	private static readonly long s_SQLBaseDateTicks = s_SQLBaseDate.Ticks;

	private static readonly int s_maxTime = s_SQLTicksPerDay - 1;

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

	private static readonly DateTime s_minDateTime = new DateTime(1753, 1, 1);

	private static readonly DateTime s_maxDateTime = DateTime.MaxValue;

	private static readonly TimeSpan s_minTimeSpan = s_minDateTime.Subtract(s_SQLBaseDate);

	private static readonly TimeSpan s_maxTimeSpan = s_maxDateTime.Subtract(s_SQLBaseDate);

	private static readonly string[] s_dateTimeFormats = new string[8] { "MMM d yyyy hh:mm:ss:ffftt", "MMM d yyyy hh:mm:ss:fff", "d MMM yyyy hh:mm:ss:ffftt", "d MMM yyyy hh:mm:ss:fff", "hh:mm:ss:ffftt", "hh:mm:ss:fff", "yyMMdd", "yyyyMMdd" };

	public static readonly SqlDateTime MinValue = new SqlDateTime(-53690, 0);

	public static readonly SqlDateTime MaxValue = new SqlDateTime(2958463, s_maxTime);

	public static readonly SqlDateTime Null = new SqlDateTime(fNull: true);

	public bool IsNull => !m_fNotNull;

	public DateTime Value
	{
		get
		{
			if (m_fNotNull)
			{
				return ToDateTime(this);
			}
			throw new SqlNullValueException();
		}
	}

	public int DayTicks
	{
		get
		{
			if (m_fNotNull)
			{
				return m_day;
			}
			throw new SqlNullValueException();
		}
	}

	public int TimeTicks
	{
		get
		{
			if (m_fNotNull)
			{
				return m_time;
			}
			throw new SqlNullValueException();
		}
	}

	private SqlDateTime(bool fNull)
	{
		m_fNotNull = false;
		m_day = 0;
		m_time = 0;
	}

	public SqlDateTime(DateTime value)
	{
		this = FromDateTime(value);
	}

	public SqlDateTime(int year, int month, int day)
		: this(year, month, day, 0, 0, 0, 0.0)
	{
	}

	public SqlDateTime(int year, int month, int day, int hour, int minute, int second)
		: this(year, month, day, hour, minute, second, 0.0)
	{
	}

	public SqlDateTime(int year, int month, int day, int hour, int minute, int second, double millisecond)
	{
		if (year >= 1753 && year <= 9999 && month >= 1 && month <= 12)
		{
			int[] array = (IsLeapYear(year) ? s_daysToMonth366 : s_daysToMonth365);
			if (day >= 1 && day <= array[month] - array[month - 1])
			{
				int num = year - 1;
				int num2 = num * 365 + num / 4 - num / 100 + num / 400 + array[month - 1] + day - 1;
				num2 -= 693595;
				if (num2 >= -53690 && num2 <= 2958463 && hour >= 0 && hour < 24 && minute >= 0 && minute < 60 && second >= 0 && second < 60 && millisecond >= 0.0 && millisecond < 1000.0)
				{
					double num3 = millisecond * 0.3 + 0.5;
					int num4 = hour * SQLTicksPerHour + minute * SQLTicksPerMinute + second * SQLTicksPerSecond + (int)num3;
					if (num4 > s_maxTime)
					{
						num4 = 0;
						num2++;
					}
					this = new SqlDateTime(num2, num4);
					return;
				}
			}
		}
		throw new SqlTypeException(SQLResource.InvalidDateTimeMessage);
	}

	public SqlDateTime(int year, int month, int day, int hour, int minute, int second, int bilisecond)
		: this(year, month, day, hour, minute, second, (double)bilisecond / 1000.0)
	{
	}

	public SqlDateTime(int dayTicks, int timeTicks)
	{
		if (dayTicks < -53690 || dayTicks > 2958463 || timeTicks < 0 || timeTicks > s_maxTime)
		{
			m_fNotNull = false;
			throw new OverflowException(SQLResource.DateTimeOverflowMessage);
		}
		m_day = dayTicks;
		m_time = timeTicks;
		m_fNotNull = true;
	}

	private static TimeSpan ToTimeSpan(SqlDateTime value)
	{
		long num = (long)((double)value.m_time / 0.3 + 0.5);
		return new TimeSpan(value.m_day * 864000000000L + num * 10000);
	}

	private static DateTime ToDateTime(SqlDateTime value)
	{
		return s_SQLBaseDate.Add(ToTimeSpan(value));
	}

	private static SqlDateTime FromTimeSpan(TimeSpan value)
	{
		if (value < s_minTimeSpan || value > s_maxTimeSpan)
		{
			throw new SqlTypeException(SQLResource.DateTimeOverflowMessage);
		}
		int num = value.Days;
		long num2 = value.Ticks - num * 864000000000L;
		if (num2 < 0)
		{
			num--;
			num2 += 864000000000L;
		}
		int num3 = (int)((double)num2 / 10000.0 * 0.3 + 0.5);
		if (num3 > s_maxTime)
		{
			num3 = 0;
			num++;
		}
		return new SqlDateTime(num, num3);
	}

	private static SqlDateTime FromDateTime(DateTime value)
	{
		if (value == DateTime.MaxValue)
		{
			return MaxValue;
		}
		return FromTimeSpan(value.Subtract(s_SQLBaseDate));
	}

	public static implicit operator SqlDateTime(DateTime value)
	{
		return new SqlDateTime(value);
	}

	public static explicit operator DateTime(SqlDateTime x)
	{
		return ToDateTime(x);
	}

	public override string ToString()
	{
		if (IsNull)
		{
			return SQLResource.NullString;
		}
		return ToDateTime(this).ToString((IFormatProvider?)null);
	}

	public static SqlDateTime Parse(string s)
	{
		if (s == SQLResource.NullString)
		{
			return Null;
		}
		DateTime value;
		try
		{
			value = DateTime.Parse(s, CultureInfo.InvariantCulture);
		}
		catch (FormatException)
		{
			DateTimeFormatInfo provider = (DateTimeFormatInfo)CultureInfo.CurrentCulture.GetFormat(typeof(DateTimeFormatInfo));
			value = DateTime.ParseExact(s, s_dateTimeFormats, provider, DateTimeStyles.AllowWhiteSpaces);
		}
		return new SqlDateTime(value);
	}

	public static SqlDateTime operator +(SqlDateTime x, TimeSpan t)
	{
		if (!x.IsNull)
		{
			return FromDateTime(ToDateTime(x) + t);
		}
		return Null;
	}

	public static SqlDateTime operator -(SqlDateTime x, TimeSpan t)
	{
		if (!x.IsNull)
		{
			return FromDateTime(ToDateTime(x) - t);
		}
		return Null;
	}

	public static SqlDateTime Add(SqlDateTime x, TimeSpan t)
	{
		return x + t;
	}

	public static SqlDateTime Subtract(SqlDateTime x, TimeSpan t)
	{
		return x - t;
	}

	public static explicit operator SqlDateTime(SqlString x)
	{
		if (!x.IsNull)
		{
			return Parse(x.Value);
		}
		return Null;
	}

	private static bool IsLeapYear(int year)
	{
		if (year % 4 == 0)
		{
			if (year % 100 == 0)
			{
				return year % 400 == 0;
			}
			return true;
		}
		return false;
	}

	public static SqlBoolean operator ==(SqlDateTime x, SqlDateTime y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_day == y.m_day && x.m_time == y.m_time);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator !=(SqlDateTime x, SqlDateTime y)
	{
		return !(x == y);
	}

	public static SqlBoolean operator <(SqlDateTime x, SqlDateTime y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_day < y.m_day || (x.m_day == y.m_day && x.m_time < y.m_time));
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator >(SqlDateTime x, SqlDateTime y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_day > y.m_day || (x.m_day == y.m_day && x.m_time > y.m_time));
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator <=(SqlDateTime x, SqlDateTime y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_day < y.m_day || (x.m_day == y.m_day && x.m_time <= y.m_time));
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator >=(SqlDateTime x, SqlDateTime y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_day > y.m_day || (x.m_day == y.m_day && x.m_time >= y.m_time));
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean Equals(SqlDateTime x, SqlDateTime y)
	{
		return x == y;
	}

	public static SqlBoolean NotEquals(SqlDateTime x, SqlDateTime y)
	{
		return x != y;
	}

	public static SqlBoolean LessThan(SqlDateTime x, SqlDateTime y)
	{
		return x < y;
	}

	public static SqlBoolean GreaterThan(SqlDateTime x, SqlDateTime y)
	{
		return x > y;
	}

	public static SqlBoolean LessThanOrEqual(SqlDateTime x, SqlDateTime y)
	{
		return x <= y;
	}

	public static SqlBoolean GreaterThanOrEqual(SqlDateTime x, SqlDateTime y)
	{
		return x >= y;
	}

	public SqlString ToSqlString()
	{
		return (SqlString)this;
	}

	public int CompareTo(object? value)
	{
		if (value is SqlDateTime value2)
		{
			return CompareTo(value2);
		}
		throw ADP.WrongType(value.GetType(), typeof(SqlDateTime));
	}

	public int CompareTo(SqlDateTime value)
	{
		if (IsNull)
		{
			if (!value.IsNull)
			{
				return -1;
			}
			return 0;
		}
		if (value.IsNull)
		{
			return 1;
		}
		if (this < value)
		{
			return -1;
		}
		if (this > value)
		{
			return 1;
		}
		return 0;
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (!(value is SqlDateTime sqlDateTime))
		{
			return false;
		}
		if (sqlDateTime.IsNull || IsNull)
		{
			if (sqlDateTime.IsNull)
			{
				return IsNull;
			}
			return false;
		}
		return (this == sqlDateTime).Value;
	}

	public override int GetHashCode()
	{
		if (!IsNull)
		{
			return Value.GetHashCode();
		}
		return 0;
	}

	XmlSchema IXmlSerializable.GetSchema()
	{
		return null;
	}

	void IXmlSerializable.ReadXml(XmlReader reader)
	{
		string attribute = reader.GetAttribute("nil", "http://www.w3.org/2001/XMLSchema-instance");
		if (attribute != null && XmlConvert.ToBoolean(attribute))
		{
			reader.ReadElementString();
			m_fNotNull = false;
			return;
		}
		DateTime value = XmlConvert.ToDateTime(reader.ReadElementString(), XmlDateTimeSerializationMode.RoundtripKind);
		if (value.Kind != 0)
		{
			throw new SqlTypeException(SQLResource.TimeZoneSpecifiedMessage);
		}
		SqlDateTime sqlDateTime = FromDateTime(value);
		m_day = sqlDateTime.DayTicks;
		m_time = sqlDateTime.TimeTicks;
		m_fNotNull = true;
	}

	void IXmlSerializable.WriteXml(XmlWriter writer)
	{
		if (IsNull)
		{
			writer.WriteAttributeString("xsi", "nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
		}
		else
		{
			writer.WriteString(XmlConvert.ToString(Value, "yyyy-MM-ddTHH:mm:ss.fff"));
		}
	}

	public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
	{
		return new XmlQualifiedName("dateTime", "http://www.w3.org/2001/XMLSchema");
	}
}
