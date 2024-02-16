using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class SelectQueryOperator<TInput, TOutput> : UnaryQueryOperator<TInput, TOutput>
{
	private sealed class SelectQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TOutput, TKey>
	{
		private readonly QueryOperatorEnumerator<TInput, TKey> _source;

		private readonly Func<TInput, TOutput> _selector;

		internal SelectQueryOperatorEnumerator(QueryOperatorEnumerator<TInput, TKey> source, Func<TInput, TOutput> selector)
		{
			_source = source;
			_selector = selector;
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TOutput currentElement, [AllowNull] ref TKey currentKey)
		{
			TInput currentElement2 = default(TInput);
			if (_source.MoveNext(ref currentElement2, ref currentKey))
			{
				currentElement = _selector(currentElement2);
				return true;
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			_source.Dispose();
		}
	}

	private sealed class SelectQueryOperatorResults : UnaryQueryOperatorResults
	{
		private readonly Func<TInput, TOutput> _selector;

		private readonly int _childCount;

		internal override bool IsIndexible => true;

		internal override int ElementsCount => _childCount;

		public static QueryResults<TOutput> NewResults(QueryResults<TInput> childQueryResults, SelectQueryOperator<TInput, TOutput> op, QuerySettings settings, bool preferStriping)
		{
			if (childQueryResults.IsIndexible)
			{
				return new SelectQueryOperatorResults(childQueryResults, op, settings, preferStriping);
			}
			return new UnaryQueryOperatorResults(childQueryResults, op, settings, preferStriping);
		}

		private SelectQueryOperatorResults(QueryResults<TInput> childQueryResults, SelectQueryOperator<TInput, TOutput> op, QuerySettings settings, bool preferStriping)
			: base(childQueryResults, (UnaryQueryOperator<TInput, TOutput>)op, settings, preferStriping)
		{
			_selector = op._selector;
			_childCount = _childQueryResults.ElementsCount;
		}

		internal override TOutput GetElement(int index)
		{
			return _selector(_childQueryResults.GetElement(index));
		}
	}

	private readonly Func<TInput, TOutput> _selector;

	internal override bool LimitsParallelism => false;

	internal SelectQueryOperator(IEnumerable<TInput> child, Func<TInput, TOutput> selector)
		: base(child)
	{
		_selector = selector;
		SetOrdinalIndexState(base.Child.OrdinalIndexState);
	}

	internal override void WrapPartitionedStream<TKey>(PartitionedStream<TInput, TKey> inputStream, IPartitionedStreamRecipient<TOutput> recipient, bool preferStriping, QuerySettings settings)
	{
		PartitionedStream<TOutput, TKey> partitionedStream = new PartitionedStream<TOutput, TKey>(inputStream.PartitionCount, inputStream.KeyComparer, OrdinalIndexState);
		for (int i = 0; i < inputStream.PartitionCount; i++)
		{
			partitionedStream[i] = new SelectQueryOperatorEnumerator<TKey>(inputStream[i], _selector);
		}
		recipient.Receive(partitionedStream);
	}

	internal override QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TInput> childQueryResults = base.Child.Open(settings, preferStriping);
		return SelectQueryOperatorResults.NewResults(childQueryResults, this, settings, preferStriping);
	}

	internal override IEnumerable<TOutput> AsSequentialQuery(CancellationToken token)
	{
		return base.Child.AsSequentialQuery(token).Select(_selector);
	}
}
