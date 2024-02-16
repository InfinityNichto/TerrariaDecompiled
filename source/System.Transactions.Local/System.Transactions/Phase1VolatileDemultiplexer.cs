namespace System.Transactions;

internal sealed class Phase1VolatileDemultiplexer : VolatileDemultiplexer
{
	public Phase1VolatileDemultiplexer(InternalTransaction transaction)
		: base(transaction)
	{
	}
}
