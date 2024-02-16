using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Collections.Generic;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class KeyNotFoundException : SystemException
{
	public KeyNotFoundException()
		: base(SR.Arg_KeyNotFound)
	{
		base.HResult = -2146232969;
	}

	public KeyNotFoundException(string? message)
		: base(message)
	{
		base.HResult = -2146232969;
	}

	public KeyNotFoundException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146232969;
	}

	protected KeyNotFoundException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
