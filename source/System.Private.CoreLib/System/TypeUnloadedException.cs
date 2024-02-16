using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class TypeUnloadedException : SystemException
{
	public TypeUnloadedException()
		: base(SR.Arg_TypeUnloadedException)
	{
		base.HResult = -2146234349;
	}

	public TypeUnloadedException(string? message)
		: base(message)
	{
		base.HResult = -2146234349;
	}

	public TypeUnloadedException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146234349;
	}

	protected TypeUnloadedException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
