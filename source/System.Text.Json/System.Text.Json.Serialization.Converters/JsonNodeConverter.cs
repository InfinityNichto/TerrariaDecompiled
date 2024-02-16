using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class JsonNodeConverter : JsonConverter<JsonNode>
{
	private static JsonNodeConverter s_nodeConverter;

	private static JsonArrayConverter s_arrayConverter;

	private static JsonObjectConverter s_objectConverter;

	private static JsonValueConverter s_valueConverter;

	public static JsonNodeConverter Instance => s_nodeConverter ?? (s_nodeConverter = new JsonNodeConverter());

	public static JsonArrayConverter ArrayConverter => s_arrayConverter ?? (s_arrayConverter = new JsonArrayConverter());

	public static JsonObjectConverter ObjectConverter => s_objectConverter ?? (s_objectConverter = new JsonObjectConverter());

	public static JsonValueConverter ValueConverter => s_valueConverter ?? (s_valueConverter = new JsonValueConverter());

	public override void Write(Utf8JsonWriter writer, JsonNode value, JsonSerializerOptions options)
	{
		if (value == null)
		{
			writer.WriteNullValue();
		}
		else if (value is JsonValue value2)
		{
			ValueConverter.Write(writer, value2, options);
		}
		else if (value is JsonObject value3)
		{
			ObjectConverter.Write(writer, value3, options);
		}
		else
		{
			ArrayConverter.Write(writer, (JsonArray)value, options);
		}
	}

	public override JsonNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
		case JsonTokenType.String:
		case JsonTokenType.Number:
		case JsonTokenType.True:
		case JsonTokenType.False:
			return ValueConverter.Read(ref reader, typeToConvert, options);
		case JsonTokenType.StartObject:
			return ObjectConverter.Read(ref reader, typeToConvert, options);
		case JsonTokenType.StartArray:
			return ArrayConverter.Read(ref reader, typeToConvert, options);
		default:
			throw new JsonException();
		}
	}

	public static JsonNode Create(JsonElement element, JsonNodeOptions? options)
	{
		return element.ValueKind switch
		{
			JsonValueKind.Null => null, 
			JsonValueKind.Object => new JsonObject(element, options), 
			JsonValueKind.Array => new JsonArray(element, options), 
			_ => new JsonValueTrimmable<JsonElement>(element, JsonMetadataServices.JsonElementConverter, options), 
		};
	}
}
