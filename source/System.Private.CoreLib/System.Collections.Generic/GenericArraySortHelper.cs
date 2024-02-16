using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System.Collections.Generic;

internal sealed class GenericArraySortHelper<T> : IArraySortHelper<T> where T : IComparable<T>
{
	public void Sort(Span<T> keys, IComparer<T> comparer)
	{
		try
		{
			if (comparer == null || comparer == Comparer<T>.Default)
			{
				if (keys.Length <= 1)
				{
					return;
				}
				if (typeof(T) == typeof(double) || typeof(T) == typeof(float) || typeof(T) == typeof(Half))
				{
					int num = SortUtils.MoveNansToFront(keys, default(Span<byte>));
					if (num == keys.Length)
					{
						return;
					}
					keys = keys.Slice(num);
				}
				IntroSort(keys, 2 * (BitOperations.Log2((uint)keys.Length) + 1));
			}
			else
			{
				ArraySortHelper<T>.IntrospectiveSort(keys, comparer.Compare);
			}
		}
		catch (IndexOutOfRangeException)
		{
			ThrowHelper.ThrowArgumentException_BadComparer(comparer);
		}
		catch (Exception e)
		{
			ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
		}
	}

	public int BinarySearch(T[] array, int index, int length, T value, IComparer<T> comparer)
	{
		try
		{
			if (comparer == null || comparer == Comparer<T>.Default)
			{
				return BinarySearch(array, index, length, value);
			}
			return ArraySortHelper<T>.InternalBinarySearch(array, index, length, value, comparer);
		}
		catch (Exception e)
		{
			ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
			return 0;
		}
	}

	private static int BinarySearch(T[] array, int index, int length, T value)
	{
		int num = index;
		int num2 = index + length - 1;
		while (num <= num2)
		{
			int num3 = num + (num2 - num >> 1);
			int num4 = ((array[num3] != null) ? array[num3].CompareTo(value) : ((value != null) ? (-1) : 0));
			if (num4 == 0)
			{
				return num3;
			}
			if (num4 < 0)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		return ~num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void SwapIfGreater(ref T i, ref T j)
	{
		if (i != null && GreaterThan(ref i, ref j))
		{
			Swap(ref i, ref j);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Swap(ref T i, ref T j)
	{
		T val = i;
		i = j;
		j = val;
	}

	private static void IntroSort(Span<T> keys, int depthLimit)
	{
		int num = keys.Length;
		while (num > 1)
		{
			if (num <= 16)
			{
				switch (num)
				{
				case 2:
					SwapIfGreater(ref keys[0], ref keys[1]);
					break;
				case 3:
				{
					ref T j = ref keys[2];
					ref T reference = ref keys[1];
					ref T i = ref keys[0];
					SwapIfGreater(ref i, ref reference);
					SwapIfGreater(ref i, ref j);
					SwapIfGreater(ref reference, ref j);
					break;
				}
				default:
					InsertionSort(keys.Slice(0, num));
					break;
				}
				break;
			}
			if (depthLimit == 0)
			{
				HeapSort(keys.Slice(0, num));
				break;
			}
			depthLimit--;
			int num2 = PickPivotAndPartition(keys.Slice(0, num));
			Span<T> span = keys;
			IntroSort(span[(num2 + 1)..num], depthLimit);
			num = num2;
		}
	}

	private static int PickPivotAndPartition(Span<T> keys)
	{
		ref T reference = ref MemoryMarshal.GetReference(keys);
		ref T j = ref Unsafe.Add(ref reference, keys.Length - 1);
		ref T reference2 = ref Unsafe.Add(ref reference, keys.Length - 1 >> 1);
		SwapIfGreater(ref reference, ref reference2);
		SwapIfGreater(ref reference, ref j);
		SwapIfGreater(ref reference2, ref j);
		ref T reference3 = ref Unsafe.Add(ref reference, keys.Length - 2);
		T left = reference2;
		Swap(ref reference2, ref reference3);
		ref T reference4 = ref reference;
		ref T reference5 = ref reference3;
		while (Unsafe.IsAddressLessThan(ref reference4, ref reference5))
		{
			if (left == null)
			{
				while (Unsafe.IsAddressLessThan(ref reference4, ref reference3))
				{
					ref T reference6 = ref Unsafe.Add(ref reference4, 1);
					reference4 = ref reference6;
					if (reference6 != null)
					{
						break;
					}
				}
				while (Unsafe.IsAddressGreaterThan(ref reference5, ref reference))
				{
					ref T reference7 = ref Unsafe.Add(ref reference5, -1);
					reference5 = ref reference7;
					if (reference7 == null)
					{
						break;
					}
				}
			}
			else
			{
				while (Unsafe.IsAddressLessThan(ref reference4, ref reference3))
				{
					ref T reference8 = ref Unsafe.Add(ref reference4, 1);
					reference4 = ref reference8;
					if (!GreaterThan(ref left, ref reference8))
					{
						break;
					}
				}
				while (Unsafe.IsAddressGreaterThan(ref reference5, ref reference))
				{
					ref T reference9 = ref Unsafe.Add(ref reference5, -1);
					reference5 = ref reference9;
					if (!LessThan(ref left, ref reference9))
					{
						break;
					}
				}
			}
			if (!Unsafe.IsAddressLessThan(ref reference4, ref reference5))
			{
				break;
			}
			Swap(ref reference4, ref reference5);
		}
		if (!Unsafe.AreSame(ref reference4, ref reference3))
		{
			Swap(ref reference4, ref reference3);
		}
		return (int)((nint)Unsafe.ByteOffset(ref reference, ref reference4) / Unsafe.SizeOf<T>());
	}

	private static void HeapSort(Span<T> keys)
	{
		int length = keys.Length;
		for (int num = length >> 1; num >= 1; num--)
		{
			DownHeap(keys, num, length);
		}
		for (int num2 = length; num2 > 1; num2--)
		{
			Swap(ref keys[0], ref keys[num2 - 1]);
			DownHeap(keys, 1, num2 - 1);
		}
	}

	private static void DownHeap(Span<T> keys, int i, int n)
	{
		T left = keys[i - 1];
		while (i <= n >> 1)
		{
			int num = 2 * i;
			if (num < n && (keys[num - 1] == null || LessThan(ref keys[num - 1], ref keys[num])))
			{
				num++;
			}
			if (keys[num - 1] == null || !LessThan(ref left, ref keys[num - 1]))
			{
				break;
			}
			keys[i - 1] = keys[num - 1];
			i = num;
		}
		keys[i - 1] = left;
	}

	private static void InsertionSort(Span<T> keys)
	{
		for (int i = 0; i < keys.Length - 1; i++)
		{
			T left = Unsafe.Add(ref MemoryMarshal.GetReference(keys), i + 1);
			int num = i;
			while (num >= 0 && (left == null || LessThan(ref left, ref Unsafe.Add(ref MemoryMarshal.GetReference(keys), num))))
			{
				Unsafe.Add(ref MemoryMarshal.GetReference(keys), num + 1) = Unsafe.Add(ref MemoryMarshal.GetReference(keys), num);
				num--;
			}
			Unsafe.Add(ref MemoryMarshal.GetReference(keys), num + 1) = left;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool LessThan(ref T left, ref T right)
	{
		if (typeof(T) == typeof(byte))
		{
			if ((byte)(object)left >= (byte)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(sbyte))
		{
			if ((sbyte)(object)left >= (sbyte)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(ushort))
		{
			if ((ushort)(object)left >= (ushort)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(short))
		{
			if ((short)(object)left >= (short)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(uint))
		{
			if ((uint)(object)left >= (uint)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(int))
		{
			if ((int)(object)left >= (int)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(ulong))
		{
			if ((ulong)(object)left >= (ulong)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(long))
		{
			if ((long)(object)left >= (long)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(UIntPtr))
		{
			if ((nuint)(UIntPtr)(object)left >= (nuint)(UIntPtr)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(IntPtr))
		{
			if ((nint)(IntPtr)(object)left >= (nint)(IntPtr)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(float))
		{
			if (!((float)(object)left < (float)(object)right))
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(double))
		{
			if (!((double)(object)left < (double)(object)right))
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(Half))
		{
			if (!((Half)(object)left < (Half)(object)right))
			{
				return false;
			}
			return true;
		}
		if (left.CompareTo(right) >= 0)
		{
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool GreaterThan(ref T left, ref T right)
	{
		if (typeof(T) == typeof(byte))
		{
			if ((byte)(object)left <= (byte)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(sbyte))
		{
			if ((sbyte)(object)left <= (sbyte)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(ushort))
		{
			if ((ushort)(object)left <= (ushort)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(short))
		{
			if ((short)(object)left <= (short)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(uint))
		{
			if ((uint)(object)left <= (uint)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(int))
		{
			if ((int)(object)left <= (int)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(ulong))
		{
			if ((ulong)(object)left <= (ulong)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(long))
		{
			if ((long)(object)left <= (long)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(UIntPtr))
		{
			if ((nuint)(UIntPtr)(object)left <= (nuint)(UIntPtr)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(IntPtr))
		{
			if ((nint)(IntPtr)(object)left <= (nint)(IntPtr)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(float))
		{
			if (!((float)(object)left > (float)(object)right))
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(double))
		{
			if (!((double)(object)left > (double)(object)right))
			{
				return false;
			}
			return true;
		}
		if (typeof(T) == typeof(Half))
		{
			if (!((Half)(object)left > (Half)(object)right))
			{
				return false;
			}
			return true;
		}
		if (left.CompareTo(right) <= 0)
		{
			return false;
		}
		return true;
	}
}
internal sealed class GenericArraySortHelper<TKey, TValue> : IArraySortHelper<TKey, TValue> where TKey : IComparable<TKey>
{
	public void Sort(Span<TKey> keys, Span<TValue> values, IComparer<TKey> comparer)
	{
		try
		{
			if (comparer == null || comparer == Comparer<TKey>.Default)
			{
				if (keys.Length <= 1)
				{
					return;
				}
				if (typeof(TKey) == typeof(double) || typeof(TKey) == typeof(float) || typeof(TKey) == typeof(Half))
				{
					int num = SortUtils.MoveNansToFront(keys, values);
					if (num == keys.Length)
					{
						return;
					}
					keys = keys.Slice(num);
					values = values.Slice(num);
				}
				IntroSort(keys, values, 2 * (BitOperations.Log2((uint)keys.Length) + 1));
			}
			else
			{
				ArraySortHelper<TKey, TValue>.IntrospectiveSort(keys, values, comparer);
			}
		}
		catch (IndexOutOfRangeException)
		{
			ThrowHelper.ThrowArgumentException_BadComparer(comparer);
		}
		catch (Exception e)
		{
			ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
		}
	}

	private static void SwapIfGreaterWithValues(Span<TKey> keys, Span<TValue> values, int i, int j)
	{
		ref TKey reference = ref keys[i];
		if (reference != null && GreaterThan(ref reference, ref keys[j]))
		{
			TKey val = reference;
			keys[i] = keys[j];
			keys[j] = val;
			TValue val2 = values[i];
			values[i] = values[j];
			values[j] = val2;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Swap(Span<TKey> keys, Span<TValue> values, int i, int j)
	{
		TKey val = keys[i];
		keys[i] = keys[j];
		keys[j] = val;
		TValue val2 = values[i];
		values[i] = values[j];
		values[j] = val2;
	}

	private static void IntroSort(Span<TKey> keys, Span<TValue> values, int depthLimit)
	{
		int num = keys.Length;
		while (num > 1)
		{
			if (num <= 16)
			{
				switch (num)
				{
				case 2:
					SwapIfGreaterWithValues(keys, values, 0, 1);
					break;
				case 3:
					SwapIfGreaterWithValues(keys, values, 0, 1);
					SwapIfGreaterWithValues(keys, values, 0, 2);
					SwapIfGreaterWithValues(keys, values, 1, 2);
					break;
				default:
					InsertionSort(keys.Slice(0, num), values.Slice(0, num));
					break;
				}
				break;
			}
			if (depthLimit == 0)
			{
				HeapSort(keys.Slice(0, num), values.Slice(0, num));
				break;
			}
			depthLimit--;
			int num2 = PickPivotAndPartition(keys.Slice(0, num), values.Slice(0, num));
			Span<TKey> span = keys;
			Span<TKey> keys2 = span[(num2 + 1)..num];
			Span<TValue> span2 = values;
			IntroSort(keys2, span2[(num2 + 1)..num], depthLimit);
			num = num2;
		}
	}

	private static int PickPivotAndPartition(Span<TKey> keys, Span<TValue> values)
	{
		int num = keys.Length - 1;
		int num2 = num >> 1;
		SwapIfGreaterWithValues(keys, values, 0, num2);
		SwapIfGreaterWithValues(keys, values, 0, num);
		SwapIfGreaterWithValues(keys, values, num2, num);
		TKey left = keys[num2];
		Swap(keys, values, num2, num - 1);
		int num3 = 0;
		int num4 = num - 1;
		while (num3 < num4)
		{
			if (left == null)
			{
				while (num3 < num - 1 && keys[++num3] == null)
				{
				}
				while (num4 > 0 && keys[--num4] != null)
				{
				}
			}
			else
			{
				while (GreaterThan(ref left, ref keys[++num3]))
				{
				}
				while (LessThan(ref left, ref keys[--num4]))
				{
				}
			}
			if (num3 >= num4)
			{
				break;
			}
			Swap(keys, values, num3, num4);
		}
		if (num3 != num - 1)
		{
			Swap(keys, values, num3, num - 1);
		}
		return num3;
	}

	private static void HeapSort(Span<TKey> keys, Span<TValue> values)
	{
		int length = keys.Length;
		for (int num = length >> 1; num >= 1; num--)
		{
			DownHeap(keys, values, num, length);
		}
		for (int num2 = length; num2 > 1; num2--)
		{
			Swap(keys, values, 0, num2 - 1);
			DownHeap(keys, values, 1, num2 - 1);
		}
	}

	private static void DownHeap(Span<TKey> keys, Span<TValue> values, int i, int n)
	{
		TKey left = keys[i - 1];
		TValue val = values[i - 1];
		while (i <= n >> 1)
		{
			int num = 2 * i;
			if (num < n && (keys[num - 1] == null || LessThan(ref keys[num - 1], ref keys[num])))
			{
				num++;
			}
			if (keys[num - 1] == null || !LessThan(ref left, ref keys[num - 1]))
			{
				break;
			}
			keys[i - 1] = keys[num - 1];
			values[i - 1] = values[num - 1];
			i = num;
		}
		keys[i - 1] = left;
		values[i - 1] = val;
	}

	private static void InsertionSort(Span<TKey> keys, Span<TValue> values)
	{
		for (int i = 0; i < keys.Length - 1; i++)
		{
			TKey left = keys[i + 1];
			TValue val = values[i + 1];
			int num = i;
			while (num >= 0 && (left == null || LessThan(ref left, ref keys[num])))
			{
				keys[num + 1] = keys[num];
				values[num + 1] = values[num];
				num--;
			}
			keys[num + 1] = left;
			values[num + 1] = val;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool LessThan(ref TKey left, ref TKey right)
	{
		if (typeof(TKey) == typeof(byte))
		{
			if ((byte)(object)left >= (byte)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(sbyte))
		{
			if ((sbyte)(object)left >= (sbyte)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(ushort))
		{
			if ((ushort)(object)left >= (ushort)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(short))
		{
			if ((short)(object)left >= (short)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(uint))
		{
			if ((uint)(object)left >= (uint)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(int))
		{
			if ((int)(object)left >= (int)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(ulong))
		{
			if ((ulong)(object)left >= (ulong)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(long))
		{
			if ((long)(object)left >= (long)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(UIntPtr))
		{
			if ((nuint)(UIntPtr)(object)left >= (nuint)(UIntPtr)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(IntPtr))
		{
			if ((nint)(IntPtr)(object)left >= (nint)(IntPtr)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(float))
		{
			if (!((float)(object)left < (float)(object)right))
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(double))
		{
			if (!((double)(object)left < (double)(object)right))
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(Half))
		{
			if (!((Half)(object)left < (Half)(object)right))
			{
				return false;
			}
			return true;
		}
		if (left.CompareTo(right) >= 0)
		{
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool GreaterThan(ref TKey left, ref TKey right)
	{
		if (typeof(TKey) == typeof(byte))
		{
			if ((byte)(object)left <= (byte)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(sbyte))
		{
			if ((sbyte)(object)left <= (sbyte)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(ushort))
		{
			if ((ushort)(object)left <= (ushort)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(short))
		{
			if ((short)(object)left <= (short)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(uint))
		{
			if ((uint)(object)left <= (uint)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(int))
		{
			if ((int)(object)left <= (int)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(ulong))
		{
			if ((ulong)(object)left <= (ulong)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(long))
		{
			if ((long)(object)left <= (long)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(UIntPtr))
		{
			if ((nuint)(UIntPtr)(object)left <= (nuint)(UIntPtr)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(IntPtr))
		{
			if ((nint)(IntPtr)(object)left <= (nint)(IntPtr)(object)right)
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(float))
		{
			if (!((float)(object)left > (float)(object)right))
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(double))
		{
			if (!((double)(object)left > (double)(object)right))
			{
				return false;
			}
			return true;
		}
		if (typeof(TKey) == typeof(Half))
		{
			if (!((Half)(object)left > (Half)(object)right))
			{
				return false;
			}
			return true;
		}
		if (left.CompareTo(right) <= 0)
		{
			return false;
		}
		return true;
	}
}
