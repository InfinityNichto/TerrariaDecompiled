namespace System.Net;

internal readonly struct SecurityStatusPal
{
	public readonly SecurityStatusPalErrorCode ErrorCode;

	public readonly Exception Exception;

	public SecurityStatusPal(SecurityStatusPalErrorCode errorCode, Exception exception = null)
	{
		ErrorCode = errorCode;
		Exception = exception;
	}

	public override string ToString()
	{
		if (Exception == null)
		{
			return $"{"ErrorCode"}={ErrorCode}";
		}
		return $"{"ErrorCode"}={ErrorCode}, {"Exception"}={Exception}";
	}
}
