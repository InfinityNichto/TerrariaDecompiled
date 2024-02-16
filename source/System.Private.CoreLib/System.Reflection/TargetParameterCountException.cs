using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Reflection;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class TargetParameterCountException : ApplicationException
{
	public TargetParameterCountException()
		: base(SR.Arg_TargetParameterCountException)
	{
		base.HResult = -2147352562;
	}

	public TargetParameterCountException(string? message)
		: base(message)
	{
		base.HResult = -2147352562;
	}

	public TargetParameterCountException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2147352562;
	}

	private TargetParameterCountException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
