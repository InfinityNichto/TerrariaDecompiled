using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class ObjectConverterFactory : JsonConverterFactory
{
	private readonly bool _useDefaultConstructorInUnannotatedStructs;

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public ObjectConverterFactory(bool useDefaultConstructorInUnannotatedStructs = true)
	{
		_useDefaultConstructorInUnannotatedStructs = useDefaultConstructorInUnannotatedStructs;
	}

	public override bool CanConvert(Type typeToConvert)
	{
		return true;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor is marked RequiresUnreferencedCode.")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern", Justification = "The ctor is marked RequiresUnreferencedCode.")]
	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		if (typeToConvert.IsKeyValuePair())
		{
			return CreateKeyValuePairConverter(typeToConvert, options);
		}
		if (!typeToConvert.TryGetDeserializationConstructor(_useDefaultConstructorInUnannotatedStructs, out var deserializationCtor))
		{
			ThrowHelper.ThrowInvalidOperationException_SerializationDuplicateTypeAttribute<JsonConstructorAttribute>(typeToConvert);
		}
		ParameterInfo[] array = deserializationCtor?.GetParameters();
		Type type;
		if (deserializationCtor == null || typeToConvert.IsAbstract || array.Length == 0)
		{
			type = typeof(ObjectDefaultConverter<>).MakeGenericType(typeToConvert);
		}
		else
		{
			int num = array.Length;
			if (num <= 4)
			{
				Type objectType = JsonTypeInfo.ObjectType;
				Type[] array2 = new Type[5] { typeToConvert, null, null, null, null };
				for (int i = 0; i < 4; i++)
				{
					if (i < num)
					{
						array2[i + 1] = array[i].ParameterType;
					}
					else
					{
						array2[i + 1] = objectType;
					}
				}
				type = typeof(SmallObjectWithParameterizedConstructorConverter<, , , , >).MakeGenericType(array2);
			}
			else
			{
				type = typeof(LargeObjectWithParameterizedConstructorConverterWithReflection<>).MakeGenericType(typeToConvert);
			}
		}
		JsonConverter jsonConverter = (JsonConverter)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public, null, null, null);
		jsonConverter.ConstructorInfo = deserializationCtor;
		return jsonConverter;
	}

	private JsonConverter CreateKeyValuePairConverter(Type type, JsonSerializerOptions options)
	{
		Type type2 = type.GetGenericArguments()[0];
		Type type3 = type.GetGenericArguments()[1];
		JsonConverter jsonConverter = (JsonConverter)Activator.CreateInstance(typeof(KeyValuePairConverter<, >).MakeGenericType(type2, type3), BindingFlags.Instance | BindingFlags.Public, null, null, null);
		jsonConverter.Initialize(options);
		return jsonConverter;
	}
}
