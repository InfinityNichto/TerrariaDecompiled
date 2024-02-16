using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class SafeArrayRankMismatchException : SystemException
{
	public SafeArrayRankMismatchException()
		: base(SR.Arg_SafeArrayRankMismatchException)
	{
		base.HResult = -2146233032;
	}

	public SafeArrayRankMismatchException(string? message)
		: base(message)
	{
		base.HResult = -2146233032;
	}

	public SafeArrayRankMismatchException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233032;
	}

	protected SafeArrayRankMismatchException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
