using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class OverflowException : ArithmeticException
{
	public OverflowException()
		: base(SR.Arg_OverflowException)
	{
		base.HResult = -2146233066;
	}

	public OverflowException(string? message)
		: base(message)
	{
		base.HResult = -2146233066;
	}

	public OverflowException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233066;
	}

	protected OverflowException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
