using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[Obsolete("ExecutionEngineException previously indicated an unspecified fatal error in the runtime. The runtime no longer raises this exception so this type is obsolete.")]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class ExecutionEngineException : SystemException
{
	public ExecutionEngineException()
		: base(SR.Arg_ExecutionEngineException)
	{
		base.HResult = -2146233082;
	}

	public ExecutionEngineException(string? message)
		: base(message)
	{
		base.HResult = -2146233082;
	}

	public ExecutionEngineException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233082;
	}

	private ExecutionEngineException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
