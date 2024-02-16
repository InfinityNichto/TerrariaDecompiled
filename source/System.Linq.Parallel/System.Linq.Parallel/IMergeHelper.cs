using System.Collections.Generic;

namespace System.Linq.Parallel;

internal interface IMergeHelper<TInputOutput>
{
	void Execute();

	IEnumerator<TInputOutput> GetEnumerator();

	TInputOutput[] GetResultsAsArray();
}
