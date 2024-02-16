namespace System.Text.Json.Serialization.Converters;

internal sealed class UInt16Converter : JsonConverter<ushort>
{
	public UInt16Converter()
	{
		IsInternalConverterForNumberType = true;
	}

	public override ushort Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return reader.GetUInt16();
	}

	public override void Write(Utf8JsonWriter writer, ushort value, JsonSerializerOptions options)
	{
		writer.WriteNumberValue((long)value);
	}

	internal override ushort ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return reader.GetUInt16WithQuotes();
	}

	internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, ushort value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		writer.WritePropertyName(value);
	}

	internal override ushort ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String && (JsonNumberHandling.AllowReadingFromString & handling) != 0)
		{
			return reader.GetUInt16WithQuotes();
		}
		return reader.GetUInt16();
	}

	internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, ushort value, JsonNumberHandling handling)
	{
		if ((JsonNumberHandling.WriteAsString & handling) != 0)
		{
			writer.WriteNumberValueAsString(value);
		}
		else
		{
			writer.WriteNumberValue((long)value);
		}
	}
}
