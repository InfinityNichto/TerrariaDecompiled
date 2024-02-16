using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Security.Cryptography;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class CryptographicUnexpectedOperationException : CryptographicException
{
	public CryptographicUnexpectedOperationException()
		: base(System.SR.Arg_CryptographyException)
	{
	}

	public CryptographicUnexpectedOperationException(string? message)
		: base(message)
	{
	}

	public CryptographicUnexpectedOperationException(string? message, Exception? inner)
		: base(message, inner)
	{
	}

	public CryptographicUnexpectedOperationException(string format, string? insert)
		: base(string.Format(CultureInfo.CurrentCulture, format, insert))
	{
	}

	protected CryptographicUnexpectedOperationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
