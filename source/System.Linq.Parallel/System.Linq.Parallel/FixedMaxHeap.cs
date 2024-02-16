using System.Collections.Generic;

namespace System.Linq.Parallel;

internal sealed class FixedMaxHeap<TElement>
{
	private readonly TElement[] _elements;

	private int _count;

	private readonly IComparer<TElement> _comparer;

	internal int Count => _count;

	internal int Size => _elements.Length;

	internal TElement MaxValue
	{
		get
		{
			if (_count == 0)
			{
				throw new InvalidOperationException(System.SR.NoElements);
			}
			return _elements[0];
		}
	}

	internal FixedMaxHeap(int maximumSize, IComparer<TElement> comparer)
	{
		_elements = new TElement[maximumSize];
		_comparer = comparer;
	}

	internal void Clear()
	{
		_count = 0;
	}

	internal bool Insert(TElement e)
	{
		if (_count < _elements.Length)
		{
			_elements[_count] = e;
			_count++;
			HeapifyLastLeaf();
			return true;
		}
		if (_comparer.Compare(e, _elements[0]) < 0)
		{
			_elements[0] = e;
			HeapifyRoot();
			return true;
		}
		return false;
	}

	internal void ReplaceMax(TElement newValue)
	{
		_elements[0] = newValue;
		HeapifyRoot();
	}

	internal void RemoveMax()
	{
		_count--;
		if (_count > 0)
		{
			_elements[0] = _elements[_count];
			HeapifyRoot();
		}
	}

	private void Swap(int i, int j)
	{
		TElement val = _elements[i];
		_elements[i] = _elements[j];
		_elements[j] = val;
	}

	private void HeapifyRoot()
	{
		int num = 0;
		int count = _count;
		while (num < count)
		{
			int num2 = (num + 1) * 2 - 1;
			int num3 = num2 + 1;
			if (num2 < count && _comparer.Compare(_elements[num], _elements[num2]) < 0)
			{
				if (num3 < count && _comparer.Compare(_elements[num2], _elements[num3]) < 0)
				{
					Swap(num, num3);
					num = num3;
				}
				else
				{
					Swap(num, num2);
					num = num2;
				}
			}
			else
			{
				if (num3 >= count || _comparer.Compare(_elements[num], _elements[num3]) >= 0)
				{
					break;
				}
				Swap(num, num3);
				num = num3;
			}
		}
	}

	private void HeapifyLastLeaf()
	{
		int num = _count - 1;
		while (num > 0)
		{
			int num2 = (num + 1) / 2 - 1;
			if (_comparer.Compare(_elements[num], _elements[num2]) > 0)
			{
				Swap(num, num2);
				num = num2;
				continue;
			}
			break;
		}
	}
}
