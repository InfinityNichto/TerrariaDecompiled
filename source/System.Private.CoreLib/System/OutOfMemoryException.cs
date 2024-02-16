using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class OutOfMemoryException : SystemException
{
	public OutOfMemoryException()
		: base(Exception.GetMessageFromNativeResources(ExceptionMessageKind.OutOfMemory))
	{
		base.HResult = -2147024882;
	}

	public OutOfMemoryException(string? message)
		: base(message)
	{
		base.HResult = -2147024882;
	}

	public OutOfMemoryException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147024882;
	}

	protected OutOfMemoryException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
