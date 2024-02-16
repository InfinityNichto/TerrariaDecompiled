namespace System.Diagnostics.Tracing;

internal sealed class StringTypeInfo : TraceLoggingTypeInfo
{
	private static StringTypeInfo s_instance;

	public StringTypeInfo()
		: base(typeof(string))
	{
	}

	public static TraceLoggingTypeInfo Instance()
	{
		return s_instance ?? (s_instance = new StringTypeInfo());
	}

	public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
	{
		if (name == null)
		{
			name = "message";
		}
		collector.AddNullTerminatedString(name, Statics.MakeDataType(TraceLoggingDataType.Utf16String, format));
	}

	public override void WriteData(PropertyValue value)
	{
		TraceLoggingDataCollector.AddNullTerminatedString((string)value.ReferenceValue);
	}

	public override object GetData(object value)
	{
		if (value == null)
		{
			return "";
		}
		return value;
	}
}
