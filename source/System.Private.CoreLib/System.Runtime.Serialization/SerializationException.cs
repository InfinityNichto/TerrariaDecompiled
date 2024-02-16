using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class SerializationException : SystemException
{
	public SerializationException()
		: base(SR.SerializationException)
	{
		base.HResult = -2146233076;
	}

	public SerializationException(string? message)
		: base(message)
	{
		base.HResult = -2146233076;
	}

	public SerializationException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233076;
	}

	protected SerializationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
