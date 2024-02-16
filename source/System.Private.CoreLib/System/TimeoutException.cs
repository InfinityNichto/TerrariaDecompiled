using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class TimeoutException : SystemException
{
	public TimeoutException()
		: base(SR.Arg_TimeoutException)
	{
		base.HResult = -2146233083;
	}

	public TimeoutException(string? message)
		: base(message)
	{
		base.HResult = -2146233083;
	}

	public TimeoutException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233083;
	}

	protected TimeoutException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
