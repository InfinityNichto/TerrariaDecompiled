using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ArithmeticException : SystemException
{
	public ArithmeticException()
		: base(SR.Arg_ArithmeticException)
	{
		base.HResult = -2147024362;
	}

	public ArithmeticException(string? message)
		: base(message)
	{
		base.HResult = -2147024362;
	}

	public ArithmeticException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147024362;
	}

	protected ArithmeticException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
