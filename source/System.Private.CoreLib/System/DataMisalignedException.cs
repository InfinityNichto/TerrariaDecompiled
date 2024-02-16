using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class DataMisalignedException : SystemException
{
	public DataMisalignedException()
		: base(SR.Arg_DataMisalignedException)
	{
		base.HResult = -2146233023;
	}

	public DataMisalignedException(string? message)
		: base(message)
	{
		base.HResult = -2146233023;
	}

	public DataMisalignedException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233023;
	}

	private DataMisalignedException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
