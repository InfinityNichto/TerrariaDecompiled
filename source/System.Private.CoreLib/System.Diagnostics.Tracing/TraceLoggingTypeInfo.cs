using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.Tracing;

internal abstract class TraceLoggingTypeInfo
{
	private readonly string name;

	private readonly EventKeywords keywords;

	private readonly EventLevel level = (EventLevel)(-1);

	private readonly EventOpcode opcode = (EventOpcode)(-1);

	private readonly EventTags tags;

	private readonly Type dataType;

	private readonly Func<object, PropertyValue> propertyValueFactory;

	[ThreadStatic]
	private static Dictionary<Type, TraceLoggingTypeInfo> threadCache;

	public string Name => name;

	public EventLevel Level => level;

	public EventOpcode Opcode => opcode;

	public EventKeywords Keywords => keywords;

	public EventTags Tags => tags;

	internal Type DataType => dataType;

	internal Func<object, PropertyValue> PropertyValueFactory => propertyValueFactory;

	internal TraceLoggingTypeInfo(Type dataType)
	{
		if (dataType == null)
		{
			throw new ArgumentNullException("dataType");
		}
		name = dataType.Name;
		this.dataType = dataType;
		propertyValueFactory = PropertyValue.GetFactory(dataType);
	}

	internal TraceLoggingTypeInfo(Type dataType, string name, EventLevel level, EventOpcode opcode, EventKeywords keywords, EventTags tags)
	{
		if (dataType == null)
		{
			throw new ArgumentNullException("dataType");
		}
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		Statics.CheckName(name);
		this.name = name;
		this.keywords = keywords;
		this.level = level;
		this.opcode = opcode;
		this.tags = tags;
		this.dataType = dataType;
		propertyValueFactory = PropertyValue.GetFactory(dataType);
	}

	public abstract void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format);

	public abstract void WriteData(PropertyValue value);

	public virtual object GetData(object value)
	{
		return value;
	}

	[RequiresUnreferencedCode("EventSource WriteEvent will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
	public static TraceLoggingTypeInfo GetInstance(Type type, List<Type> recursionCheck)
	{
		Dictionary<Type, TraceLoggingTypeInfo> dictionary = threadCache ?? (threadCache = new Dictionary<Type, TraceLoggingTypeInfo>());
		if (!dictionary.TryGetValue(type, out var value))
		{
			if (recursionCheck == null)
			{
				recursionCheck = new List<Type>();
			}
			int count = recursionCheck.Count;
			value = (dictionary[type] = Statics.CreateDefaultTypeInfo(type, recursionCheck));
			recursionCheck.RemoveRange(count, recursionCheck.Count - count);
		}
		return value;
	}
}
