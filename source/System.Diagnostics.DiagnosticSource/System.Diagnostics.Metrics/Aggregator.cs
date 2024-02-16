namespace System.Diagnostics.Metrics;

internal abstract class Aggregator
{
	public abstract void Update(double measurement);

	public abstract IAggregationStatistics Collect();
}
