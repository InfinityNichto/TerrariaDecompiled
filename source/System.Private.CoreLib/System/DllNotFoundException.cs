using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class DllNotFoundException : TypeLoadException
{
	public DllNotFoundException()
		: base(SR.Arg_DllNotFoundException)
	{
		base.HResult = -2146233052;
	}

	public DllNotFoundException(string? message)
		: base(message)
	{
		base.HResult = -2146233052;
	}

	public DllNotFoundException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233052;
	}

	protected DllNotFoundException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
