using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.IO;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class PathTooLongException : IOException
{
	public PathTooLongException()
		: base(SR.IO_PathTooLong)
	{
		base.HResult = -2147024690;
	}

	public PathTooLongException(string? message)
		: base(message)
	{
		base.HResult = -2147024690;
	}

	public PathTooLongException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147024690;
	}

	protected PathTooLongException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
