namespace System.Diagnostics.Tracing;

internal sealed class DateTimeTypeInfo : TraceLoggingTypeInfo
{
	private static DateTimeTypeInfo s_instance;

	public DateTimeTypeInfo()
		: base(typeof(DateTime))
	{
	}

	public static TraceLoggingTypeInfo Instance()
	{
		return s_instance ?? (s_instance = new DateTimeTypeInfo());
	}

	public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
	{
		collector.AddScalar(name, Statics.MakeDataType(TraceLoggingDataType.FileTime, format));
	}

	public override void WriteData(PropertyValue value)
	{
		DateTime asDateTime = value.ScalarValue.AsDateTime;
		long value2 = 0L;
		if (asDateTime.Ticks > 504911232000000000L)
		{
			value2 = asDateTime.ToFileTimeUtc();
		}
		TraceLoggingDataCollector.AddScalar(value2);
	}
}
