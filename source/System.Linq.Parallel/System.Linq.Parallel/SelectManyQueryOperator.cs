using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class SelectManyQueryOperator<TLeftInput, TRightInput, TOutput> : UnaryQueryOperator<TLeftInput, TOutput>
{
	private sealed class IndexedSelectManyQueryOperatorEnumerator : QueryOperatorEnumerator<TOutput, Pair<int, int>>
	{
		private sealed class Mutables
		{
			internal int _currentRightSourceIndex = -1;

			internal TLeftInput _currentLeftElement;

			internal int _currentLeftSourceIndex;

			internal int _lhsCount;
		}

		private readonly QueryOperatorEnumerator<TLeftInput, int> _leftSource;

		private readonly SelectManyQueryOperator<TLeftInput, TRightInput, TOutput> _selectManyOperator;

		private IEnumerator<TRightInput> _currentRightSource;

		private IEnumerator<TOutput> _currentRightSourceAsOutput;

		private Mutables _mutables;

		private readonly CancellationToken _cancellationToken;

		internal IndexedSelectManyQueryOperatorEnumerator(QueryOperatorEnumerator<TLeftInput, int> leftSource, SelectManyQueryOperator<TLeftInput, TRightInput, TOutput> selectManyOperator, CancellationToken cancellationToken)
		{
			_leftSource = leftSource;
			_selectManyOperator = selectManyOperator;
			_cancellationToken = cancellationToken;
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TOutput currentElement, ref Pair<int, int> currentKey)
		{
			while (true)
			{
				if (_currentRightSource == null)
				{
					_mutables = new Mutables();
					if ((_mutables._lhsCount++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					if (!_leftSource.MoveNext(ref _mutables._currentLeftElement, ref _mutables._currentLeftSourceIndex))
					{
						return false;
					}
					IEnumerable<TRightInput> enumerable = _selectManyOperator._indexedRightChildSelector(_mutables._currentLeftElement, _mutables._currentLeftSourceIndex);
					_currentRightSource = enumerable.GetEnumerator();
					if (_selectManyOperator._resultSelector == null)
					{
						_currentRightSourceAsOutput = (IEnumerator<TOutput>)_currentRightSource;
					}
				}
				if (_currentRightSource.MoveNext())
				{
					break;
				}
				_currentRightSource.Dispose();
				_currentRightSource = null;
				_currentRightSourceAsOutput = null;
			}
			_mutables._currentRightSourceIndex++;
			if (_selectManyOperator._resultSelector != null)
			{
				currentElement = _selectManyOperator._resultSelector(_mutables._currentLeftElement, _currentRightSource.Current);
			}
			else
			{
				currentElement = _currentRightSourceAsOutput.Current;
			}
			currentKey = new Pair<int, int>(_mutables._currentLeftSourceIndex, _mutables._currentRightSourceIndex);
			return true;
		}

		protected override void Dispose(bool disposing)
		{
			_leftSource.Dispose();
			if (_currentRightSource != null)
			{
				_currentRightSource.Dispose();
			}
		}
	}

	private sealed class SelectManyQueryOperatorEnumerator<TLeftKey> : QueryOperatorEnumerator<TOutput, Pair<TLeftKey, int>>
	{
		private sealed class Mutables
		{
			internal int _currentRightSourceIndex = -1;

			internal TLeftInput _currentLeftElement;

			internal TLeftKey _currentLeftKey;

			internal int _lhsCount;
		}

		private readonly QueryOperatorEnumerator<TLeftInput, TLeftKey> _leftSource;

		private readonly SelectManyQueryOperator<TLeftInput, TRightInput, TOutput> _selectManyOperator;

		private IEnumerator<TRightInput> _currentRightSource;

		private IEnumerator<TOutput> _currentRightSourceAsOutput;

		private Mutables _mutables;

		private readonly CancellationToken _cancellationToken;

		internal SelectManyQueryOperatorEnumerator(QueryOperatorEnumerator<TLeftInput, TLeftKey> leftSource, SelectManyQueryOperator<TLeftInput, TRightInput, TOutput> selectManyOperator, CancellationToken cancellationToken)
		{
			_leftSource = leftSource;
			_selectManyOperator = selectManyOperator;
			_cancellationToken = cancellationToken;
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TOutput currentElement, ref Pair<TLeftKey, int> currentKey)
		{
			while (true)
			{
				if (_currentRightSource == null)
				{
					_mutables = new Mutables();
					if ((_mutables._lhsCount++ & 0x3F) == 0)
					{
						_cancellationToken.ThrowIfCancellationRequested();
					}
					if (!_leftSource.MoveNext(ref _mutables._currentLeftElement, ref _mutables._currentLeftKey))
					{
						return false;
					}
					IEnumerable<TRightInput> enumerable = _selectManyOperator._rightChildSelector(_mutables._currentLeftElement);
					_currentRightSource = enumerable.GetEnumerator();
					if (_selectManyOperator._resultSelector == null)
					{
						_currentRightSourceAsOutput = (IEnumerator<TOutput>)_currentRightSource;
					}
				}
				if (_currentRightSource.MoveNext())
				{
					break;
				}
				_currentRightSource.Dispose();
				_currentRightSource = null;
				_currentRightSourceAsOutput = null;
			}
			_mutables._currentRightSourceIndex++;
			if (_selectManyOperator._resultSelector != null)
			{
				currentElement = _selectManyOperator._resultSelector(_mutables._currentLeftElement, _currentRightSource.Current);
			}
			else
			{
				currentElement = _currentRightSourceAsOutput.Current;
			}
			currentKey = new Pair<TLeftKey, int>(_mutables._currentLeftKey, _mutables._currentRightSourceIndex);
			return true;
		}

		protected override void Dispose(bool disposing)
		{
			_leftSource.Dispose();
			if (_currentRightSource != null)
			{
				_currentRightSource.Dispose();
			}
		}
	}

	private readonly Func<TLeftInput, IEnumerable<TRightInput>> _rightChildSelector;

	private readonly Func<TLeftInput, int, IEnumerable<TRightInput>> _indexedRightChildSelector;

	private readonly Func<TLeftInput, TRightInput, TOutput> _resultSelector;

	private bool _prematureMerge;

	private bool _limitsParallelism;

	internal override bool LimitsParallelism => _limitsParallelism;

	internal SelectManyQueryOperator(IEnumerable<TLeftInput> leftChild, Func<TLeftInput, IEnumerable<TRightInput>> rightChildSelector, Func<TLeftInput, int, IEnumerable<TRightInput>> indexedRightChildSelector, Func<TLeftInput, TRightInput, TOutput> resultSelector)
		: base(leftChild)
	{
		_rightChildSelector = rightChildSelector;
		_indexedRightChildSelector = indexedRightChildSelector;
		_resultSelector = resultSelector;
		_outputOrdered = base.Child.OutputOrdered || indexedRightChildSelector != null;
		InitOrderIndex();
	}

	private void InitOrderIndex()
	{
		OrdinalIndexState ordinalIndexState = base.Child.OrdinalIndexState;
		if (_indexedRightChildSelector != null)
		{
			_prematureMerge = ordinalIndexState.IsWorseThan(OrdinalIndexState.Correct);
			_limitsParallelism = _prematureMerge && ordinalIndexState != OrdinalIndexState.Shuffled;
		}
		else if (base.OutputOrdered)
		{
			_prematureMerge = ordinalIndexState.IsWorseThan(OrdinalIndexState.Increasing);
		}
		SetOrdinalIndexState(OrdinalIndexState.Increasing);
	}

	internal override void WrapPartitionedStream<TLeftKey>(PartitionedStream<TLeftInput, TLeftKey> inputStream, IPartitionedStreamRecipient<TOutput> recipient, bool preferStriping, QuerySettings settings)
	{
		int partitionCount = inputStream.PartitionCount;
		if (_indexedRightChildSelector != null)
		{
			PartitionedStream<TLeftInput, int> inputStream2;
			if (_prematureMerge)
			{
				ListQueryResults<TLeftInput> listQueryResults = QueryOperator<TLeftInput>.ExecuteAndCollectResults(inputStream, partitionCount, base.OutputOrdered, preferStriping, settings);
				inputStream2 = listQueryResults.GetPartitionedStream();
			}
			else
			{
				inputStream2 = (PartitionedStream<TLeftInput, int>)(object)inputStream;
			}
			WrapPartitionedStreamIndexed(inputStream2, recipient, settings);
		}
		else if (_prematureMerge)
		{
			PartitionedStream<TLeftInput, int> partitionedStream = QueryOperator<TLeftInput>.ExecuteAndCollectResults(inputStream, partitionCount, base.OutputOrdered, preferStriping, settings).GetPartitionedStream();
			WrapPartitionedStreamNotIndexed(partitionedStream, recipient, settings);
		}
		else
		{
			WrapPartitionedStreamNotIndexed(inputStream, recipient, settings);
		}
	}

	private void WrapPartitionedStreamNotIndexed<TLeftKey>(PartitionedStream<TLeftInput, TLeftKey> inputStream, IPartitionedStreamRecipient<TOutput> recipient, QuerySettings settings)
	{
		int partitionCount = inputStream.PartitionCount;
		PairComparer<TLeftKey, int> keyComparer = new PairComparer<TLeftKey, int>(inputStream.KeyComparer, Util.GetDefaultComparer<int>());
		PartitionedStream<TOutput, Pair<TLeftKey, int>> partitionedStream = new PartitionedStream<TOutput, Pair<TLeftKey, int>>(partitionCount, keyComparer, OrdinalIndexState);
		for (int i = 0; i < partitionCount; i++)
		{
			partitionedStream[i] = new SelectManyQueryOperatorEnumerator<TLeftKey>(inputStream[i], this, settings.CancellationState.MergedCancellationToken);
		}
		recipient.Receive(partitionedStream);
	}

	private void WrapPartitionedStreamIndexed(PartitionedStream<TLeftInput, int> inputStream, IPartitionedStreamRecipient<TOutput> recipient, QuerySettings settings)
	{
		PairComparer<int, int> keyComparer = new PairComparer<int, int>(inputStream.KeyComparer, Util.GetDefaultComparer<int>());
		PartitionedStream<TOutput, Pair<int, int>> partitionedStream = new PartitionedStream<TOutput, Pair<int, int>>(inputStream.PartitionCount, keyComparer, OrdinalIndexState);
		for (int i = 0; i < inputStream.PartitionCount; i++)
		{
			partitionedStream[i] = new IndexedSelectManyQueryOperatorEnumerator(inputStream[i], this, settings.CancellationState.MergedCancellationToken);
		}
		recipient.Receive(partitionedStream);
	}

	internal override QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TLeftInput> childQueryResults = base.Child.Open(settings, preferStriping);
		return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
	}

	internal override IEnumerable<TOutput> AsSequentialQuery(CancellationToken token)
	{
		if (_rightChildSelector != null)
		{
			if (_resultSelector != null)
			{
				return CancellableEnumerable.Wrap(base.Child.AsSequentialQuery(token), token).SelectMany(_rightChildSelector, _resultSelector);
			}
			return (IEnumerable<TOutput>)CancellableEnumerable.Wrap(base.Child.AsSequentialQuery(token), token).SelectMany(_rightChildSelector);
		}
		if (_resultSelector != null)
		{
			return CancellableEnumerable.Wrap(base.Child.AsSequentialQuery(token), token).SelectMany(_indexedRightChildSelector, _resultSelector);
		}
		return (IEnumerable<TOutput>)CancellableEnumerable.Wrap(base.Child.AsSequentialQuery(token), token).SelectMany(_indexedRightChildSelector);
	}
}
