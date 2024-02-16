using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Security.Cryptography;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class CryptographicException : SystemException
{
	public CryptographicException()
		: base(SR.Arg_CryptographyException)
	{
	}

	public CryptographicException(int hr)
		: base(SR.Arg_CryptographyException)
	{
		base.HResult = hr;
	}

	public CryptographicException(string? message)
		: base(message)
	{
	}

	public CryptographicException(string? message, Exception? inner)
		: base(message, inner)
	{
	}

	public CryptographicException(string format, string? insert)
		: base(string.Format(format, insert))
	{
	}

	protected CryptographicException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
