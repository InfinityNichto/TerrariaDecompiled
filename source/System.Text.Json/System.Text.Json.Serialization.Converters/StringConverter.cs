namespace System.Text.Json.Serialization.Converters;

internal sealed class StringConverter : JsonConverter<string>
{
	public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return reader.GetString();
	}

	public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
	{
		if (value == null)
		{
			writer.WriteNullValue();
		}
		else
		{
			writer.WriteStringValue(value.AsSpan());
		}
	}

	internal override string ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return reader.GetString();
	}

	internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, string value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		if (options.DictionaryKeyPolicy != null && !isWritingExtensionDataProperty)
		{
			value = options.DictionaryKeyPolicy.ConvertName(value);
			if (value == null)
			{
				ThrowHelper.ThrowInvalidOperationException_NamingPolicyReturnNull(options.DictionaryKeyPolicy);
			}
		}
		writer.WritePropertyName(value);
	}
}
