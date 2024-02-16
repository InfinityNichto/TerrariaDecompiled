using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class FieldAccessException : MemberAccessException
{
	public FieldAccessException()
		: base(SR.Arg_FieldAccessException)
	{
		base.HResult = -2146233081;
	}

	public FieldAccessException(string? message)
		: base(message)
	{
		base.HResult = -2146233081;
	}

	public FieldAccessException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233081;
	}

	protected FieldAccessException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
