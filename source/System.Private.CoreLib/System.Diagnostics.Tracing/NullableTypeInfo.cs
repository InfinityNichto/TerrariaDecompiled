using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Tracing;

internal sealed class NullableTypeInfo : TraceLoggingTypeInfo
{
	private readonly TraceLoggingTypeInfo valueInfo;

	[RequiresUnreferencedCode("EventSource WriteEvent will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
	public NullableTypeInfo(Type type, List<Type> recursionCheck)
		: base(type)
	{
		Type[] genericTypeArguments = type.GenericTypeArguments;
		valueInfo = TraceLoggingTypeInfo.GetInstance(genericTypeArguments[0], recursionCheck);
	}

	public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
	{
		TraceLoggingMetadataCollector traceLoggingMetadataCollector = collector.AddGroup(name);
		traceLoggingMetadataCollector.AddScalar("HasValue", TraceLoggingDataType.Boolean8);
		valueInfo.WriteMetadata(traceLoggingMetadataCollector, "Value", format);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:UnrecognizedReflectionPattern", Justification = "The underlying type of Nullable<T> must be defaultable")]
	public override void WriteData(PropertyValue value)
	{
		object referenceValue = value.ReferenceValue;
		bool flag = referenceValue != null;
		TraceLoggingDataCollector.AddScalar(flag);
		PropertyValue value2 = valueInfo.PropertyValueFactory(flag ? referenceValue : RuntimeHelpers.GetUninitializedObject(valueInfo.DataType));
		valueInfo.WriteData(value2);
	}
}
