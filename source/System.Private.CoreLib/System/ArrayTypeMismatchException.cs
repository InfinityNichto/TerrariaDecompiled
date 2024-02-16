using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ArrayTypeMismatchException : SystemException
{
	public ArrayTypeMismatchException()
		: base(SR.Arg_ArrayTypeMismatchException)
	{
		base.HResult = -2146233085;
	}

	public ArrayTypeMismatchException(string? message)
		: base(message)
	{
		base.HResult = -2146233085;
	}

	public ArrayTypeMismatchException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233085;
	}

	protected ArrayTypeMismatchException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
