using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Security.Authentication;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class AuthenticationException : SystemException
{
	public AuthenticationException()
	{
	}

	public AuthenticationException(string? message)
		: base(message)
	{
	}

	public AuthenticationException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	protected AuthenticationException(SerializationInfo serializationInfo, StreamingContext streamingContext)
		: base(serializationInfo, streamingContext)
	{
	}
}
