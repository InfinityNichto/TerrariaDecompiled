using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface, AllowMultiple = false)]
public class JsonConverterAttribute : JsonAttribute
{
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	public Type? ConverterType { get; private set; }

	public JsonConverterAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type converterType)
	{
		ConverterType = converterType;
	}

	protected JsonConverterAttribute()
	{
	}

	public virtual JsonConverter? CreateConverter(Type typeToConvert)
	{
		return null;
	}
}
