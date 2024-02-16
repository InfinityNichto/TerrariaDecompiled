namespace System.Runtime.Serialization;

internal sealed class SurrogateForCyclicalReference : ISerializationSurrogate
{
	private readonly ISerializationSurrogate _innerSurrogate;

	internal SurrogateForCyclicalReference(ISerializationSurrogate innerSurrogate)
	{
		_innerSurrogate = innerSurrogate;
	}

	public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
	{
		_innerSurrogate.GetObjectData(obj, info, context);
	}

	public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
	{
		return _innerSurrogate.SetObjectData(obj, info, context, selector);
	}
}
