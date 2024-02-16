namespace System.Diagnostics.Metrics;

internal sealed class LastValue : Aggregator
{
	private double? _lastValue;

	public override void Update(double value)
	{
		_lastValue = value;
	}

	public override IAggregationStatistics Collect()
	{
		lock (this)
		{
			LastValueStatistics result = new LastValueStatistics(_lastValue);
			_lastValue = null;
			return result;
		}
	}
}
