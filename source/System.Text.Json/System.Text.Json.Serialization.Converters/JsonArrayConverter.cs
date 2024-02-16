using System.Text.Json.Nodes;

namespace System.Text.Json.Serialization.Converters;

internal sealed class JsonArrayConverter : JsonConverter<JsonArray>
{
	public override void Write(Utf8JsonWriter writer, JsonArray value, JsonSerializerOptions options)
	{
		value.WriteTo(writer, options);
	}

	public override JsonArray Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		JsonTokenType tokenType = reader.TokenType;
		if (tokenType == JsonTokenType.StartArray)
		{
			return ReadList(ref reader, options.GetNodeOptions());
		}
		throw ThrowHelper.GetInvalidOperationException_ExpectedArray(reader.TokenType);
	}

	public JsonArray ReadList(ref Utf8JsonReader reader, JsonNodeOptions? options = null)
	{
		JsonElement element = JsonElement.ParseValue(ref reader);
		return new JsonArray(element, options);
	}
}
