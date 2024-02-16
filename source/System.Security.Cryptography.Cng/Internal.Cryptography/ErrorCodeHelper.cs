using System.Security.Cryptography;

namespace Internal.Cryptography;

internal static class ErrorCodeHelper
{
	public static CryptographicException ToCryptographicException(this global::Interop.NCrypt.ErrorCode errorCode)
	{
		return ((int)errorCode).ToCryptographicException();
	}
}
