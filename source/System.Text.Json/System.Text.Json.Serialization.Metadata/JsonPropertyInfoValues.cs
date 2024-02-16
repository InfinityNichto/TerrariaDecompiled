namespace System.Text.Json.Serialization.Metadata;

public sealed class JsonPropertyInfoValues<T>
{
	public bool IsProperty { get; init; }

	public bool IsPublic { get; init; }

	public bool IsVirtual { get; init; }

	public Type DeclaringType { get; init; }

	public JsonTypeInfo PropertyTypeInfo { get; init; }

	public JsonConverter<T>? Converter { get; init; }

	public Func<object, T?>? Getter { get; init; }

	public Action<object, T?>? Setter { get; init; }

	public JsonIgnoreCondition? IgnoreCondition { get; init; }

	public bool HasJsonInclude { get; init; }

	public bool IsExtensionData { get; init; }

	public JsonNumberHandling? NumberHandling { get; init; }

	public string PropertyName { get; init; }

	public string? JsonPropertyName { get; init; }
}
