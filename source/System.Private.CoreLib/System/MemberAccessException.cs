using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class MemberAccessException : SystemException
{
	public MemberAccessException()
		: base(SR.Arg_AccessException)
	{
		base.HResult = -2146233062;
	}

	public MemberAccessException(string? message)
		: base(message)
	{
		base.HResult = -2146233062;
	}

	public MemberAccessException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233062;
	}

	protected MemberAccessException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
