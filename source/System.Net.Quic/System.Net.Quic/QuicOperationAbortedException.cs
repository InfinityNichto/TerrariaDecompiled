namespace System.Net.Quic;

public class QuicOperationAbortedException : QuicException
{
	internal QuicOperationAbortedException()
		: base(System.SR.net_quic_operationaborted)
	{
	}

	public QuicOperationAbortedException(string message)
		: base(message)
	{
	}
}
