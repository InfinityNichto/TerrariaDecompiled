using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class MarshalDirectiveException : SystemException
{
	public MarshalDirectiveException()
		: base(SR.Arg_MarshalDirectiveException)
	{
		base.HResult = -2146233035;
	}

	public MarshalDirectiveException(string? message)
		: base(message)
	{
		base.HResult = -2146233035;
	}

	public MarshalDirectiveException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233035;
	}

	protected MarshalDirectiveException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
