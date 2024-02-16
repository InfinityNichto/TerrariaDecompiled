using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Reflection;

namespace System.Text.Json.Serialization.Converters;

internal sealed class NullableConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert)
	{
		return typeToConvert.IsNullableOfT();
	}

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		Type type = typeToConvert.GetGenericArguments()[0];
		JsonConverter converterInternal = options.GetConverterInternal(type);
		if (!converterInternal.TypeToConvert.IsValueType && type.IsValueType)
		{
			return converterInternal;
		}
		return CreateValueConverter(type, converterInternal);
	}

	public static JsonConverter CreateValueConverter(Type valueTypeToConvert, JsonConverter valueConverter)
	{
		return (JsonConverter)Activator.CreateInstance(GetNullableConverterType(valueTypeToConvert), BindingFlags.Instance | BindingFlags.Public, null, new object[1] { valueConverter }, null);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:MakeGenericType", Justification = "'NullableConverter<T> where T : struct' implies 'T : new()', so the trimmer is warning calling MakeGenericType here because valueTypeToConvert's constructors are not annotated. But NullableConverter doesn't call new T(), so this is safe.")]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	private static Type GetNullableConverterType(Type valueTypeToConvert)
	{
		return typeof(NullableConverter<>).MakeGenericType(valueTypeToConvert);
	}
}
