using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class IndexOutOfRangeException : SystemException
{
	public IndexOutOfRangeException()
		: base(SR.Arg_IndexOutOfRangeException)
	{
		base.HResult = -2146233080;
	}

	public IndexOutOfRangeException(string? message)
		: base(message)
	{
		base.HResult = -2146233080;
	}

	public IndexOutOfRangeException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233080;
	}

	private IndexOutOfRangeException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
