using System.Collections;
using System.Collections.Generic;

namespace System.Linq;

internal sealed class OrderedPartition<TElement> : IPartition<TElement>, IIListProvider<TElement>, IEnumerable<TElement>, IEnumerable
{
	private readonly OrderedEnumerable<TElement> _source;

	private readonly int _minIndexInclusive;

	private readonly int _maxIndexInclusive;

	public OrderedPartition(OrderedEnumerable<TElement> source, int minIdxInclusive, int maxIdxInclusive)
	{
		_source = source;
		_minIndexInclusive = minIdxInclusive;
		_maxIndexInclusive = maxIdxInclusive;
	}

	public IEnumerator<TElement> GetEnumerator()
	{
		return _source.GetEnumerator(_minIndexInclusive, _maxIndexInclusive);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public IPartition<TElement> Skip(int count)
	{
		int num = _minIndexInclusive + count;
		if ((uint)num <= (uint)_maxIndexInclusive)
		{
			return new OrderedPartition<TElement>(_source, num, _maxIndexInclusive);
		}
		return EmptyPartition<TElement>.Instance;
	}

	public IPartition<TElement> Take(int count)
	{
		int num = _minIndexInclusive + count - 1;
		if ((uint)num >= (uint)_maxIndexInclusive)
		{
			return this;
		}
		return new OrderedPartition<TElement>(_source, _minIndexInclusive, num);
	}

	public TElement TryGetElementAt(int index, out bool found)
	{
		if ((uint)index <= (uint)(_maxIndexInclusive - _minIndexInclusive))
		{
			return _source.TryGetElementAt(index + _minIndexInclusive, out found);
		}
		found = false;
		return default(TElement);
	}

	public TElement TryGetFirst(out bool found)
	{
		return _source.TryGetElementAt(_minIndexInclusive, out found);
	}

	public TElement TryGetLast(out bool found)
	{
		return _source.TryGetLast(_minIndexInclusive, _maxIndexInclusive, out found);
	}

	public TElement[] ToArray()
	{
		return _source.ToArray(_minIndexInclusive, _maxIndexInclusive);
	}

	public List<TElement> ToList()
	{
		return _source.ToList(_minIndexInclusive, _maxIndexInclusive);
	}

	public int GetCount(bool onlyIfCheap)
	{
		return _source.GetCount(_minIndexInclusive, _maxIndexInclusive, onlyIfCheap);
	}
}
