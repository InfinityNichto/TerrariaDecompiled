using System.Collections.Generic;

namespace System.Linq;

internal abstract class EnumerableSorter<TElement>
{
	internal abstract void ComputeKeys(TElement[] elements, int count);

	internal abstract int CompareAnyKeys(int index1, int index2);

	private int[] ComputeMap(TElement[] elements, int count)
	{
		ComputeKeys(elements, count);
		int[] array = new int[count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = i;
		}
		return array;
	}

	internal int[] Sort(TElement[] elements, int count)
	{
		int[] array = ComputeMap(elements, count);
		QuickSort(array, 0, count - 1);
		return array;
	}

	internal int[] Sort(TElement[] elements, int count, int minIdx, int maxIdx)
	{
		int[] array = ComputeMap(elements, count);
		PartialQuickSort(array, 0, count - 1, minIdx, maxIdx);
		return array;
	}

	internal TElement ElementAt(TElement[] elements, int count, int idx)
	{
		int[] map = ComputeMap(elements, count);
		if (idx != 0)
		{
			return elements[QuickSelect(map, count - 1, idx)];
		}
		return elements[Min(map, count)];
	}

	protected abstract void QuickSort(int[] map, int left, int right);

	protected abstract void PartialQuickSort(int[] map, int left, int right, int minIdx, int maxIdx);

	protected abstract int QuickSelect(int[] map, int right, int idx);

	protected abstract int Min(int[] map, int count);
}
internal sealed class EnumerableSorter<TElement, TKey> : EnumerableSorter<TElement>
{
	private readonly Func<TElement, TKey> _keySelector;

	private readonly IComparer<TKey> _comparer;

	private readonly bool _descending;

	private readonly EnumerableSorter<TElement> _next;

	private TKey[] _keys;

	internal EnumerableSorter(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending, EnumerableSorter<TElement> next)
	{
		_keySelector = keySelector;
		_comparer = comparer;
		_descending = descending;
		_next = next;
	}

	internal override void ComputeKeys(TElement[] elements, int count)
	{
		_keys = new TKey[count];
		for (int i = 0; i < count; i++)
		{
			_keys[i] = _keySelector(elements[i]);
		}
		_next?.ComputeKeys(elements, count);
	}

	internal override int CompareAnyKeys(int index1, int index2)
	{
		int num = _comparer.Compare(_keys[index1], _keys[index2]);
		if (num == 0)
		{
			if (_next == null)
			{
				return index1 - index2;
			}
			return _next.CompareAnyKeys(index1, index2);
		}
		if (_descending == num > 0)
		{
			return -1;
		}
		return 1;
	}

	private int CompareKeys(int index1, int index2)
	{
		if (index1 != index2)
		{
			return CompareAnyKeys(index1, index2);
		}
		return 0;
	}

	protected override void QuickSort(int[] keys, int lo, int hi)
	{
		new Span<int>(keys, lo, hi - lo + 1).Sort(CompareAnyKeys);
	}

	protected override void PartialQuickSort(int[] map, int left, int right, int minIdx, int maxIdx)
	{
		do
		{
			int num = left;
			int num2 = right;
			int index = map[num + (num2 - num >> 1)];
			while (true)
			{
				if (num < map.Length && CompareKeys(index, map[num]) > 0)
				{
					num++;
					continue;
				}
				while (num2 >= 0 && CompareKeys(index, map[num2]) < 0)
				{
					num2--;
				}
				if (num > num2)
				{
					break;
				}
				if (num < num2)
				{
					int num3 = map[num];
					map[num] = map[num2];
					map[num2] = num3;
				}
				num++;
				num2--;
				if (num > num2)
				{
					break;
				}
			}
			if (minIdx >= num)
			{
				left = num + 1;
			}
			else if (maxIdx <= num2)
			{
				right = num2 - 1;
			}
			if (num2 - left <= right - num)
			{
				if (left < num2)
				{
					PartialQuickSort(map, left, num2, minIdx, maxIdx);
				}
				left = num;
			}
			else
			{
				if (num < right)
				{
					PartialQuickSort(map, num, right, minIdx, maxIdx);
				}
				right = num2;
			}
		}
		while (left < right);
	}

	protected override int QuickSelect(int[] map, int right, int idx)
	{
		int num = 0;
		do
		{
			int num2 = num;
			int num3 = right;
			int index = map[num2 + (num3 - num2 >> 1)];
			while (true)
			{
				if (num2 < map.Length && CompareKeys(index, map[num2]) > 0)
				{
					num2++;
					continue;
				}
				while (num3 >= 0 && CompareKeys(index, map[num3]) < 0)
				{
					num3--;
				}
				if (num2 > num3)
				{
					break;
				}
				if (num2 < num3)
				{
					int num4 = map[num2];
					map[num2] = map[num3];
					map[num3] = num4;
				}
				num2++;
				num3--;
				if (num2 > num3)
				{
					break;
				}
			}
			if (num2 <= idx)
			{
				num = num2 + 1;
			}
			else
			{
				right = num3 - 1;
			}
			if (num3 - num <= right - num2)
			{
				if (num < num3)
				{
					right = num3;
				}
				num = num2;
			}
			else
			{
				if (num2 < right)
				{
					num = num2;
				}
				right = num3;
			}
		}
		while (num < right);
		return map[idx];
	}

	protected override int Min(int[] map, int count)
	{
		int num = 0;
		for (int i = 1; i < count; i++)
		{
			if (CompareKeys(map[i], map[num]) < 0)
			{
				num = i;
			}
		}
		return map[num];
	}
}
