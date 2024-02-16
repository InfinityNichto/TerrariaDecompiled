namespace System.Net.Quic.Implementations.MsQuic;

internal static class ThrowHelper
{
	internal static Exception GetConnectionAbortedException(long errorCode)
	{
		if (errorCode == -1)
		{
			return new QuicOperationAbortedException();
		}
		return new QuicConnectionAbortedException(errorCode);
	}

	internal static Exception GetStreamAbortedException(long errorCode)
	{
		if (errorCode == -1)
		{
			return new QuicOperationAbortedException();
		}
		return new QuicStreamAbortedException(errorCode);
	}
}
