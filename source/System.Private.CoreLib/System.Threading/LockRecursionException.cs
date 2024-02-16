using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Threading;

[Serializable]
[TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class LockRecursionException : Exception
{
	public LockRecursionException()
	{
	}

	public LockRecursionException(string? message)
		: base(message)
	{
	}

	public LockRecursionException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	protected LockRecursionException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
