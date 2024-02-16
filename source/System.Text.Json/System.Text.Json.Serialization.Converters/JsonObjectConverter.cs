using System.Text.Json.Nodes;

namespace System.Text.Json.Serialization.Converters;

internal sealed class JsonObjectConverter : JsonConverter<JsonObject>
{
	internal override object CreateObject(JsonSerializerOptions options)
	{
		return new JsonObject(options.GetNodeOptions());
	}

	internal override void ReadElementAndSetProperty(object obj, string propertyName, ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state)
	{
		JsonNode value;
		bool flag = JsonNodeConverter.Instance.TryRead(ref reader, typeof(JsonNode), options, ref state, out value);
		JsonObject jsonObject = (JsonObject)obj;
		JsonNode value2 = value;
		jsonObject[propertyName] = value2;
	}

	public override void Write(Utf8JsonWriter writer, JsonObject value, JsonSerializerOptions options)
	{
		value.WriteTo(writer, options);
	}

	public override JsonObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		JsonTokenType tokenType = reader.TokenType;
		if (tokenType == JsonTokenType.StartObject)
		{
			return ReadObject(ref reader, options.GetNodeOptions());
		}
		throw ThrowHelper.GetInvalidOperationException_ExpectedObject(reader.TokenType);
	}

	public JsonObject ReadObject(ref Utf8JsonReader reader, JsonNodeOptions? options)
	{
		JsonElement element = JsonElement.ParseValue(ref reader);
		return new JsonObject(element, options);
	}
}
