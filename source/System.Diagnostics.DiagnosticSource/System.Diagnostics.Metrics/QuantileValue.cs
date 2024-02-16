namespace System.Diagnostics.Metrics;

internal readonly struct QuantileValue
{
	public double Quantile { get; }

	public double Value { get; }

	public QuantileValue(double quantile, double value)
	{
		Quantile = quantile;
		Value = value;
	}
}
