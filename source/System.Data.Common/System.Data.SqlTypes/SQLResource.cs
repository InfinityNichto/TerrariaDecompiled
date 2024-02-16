namespace System.Data.SqlTypes;

internal static class SQLResource
{
	internal static string NullString => System.SR.SqlMisc_NullString;

	internal static string ArithOverflowMessage => System.SR.SqlMisc_ArithOverflowMessage;

	internal static string DivideByZeroMessage => System.SR.SqlMisc_DivideByZeroMessage;

	internal static string NullValueMessage => System.SR.SqlMisc_NullValueMessage;

	internal static string TruncationMessage => System.SR.SqlMisc_TruncationMessage;

	internal static string DateTimeOverflowMessage => System.SR.SqlMisc_DateTimeOverflowMessage;

	internal static string ConcatDiffCollationMessage => System.SR.SqlMisc_ConcatDiffCollationMessage;

	internal static string CompareDiffCollationMessage => System.SR.SqlMisc_CompareDiffCollationMessage;

	internal static string ConversionOverflowMessage => System.SR.SqlMisc_ConversionOverflowMessage;

	internal static string InvalidDateTimeMessage => System.SR.SqlMisc_InvalidDateTimeMessage;

	internal static string TimeZoneSpecifiedMessage => System.SR.SqlMisc_TimeZoneSpecifiedMessage;

	internal static string InvalidArraySizeMessage => System.SR.SqlMisc_InvalidArraySizeMessage;

	internal static string InvalidPrecScaleMessage => System.SR.SqlMisc_InvalidPrecScaleMessage;

	internal static string FormatMessage => System.SR.SqlMisc_FormatMessage;

	internal static string NotFilledMessage => System.SR.SqlMisc_NotFilledMessage;

	internal static string AlreadyFilledMessage => System.SR.SqlMisc_AlreadyFilledMessage;

	internal static string ClosedXmlReaderMessage => System.SR.SqlMisc_ClosedXmlReaderMessage;

	internal static string InvalidOpStreamClosed(string method)
	{
		return System.SR.Format(System.SR.SqlMisc_InvalidOpStreamClosed, method);
	}

	internal static string InvalidOpStreamNonWritable(string method)
	{
		return System.SR.Format(System.SR.SqlMisc_InvalidOpStreamNonWritable, method);
	}

	internal static string InvalidOpStreamNonReadable(string method)
	{
		return System.SR.Format(System.SR.SqlMisc_InvalidOpStreamNonReadable, method);
	}

	internal static string InvalidOpStreamNonSeekable(string method)
	{
		return System.SR.Format(System.SR.SqlMisc_InvalidOpStreamNonSeekable, method);
	}
}
