namespace System.Collections.Generic;

internal sealed class ComparisonComparer<T> : Comparer<T>
{
	private readonly Comparison<T> _comparison;

	public ComparisonComparer(Comparison<T> comparison)
	{
		_comparison = comparison;
	}

	public override int Compare(T x, T y)
	{
		return _comparison(x, y);
	}
}
