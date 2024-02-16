using System.Collections.Generic;

namespace System.Linq.Parallel;

internal sealed class ProducerComparerInt : IComparer<Producer<int>>
{
	public static readonly ProducerComparerInt Instance = new ProducerComparerInt();

	private ProducerComparerInt()
	{
	}

	public int Compare(Producer<int> x, Producer<int> y)
	{
		return y.MaxKey - x.MaxKey;
	}
}
