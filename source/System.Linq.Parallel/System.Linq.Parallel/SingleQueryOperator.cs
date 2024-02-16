using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class SingleQueryOperator<TSource> : UnaryQueryOperator<TSource, TSource>
{
	private sealed class SingleQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TSource, int>
	{
		private readonly QueryOperatorEnumerator<TSource, TKey> _source;

		private readonly Func<TSource, bool> _predicate;

		private bool _alreadySearched;

		private bool _yieldExtra;

		private readonly Shared<int> _totalElementCount;

		internal SingleQueryOperatorEnumerator(QueryOperatorEnumerator<TSource, TKey> source, Func<TSource, bool> predicate, Shared<int> totalElementCount)
		{
			_source = source;
			_predicate = predicate;
			_totalElementCount = totalElementCount;
		}

		internal override bool MoveNext([AllowNull] ref TSource currentElement, ref int currentKey)
		{
			if (_alreadySearched)
			{
				if (_yieldExtra)
				{
					_yieldExtra = false;
					currentElement = default(TSource);
					currentKey = 0;
					return true;
				}
				return false;
			}
			bool flag = false;
			TSource currentElement2 = default(TSource);
			TKey currentKey2 = default(TKey);
			while (_source.MoveNext(ref currentElement2, ref currentKey2))
			{
				if (_predicate == null || _predicate(currentElement2))
				{
					Interlocked.Increment(ref _totalElementCount.Value);
					currentElement = currentElement2;
					currentKey = 0;
					if (flag)
					{
						_yieldExtra = true;
						break;
					}
					flag = true;
				}
				if (Volatile.Read(ref _totalElementCount.Value) > 1)
				{
					break;
				}
			}
			_alreadySearched = true;
			return flag;
		}

		protected override void Dispose(bool disposing)
		{
			_source.Dispose();
		}
	}

	private readonly Func<TSource, bool> _predicate;

	internal override bool LimitsParallelism => false;

	internal SingleQueryOperator(IEnumerable<TSource> child, Func<TSource, bool> predicate)
		: base(child)
	{
		_predicate = predicate;
	}

	internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TSource> childQueryResults = base.Child.Open(settings, preferStriping: false);
		return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
	}

	internal override void WrapPartitionedStream<TKey>(PartitionedStream<TSource, TKey> inputStream, IPartitionedStreamRecipient<TSource> recipient, bool preferStriping, QuerySettings settings)
	{
		int partitionCount = inputStream.PartitionCount;
		PartitionedStream<TSource, int> partitionedStream = new PartitionedStream<TSource, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Shuffled);
		Shared<int> totalElementCount = new Shared<int>(0);
		for (int i = 0; i < partitionCount; i++)
		{
			partitionedStream[i] = new SingleQueryOperatorEnumerator<TKey>(inputStream[i], _predicate, totalElementCount);
		}
		recipient.Receive(partitionedStream);
	}

	[ExcludeFromCodeCoverage(Justification = "This method should never be called as it is an ending operator with LimitsParallelism=false")]
	internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
	{
		throw new NotSupportedException();
	}
}
