namespace System.Transactions;

internal sealed class Phase0VolatileDemultiplexer : VolatileDemultiplexer
{
	public Phase0VolatileDemultiplexer(InternalTransaction transaction)
		: base(transaction)
	{
	}
}
