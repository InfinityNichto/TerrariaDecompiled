namespace System.Text.Json.Serialization.Converters;

internal sealed class ObjectConverter : JsonConverter<object>
{
	public ObjectConverter()
	{
		IsInternalConverterForNumberType = true;
	}

	public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (options.UnknownTypeHandling == JsonUnknownTypeHandling.JsonElement)
		{
			return JsonElement.ParseValue(ref reader);
		}
		return JsonNodeConverter.Instance.Read(ref reader, typeToConvert, options);
	}

	public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteEndObject();
	}

	internal override object ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(TypeToConvert, this);
		return null;
	}

	internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, object value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		Type type = value.GetType();
		JsonConverter converterInternal = options.GetConverterInternal(type);
		if (converterInternal == this)
		{
			ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(type, this);
		}
		converterInternal.WriteAsPropertyNameCoreAsObject(writer, value, options, isWritingExtensionDataProperty);
	}

	internal override object ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
	{
		if (options.UnknownTypeHandling == JsonUnknownTypeHandling.JsonElement)
		{
			return JsonElement.ParseValue(ref reader);
		}
		return JsonNodeConverter.Instance.Read(ref reader, typeof(object), options);
	}
}
