using System.Runtime.InteropServices;

namespace System.Text.Json.Serialization.Converters;

internal sealed class CharConverter : JsonConverter<char>
{
	public override char Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		string @string = reader.GetString();
		if (string.IsNullOrEmpty(@string) || @string.Length > 1)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedChar(reader.TokenType);
		}
		return @string[0];
	}

	public override void Write(Utf8JsonWriter writer, char value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(MemoryMarshal.CreateSpan(ref value, 1));
	}

	internal override char ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return Read(ref reader, typeToConvert, options);
	}

	internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, char value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		writer.WritePropertyName(MemoryMarshal.CreateSpan(ref value, 1));
	}
}
