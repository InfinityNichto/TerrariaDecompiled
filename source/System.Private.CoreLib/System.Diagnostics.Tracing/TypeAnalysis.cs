using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Diagnostics.Tracing;

internal sealed class TypeAnalysis
{
	internal readonly PropertyAnalysis[] properties;

	internal readonly string name;

	internal readonly EventKeywords keywords;

	internal readonly EventLevel level = (EventLevel)(-1);

	internal readonly EventOpcode opcode = (EventOpcode)(-1);

	internal readonly EventTags tags;

	[RequiresUnreferencedCode("EventSource WriteEvent will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
	public TypeAnalysis(Type dataType, EventDataAttribute eventAttrib, List<Type> recursionCheck)
	{
		List<PropertyAnalysis> list = new List<PropertyAnalysis>();
		PropertyInfo[] array = dataType.GetProperties();
		foreach (PropertyInfo propertyInfo in array)
		{
			if (!Statics.HasCustomAttribute(propertyInfo, typeof(EventIgnoreAttribute)) && propertyInfo.CanRead && propertyInfo.GetIndexParameters().Length == 0)
			{
				MethodInfo getMethod = propertyInfo.GetGetMethod();
				if (!(getMethod == null) && !getMethod.IsStatic && getMethod.IsPublic)
				{
					Type propertyType = propertyInfo.PropertyType;
					TraceLoggingTypeInfo instance = TraceLoggingTypeInfo.GetInstance(propertyType, recursionCheck);
					EventFieldAttribute customAttribute = Statics.GetCustomAttribute<EventFieldAttribute>(propertyInfo);
					string text = ((customAttribute != null && customAttribute.Name != null) ? customAttribute.Name : (Statics.ShouldOverrideFieldName(propertyInfo.Name) ? instance.Name : propertyInfo.Name));
					list.Add(new PropertyAnalysis(text, propertyInfo, instance, customAttribute));
				}
			}
		}
		properties = list.ToArray();
		PropertyAnalysis[] array2 = properties;
		foreach (PropertyAnalysis propertyAnalysis in array2)
		{
			TraceLoggingTypeInfo typeInfo = propertyAnalysis.typeInfo;
			level = (EventLevel)Statics.Combine((int)typeInfo.Level, (int)level);
			opcode = (EventOpcode)Statics.Combine((int)typeInfo.Opcode, (int)opcode);
			keywords |= typeInfo.Keywords;
			tags |= typeInfo.Tags;
		}
		if (eventAttrib != null)
		{
			level = (EventLevel)Statics.Combine((int)eventAttrib.Level, (int)level);
			opcode = (EventOpcode)Statics.Combine((int)eventAttrib.Opcode, (int)opcode);
			keywords |= eventAttrib.Keywords;
			tags |= eventAttrib.Tags;
			name = eventAttrib.Name;
		}
		if (name == null)
		{
			name = dataType.Name;
		}
	}
}
