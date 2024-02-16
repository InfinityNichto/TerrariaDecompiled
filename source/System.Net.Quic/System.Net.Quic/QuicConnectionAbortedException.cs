namespace System.Net.Quic;

public class QuicConnectionAbortedException : QuicException
{
	public long ErrorCode { get; }

	internal QuicConnectionAbortedException(long errorCode)
		: this(System.SR.Format(System.SR.net_quic_connectionaborted, errorCode), errorCode)
	{
	}

	public QuicConnectionAbortedException(string message, long errorCode)
		: base(message)
	{
		ErrorCode = errorCode;
	}
}
