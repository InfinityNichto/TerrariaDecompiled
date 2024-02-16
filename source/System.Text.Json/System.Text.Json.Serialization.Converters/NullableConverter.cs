namespace System.Text.Json.Serialization.Converters;

internal sealed class NullableConverter<T> : JsonConverter<T?> where T : struct
{
	private readonly JsonConverter<T> _converter;

	public NullableConverter(JsonConverter<T> converter)
	{
		_converter = converter;
		IsInternalConverterForNumberType = converter.IsInternalConverterForNumberType;
	}

	public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}
		return _converter.Read(ref reader, typeof(T), options);
	}

	public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
	{
		if (!value.HasValue)
		{
			writer.WriteNullValue();
		}
		else
		{
			_converter.Write(writer, value.Value, options);
		}
	}

	internal override T? ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling numberHandling, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}
		return _converter.ReadNumberWithCustomHandling(ref reader, numberHandling, options);
	}

	internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, T? value, JsonNumberHandling handling)
	{
		if (!value.HasValue)
		{
			writer.WriteNullValue();
		}
		else
		{
			_converter.WriteNumberWithCustomHandling(writer, value.Value, handling);
		}
	}
}
