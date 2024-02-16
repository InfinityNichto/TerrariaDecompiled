using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class SystemException : Exception
{
	public SystemException()
		: base(SR.Arg_SystemException)
	{
		base.HResult = -2146233087;
	}

	public SystemException(string? message)
		: base(message)
	{
		base.HResult = -2146233087;
	}

	public SystemException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233087;
	}

	protected SystemException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
