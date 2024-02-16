using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class DuplicateWaitObjectException : ArgumentException
{
	public DuplicateWaitObjectException()
		: base(SR.Arg_DuplicateWaitObjectException)
	{
		base.HResult = -2146233047;
	}

	public DuplicateWaitObjectException(string? parameterName)
		: base(SR.Arg_DuplicateWaitObjectException, parameterName)
	{
		base.HResult = -2146233047;
	}

	public DuplicateWaitObjectException(string? parameterName, string? message)
		: base(message, parameterName)
	{
		base.HResult = -2146233047;
	}

	public DuplicateWaitObjectException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233047;
	}

	protected DuplicateWaitObjectException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
