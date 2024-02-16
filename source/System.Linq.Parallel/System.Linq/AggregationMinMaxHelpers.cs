using System.Collections.Generic;
using System.Linq.Parallel;

namespace System.Linq;

internal static class AggregationMinMaxHelpers<T>
{
	private static T Reduce(IEnumerable<T> source, int sign)
	{
		Func<Pair<bool, T>, T, Pair<bool, T>> intermediateReduce = MakeIntermediateReduceFunction(sign);
		Func<Pair<bool, T>, Pair<bool, T>, Pair<bool, T>> finalReduce = MakeFinalReduceFunction(sign);
		Func<Pair<bool, T>, T> resultSelector = MakeResultSelectorFunction();
		AssociativeAggregationOperator<T, Pair<bool, T>, T> associativeAggregationOperator = new AssociativeAggregationOperator<T, Pair<bool, T>, T>(source, new Pair<bool, T>(first: false, default(T)), null, seedIsSpecified: true, intermediateReduce, finalReduce, resultSelector, default(T) != null, QueryAggregationOptions.AssociativeCommutative);
		return associativeAggregationOperator.Aggregate();
	}

	internal static T ReduceMin(IEnumerable<T> source)
	{
		return Reduce(source, -1);
	}

	internal static T ReduceMax(IEnumerable<T> source)
	{
		return Reduce(source, 1);
	}

	private static Func<Pair<bool, T>, T, Pair<bool, T>> MakeIntermediateReduceFunction(int sign)
	{
		Comparer<T> comparer = Util.GetDefaultComparer<T>();
		return (Pair<bool, T> accumulator, T element) => ((default(T) != null || element != null) && (!accumulator.First || Util.Sign(comparer.Compare(element, accumulator.Second)) == sign)) ? new Pair<bool, T>(first: true, element) : accumulator;
	}

	private static Func<Pair<bool, T>, Pair<bool, T>, Pair<bool, T>> MakeFinalReduceFunction(int sign)
	{
		Comparer<T> comparer = Util.GetDefaultComparer<T>();
		return (Pair<bool, T> accumulator, Pair<bool, T> element) => (element.First && (!accumulator.First || Util.Sign(comparer.Compare(element.Second, accumulator.Second)) == sign)) ? new Pair<bool, T>(first: true, element.Second) : accumulator;
	}

	private static Func<Pair<bool, T>, T> MakeResultSelectorFunction()
	{
		return (Pair<bool, T> accumulator) => accumulator.Second;
	}
}
