using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Parallel;

internal sealed class ArrayMergeHelper<TInputOutput> : IMergeHelper<TInputOutput>
{
	private readonly QueryResults<TInputOutput> _queryResults;

	private readonly TInputOutput[] _outputArray;

	private QuerySettings _settings;

	public ArrayMergeHelper(QuerySettings settings, QueryResults<TInputOutput> queryResults)
	{
		_settings = settings;
		_queryResults = queryResults;
		int count = _queryResults.Count;
		_outputArray = new TInputOutput[count];
	}

	private void ToArrayElement(int index)
	{
		_outputArray[index] = _queryResults[index];
	}

	public void Execute()
	{
		ParallelQuery<int> source = ParallelEnumerable.Range(0, _queryResults.Count);
		source = new QueryExecutionOption<int>(QueryOperator<int>.AsQueryOperator(source), _settings);
		source.ForAll(ToArrayElement);
	}

	[ExcludeFromCodeCoverage(Justification = "ArrayMergeHelper<>.GetEnumerator() is not intended to be used")]
	public IEnumerator<TInputOutput> GetEnumerator()
	{
		return ((IEnumerable<TInputOutput>)GetResultsAsArray()).GetEnumerator();
	}

	public TInputOutput[] GetResultsAsArray()
	{
		return _outputArray;
	}
}
