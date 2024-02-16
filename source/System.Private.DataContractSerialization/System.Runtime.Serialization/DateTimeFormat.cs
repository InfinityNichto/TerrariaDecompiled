using System.Globalization;

namespace System.Runtime.Serialization;

public class DateTimeFormat
{
	private readonly string _formatString;

	private readonly IFormatProvider _formatProvider;

	private DateTimeStyles _dateTimeStyles;

	public string FormatString => _formatString;

	public IFormatProvider FormatProvider => _formatProvider;

	public DateTimeStyles DateTimeStyles
	{
		get
		{
			return _dateTimeStyles;
		}
		set
		{
			_dateTimeStyles = value;
		}
	}

	public DateTimeFormat(string formatString)
		: this(formatString, DateTimeFormatInfo.CurrentInfo)
	{
	}

	public DateTimeFormat(string formatString, IFormatProvider formatProvider)
	{
		if (formatString == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("formatString");
		}
		if (formatProvider == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("formatProvider");
		}
		_formatString = formatString;
		_formatProvider = formatProvider;
		_dateTimeStyles = DateTimeStyles.RoundtripKind;
	}
}
