using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

[TypeDependency("System.Collections.Generic.GenericArraySortHelper`1")]
internal sealed class ArraySortHelper<T> : IArraySortHelper<T>
{
	private static readonly IArraySortHelper<T> s_defaultArraySortHelper = CreateArraySortHelper();

	public static IArraySortHelper<T> Default => s_defaultArraySortHelper;

	[DynamicDependency("#ctor", typeof(GenericArraySortHelper<>))]
	private static IArraySortHelper<T> CreateArraySortHelper()
	{
		if (typeof(IComparable<T>).IsAssignableFrom(typeof(T)))
		{
			return (IArraySortHelper<T>)RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(GenericArraySortHelper<string>), (RuntimeType)typeof(T));
		}
		return new ArraySortHelper<T>();
	}

	public void Sort(Span<T> keys, IComparer<T> comparer)
	{
		try
		{
			if (comparer == null)
			{
				comparer = Comparer<T>.Default;
			}
			IntrospectiveSort(keys, comparer.Compare);
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
			if (comparer == null)
			{
				comparer = Comparer<T>.Default;
			}
			return InternalBinarySearch(array, index, length, value, comparer);
		}
		catch (Exception e)
		{
			ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
			return 0;
		}
	}

	internal static void Sort(Span<T> keys, Comparison<T> comparer)
	{
		try
		{
			IntrospectiveSort(keys, comparer);
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

	internal static int InternalBinarySearch(T[] array, int index, int length, T value, IComparer<T> comparer)
	{
		int num = index;
		int num2 = index + length - 1;
		while (num <= num2)
		{
			int num3 = num + (num2 - num >> 1);
			int num4 = comparer.Compare(array[num3], value);
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

	private static void SwapIfGreater(Span<T> keys, Comparison<T> comparer, int i, int j)
	{
		if (comparer(keys[i], keys[j]) > 0)
		{
			T val = keys[i];
			keys[i] = keys[j];
			keys[j] = val;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Swap(Span<T> a, int i, int j)
	{
		T val = a[i];
		a[i] = a[j];
		a[j] = val;
	}

	internal static void IntrospectiveSort(Span<T> keys, Comparison<T> comparer)
	{
		if (keys.Length > 1)
		{
			IntroSort(keys, 2 * (BitOperations.Log2((uint)keys.Length) + 1), comparer);
		}
	}

	private static void IntroSort(Span<T> keys, int depthLimit, Comparison<T> comparer)
	{
		int num = keys.Length;
		while (num > 1)
		{
			if (num <= 16)
			{
				switch (num)
				{
				case 2:
					SwapIfGreater(keys, comparer, 0, 1);
					break;
				case 3:
					SwapIfGreater(keys, comparer, 0, 1);
					SwapIfGreater(keys, comparer, 0, 2);
					SwapIfGreater(keys, comparer, 1, 2);
					break;
				default:
					InsertionSort(keys.Slice(0, num), comparer);
					break;
				}
				break;
			}
			if (depthLimit == 0)
			{
				HeapSort(keys.Slice(0, num), comparer);
				break;
			}
			depthLimit--;
			int num2 = PickPivotAndPartition(keys.Slice(0, num), comparer);
			Span<T> span = keys;
			IntroSort(span[(num2 + 1)..num], depthLimit, comparer);
			num = num2;
		}
	}

	private static int PickPivotAndPartition(Span<T> keys, Comparison<T> comparer)
	{
		int num = keys.Length - 1;
		int num2 = num >> 1;
		SwapIfGreater(keys, comparer, 0, num2);
		SwapIfGreater(keys, comparer, 0, num);
		SwapIfGreater(keys, comparer, num2, num);
		T val = keys[num2];
		Swap(keys, num2, num - 1);
		int num3 = 0;
		int num4 = num - 1;
		while (num3 < num4)
		{
			while (comparer(keys[++num3], val) < 0)
			{
			}
			while (comparer(val, keys[--num4]) < 0)
			{
			}
			if (num3 >= num4)
			{
				break;
			}
			Swap(keys, num3, num4);
		}
		if (num3 != num - 1)
		{
			Swap(keys, num3, num - 1);
		}
		return num3;
	}

	private static void HeapSort(Span<T> keys, Comparison<T> comparer)
	{
		int length = keys.Length;
		for (int num = length >> 1; num >= 1; num--)
		{
			DownHeap(keys, num, length, comparer);
		}
		for (int num2 = length; num2 > 1; num2--)
		{
			Swap(keys, 0, num2 - 1);
			DownHeap(keys, 1, num2 - 1, comparer);
		}
	}

	private static void DownHeap(Span<T> keys, int i, int n, Comparison<T> comparer)
	{
		T val = keys[i - 1];
		while (i <= n >> 1)
		{
			int num = 2 * i;
			if (num < n && comparer(keys[num - 1], keys[num]) < 0)
			{
				num++;
			}
			if (comparer(val, keys[num - 1]) >= 0)
			{
				break;
			}
			keys[i - 1] = keys[num - 1];
			i = num;
		}
		keys[i - 1] = val;
	}

	private static void InsertionSort(Span<T> keys, Comparison<T> comparer)
	{
		for (int i = 0; i < keys.Length - 1; i++)
		{
			T val = keys[i + 1];
			int num = i;
			while (num >= 0 && comparer(val, keys[num]) < 0)
			{
				keys[num + 1] = keys[num];
				num--;
			}
			keys[num + 1] = val;
		}
	}
}
[TypeDependency("System.Collections.Generic.GenericArraySortHelper`2")]
internal sealed class ArraySortHelper<TKey, TValue> : IArraySortHelper<TKey, TValue>
{
	private static readonly IArraySortHelper<TKey, TValue> s_defaultArraySortHelper = CreateArraySortHelper();

	public static IArraySortHelper<TKey, TValue> Default => s_defaultArraySortHelper;

	[DynamicDependency("#ctor", typeof(GenericArraySortHelper<, >))]
	private static IArraySortHelper<TKey, TValue> CreateArraySortHelper()
	{
		if (typeof(IComparable<TKey>).IsAssignableFrom(typeof(TKey)))
		{
			return (IArraySortHelper<TKey, TValue>)RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(GenericArraySortHelper<string, string>), (RuntimeType)typeof(TKey), (RuntimeType)typeof(TValue));
		}
		return new ArraySortHelper<TKey, TValue>();
	}

	public void Sort(Span<TKey> keys, Span<TValue> values, IComparer<TKey> comparer)
	{
		try
		{
			IntrospectiveSort(keys, values, comparer ?? Comparer<TKey>.Default);
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

	private static void SwapIfGreaterWithValues(Span<TKey> keys, Span<TValue> values, IComparer<TKey> comparer, int i, int j)
	{
		if (comparer.Compare(keys[i], keys[j]) > 0)
		{
			TKey val = keys[i];
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

	internal static void IntrospectiveSort(Span<TKey> keys, Span<TValue> values, IComparer<TKey> comparer)
	{
		if (keys.Length > 1)
		{
			IntroSort(keys, values, 2 * (BitOperations.Log2((uint)keys.Length) + 1), comparer);
		}
	}

	private static void IntroSort(Span<TKey> keys, Span<TValue> values, int depthLimit, IComparer<TKey> comparer)
	{
		int num = keys.Length;
		while (num > 1)
		{
			if (num <= 16)
			{
				switch (num)
				{
				case 2:
					SwapIfGreaterWithValues(keys, values, comparer, 0, 1);
					break;
				case 3:
					SwapIfGreaterWithValues(keys, values, comparer, 0, 1);
					SwapIfGreaterWithValues(keys, values, comparer, 0, 2);
					SwapIfGreaterWithValues(keys, values, comparer, 1, 2);
					break;
				default:
					InsertionSort(keys.Slice(0, num), values.Slice(0, num), comparer);
					break;
				}
				break;
			}
			if (depthLimit == 0)
			{
				HeapSort(keys.Slice(0, num), values.Slice(0, num), comparer);
				break;
			}
			depthLimit--;
			int num2 = PickPivotAndPartition(keys.Slice(0, num), values.Slice(0, num), comparer);
			Span<TKey> span = keys;
			Span<TKey> keys2 = span[(num2 + 1)..num];
			Span<TValue> span2 = values;
			IntroSort(keys2, span2[(num2 + 1)..num], depthLimit, comparer);
			num = num2;
		}
	}

	private static int PickPivotAndPartition(Span<TKey> keys, Span<TValue> values, IComparer<TKey> comparer)
	{
		int num = keys.Length - 1;
		int num2 = num >> 1;
		SwapIfGreaterWithValues(keys, values, comparer, 0, num2);
		SwapIfGreaterWithValues(keys, values, comparer, 0, num);
		SwapIfGreaterWithValues(keys, values, comparer, num2, num);
		TKey val = keys[num2];
		Swap(keys, values, num2, num - 1);
		int num3 = 0;
		int num4 = num - 1;
		while (num3 < num4)
		{
			while (comparer.Compare(keys[++num3], val) < 0)
			{
			}
			while (comparer.Compare(val, keys[--num4]) < 0)
			{
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

	private static void HeapSort(Span<TKey> keys, Span<TValue> values, IComparer<TKey> comparer)
	{
		int length = keys.Length;
		for (int num = length >> 1; num >= 1; num--)
		{
			DownHeap(keys, values, num, length, comparer);
		}
		for (int num2 = length; num2 > 1; num2--)
		{
			Swap(keys, values, 0, num2 - 1);
			DownHeap(keys, values, 1, num2 - 1, comparer);
		}
	}

	private static void DownHeap(Span<TKey> keys, Span<TValue> values, int i, int n, IComparer<TKey> comparer)
	{
		TKey val = keys[i - 1];
		TValue val2 = values[i - 1];
		while (i <= n >> 1)
		{
			int num = 2 * i;
			if (num < n && comparer.Compare(keys[num - 1], keys[num]) < 0)
			{
				num++;
			}
			if (comparer.Compare(val, keys[num - 1]) >= 0)
			{
				break;
			}
			keys[i - 1] = keys[num - 1];
			values[i - 1] = values[num - 1];
			i = num;
		}
		keys[i - 1] = val;
		values[i - 1] = val2;
	}

	private static void InsertionSort(Span<TKey> keys, Span<TValue> values, IComparer<TKey> comparer)
	{
		for (int i = 0; i < keys.Length - 1; i++)
		{
			TKey val = keys[i + 1];
			TValue val2 = values[i + 1];
			int num = i;
			while (num >= 0 && comparer.Compare(val, keys[num]) < 0)
			{
				keys[num + 1] = keys[num];
				values[num + 1] = values[num];
				num--;
			}
			keys[num + 1] = val;
			values[num + 1] = val2;
		}
	}
}
