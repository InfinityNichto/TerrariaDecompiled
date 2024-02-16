using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class TypeAccessException : TypeLoadException
{
	public TypeAccessException()
		: base(SR.Arg_TypeAccessException)
	{
		base.HResult = -2146233021;
	}

	public TypeAccessException(string? message)
		: base(message)
	{
		base.HResult = -2146233021;
	}

	public TypeAccessException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233021;
	}

	protected TypeAccessException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
