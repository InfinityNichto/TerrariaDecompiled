namespace System.Text.Json.Serialization;

internal abstract class JsonResumableConverter<T> : JsonConverter<T>
{
	public sealed override bool HandleNull => false;

	public sealed override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (options == null)
		{
			throw new ArgumentNullException("options");
		}
		ReadStack state = default(ReadStack);
		state.Initialize(typeToConvert, options, supportContinuation: false);
		TryRead(ref reader, typeToConvert, options, ref state, out var value);
		return value;
	}

	public sealed override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		if (options == null)
		{
			throw new ArgumentNullException("options");
		}
		WriteStack state = default(WriteStack);
		state.Initialize(typeof(T), options, supportContinuation: false);
		try
		{
			TryWrite(writer, in value, options, ref state);
		}
		catch
		{
			state.DisposePendingDisposablesOnException();
			throw;
		}
	}
}
