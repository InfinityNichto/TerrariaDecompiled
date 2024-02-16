using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class OrderedHashRepartitionEnumerator<TInputOutput, THashKey, TOrderKey> : QueryOperatorEnumerator<Pair<TInputOutput, THashKey>, TOrderKey>
{
	private sealed class Mutables
	{
		internal int _currentBufferIndex;

		internal ListChunk<Pair<TInputOutput, THashKey>> _currentBuffer;

		internal ListChunk<TOrderKey> _currentKeyBuffer;

		internal int _currentIndex;

		internal Mutables()
		{
			_currentBufferIndex = -1;
		}
	}

	private readonly int _partitionCount;

	private readonly int _partitionIndex;

	private readonly Func<TInputOutput, THashKey> _keySelector;

	private readonly HashRepartitionStream<TInputOutput, THashKey, TOrderKey> _repartitionStream;

	private readonly ListChunk<Pair<TInputOutput, THashKey>>[][] _valueExchangeMatrix;

	private readonly ListChunk<TOrderKey>[][] _keyExchangeMatrix;

	private readonly QueryOperatorEnumerator<TInputOutput, TOrderKey> _source;

	private CountdownEvent _barrier;

	private readonly CancellationToken _cancellationToken;

	private Mutables _mutables;

	internal OrderedHashRepartitionEnumerator(QueryOperatorEnumerator<TInputOutput, TOrderKey> source, int partitionCount, int partitionIndex, Func<TInputOutput, THashKey> keySelector, OrderedHashRepartitionStream<TInputOutput, THashKey, TOrderKey> repartitionStream, CountdownEvent barrier, ListChunk<Pair<TInputOutput, THashKey>>[][] valueExchangeMatrix, ListChunk<TOrderKey>[][] keyExchangeMatrix, CancellationToken cancellationToken)
	{
		_source = source;
		_partitionCount = partitionCount;
		_partitionIndex = partitionIndex;
		_keySelector = keySelector;
		_repartitionStream = repartitionStream;
		_barrier = barrier;
		_valueExchangeMatrix = valueExchangeMatrix;
		_keyExchangeMatrix = keyExchangeMatrix;
		_cancellationToken = cancellationToken;
	}

	internal override bool MoveNext(ref Pair<TInputOutput, THashKey> currentElement, [AllowNull] ref TOrderKey currentKey)
	{
		if (_partitionCount == 1)
		{
			TInputOutput currentElement2 = default(TInputOutput);
			if (_source.MoveNext(ref currentElement2, ref currentKey))
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
					currentKey = mutables._currentKeyBuffer._chunk[mutables._currentIndex];
					return true;
				}
				mutables._currentIndex = -1;
				mutables._currentBuffer = mutables._currentBuffer.Next;
				mutables._currentKeyBuffer = mutables._currentKeyBuffer.Next;
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
				mutables._currentKeyBuffer = _keyExchangeMatrix[mutables._currentBufferIndex][_partitionIndex];
			}
		}
		return false;
	}

	private void EnumerateAndRedistributeElements()
	{
		Mutables mutables = _mutables;
		ListChunk<Pair<TInputOutput, THashKey>>[] array = new ListChunk<Pair<TInputOutput, THashKey>>[_partitionCount];
		ListChunk<TOrderKey>[] array2 = new ListChunk<TOrderKey>[_partitionCount];
		TInputOutput currentElement = default(TInputOutput);
		TOrderKey currentKey = default(TOrderKey);
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
			ListChunk<TOrderKey> listChunk2 = array2[num2];
			if (listChunk == null)
			{
				listChunk = (array[num2] = new ListChunk<Pair<TInputOutput, THashKey>>(128));
				listChunk2 = (array2[num2] = new ListChunk<TOrderKey>(128));
			}
			listChunk.Add(new Pair<TInputOutput, THashKey>(currentElement, val));
			listChunk2.Add(currentKey);
		}
		for (int i = 0; i < _partitionCount; i++)
		{
			_valueExchangeMatrix[_partitionIndex][i] = array[i];
			_keyExchangeMatrix[_partitionIndex][i] = array2[i];
		}
		_barrier.Signal();
		mutables._currentBufferIndex = _partitionIndex;
		mutables._currentBuffer = array[_partitionIndex];
		mutables._currentKeyBuffer = array2[_partitionIndex];
		mutables._currentIndex = -1;
	}

	protected override void Dispose(bool disposing)
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
