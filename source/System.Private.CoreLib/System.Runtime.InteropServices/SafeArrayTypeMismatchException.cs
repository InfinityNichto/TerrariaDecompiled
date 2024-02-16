using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class SafeArrayTypeMismatchException : SystemException
{
	public SafeArrayTypeMismatchException()
		: base(SR.Arg_SafeArrayTypeMismatchException)
	{
		base.HResult = -2146233037;
	}

	public SafeArrayTypeMismatchException(string? message)
		: base(message)
	{
		base.HResult = -2146233037;
	}

	public SafeArrayTypeMismatchException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233037;
	}

	protected SafeArrayTypeMismatchException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
