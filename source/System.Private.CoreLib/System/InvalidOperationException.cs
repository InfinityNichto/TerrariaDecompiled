using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class InvalidOperationException : SystemException
{
	public InvalidOperationException()
		: base(SR.Arg_InvalidOperationException)
	{
		base.HResult = -2146233079;
	}

	public InvalidOperationException(string? message)
		: base(message)
	{
		base.HResult = -2146233079;
	}

	public InvalidOperationException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233079;
	}

	protected InvalidOperationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
