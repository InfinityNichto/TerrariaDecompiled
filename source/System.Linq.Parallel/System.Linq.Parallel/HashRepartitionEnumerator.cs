using System.Threading;

namespace System.Linq.Parallel;

internal sealed class HashRepartitionEnumerator<TInputOutput, THashKey, TIgnoreKey> : QueryOperatorEnumerator<Pair<TInputOutput, THashKey>, int>
{
	private sealed class Mutables
	{
		internal int _currentBufferIndex;

		internal ListChunk<Pair<TInputOutput, THashKey>> _currentBuffer;

		internal int _currentIndex;

		internal Mutables()
		{
			_currentBufferIndex = -1;
		}
	}

	private readonly int _partitionCount;

	private readonly int _partitionIndex;

	private readonly Func<TInputOutput, THashKey> _keySelector;

	private readonly HashRepartitionStream<TInputOutput, THashKey, int> _repartitionStream;

	private readonly ListChunk<Pair<TInputOutput, THashKey>>[][] _valueExchangeMatrix;

	private readonly QueryOperatorEnumerator<TInputOutput, TIgnoreKey> _source;

	private CountdownEvent _barrier;

	private readonly CancellationToken _cancellationToken;

	private Mutables _mutables;

	internal HashRepartitionEnumerator(QueryOperatorEnumerator<TInputOutput, TIgnoreKey> source, int partitionCount, int partitionIndex, Func<TInputOutput, THashKey> keySelector, HashRepartitionStream<TInputOutput, THashKey, int> repartitionStream, CountdownEvent barrier, ListChunk<Pair<TInputOutput, THashKey>>[][] valueExchangeMatrix, CancellationToken cancellationToken)
	{
		_source = source;
		_partitionCount = partitionCount;
		_partitionIndex = partitionIndex;
		_keySelector = keySelector;
		_repartitionStream = repartitionStream;
		_barrier = barrier;
		_valueExchangeMatrix = valueExchangeMatrix;
		_cancellationToken = cancellationToken;
	}

	internal override bool MoveNext(ref Pair<TInputOutput, THashKey> currentElement, ref int currentKey)
	{
		if (_partitionCount == 1)
		{
			TIgnoreKey currentKey2 = default(TIgnoreKey);
			TInputOutput currentElement2 = default(TInputOutput);
			if (_source.MoveNext(ref currentElement2, ref currentKey2))
			{
				currentElement = new Pair<TInputOutput, THashKey>(currentElement2, (_keySelector == null) ? default(THashKey) : _keySelector(currentElement2));
				return true;
			}
			return false;
		}
		Mutables mutables = _mutables;
		if (mutables == null)
		{
			mutables = (_mutables = new Mutables());
		}
		if (mutables._currentBufferIndex == -1)
		{
			EnumerateAndRedistributeElements();
		}
		while (mutables._currentBufferIndex < _partitionCount)
		{
			if (mutables._currentBuffer != null)
			{
				if (++mutables._currentIndex < mutables._currentBuffer.Count)
				{
					currentElement = mutables._currentBuffer._chunk[mutables._currentIndex];
					return true;
				}
				mutables._currentIndex = -1;
				mutables._currentBuffer = mutables._currentBuffer.Next;
				continue;
			}
			if (mutables._currentBufferIndex == _partitionIndex)
			{
				_barrier.Wait(_cancellationToken);
				mutables._currentBufferIndex = -1;
			}
			mutables._currentBufferIndex++;
			mutables._currentIndex = -1;
			if (mutables._currentBufferIndex == _partitionIndex)
			{
				mutables._currentBufferIndex++;
			}
			if (mutables._currentBufferIndex < _partitionCount)
			{
				mutables._currentBuffer = _valueExchangeMatrix[mutables._currentBufferIndex][_partitionIndex];
			}
		}
		return false;
	}

	private void EnumerateAndRedistributeElements()
	{
		Mutables mutables = _mutables;
		ListChunk<Pair<TInputOutput, THashKey>>[] array = new ListChunk<Pair<TInputOutput, THashKey>>[_partitionCount];
		TInputOutput currentElement = default(TInputOutput);
		TIgnoreKey currentKey = default(TIgnoreKey);
		int num = 0;
		while (_source.MoveNext(ref currentElement, ref currentKey))
		{
			if ((num++ & 0x3F) == 0)
			{
				_cancellationToken.ThrowIfCancellationRequested();
			}
			THashKey val = default(THashKey);
			int num2;
			if (_keySelector != null)
			{
				val = _keySelector(currentElement);
				num2 = _repartitionStream.GetHashCode(val) % _partitionCount;
			}
			else
			{
				num2 = _repartitionStream.GetHashCode(currentElement) % _partitionCount;
			}
			ListChunk<Pair<TInputOutput, THashKey>> listChunk = array[num2];
			if (listChunk == null)
			{
				listChunk = (array[num2] = new ListChunk<Pair<TInputOutput, THashKey>>(128));
			}
			listChunk.Add(new Pair<TInputOutput, THashKey>(currentElement, val));
		}
		for (int i = 0; i < _partitionCount; i++)
		{
			_valueExchangeMatrix[_partitionIndex][i] = array[i];
		}
		_barrier.Signal();
		mutables._currentBufferIndex = _partitionIndex;
		mutables._currentBuffer = array[_partitionIndex];
		mutables._currentIndex = -1;
	}

	protected override void Dispose(bool disposed)
	{
		if (_barrier != null)
		{
			if (_mutables == null || _mutables._currentBufferIndex == -1)
			{
				_barrier.Signal();
				_barrier = null;
			}
			_source.Dispose();
		}
	}
}
