using System.Globalization;
using System.Xml;

namespace System.Runtime.Serialization;

[DataContract(Name = "DateTimeOffset", Namespace = "http://schemas.datacontract.org/2004/07/System")]
internal struct DateTimeOffsetAdapter
{
	private DateTime _utcDateTime;

	private short _offsetMinutes;

	[DataMember(Name = "DateTime", IsRequired = true)]
	public DateTime UtcDateTime
	{
		get
		{
			return _utcDateTime;
		}
		set
		{
			_utcDateTime = value;
		}
	}

	[DataMember(Name = "OffsetMinutes", IsRequired = true)]
	public short OffsetMinutes
	{
		get
		{
			return _offsetMinutes;
		}
		set
		{
			_offsetMinutes = value;
		}
	}

	public DateTimeOffsetAdapter(DateTime dateTime, short offsetMinutes)
	{
		_utcDateTime = dateTime;
		_offsetMinutes = offsetMinutes;
	}

	public static DateTimeOffset GetDateTimeOffset(DateTimeOffsetAdapter value)
	{
		try
		{
			if (value.UtcDateTime.Kind == DateTimeKind.Unspecified)
			{
				return new DateTimeOffset(value.UtcDateTime, new TimeSpan(0, value.OffsetMinutes, 0));
			}
			return new DateTimeOffset(value.UtcDateTime).ToOffset(new TimeSpan(0, value.OffsetMinutes, 0));
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value.ToString(CultureInfo.InvariantCulture), "DateTimeOffset", exception));
		}
	}

	public static DateTimeOffsetAdapter GetDateTimeOffsetAdapter(DateTimeOffset value)
	{
		return new DateTimeOffsetAdapter(value.UtcDateTime, (short)value.Offset.TotalMinutes);
	}

	public string ToString(IFormatProvider provider)
	{
		return "DateTime: " + UtcDateTime.ToString() + ", Offset: " + OffsetMinutes;
	}
}
