namespace System.Net.Quic;

public class QuicException : Exception
{
	public QuicException(string? message)
		: base(message)
	{
	}

	public QuicException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	public QuicException(string? message, Exception? innerException, int result)
		: base(message, innerException)
	{
		base.HResult = result;
	}
}
