using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class MulticastNotSupportedException : SystemException
{
	public MulticastNotSupportedException()
		: base(SR.Arg_MulticastNotSupportedException)
	{
		base.HResult = -2146233068;
	}

	public MulticastNotSupportedException(string? message)
		: base(message)
	{
		base.HResult = -2146233068;
	}

	public MulticastNotSupportedException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233068;
	}

	private MulticastNotSupportedException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
