using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal abstract class InlinedAggregationOperatorEnumerator<TIntermediate> : QueryOperatorEnumerator<TIntermediate, int>
{
	private readonly int _partitionIndex;

	private bool _done;

	protected CancellationToken _cancellationToken;

	internal InlinedAggregationOperatorEnumerator(int partitionIndex, CancellationToken cancellationToken)
	{
		_partitionIndex = partitionIndex;
		_cancellationToken = cancellationToken;
	}

	internal sealed override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TIntermediate currentElement, ref int currentKey)
	{
		if (!_done && MoveNextCore(ref currentElement))
		{
			currentKey = _partitionIndex;
			_done = true;
			return true;
		}
		return false;
	}

	protected abstract bool MoveNextCore([MaybeNullWhen(false)][AllowNull] ref TIntermediate currentElement);
}
