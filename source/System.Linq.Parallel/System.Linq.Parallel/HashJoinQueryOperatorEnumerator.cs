using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class HashJoinQueryOperatorEnumerator<TLeftInput, TLeftKey, TRightInput, TRightKey, THashKey, TOutput, TOutputKey> : QueryOperatorEnumerator<TOutput, TOutputKey>
{
	private sealed class Mutables
	{
		internal TLeftInput _currentLeft;

		internal TLeftKey _currentLeftKey;

		internal HashJoinHashLookup<THashKey, TRightInput, TRightKey> _rightHashLookup;

		internal ListChunk<Pair<TRightInput, TRightKey>> _currentRightMatches;

		internal int _currentRightMatchesIndex;

		internal int _outputLoopCount;
	}

	private readonly QueryOperatorEnumerator<Pair<TLeftInput, THashKey>, TLeftKey> _leftSource;

	private readonly HashLookupBuilder<TRightInput, TRightKey, THashKey> _rightLookupBuilder;

	private readonly Func<TLeftInput, TRightInput, TOutput> _resultSelector;

	private readonly HashJoinOutputKeyBuilder<TLeftKey, TRightKey, TOutputKey> _outputKeyBuilder;

	private readonly CancellationToken _cancellationToken;

	private Mutables _mutables;

	internal HashJoinQueryOperatorEnumerator(QueryOperatorEnumerator<Pair<TLeftInput, THashKey>, TLeftKey> leftSource, HashLookupBuilder<TRightInput, TRightKey, THashKey> rightLookupBuilder, Func<TLeftInput, TRightInput, TOutput> resultSelector, HashJoinOutputKeyBuilder<TLeftKey, TRightKey, TOutputKey> outputKeyBuilder, CancellationToken cancellationToken)
	{
		_leftSource = leftSource;
		_rightLookupBuilder = rightLookupBuilder;
		_resultSelector = resultSelector;
		_outputKeyBuilder = outputKeyBuilder;
		_cancellationToken = cancellationToken;
	}

	internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TOutput currentElement, [AllowNull] ref TOutputKey currentKey)
	{
		Mutables mutables = _mutables;
		if (mutables == null)
		{
			mutables = (_mutables = new Mutables());
			mutables._rightHashLookup = _rightLookupBuilder.BuildHashLookup(_cancellationToken);
		}
		ListChunk<Pair<TRightInput, TRightKey>> currentRightMatches = mutables._currentRightMatches;
		if (currentRightMatches != null && mutables._currentRightMatchesIndex == currentRightMatches.Count)
		{
			mutables._currentRightMatches = currentRightMatches.Next;
			mutables._currentRightMatchesIndex = 0;
		}
		if (mutables._currentRightMatches == null)
		{
			Pair<TLeftInput, THashKey> currentElement2 = default(Pair<TLeftInput, THashKey>);
			TLeftKey currentKey2 = default(TLeftKey);
			while (_leftSource.MoveNext(ref currentElement2, ref currentKey2))
			{
				if ((mutables._outputLoopCount++ & 0x3F) == 0)
				{
					_cancellationToken.ThrowIfCancellationRequested();
				}
				HashLookupValueList<TRightInput, TRightKey> value = default(HashLookupValueList<TRightInput, TRightKey>);
				TLeftInput first = currentElement2.First;
				THashKey second = currentElement2.Second;
				if (second != null && mutables._rightHashLookup.TryGetValue(second, ref value))
				{
					mutables._currentRightMatches = value.Tail;
					mutables._currentRightMatchesIndex = 0;
					currentElement = _resultSelector(first, value.Head.First);
					currentKey = _outputKeyBuilder.Combine(currentKey2, value.Head.Second);
					if (value.Tail != null)
					{
						mutables._currentLeft = first;
						mutables._currentLeftKey = currentKey2;
					}
					return true;
				}
			}
			return false;
		}
		Pair<TRightInput, TRightKey> pair = mutables._currentRightMatches._chunk[mutables._currentRightMatchesIndex];
		currentElement = _resultSelector(mutables._currentLeft, pair.First);
		currentKey = _outputKeyBuilder.Combine(mutables._currentLeftKey, pair.Second);
		mutables._currentRightMatchesIndex++;
		return true;
	}

	protected override void Dispose(bool disposing)
	{
		_leftSource.Dispose();
		_rightLookupBuilder.Dispose();
	}
}
