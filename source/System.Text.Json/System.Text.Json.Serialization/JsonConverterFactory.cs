using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization;

public abstract class JsonConverterFactory : JsonConverter
{
	internal sealed override ConverterStrategy ConverterStrategy => ConverterStrategy.None;

	internal sealed override Type? KeyType => null;

	internal sealed override Type? ElementType => null;

	internal sealed override Type TypeToConvert => null;

	public abstract JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options);

	internal override JsonPropertyInfo CreateJsonPropertyInfo()
	{
		throw new InvalidOperationException();
	}

	internal override JsonParameterInfo CreateJsonParameterInfo()
	{
		throw new InvalidOperationException();
	}

	internal JsonConverter GetConverterInternal(Type typeToConvert, JsonSerializerOptions options)
	{
		JsonConverter jsonConverter = CreateConverter(typeToConvert, options);
		if (jsonConverter == null)
		{
			ThrowHelper.ThrowInvalidOperationException_SerializerConverterFactoryReturnsNull(GetType());
		}
		if (jsonConverter is JsonConverterFactory)
		{
			ThrowHelper.ThrowInvalidOperationException_SerializerConverterFactoryReturnsJsonConverterFactorty(GetType());
		}
		return jsonConverter;
	}

	internal sealed override object ReadCoreAsObject(ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state)
	{
		throw new InvalidOperationException();
	}

	internal sealed override bool TryReadAsObject(ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state, out object value)
	{
		throw new InvalidOperationException();
	}

	internal sealed override bool TryWriteAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, ref WriteStack state)
	{
		throw new InvalidOperationException();
	}

	internal sealed override bool WriteCoreAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, ref WriteStack state)
	{
		throw new InvalidOperationException();
	}

	internal sealed override void WriteAsPropertyNameCoreAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		throw new InvalidOperationException();
	}
}
