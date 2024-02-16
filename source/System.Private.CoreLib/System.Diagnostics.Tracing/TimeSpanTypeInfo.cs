namespace System.Diagnostics.Tracing;

internal sealed class TimeSpanTypeInfo : TraceLoggingTypeInfo
{
	private static TimeSpanTypeInfo s_instance;

	public TimeSpanTypeInfo()
		: base(typeof(TimeSpan))
	{
	}

	public static TraceLoggingTypeInfo Instance()
	{
		return s_instance ?? (s_instance = new TimeSpanTypeInfo());
	}

	public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
	{
		collector.AddScalar(name, Statics.MakeDataType(TraceLoggingDataType.Int64, format));
	}

	public override void WriteData(PropertyValue value)
	{
		TraceLoggingDataCollector.AddScalar(value.ScalarValue.AsTimeSpan.Ticks);
	}
}
