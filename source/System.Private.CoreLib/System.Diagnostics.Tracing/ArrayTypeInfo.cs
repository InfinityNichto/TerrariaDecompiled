namespace System.Diagnostics.Tracing;

internal sealed class ArrayTypeInfo : TraceLoggingTypeInfo
{
	private readonly TraceLoggingTypeInfo elementInfo;

	public ArrayTypeInfo(Type type, TraceLoggingTypeInfo elementInfo)
		: base(type)
	{
		this.elementInfo = elementInfo;
	}

	public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
	{
		collector.BeginBufferedArray();
		elementInfo.WriteMetadata(collector, name, format);
		collector.EndBufferedArray();
	}

	public override void WriteData(PropertyValue value)
	{
		int bookmark = TraceLoggingDataCollector.BeginBufferedArray();
		int count = 0;
		Array array = (Array)value.ReferenceValue;
		if (array != null)
		{
			count = array.Length;
			for (int i = 0; i < array.Length; i++)
			{
				elementInfo.WriteData(elementInfo.PropertyValueFactory(array.GetValue(i)));
			}
		}
		TraceLoggingDataCollector.EndBufferedArray(bookmark, count);
	}

	public override object GetData(object value)
	{
		Array array = (Array)value;
		object[] array2 = new object[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = elementInfo.GetData(array.GetValue(i));
		}
		return array2;
	}
}
