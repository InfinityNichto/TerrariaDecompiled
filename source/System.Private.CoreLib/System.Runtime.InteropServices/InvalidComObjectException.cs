using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class InvalidComObjectException : SystemException
{
	public InvalidComObjectException()
		: base(SR.Arg_InvalidComObjectException)
	{
		base.HResult = -2146233049;
	}

	public InvalidComObjectException(string? message)
		: base(message)
	{
		base.HResult = -2146233049;
	}

	public InvalidComObjectException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233049;
	}

	protected InvalidComObjectException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
