using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class NullReferenceException : SystemException
{
	public NullReferenceException()
		: base(SR.Arg_NullReferenceException)
	{
		base.HResult = -2147467261;
	}

	public NullReferenceException(string? message)
		: base(message)
	{
		base.HResult = -2147467261;
	}

	public NullReferenceException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147467261;
	}

	protected NullReferenceException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
