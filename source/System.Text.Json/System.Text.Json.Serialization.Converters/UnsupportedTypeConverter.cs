namespace System.Text.Json.Serialization.Converters;

internal sealed class UnsupportedTypeConverter<T> : JsonConverter<T>
{
	public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.SerializeTypeInstanceNotSupported, typeof(T).FullName));
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.SerializeTypeInstanceNotSupported, typeof(T).FullName));
	}
}
