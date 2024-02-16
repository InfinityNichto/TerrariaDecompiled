using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class PlatformNotSupportedException : NotSupportedException
{
	public PlatformNotSupportedException()
		: base(SR.Arg_PlatformNotSupported)
	{
		base.HResult = -2146233031;
	}

	public PlatformNotSupportedException(string? message)
		: base(message)
	{
		base.HResult = -2146233031;
	}

	public PlatformNotSupportedException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233031;
	}

	protected PlatformNotSupportedException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
