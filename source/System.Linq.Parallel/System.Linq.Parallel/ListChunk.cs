using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal sealed class ListChunk<TInputOutput> : IEnumerable<TInputOutput>, IEnumerable
{
	internal TInputOutput[] _chunk;

	private int _chunkCount;

	private ListChunk<TInputOutput> _nextChunk;

	private ListChunk<TInputOutput> _tailChunk;

	internal ListChunk<TInputOutput> Next => _nextChunk;

	internal int Count => _chunkCount;

	internal ListChunk(int size)
	{
		_chunk = new TInputOutput[size];
		_chunkCount = 0;
		_tailChunk = this;
	}

	internal void Add(TInputOutput e)
	{
		ListChunk<TInputOutput> listChunk = _tailChunk;
		if (listChunk._chunkCount == listChunk._chunk.Length)
		{
			_tailChunk = new ListChunk<TInputOutput>(listChunk._chunkCount * 2);
			listChunk = (listChunk._nextChunk = _tailChunk);
		}
		listChunk._chunk[listChunk._chunkCount++] = e;
	}

	public IEnumerator<TInputOutput> GetEnumerator()
	{
		for (ListChunk<TInputOutput> curr = this; curr != null; curr = curr._nextChunk)
		{
			for (int i = 0; i < curr._chunkCount; i++)
			{
				yield return curr._chunk[i];
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable<TInputOutput>)this).GetEnumerator();
	}
}
