using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Threading;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ThreadStateException : SystemException
{
	public ThreadStateException()
		: base(SR.Arg_ThreadStateException)
	{
		base.HResult = -2146233056;
	}

	public ThreadStateException(string? message)
		: base(message)
	{
		base.HResult = -2146233056;
	}

	public ThreadStateException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233056;
	}

	protected ThreadStateException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
