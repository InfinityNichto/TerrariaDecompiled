using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class DivideByZeroException : ArithmeticException
{
	public DivideByZeroException()
		: base(SR.Arg_DivideByZero)
	{
		base.HResult = -2147352558;
	}

	public DivideByZeroException(string? message)
		: base(message)
	{
		base.HResult = -2147352558;
	}

	public DivideByZeroException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147352558;
	}

	protected DivideByZeroException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
