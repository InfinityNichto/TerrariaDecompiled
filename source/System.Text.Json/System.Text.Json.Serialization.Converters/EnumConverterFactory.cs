using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters;

internal sealed class EnumConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type type)
	{
		return type.IsEnum;
	}

	public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
	{
		return Create(type, EnumConverterOptions.AllowNumbers, options);
	}

	internal static JsonConverter Create(Type enumType, EnumConverterOptions converterOptions, JsonSerializerOptions serializerOptions)
	{
		return (JsonConverter)Activator.CreateInstance(GetEnumConverterType(enumType), converterOptions, serializerOptions);
	}

	internal static JsonConverter Create(Type enumType, EnumConverterOptions converterOptions, JsonNamingPolicy namingPolicy, JsonSerializerOptions serializerOptions)
	{
		return (JsonConverter)Activator.CreateInstance(GetEnumConverterType(enumType), converterOptions, namingPolicy, serializerOptions);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:MakeGenericType", Justification = "'EnumConverter<T> where T : struct' implies 'T : new()', so the trimmer is warning calling MakeGenericType here because enumType's constructors are not annotated. But EnumConverter doesn't call new T(), so this is safe.")]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	private static Type GetEnumConverterType(Type enumType)
	{
		return typeof(EnumConverter<>).MakeGenericType(enumType);
	}
}
