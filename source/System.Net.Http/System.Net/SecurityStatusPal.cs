namespace System.Net;

internal readonly struct SecurityStatusPal
{
	public readonly System.Net.SecurityStatusPalErrorCode ErrorCode;

	public readonly Exception Exception;

	public SecurityStatusPal(System.Net.SecurityStatusPalErrorCode errorCode, Exception exception = null)
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
