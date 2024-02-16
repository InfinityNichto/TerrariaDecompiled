using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class ContainsSearchOperator<TInput> : UnaryQueryOperator<TInput, bool>
{
	private sealed class ContainsSearchOperatorEnumerator<TKey> : QueryOperatorEnumerator<bool, int>
	{
		private readonly QueryOperatorEnumerator<TInput, TKey> _source;

		private readonly TInput _searchValue;

		private readonly IEqualityComparer<TInput> _comparer;

		private readonly int _partitionIndex;

		private readonly Shared<bool> _resultFoundFlag;

		private readonly CancellationToken _cancellationToken;

		internal ContainsSearchOperatorEnumerator(QueryOperatorEnumerator<TInput, TKey> source, TInput searchValue, IEqualityComparer<TInput> comparer, int partitionIndex, Shared<bool> resultFoundFlag, CancellationToken cancellationToken)
		{
			_source = source;
			_searchValue = searchValue;
			_comparer = comparer;
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
				currentElement = false;
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
					if (_comparer.Equals(currentElement2, _searchValue))
					{
						_resultFoundFlag.Value = true;
						currentElement = true;
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

	private readonly TInput _searchValue;

	private readonly IEqualityComparer<TInput> _comparer;

	internal override bool LimitsParallelism => false;

	internal ContainsSearchOperator(IEnumerable<TInput> child, TInput searchValue, IEqualityComparer<TInput> comparer)
		: base(child)
	{
		_searchValue = searchValue;
		if (comparer == null)
		{
			_comparer = EqualityComparer<TInput>.Default;
		}
		else
		{
			_comparer = comparer;
		}
	}

	internal bool Aggregate()
	{
		using (IEnumerator<bool> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true))
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current)
				{
					return true;
				}
			}
		}
		return false;
	}

	internal override QueryResults<bool> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TInput> childQueryResults = base.Child.Open(settings, preferStriping);
		return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
	}

	internal override void WrapPartitionedStream<TKey>(PartitionedStream<TInput, TKey> inputStream, IPartitionedStreamRecipient<bool> recipient, bool preferStriping, QuerySettings settings)
	{
		int partitionCount = inputStream.PartitionCount;
		PartitionedStream<bool, int> partitionedStream = new PartitionedStream<bool, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Correct);
		Shared<bool> resultFoundFlag = new Shared<bool>(value: false);
		for (int i = 0; i < partitionCount; i++)
		{
			partitionedStream[i] = new ContainsSearchOperatorEnumerator<TKey>(inputStream[i], _searchValue, _comparer, i, resultFoundFlag, settings.CancellationState.MergedCancellationToken);
		}
		recipient.Receive(partitionedStream);
	}

	[ExcludeFromCodeCoverage(Justification = "This method should never be called as it is an ending operator with LimitsParallelism=false")]
	internal override IEnumerable<bool> AsSequentialQuery(CancellationToken token)
	{
		throw new NotSupportedException();
	}
}
