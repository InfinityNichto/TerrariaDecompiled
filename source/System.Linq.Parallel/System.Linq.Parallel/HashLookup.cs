using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Parallel;

internal sealed class HashLookup<TKey, TValue>
{
	internal struct Slot
	{
		internal int hashCode;

		internal int next;

		internal TKey key;

		internal TValue value;
	}

	private int[] buckets;

	private Slot[] slots;

	private int count;

	private int freeList;

	private readonly IEqualityComparer<TKey> comparer;

	internal TValue this[TKey key]
	{
		set
		{
			TValue value2 = value;
			Find(key, add: false, set: true, ref value2);
		}
	}

	internal int Count => count;

	internal KeyValuePair<TKey, TValue> this[int index] => new KeyValuePair<TKey, TValue>(slots[index].key, slots[index].value);

	internal HashLookup(IEqualityComparer<TKey> comparer)
	{
		this.comparer = comparer;
		buckets = new int[7];
		slots = new Slot[7];
		freeList = -1;
	}

	internal bool Add(TKey key, TValue value)
	{
		return !Find(key, add: true, set: false, ref value);
	}

	internal bool TryGetValue(TKey key, [MaybeNullWhen(false)][AllowNull] ref TValue value)
	{
		return Find(key, add: false, set: false, ref value);
	}

	private int GetKeyHashCode(TKey key)
	{
		return 0x7FFFFFFF & ((key != null) ? (comparer?.GetHashCode(key) ?? key.GetHashCode()) : 0);
	}

	private bool AreKeysEqual(TKey key1, TKey key2)
	{
		if (comparer != null)
		{
			return comparer.Equals(key1, key2);
		}
		if (key1 != null || key2 != null)
		{
			return key1?.Equals(key2) ?? false;
		}
		return true;
	}

	private bool Find(TKey key, bool add, bool set, [MaybeNullWhen(false)] ref TValue value)
	{
		int keyHashCode = GetKeyHashCode(key);
		for (int num = buckets[keyHashCode % buckets.Length] - 1; num >= 0; num = slots[num].next)
		{
			if (slots[num].hashCode == keyHashCode && AreKeysEqual(slots[num].key, key))
			{
				if (set)
				{
					slots[num].value = value;
					return true;
				}
				value = slots[num].value;
				return true;
			}
		}
		if (add)
		{
			int num2;
			if (freeList >= 0)
			{
				num2 = freeList;
				freeList = slots[num2].next;
			}
			else
			{
				if (count == slots.Length)
				{
					Resize();
				}
				num2 = count;
				count++;
			}
			int num3 = keyHashCode % buckets.Length;
			slots[num2].hashCode = keyHashCode;
			slots[num2].key = key;
			slots[num2].value = value;
			slots[num2].next = buckets[num3] - 1;
			buckets[num3] = num2 + 1;
		}
		return false;
	}

	private void Resize()
	{
		int num = checked(count * 2 + 1);
		int[] array = new int[num];
		Slot[] array2 = new Slot[num];
		Array.Copy(slots, array2, count);
		for (int i = 0; i < count; i++)
		{
			int num2 = array2[i].hashCode % num;
			array2[i].next = array[num2] - 1;
			array[num2] = i + 1;
		}
		buckets = array;
		slots = array2;
	}
}
