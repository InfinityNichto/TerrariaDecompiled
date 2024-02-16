namespace System.Diagnostics.Tracing;

internal sealed class InvokeTypeInfo : TraceLoggingTypeInfo
{
	internal readonly PropertyAnalysis[] properties;

	public InvokeTypeInfo(Type type, TypeAnalysis typeAnalysis)
		: base(type, typeAnalysis.name, typeAnalysis.level, typeAnalysis.opcode, typeAnalysis.keywords, typeAnalysis.tags)
	{
		if (typeAnalysis.properties.Length != 0)
		{
			properties = typeAnalysis.properties;
		}
	}

	public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
	{
		TraceLoggingMetadataCollector traceLoggingMetadataCollector = collector.AddGroup(name);
		if (properties == null)
		{
			return;
		}
		PropertyAnalysis[] array = properties;
		foreach (PropertyAnalysis propertyAnalysis in array)
		{
			EventFieldFormat format2 = EventFieldFormat.Default;
			EventFieldAttribute fieldAttribute = propertyAnalysis.fieldAttribute;
			if (fieldAttribute != null)
			{
				traceLoggingMetadataCollector.Tags = fieldAttribute.Tags;
				format2 = fieldAttribute.Format;
			}
			propertyAnalysis.typeInfo.WriteMetadata(traceLoggingMetadataCollector, propertyAnalysis.name, format2);
		}
	}

	public override void WriteData(PropertyValue value)
	{
		if (properties != null)
		{
			PropertyAnalysis[] array = properties;
			foreach (PropertyAnalysis propertyAnalysis in array)
			{
				propertyAnalysis.typeInfo.WriteData(propertyAnalysis.getter(value));
			}
		}
	}

	public override object GetData(object value)
	{
		if (properties != null)
		{
			string[] array = new string[properties.Length];
			object[] array2 = new object[properties.Length];
			for (int i = 0; i < properties.Length; i++)
			{
				object value2 = properties[i].propertyInfo.GetValue(value);
				array[i] = properties[i].name;
				array2[i] = properties[i].typeInfo.GetData(value2);
			}
			return new EventPayload(array, array2);
		}
		return null;
	}
}
