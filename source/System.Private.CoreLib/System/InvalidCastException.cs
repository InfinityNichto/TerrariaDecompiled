using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class InvalidCastException : SystemException
{
	public InvalidCastException()
		: base(SR.Arg_InvalidCastException)
	{
		base.HResult = -2147467262;
	}

	public InvalidCastException(string? message)
		: base(message)
	{
		base.HResult = -2147467262;
	}

	public InvalidCastException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147467262;
	}

	public InvalidCastException(string? message, int errorCode)
		: base(message)
	{
		base.HResult = errorCode;
	}

	protected InvalidCastException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
