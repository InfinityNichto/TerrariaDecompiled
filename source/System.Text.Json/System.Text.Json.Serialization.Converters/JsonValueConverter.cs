using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class JsonValueConverter : JsonConverter<JsonValue>
{
	public override void Write(Utf8JsonWriter writer, JsonValue value, JsonSerializerOptions options)
	{
		value.WriteTo(writer, options);
	}

	public override JsonValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		JsonElement value = JsonElement.ParseValue(ref reader);
		return new JsonValueTrimmable<JsonElement>(value, JsonMetadataServices.JsonElementConverter, options.GetNodeOptions());
	}
}
