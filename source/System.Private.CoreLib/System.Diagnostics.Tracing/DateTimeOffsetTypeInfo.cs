namespace System.Diagnostics.Tracing;

internal sealed class DateTimeOffsetTypeInfo : TraceLoggingTypeInfo
{
	private static DateTimeOffsetTypeInfo s_instance;

	public DateTimeOffsetTypeInfo()
		: base(typeof(DateTimeOffset))
	{
	}

	public static TraceLoggingTypeInfo Instance()
	{
		return s_instance ?? (s_instance = new DateTimeOffsetTypeInfo());
	}

	public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
	{
		TraceLoggingMetadataCollector traceLoggingMetadataCollector = collector.AddGroup(name);
		traceLoggingMetadataCollector.AddScalar("Ticks", Statics.MakeDataType(TraceLoggingDataType.FileTime, format));
		traceLoggingMetadataCollector.AddScalar("Offset", TraceLoggingDataType.Int64);
	}

	public override void WriteData(PropertyValue value)
	{
		DateTimeOffset asDateTimeOffset = value.ScalarValue.AsDateTimeOffset;
		long ticks = asDateTimeOffset.Ticks;
		TraceLoggingDataCollector.AddScalar((ticks < 504911232000000000L) ? 0 : (ticks - 504911232000000000L));
		TraceLoggingDataCollector.AddScalar(asDateTimeOffset.Offset.Ticks);
	}
}
