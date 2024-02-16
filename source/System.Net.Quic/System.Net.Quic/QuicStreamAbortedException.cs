namespace System.Net.Quic;

public class QuicStreamAbortedException : QuicException
{
	public long ErrorCode { get; }

	internal QuicStreamAbortedException(long errorCode)
		: this(System.SR.Format(System.SR.net_quic_streamaborted, errorCode), errorCode)
	{
	}

	public QuicStreamAbortedException(string message, long errorCode)
		: base(message)
	{
		ErrorCode = errorCode;
	}
}
