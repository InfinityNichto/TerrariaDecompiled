using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Resources;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class MissingManifestResourceException : SystemException
{
	public MissingManifestResourceException()
		: base(SR.Arg_MissingManifestResourceException)
	{
		base.HResult = -2146233038;
	}

	public MissingManifestResourceException(string? message)
		: base(message)
	{
		base.HResult = -2146233038;
	}

	public MissingManifestResourceException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233038;
	}

	protected MissingManifestResourceException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
