namespace System.Diagnostics.Metrics;

internal sealed class RateSumAggregator : Aggregator
{
	private double _sum;

	public override void Update(double value)
	{
		lock (this)
		{
			_sum += value;
		}
	}

	public override IAggregationStatistics Collect()
	{
		lock (this)
		{
			RateStatistics result = new RateStatistics(_sum);
			_sum = 0.0;
			return result;
		}
	}
}
