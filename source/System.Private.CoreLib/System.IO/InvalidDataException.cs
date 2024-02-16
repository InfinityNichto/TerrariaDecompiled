using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.IO;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class InvalidDataException : SystemException
{
	public InvalidDataException()
		: base(SR.GenericInvalidData)
	{
	}

	public InvalidDataException(string? message)
		: base(message)
	{
	}

	public InvalidDataException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	private InvalidDataException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
