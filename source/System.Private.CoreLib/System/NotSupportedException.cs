using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class NotSupportedException : SystemException
{
	public NotSupportedException()
		: base(SR.Arg_NotSupportedException)
	{
		base.HResult = -2146233067;
	}

	public NotSupportedException(string? message)
		: base(message)
	{
		base.HResult = -2146233067;
	}

	public NotSupportedException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233067;
	}

	protected NotSupportedException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
