using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Nodes;

internal sealed class JsonValueNotTrimmable<TValue> : JsonValue<TValue>
{
	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public JsonValueNotTrimmable(TValue value, JsonNodeOptions? options = null)
		: base(value, options)
	{
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor is marked with RequiresUnreferencedCode.")]
	public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions options = null)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		JsonSerializer.Serialize(writer, _value, options);
	}
}
