using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class AppDomainUnloadedException : SystemException
{
	public AppDomainUnloadedException()
		: base(SR.Arg_AppDomainUnloadedException)
	{
		base.HResult = -2146234348;
	}

	public AppDomainUnloadedException(string? message)
		: base(message)
	{
		base.HResult = -2146234348;
	}

	public AppDomainUnloadedException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146234348;
	}

	protected AppDomainUnloadedException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
