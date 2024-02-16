using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ContextMarshalException : SystemException
{
	public ContextMarshalException()
		: this(SR.Arg_ContextMarshalException, null)
	{
	}

	public ContextMarshalException(string? message)
		: this(message, null)
	{
	}

	public ContextMarshalException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233084;
	}

	protected ContextMarshalException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
