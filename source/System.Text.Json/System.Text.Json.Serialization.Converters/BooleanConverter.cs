using System.Buffers.Text;

namespace System.Text.Json.Serialization.Converters;

internal sealed class BooleanConverter : JsonConverter<bool>
{
	public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return reader.GetBoolean();
	}

	public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
	{
		writer.WriteBooleanValue(value);
	}

	internal override bool ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		ReadOnlySpan<byte> span = reader.GetSpan();
		if (Utf8Parser.TryParse(span, out bool value, out int bytesConsumed, '\0') && span.Length == bytesConsumed)
		{
			return value;
		}
		throw ThrowHelper.GetFormatException(DataType.Boolean);
	}

	internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, bool value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		writer.WritePropertyName(value);
	}
}
