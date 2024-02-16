namespace System.Text.Json.Serialization.Converters;

internal sealed class JsonElementConverter : JsonConverter<JsonElement>
{
	public override JsonElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return JsonElement.ParseValue(ref reader);
	}

	public override void Write(Utf8JsonWriter writer, JsonElement value, JsonSerializerOptions options)
	{
		value.WriteTo(writer);
	}
}
