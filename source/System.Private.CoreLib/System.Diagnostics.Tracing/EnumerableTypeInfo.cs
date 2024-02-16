using System.Collections;
using System.Collections.Generic;

namespace System.Diagnostics.Tracing;

internal sealed class EnumerableTypeInfo : TraceLoggingTypeInfo
{
	private readonly TraceLoggingTypeInfo elementInfo;

	internal TraceLoggingTypeInfo ElementInfo => elementInfo;

	public EnumerableTypeInfo(Type type, TraceLoggingTypeInfo elementInfo)
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
		int num = 0;
		IEnumerable enumerable = (IEnumerable)value.ReferenceValue;
		if (enumerable != null)
		{
			foreach (object item in enumerable)
			{
				elementInfo.WriteData(elementInfo.PropertyValueFactory(item));
				num++;
			}
		}
		TraceLoggingDataCollector.EndBufferedArray(bookmark, num);
	}

	public override object GetData(object value)
	{
		IEnumerable enumerable = (IEnumerable)value;
		List<object> list = new List<object>();
		foreach (object item in enumerable)
		{
			list.Add(elementInfo.GetData(item));
		}
		return list.ToArray();
	}
}
