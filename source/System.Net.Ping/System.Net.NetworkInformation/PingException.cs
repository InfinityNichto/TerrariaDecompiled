using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Net.NetworkInformation;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class PingException : InvalidOperationException
{
	public PingException(string? message)
		: base(message)
	{
	}

	public PingException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	protected PingException(SerializationInfo serializationInfo, StreamingContext streamingContext)
		: base(serializationInfo, streamingContext)
	{
	}
}
