namespace System.Text.Json.Serialization.Converters;

internal sealed class UriConverter : JsonConverter<Uri>
{
	public override Uri Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		string @string = reader.GetString();
		if (Uri.TryCreate(@string, UriKind.RelativeOrAbsolute, out Uri result))
		{
			return result;
		}
		ThrowHelper.ThrowJsonException();
		return null;
	}

	public override void Write(Utf8JsonWriter writer, Uri value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.OriginalString);
	}
}
