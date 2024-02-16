namespace System.Text.Json.Serialization.Converters;

internal sealed class VersionConverter : JsonConverter<Version>
{
	public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		string @string = reader.GetString();
		if (Version.TryParse(@string, out Version result))
		{
			return result;
		}
		ThrowHelper.ThrowJsonException();
		return null;
	}

	public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString());
	}
}
