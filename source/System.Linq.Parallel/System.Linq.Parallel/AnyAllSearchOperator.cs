using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class AnyAllSearchOperator<TInput> : UnaryQueryOperator<TInput, bool>
{
	private sealed class AnyAllSearchOperatorEnumerator<TKey> : QueryOperatorEnumerator<bool, int>
	{
		private readonly QueryOperatorEnumerator<TInput, TKey> _source;

		private readonly Func<TInput, bool> _predicate;

		private readonly bool _qualification;

		private readonly int _partitionIndex;

		private readonly Shared<bool> _resultFoundFlag;

		private readonly CancellationToken _cancellationToken;

		internal AnyAllSearchOperatorEnumerator(QueryOperatorEnumerator<TInput, TKey> source, bool qualification, Func<TInput, bool> predicate, int partitionIndex, Shared<bool> resultFoundFlag, CancellationToken cancellationToken)
		{
			_source = source;
			_qualification = qualification;
			_predicate = predicate;
			_partitionIndex = partitionIndex;
			_resultFoundFlag = resultFoundFlag;
			_cancellationToken = cancellationToken;
		}

		internal override bool MoveNext(ref bool currentElement, ref int currentKey)
		{
			if (_resultFoundFlag.Value)
			{
				return false;
			}
			TInput currentElement2 = default(TInput);
			TKey currentKey2 = default(TKey);
			if (_source.MoveNext(ref currentElement2, ref currentKey2))
			{
				currentElement = !_qualification;
				currentKey = _partitionIndex;
				int num = 0;
				do
				{
					if ((num++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					if (_resultFoundFlag.Value)
					{
						return false;
					}
					if (_predicate(currentElement2) == _qualification)
					{
						_resultFoundFlag.Value = true;
						currentElement = _qualification;
						break;
					}
				}
				while (_source.MoveNext(ref currentElement2, ref currentKey2));
				return true;
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			_source.Dispose();
		}
	}

	private readonly Func<TInput, bool> _predicate;

	private readonly bool _qualification;

	internal override bool LimitsParallelism => false;

	internal AnyAllSearchOperator(IEnumerable<TInput> child, bool qualification, Func<TInput, bool> predicate)
		: base(child)
	{
		_qualification = qualification;
		_predicate = predicate;
	}

	internal bool Aggregate()
	{
		using (IEnumerator<bool> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true))
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current == _qualification)
				{
					return _qualification;
				}
			}
		}
		return !_qualification;
	}

	internal override QueryResults<bool> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TInput> childQueryResults = base.Child.Open(settings, preferStriping);
		return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
	}

	internal override void WrapPartitionedStream<TKey>(PartitionedStream<TInput, TKey> inputStream, IPartitionedStreamRecipient<bool> recipient, bool preferStriping, QuerySettings settings)
	{
		Shared<bool> resultFoundFlag = new Shared<bool>(value: false);
		int partitionCount = inputStream.PartitionCount;
		PartitionedStream<bool, int> partitionedStream = new PartitionedStream<bool, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Correct);
		for (int i = 0; i < partitionCount; i++)
		{
			partitionedStream[i] = new AnyAllSearchOperatorEnumerator<TKey>(inputStream[i], _qualification, _predicate, i, resultFoundFlag, settings.CancellationState.MergedCancellationToken);
		}
		recipient.Receive(partitionedStream);
	}

	[ExcludeFromCodeCoverage(Justification = "This method should never be called as it is an ending operator with LimitsParallelism=false")]
	internal override IEnumerable<bool> AsSequentialQuery(CancellationToken token)
	{
		throw new NotSupportedException();
	}
}
