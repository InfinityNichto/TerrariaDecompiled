namespace System.Diagnostics.Tracing;

internal sealed class DecimalTypeInfo : TraceLoggingTypeInfo
{
	private static DecimalTypeInfo s_instance;

	public DecimalTypeInfo()
		: base(typeof(decimal))
	{
	}

	public static TraceLoggingTypeInfo Instance()
	{
		return s_instance ?? (s_instance = new DecimalTypeInfo());
	}

	public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
	{
		collector.AddScalar(name, Statics.MakeDataType(TraceLoggingDataType.Double, format));
	}

	public override void WriteData(PropertyValue value)
	{
		TraceLoggingDataCollector.AddScalar((double)value.ScalarValue.AsDecimal);
	}
}
