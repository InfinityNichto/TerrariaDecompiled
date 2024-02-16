namespace System.Transactions;

internal struct VolatileEnlistmentSet
{
	internal InternalEnlistment[] _volatileEnlistments;

	internal int _volatileEnlistmentCount;

	internal int _volatileEnlistmentSize;

	internal int _dependentClones;

	internal int _preparedVolatileEnlistments;

	private VolatileDemultiplexer _volatileDemux;

	internal VolatileDemultiplexer VolatileDemux
	{
		get
		{
			return _volatileDemux;
		}
		set
		{
			_volatileDemux = value;
		}
	}
}
