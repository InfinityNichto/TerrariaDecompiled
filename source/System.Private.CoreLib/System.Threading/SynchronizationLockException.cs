using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Threading;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class SynchronizationLockException : SystemException
{
	public SynchronizationLockException()
		: base(SR.Arg_SynchronizationLockException)
	{
		base.HResult = -2146233064;
	}

	public SynchronizationLockException(string? message)
		: base(message)
	{
		base.HResult = -2146233064;
	}

	public SynchronizationLockException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233064;
	}

	protected SynchronizationLockException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
