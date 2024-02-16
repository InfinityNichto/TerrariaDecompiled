namespace System.Diagnostics.Metrics;

internal sealed class LastValueStatistics : IAggregationStatistics
{
	public double? LastValue { get; }

	internal LastValueStatistics(double? lastValue)
	{
		LastValue = lastValue;
	}
}
