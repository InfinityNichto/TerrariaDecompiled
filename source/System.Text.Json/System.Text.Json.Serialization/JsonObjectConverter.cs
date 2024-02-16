namespace System.Text.Json.Serialization;

internal abstract class JsonObjectConverter<T> : JsonResumableConverter<T>
{
	internal sealed override ConverterStrategy ConverterStrategy => ConverterStrategy.Object;

	internal sealed override Type ElementType => null;
}
