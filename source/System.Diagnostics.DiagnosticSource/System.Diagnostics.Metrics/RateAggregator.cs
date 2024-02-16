namespace System.Diagnostics.Metrics;

internal sealed class RateAggregator : Aggregator
{
	private double? _prevValue;

	private double _value;

	public override void Update(double value)
	{
		lock (this)
		{
			_value = value;
		}
	}

	public override IAggregationStatistics Collect()
	{
		lock (this)
		{
			double? delta = null;
			if (_prevValue.HasValue)
			{
				delta = _value - _prevValue.Value;
			}
			RateStatistics result = new RateStatistics(delta);
			_prevValue = _value;
			return result;
		}
	}
}
