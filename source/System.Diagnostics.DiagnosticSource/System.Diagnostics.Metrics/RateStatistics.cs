namespace System.Diagnostics.Metrics;

internal sealed class RateStatistics : IAggregationStatistics
{
	public double? Delta { get; }

	public RateStatistics(double? delta)
	{
		Delta = delta;
	}
}
