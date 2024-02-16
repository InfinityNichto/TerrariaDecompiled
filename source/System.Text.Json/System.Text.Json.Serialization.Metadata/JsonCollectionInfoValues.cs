namespace System.Text.Json.Serialization.Metadata;

public sealed class JsonCollectionInfoValues<TCollection>
{
	public Func<TCollection>? ObjectCreator { get; init; }

	public JsonTypeInfo? KeyInfo { get; init; }

	public JsonTypeInfo ElementInfo { get; init; }

	public JsonNumberHandling NumberHandling { get; init; }

	public Action<Utf8JsonWriter, TCollection>? SerializeHandler { get; init; }
}
