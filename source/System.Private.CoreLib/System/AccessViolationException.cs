using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class AccessViolationException : SystemException
{
	private IntPtr _ip;

	private IntPtr _target;

	private int _accessType;

	public AccessViolationException()
		: base(SR.Arg_AccessViolationException)
	{
		base.HResult = -2147467261;
	}

	public AccessViolationException(string? message)
		: base(message)
	{
		base.HResult = -2147467261;
	}

	public AccessViolationException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147467261;
	}

	protected AccessViolationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
