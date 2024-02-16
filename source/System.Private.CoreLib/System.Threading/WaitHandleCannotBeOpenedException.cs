using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Threading;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class WaitHandleCannotBeOpenedException : ApplicationException
{
	public WaitHandleCannotBeOpenedException()
		: base(SR.Threading_WaitHandleCannotBeOpenedException)
	{
		base.HResult = -2146233044;
	}

	public WaitHandleCannotBeOpenedException(string? message)
		: base(message)
	{
		base.HResult = -2146233044;
	}

	public WaitHandleCannotBeOpenedException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233044;
	}

	protected WaitHandleCannotBeOpenedException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
