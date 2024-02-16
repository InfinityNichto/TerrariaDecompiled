using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Nodes;

internal sealed class JsonValueTrimmable<TValue> : JsonValue<TValue>
{
	private readonly JsonTypeInfo<TValue> _jsonTypeInfo;

	private readonly JsonConverter<TValue> _converter;

	public JsonValueTrimmable(TValue value, JsonTypeInfo<TValue> jsonTypeInfo, JsonNodeOptions? options = null)
		: base(value, options)
	{
		_jsonTypeInfo = jsonTypeInfo;
	}

	public JsonValueTrimmable(TValue value, JsonConverter<TValue> converter, JsonNodeOptions? options = null)
		: base(value, options)
	{
		_converter = converter;
	}

	public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions options = null)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (_converter != null)
		{
			if (options == null)
			{
				options = JsonSerializerOptions.s_defaultOptions;
			}
			if (_converter.IsInternalConverterForNumberType)
			{
				_converter.WriteNumberWithCustomHandling(writer, _value, options.NumberHandling);
			}
			else
			{
				_converter.Write(writer, _value, options);
			}
		}
		else
		{
			JsonSerializer.Serialize(writer, _value, _jsonTypeInfo);
		}
	}
}
