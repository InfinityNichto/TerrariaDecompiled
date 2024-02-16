using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class InsufficientMemoryException : OutOfMemoryException
{
	public InsufficientMemoryException()
		: base(Exception.GetMessageFromNativeResources(ExceptionMessageKind.OutOfMemory))
	{
		base.HResult = -2146233027;
	}

	public InsufficientMemoryException(string? message)
		: base(message)
	{
		base.HResult = -2146233027;
	}

	public InsufficientMemoryException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233027;
	}

	private InsufficientMemoryException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
