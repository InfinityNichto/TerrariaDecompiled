using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization;

[Serializable]
[TypeForwardedFrom("System.Runtime.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class InvalidDataContractException : Exception
{
	public InvalidDataContractException()
	{
	}

	public InvalidDataContractException(string? message)
		: base(message)
	{
	}

	public InvalidDataContractException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	protected InvalidDataContractException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
