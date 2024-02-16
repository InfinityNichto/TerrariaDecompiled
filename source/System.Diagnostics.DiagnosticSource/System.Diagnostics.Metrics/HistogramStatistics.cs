namespace System.Diagnostics.Metrics;

internal sealed class HistogramStatistics : IAggregationStatistics
{
	public QuantileValue[] Quantiles { get; }

	internal HistogramStatistics(QuantileValue[] quantiles)
	{
		Quantiles = quantiles;
	}
}
