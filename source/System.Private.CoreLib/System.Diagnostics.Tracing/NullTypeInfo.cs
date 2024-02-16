namespace System.Diagnostics.Tracing;

internal sealed class NullTypeInfo : TraceLoggingTypeInfo
{
	private static NullTypeInfo s_instance;

	public NullTypeInfo()
		: base(typeof(EmptyStruct))
	{
	}

	public static TraceLoggingTypeInfo Instance()
	{
		return s_instance ?? (s_instance = new NullTypeInfo());
	}

	public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
	{
		collector.AddGroup(name);
	}

	public override void WriteData(PropertyValue value)
	{
	}

	public override object GetData(object value)
	{
		return null;
	}
}
