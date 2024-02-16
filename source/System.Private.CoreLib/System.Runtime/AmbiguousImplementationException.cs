using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Runtime;

[Serializable]
[TypeForwardedFrom("System.Runtime, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
public sealed class AmbiguousImplementationException : Exception
{
	public AmbiguousImplementationException()
		: base(SR.AmbiguousImplementationException_NullMessage)
	{
		base.HResult = -2146234262;
	}

	public AmbiguousImplementationException(string? message)
		: base(message)
	{
		base.HResult = -2146234262;
	}

	public AmbiguousImplementationException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146234262;
	}

	private AmbiguousImplementationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
