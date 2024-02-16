namespace System.Text.Json.Serialization.Metadata;

public sealed class JsonObjectInfoValues<T>
{
	public Func<T>? ObjectCreator { get; init; }

	public Func<object[], T>? ObjectWithParameterizedConstructorCreator { get; init; }

	public Func<JsonSerializerContext, JsonPropertyInfo[]>? PropertyMetadataInitializer { get; init; }

	public Func<JsonParameterInfoValues[]>? ConstructorParameterMetadataInitializer { get; init; }

	public JsonNumberHandling NumberHandling { get; init; }

	public Action<Utf8JsonWriter, T>? SerializeHandler { get; init; }
}
