namespace System.Linq.Parallel;

internal struct HashLookupValueList<TElement, TOrderKey>
{
	private readonly Pair<TElement, TOrderKey> _head;

	private ListChunk<Pair<TElement, TOrderKey>> _tail;

	internal Pair<TElement, TOrderKey> Head => _head;

	internal ListChunk<Pair<TElement, TOrderKey>> Tail => _tail;

	internal HashLookupValueList(TElement firstValue, TOrderKey firstOrderKey)
	{
		_head = CreatePair(firstValue, firstOrderKey);
		_tail = null;
	}

	internal bool Add(TElement value, TOrderKey orderKey)
	{
		bool flag = _tail == null;
		if (flag)
		{
			_tail = new ListChunk<Pair<TElement, TOrderKey>>(2);
		}
		_tail.Add(CreatePair(value, orderKey));
		return flag;
	}

	private static Pair<TElement, TOrderKey> CreatePair(TElement value, TOrderKey orderKey)
	{
		return new Pair<TElement, TOrderKey>(value, orderKey);
	}
}
