using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Threading;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class ThreadStartException : SystemException
{
	internal ThreadStartException()
		: base(SR.Arg_ThreadStartException)
	{
		base.HResult = -2146233051;
	}

	internal ThreadStartException(Exception reason)
		: base(SR.Arg_ThreadStartException, reason)
	{
		base.HResult = -2146233051;
	}

	private ThreadStartException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
