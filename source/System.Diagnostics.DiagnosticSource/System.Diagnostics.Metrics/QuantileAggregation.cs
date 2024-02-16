namespace System.Diagnostics.Metrics;

internal sealed class QuantileAggregation
{
	public double[] Quantiles { get; set; }

	public double MaxRelativeError { get; } = 0.001;


	public QuantileAggregation(params double[] quantiles)
	{
		Quantiles = quantiles;
		Array.Sort(Quantiles);
	}
}
