using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public abstract class Array : ICloneable, IList, ICollection, IEnumerable, IStructuralComparable, IStructuralEquatable
{
	private static class EmptyArray<T>
	{
		internal static readonly T[] Value = new T[0];
	}

	private readonly struct SorterObjectArray
	{
		private readonly object[] keys;

		private readonly object[] items;

		private readonly IComparer comparer;

		internal SorterObjectArray(object[] keys, object[] items, IComparer comparer)
		{
			this.keys = keys;
			this.items = items;
			this.comparer = comparer;
		}

		internal void SwapIfGreater(int a, int b)
		{
			if (a != b && comparer.Compare(keys[a], keys[b]) > 0)
			{
				object obj = keys[a];
				keys[a] = keys[b];
				keys[b] = obj;
				if (items != null)
				{
					object obj2 = items[a];
					items[a] = items[b];
					items[b] = obj2;
				}
			}
		}

		private void Swap(int i, int j)
		{
			object obj = keys[i];
			keys[i] = keys[j];
			keys[j] = obj;
			if (items != null)
			{
				object obj2 = items[i];
				items[i] = items[j];
				items[j] = obj2;
			}
		}

		internal void Sort(int left, int length)
		{
			IntrospectiveSort(left, length);
		}

		private void IntrospectiveSort(int left, int length)
		{
			if (length < 2)
			{
				return;
			}
			try
			{
				IntroSort(left, length + left - 1, 2 * (BitOperations.Log2((uint)length) + 1));
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

		private void IntroSort(int lo, int hi, int depthLimit)
		{
			while (hi > lo)
			{
				int num = hi - lo + 1;
				if (num <= 16)
				{
					switch (num)
					{
					case 2:
						SwapIfGreater(lo, hi);
						break;
					case 3:
						SwapIfGreater(lo, hi - 1);
						SwapIfGreater(lo, hi);
						SwapIfGreater(hi - 1, hi);
						break;
					default:
						InsertionSort(lo, hi);
						break;
					}
					break;
				}
				if (depthLimit == 0)
				{
					Heapsort(lo, hi);
					break;
				}
				depthLimit--;
				int num2 = PickPivotAndPartition(lo, hi);
				IntroSort(num2 + 1, hi, depthLimit);
				hi = num2 - 1;
			}
		}

		private int PickPivotAndPartition(int lo, int hi)
		{
			int num = lo + (hi - lo) / 2;
			SwapIfGreater(lo, num);
			SwapIfGreater(lo, hi);
			SwapIfGreater(num, hi);
			object obj = keys[num];
			Swap(num, hi - 1);
			int num2 = lo;
			int num3 = hi - 1;
			while (num2 < num3)
			{
				while (comparer.Compare(keys[++num2], obj) < 0)
				{
				}
				while (comparer.Compare(obj, keys[--num3]) < 0)
				{
				}
				if (num2 >= num3)
				{
					break;
				}
				Swap(num2, num3);
			}
			if (num2 != hi - 1)
			{
				Swap(num2, hi - 1);
			}
			return num2;
		}

		private void Heapsort(int lo, int hi)
		{
			int num = hi - lo + 1;
			for (int num2 = num / 2; num2 >= 1; num2--)
			{
				DownHeap(num2, num, lo);
			}
			for (int num3 = num; num3 > 1; num3--)
			{
				Swap(lo, lo + num3 - 1);
				DownHeap(1, num3 - 1, lo);
			}
		}

		private void DownHeap(int i, int n, int lo)
		{
			object obj = keys[lo + i - 1];
			object[] array = items;
			object obj2 = ((array != null) ? array[lo + i - 1] : null);
			while (i <= n / 2)
			{
				int num = 2 * i;
				if (num < n && comparer.Compare(keys[lo + num - 1], keys[lo + num]) < 0)
				{
					num++;
				}
				if (comparer.Compare(obj, keys[lo + num - 1]) >= 0)
				{
					break;
				}
				keys[lo + i - 1] = keys[lo + num - 1];
				if (items != null)
				{
					items[lo + i - 1] = items[lo + num - 1];
				}
				i = num;
			}
			keys[lo + i - 1] = obj;
			if (items != null)
			{
				items[lo + i - 1] = obj2;
			}
		}

		private void InsertionSort(int lo, int hi)
		{
			for (int i = lo; i < hi; i++)
			{
				int num = i;
				object obj = keys[i + 1];
				object[] array = items;
				object obj2 = ((array != null) ? array[i + 1] : null);
				while (num >= lo && comparer.Compare(obj, keys[num]) < 0)
				{
					keys[num + 1] = keys[num];
					if (items != null)
					{
						items[num + 1] = items[num];
					}
					num--;
				}
				keys[num + 1] = obj;
				if (items != null)
				{
					items[num + 1] = obj2;
				}
			}
		}
	}

	private readonly struct SorterGenericArray
	{
		private readonly Array keys;

		private readonly Array items;

		private readonly IComparer comparer;

		internal SorterGenericArray(Array keys, Array items, IComparer comparer)
		{
			this.keys = keys;
			this.items = items;
			this.comparer = comparer;
		}

		internal void SwapIfGreater(int a, int b)
		{
			if (a != b && comparer.Compare(keys.GetValue(a), keys.GetValue(b)) > 0)
			{
				object value = keys.GetValue(a);
				keys.SetValue(keys.GetValue(b), a);
				keys.SetValue(value, b);
				if (items != null)
				{
					object value2 = items.GetValue(a);
					items.SetValue(items.GetValue(b), a);
					items.SetValue(value2, b);
				}
			}
		}

		private void Swap(int i, int j)
		{
			object value = keys.GetValue(i);
			keys.SetValue(keys.GetValue(j), i);
			keys.SetValue(value, j);
			if (items != null)
			{
				object value2 = items.GetValue(i);
				items.SetValue(items.GetValue(j), i);
				items.SetValue(value2, j);
			}
		}

		internal void Sort(int left, int length)
		{
			IntrospectiveSort(left, length);
		}

		private void IntrospectiveSort(int left, int length)
		{
			if (length < 2)
			{
				return;
			}
			try
			{
				IntroSort(left, length + left - 1, 2 * (BitOperations.Log2((uint)length) + 1));
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

		private void IntroSort(int lo, int hi, int depthLimit)
		{
			while (hi > lo)
			{
				int num = hi - lo + 1;
				if (num <= 16)
				{
					switch (num)
					{
					case 2:
						SwapIfGreater(lo, hi);
						break;
					case 3:
						SwapIfGreater(lo, hi - 1);
						SwapIfGreater(lo, hi);
						SwapIfGreater(hi - 1, hi);
						break;
					default:
						InsertionSort(lo, hi);
						break;
					}
					break;
				}
				if (depthLimit == 0)
				{
					Heapsort(lo, hi);
					break;
				}
				depthLimit--;
				int num2 = PickPivotAndPartition(lo, hi);
				IntroSort(num2 + 1, hi, depthLimit);
				hi = num2 - 1;
			}
		}

		private int PickPivotAndPartition(int lo, int hi)
		{
			int num = lo + (hi - lo) / 2;
			SwapIfGreater(lo, num);
			SwapIfGreater(lo, hi);
			SwapIfGreater(num, hi);
			object value = keys.GetValue(num);
			Swap(num, hi - 1);
			int num2 = lo;
			int num3 = hi - 1;
			while (num2 < num3)
			{
				while (comparer.Compare(keys.GetValue(++num2), value) < 0)
				{
				}
				while (comparer.Compare(value, keys.GetValue(--num3)) < 0)
				{
				}
				if (num2 >= num3)
				{
					break;
				}
				Swap(num2, num3);
			}
			if (num2 != hi - 1)
			{
				Swap(num2, hi - 1);
			}
			return num2;
		}

		private void Heapsort(int lo, int hi)
		{
			int num = hi - lo + 1;
			for (int num2 = num / 2; num2 >= 1; num2--)
			{
				DownHeap(num2, num, lo);
			}
			for (int num3 = num; num3 > 1; num3--)
			{
				Swap(lo, lo + num3 - 1);
				DownHeap(1, num3 - 1, lo);
			}
		}

		private void DownHeap(int i, int n, int lo)
		{
			object value = keys.GetValue(lo + i - 1);
			object value2 = items?.GetValue(lo + i - 1);
			while (i <= n / 2)
			{
				int num = 2 * i;
				if (num < n && comparer.Compare(keys.GetValue(lo + num - 1), keys.GetValue(lo + num)) < 0)
				{
					num++;
				}
				if (comparer.Compare(value, keys.GetValue(lo + num - 1)) >= 0)
				{
					break;
				}
				keys.SetValue(keys.GetValue(lo + num - 1), lo + i - 1);
				if (items != null)
				{
					items.SetValue(items.GetValue(lo + num - 1), lo + i - 1);
				}
				i = num;
			}
			keys.SetValue(value, lo + i - 1);
			if (items != null)
			{
				items.SetValue(value2, lo + i - 1);
			}
		}

		private void InsertionSort(int lo, int hi)
		{
			for (int i = lo; i < hi; i++)
			{
				int num = i;
				object value = keys.GetValue(i + 1);
				object value2 = items?.GetValue(i + 1);
				while (num >= lo && comparer.Compare(value, keys.GetValue(num)) < 0)
				{
					keys.SetValue(keys.GetValue(num), num + 1);
					if (items != null)
					{
						items.SetValue(items.GetValue(num), num + 1);
					}
					num--;
				}
				keys.SetValue(value, num + 1);
				if (items != null)
				{
					items.SetValue(value2, num + 1);
				}
			}
		}
	}

	public int Length => checked((int)Unsafe.As<RawArrayData>(this).Length);

	internal nuint NativeLength => Unsafe.As<RawArrayData>(this).Length;

	public long LongLength => (long)NativeLength;

	public int Rank
	{
		get
		{
			int multiDimensionalArrayRank = RuntimeHelpers.GetMultiDimensionalArrayRank(this);
			if (multiDimensionalArrayRank == 0)
			{
				return 1;
			}
			return multiDimensionalArrayRank;
		}
	}

	int ICollection.Count => Length;

	public object SyncRoot => this;

	public bool IsReadOnly => false;

	public bool IsFixedSize => true;

	public bool IsSynchronized => false;

	object? IList.this[int index]
	{
		get
		{
			return GetValue(index);
		}
		set
		{
			SetValue(value, index);
		}
	}

	public static int MaxLength => 2147483591;

	public unsafe static Array CreateInstance(Type elementType, int length)
	{
		if ((object)elementType == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementType);
		}
		if (length < 0)
		{
			ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
		}
		RuntimeType runtimeType = elementType.UnderlyingSystemType as RuntimeType;
		if (runtimeType == null)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);
		}
		return InternalCreate((void*)runtimeType.TypeHandle.Value, 1, &length, null);
	}

	public unsafe static Array CreateInstance(Type elementType, int length1, int length2)
	{
		if ((object)elementType == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementType);
		}
		if (length1 < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length1, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (length2 < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length2, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		RuntimeType runtimeType = elementType.UnderlyingSystemType as RuntimeType;
		if (runtimeType == null)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);
		}
		int* ptr = stackalloc int[2];
		*ptr = length1;
		ptr[1] = length2;
		return InternalCreate((void*)runtimeType.TypeHandle.Value, 2, ptr, null);
	}

	public unsafe static Array CreateInstance(Type elementType, int length1, int length2, int length3)
	{
		if ((object)elementType == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementType);
		}
		if (length1 < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length1, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (length2 < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length2, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (length3 < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length3, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		RuntimeType runtimeType = elementType.UnderlyingSystemType as RuntimeType;
		if (runtimeType == null)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);
		}
		int* ptr = stackalloc int[3];
		*ptr = length1;
		ptr[1] = length2;
		ptr[2] = length3;
		return InternalCreate((void*)runtimeType.TypeHandle.Value, 3, ptr, null);
	}

	public unsafe static Array CreateInstance(Type elementType, params int[] lengths)
	{
		if ((object)elementType == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementType);
		}
		if (lengths == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.lengths);
		}
		if (lengths.Length == 0)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NeedAtLeast1Rank);
		}
		RuntimeType runtimeType = elementType.UnderlyingSystemType as RuntimeType;
		if (runtimeType == null)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);
		}
		for (int i = 0; i < lengths.Length; i++)
		{
			if (lengths[i] < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.lengths, i, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
		}
		fixed (int* pLengths = &lengths[0])
		{
			return InternalCreate((void*)runtimeType.TypeHandle.Value, lengths.Length, pLengths, null);
		}
	}

	public unsafe static Array CreateInstance(Type elementType, int[] lengths, int[] lowerBounds)
	{
		if (elementType == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementType);
		}
		if (lengths == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.lengths);
		}
		if (lowerBounds == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.lowerBounds);
		}
		if (lengths.Length != lowerBounds.Length)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RanksAndBounds);
		}
		if (lengths.Length == 0)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NeedAtLeast1Rank);
		}
		RuntimeType runtimeType = elementType.UnderlyingSystemType as RuntimeType;
		if (runtimeType == null)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);
		}
		for (int i = 0; i < lengths.Length; i++)
		{
			if (lengths[i] < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.lengths, i, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
		}
		fixed (int* pLengths = &lengths[0])
		{
			fixed (int* pLowerBounds = &lowerBounds[0])
			{
				return InternalCreate((void*)runtimeType.TypeHandle.Value, lengths.Length, pLengths, pLowerBounds);
			}
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern Array InternalCreate(void* elementType, int rank, int* pLengths, int* pLowerBounds);

	public unsafe static void Copy(Array sourceArray, Array destinationArray, int length)
	{
		if (sourceArray == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.sourceArray);
		}
		if (destinationArray == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.destinationArray);
		}
		MethodTable* methodTable = RuntimeHelpers.GetMethodTable(sourceArray);
		if (methodTable == RuntimeHelpers.GetMethodTable(destinationArray) && !methodTable->IsMultiDimensionalArray && (uint)length <= sourceArray.NativeLength && (uint)length <= destinationArray.NativeLength)
		{
			nuint num = (nuint)(uint)length * (nuint)methodTable->ComponentSize;
			ref byte data = ref Unsafe.As<RawArrayData>(sourceArray).Data;
			ref byte data2 = ref Unsafe.As<RawArrayData>(destinationArray).Data;
			if (methodTable->ContainsGCPointers)
			{
				Buffer.BulkMoveWithWriteBarrier(ref data2, ref data, num);
			}
			else
			{
				Buffer.Memmove(ref data2, ref data, num);
			}
		}
		else
		{
			Copy(sourceArray, sourceArray.GetLowerBound(0), destinationArray, destinationArray.GetLowerBound(0), length, reliable: false);
		}
	}

	public unsafe static void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
	{
		if (sourceArray != null && destinationArray != null)
		{
			MethodTable* methodTable = RuntimeHelpers.GetMethodTable(sourceArray);
			if (methodTable == RuntimeHelpers.GetMethodTable(destinationArray) && !methodTable->IsMultiDimensionalArray && length >= 0 && sourceIndex >= 0 && destinationIndex >= 0 && (uint)(sourceIndex + length) <= sourceArray.NativeLength && (uint)(destinationIndex + length) <= destinationArray.NativeLength)
			{
				nuint num = methodTable->ComponentSize;
				nuint num2 = (uint)length * num;
				ref byte reference = ref Unsafe.AddByteOffset(ref Unsafe.As<RawArrayData>(sourceArray).Data, (uint)sourceIndex * num);
				ref byte reference2 = ref Unsafe.AddByteOffset(ref Unsafe.As<RawArrayData>(destinationArray).Data, (uint)destinationIndex * num);
				if (methodTable->ContainsGCPointers)
				{
					Buffer.BulkMoveWithWriteBarrier(ref reference2, ref reference, num2);
				}
				else
				{
					Buffer.Memmove(ref reference2, ref reference, num2);
				}
				return;
			}
		}
		Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length, reliable: false);
	}

	private unsafe static void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, bool reliable)
	{
		if (sourceArray == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.sourceArray);
		}
		if (destinationArray == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.destinationArray);
		}
		if (sourceArray.GetType() != destinationArray.GetType() && sourceArray.Rank != destinationArray.Rank)
		{
			throw new RankException(SR.Rank_MustMatch);
		}
		if (length < 0)
		{
			throw new ArgumentOutOfRangeException("length", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		int lowerBound = sourceArray.GetLowerBound(0);
		if (sourceIndex < lowerBound || sourceIndex - lowerBound < 0)
		{
			throw new ArgumentOutOfRangeException("sourceIndex", SR.ArgumentOutOfRange_ArrayLB);
		}
		sourceIndex -= lowerBound;
		int lowerBound2 = destinationArray.GetLowerBound(0);
		if (destinationIndex < lowerBound2 || destinationIndex - lowerBound2 < 0)
		{
			throw new ArgumentOutOfRangeException("destinationIndex", SR.ArgumentOutOfRange_ArrayLB);
		}
		destinationIndex -= lowerBound2;
		if ((uint)(sourceIndex + length) > sourceArray.NativeLength)
		{
			throw new ArgumentException(SR.Arg_LongerThanSrcArray, "sourceArray");
		}
		if ((uint)(destinationIndex + length) > destinationArray.NativeLength)
		{
			throw new ArgumentException(SR.Arg_LongerThanDestArray, "destinationArray");
		}
		if (sourceArray.GetType() == destinationArray.GetType() || IsSimpleCopy(sourceArray, destinationArray))
		{
			MethodTable* methodTable = RuntimeHelpers.GetMethodTable(sourceArray);
			nuint num = methodTable->ComponentSize;
			nuint num2 = (uint)length * num;
			ref byte reference = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(sourceArray), (uint)sourceIndex * num);
			ref byte reference2 = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(destinationArray), (uint)destinationIndex * num);
			if (methodTable->ContainsGCPointers)
			{
				Buffer.BulkMoveWithWriteBarrier(ref reference2, ref reference, num2);
			}
			else
			{
				Buffer.Memmove(ref reference2, ref reference, num2);
			}
		}
		else
		{
			if (reliable)
			{
				throw new ArrayTypeMismatchException(SR.ArrayTypeMismatch_ConstrainedCopy);
			}
			CopySlow(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool IsSimpleCopy(Array sourceArray, Array destinationArray);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void CopySlow(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length);

	public static void ConstrainedCopy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
	{
		Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length, reliable: true);
	}

	public unsafe static void Clear(Array array)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		MethodTable* methodTable = RuntimeHelpers.GetMethodTable(array);
		nuint num = methodTable->ComponentSize * array.NativeLength;
		ref byte arrayDataReference = ref MemoryMarshal.GetArrayDataReference(array);
		if (!methodTable->ContainsGCPointers)
		{
			SpanHelpers.ClearWithoutReferences(ref arrayDataReference, num);
		}
		else
		{
			SpanHelpers.ClearWithReferences(ref Unsafe.As<byte, IntPtr>(ref arrayDataReference), num / (nuint)sizeof(IntPtr));
		}
	}

	public unsafe static void Clear(Array array, int index, int length)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		ref byte source = ref Unsafe.As<RawArrayData>(array).Data;
		int num = 0;
		MethodTable* methodTable = RuntimeHelpers.GetMethodTable(array);
		if (methodTable->IsMultiDimensionalArray)
		{
			int multiDimensionalArrayRank = methodTable->MultiDimensionalArrayRank;
			num = Unsafe.Add(ref Unsafe.As<byte, int>(ref source), multiDimensionalArrayRank);
			source = ref Unsafe.Add(ref source, 8 * multiDimensionalArrayRank);
		}
		int num2 = index - num;
		if (index < num || num2 < 0 || length < 0 || (uint)(num2 + length) > array.NativeLength)
		{
			ThrowHelper.ThrowIndexOutOfRangeException();
		}
		nuint num3 = methodTable->ComponentSize;
		ref byte reference = ref Unsafe.AddByteOffset(ref source, (uint)num2 * num3);
		nuint num4 = (uint)length * num3;
		if (methodTable->ContainsGCPointers)
		{
			SpanHelpers.ClearWithReferences(ref Unsafe.As<byte, IntPtr>(ref reference), num4 / (uint)sizeof(IntPtr));
		}
		else
		{
			SpanHelpers.ClearWithoutReferences(ref reference, num4);
		}
	}

	private unsafe nint GetFlattenedIndex(ReadOnlySpan<int> indices)
	{
		if (RuntimeHelpers.GetMethodTable(this)->IsMultiDimensionalArray)
		{
			ref int multiDimensionalArrayBounds = ref RuntimeHelpers.GetMultiDimensionalArrayBounds(this);
			nint num = 0;
			for (int i = 0; i < indices.Length; i++)
			{
				int num2 = indices[i] - Unsafe.Add(ref multiDimensionalArrayBounds, indices.Length + i);
				int num3 = Unsafe.Add(ref multiDimensionalArrayBounds, i);
				if ((uint)num2 >= (uint)num3)
				{
					ThrowHelper.ThrowIndexOutOfRangeException();
				}
				num = num3 * num + num2;
			}
			return num;
		}
		int num4 = indices[0];
		if ((uint)num4 >= (uint)LongLength)
		{
			ThrowHelper.ThrowIndexOutOfRangeException();
		}
		return num4;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal extern object InternalGetValue(nint flattenedIndex);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern void InternalSetValue(object value, nint flattenedIndex);

	public int GetLength(int dimension)
	{
		int multiDimensionalArrayRank = RuntimeHelpers.GetMultiDimensionalArrayRank(this);
		if (multiDimensionalArrayRank == 0 && dimension == 0)
		{
			return Length;
		}
		if ((uint)dimension >= (uint)multiDimensionalArrayRank)
		{
			throw new IndexOutOfRangeException(SR.IndexOutOfRange_ArrayRankIndex);
		}
		return Unsafe.Add(ref RuntimeHelpers.GetMultiDimensionalArrayBounds(this), dimension);
	}

	public int GetUpperBound(int dimension)
	{
		int multiDimensionalArrayRank = RuntimeHelpers.GetMultiDimensionalArrayRank(this);
		if (multiDimensionalArrayRank == 0 && dimension == 0)
		{
			return Length - 1;
		}
		if ((uint)dimension >= (uint)multiDimensionalArrayRank)
		{
			throw new IndexOutOfRangeException(SR.IndexOutOfRange_ArrayRankIndex);
		}
		ref int multiDimensionalArrayBounds = ref RuntimeHelpers.GetMultiDimensionalArrayBounds(this);
		return Unsafe.Add(ref multiDimensionalArrayBounds, dimension) + Unsafe.Add(ref multiDimensionalArrayBounds, multiDimensionalArrayRank + dimension) - 1;
	}

	public int GetLowerBound(int dimension)
	{
		int multiDimensionalArrayRank = RuntimeHelpers.GetMultiDimensionalArrayRank(this);
		if (multiDimensionalArrayRank == 0 && dimension == 0)
		{
			return 0;
		}
		if ((uint)dimension >= (uint)multiDimensionalArrayRank)
		{
			throw new IndexOutOfRangeException(SR.IndexOutOfRange_ArrayRankIndex);
		}
		return Unsafe.Add(ref RuntimeHelpers.GetMultiDimensionalArrayBounds(this), multiDimensionalArrayRank + dimension);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal extern CorElementType GetCorElementTypeOfElementType();

	private unsafe bool IsValueOfElementType(object value)
	{
		MethodTable* methodTable = RuntimeHelpers.GetMethodTable(this);
		return (IntPtr)methodTable->ElementType == (IntPtr)RuntimeHelpers.GetMethodTable(value);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	public extern void Initialize();

	private protected Array()
	{
	}

	public static ReadOnlyCollection<T> AsReadOnly<T>(T[] array)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		return new ReadOnlyCollection<T>(array);
	}

	public static void Resize<T>([NotNull] ref T[]? array, int newSize)
	{
		if (newSize < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.newSize, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		T[] array2 = array;
		if (array2 == null)
		{
			array = new T[newSize];
		}
		else if (array2.Length != newSize)
		{
			T[] array3 = new T[newSize];
			Buffer.Memmove(ref MemoryMarshal.GetArrayDataReference(array3), ref MemoryMarshal.GetArrayDataReference(array2), (uint)Math.Min(newSize, array2.Length));
			array = array3;
		}
	}

	public static Array CreateInstance(Type elementType, params long[] lengths)
	{
		if (lengths == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.lengths);
		}
		if (lengths.Length == 0)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NeedAtLeast1Rank);
		}
		int[] array = new int[lengths.Length];
		for (int i = 0; i < lengths.Length; i++)
		{
			long num = lengths[i];
			int num2 = (int)num;
			if (num != num2)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.len, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
			}
			array[i] = num2;
		}
		return CreateInstance(elementType, array);
	}

	public static void Copy(Array sourceArray, Array destinationArray, long length)
	{
		int num = (int)length;
		if (length != num)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
		}
		Copy(sourceArray, destinationArray, num);
	}

	public static void Copy(Array sourceArray, long sourceIndex, Array destinationArray, long destinationIndex, long length)
	{
		int num = (int)sourceIndex;
		int num2 = (int)destinationIndex;
		int num3 = (int)length;
		if (sourceIndex != num)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.sourceIndex, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
		}
		if (destinationIndex != num2)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.destinationIndex, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
		}
		if (length != num3)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
		}
		Copy(sourceArray, num, destinationArray, num2, num3);
	}

	public object? GetValue(params int[] indices)
	{
		if (indices == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indices);
		}
		if (Rank != indices.Length)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);
		}
		return InternalGetValue(GetFlattenedIndex(new ReadOnlySpan<int>(indices)));
	}

	public unsafe object? GetValue(int index)
	{
		if (Rank != 1)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_Need1DArray);
		}
		return InternalGetValue(GetFlattenedIndex(new ReadOnlySpan<int>(&index, 1)));
	}

	public object? GetValue(int index1, int index2)
	{
		if (Rank != 2)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_Need2DArray);
		}
		Span<int> span = stackalloc int[2] { index1, index2 };
		return InternalGetValue(GetFlattenedIndex(span));
	}

	public object? GetValue(int index1, int index2, int index3)
	{
		if (Rank != 3)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_Need3DArray);
		}
		Span<int> span = stackalloc int[3] { index1, index2, index3 };
		return InternalGetValue(GetFlattenedIndex(span));
	}

	public unsafe void SetValue(object? value, int index)
	{
		if (Rank != 1)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_Need1DArray);
		}
		InternalSetValue(value, GetFlattenedIndex(new ReadOnlySpan<int>(&index, 1)));
	}

	public void SetValue(object? value, int index1, int index2)
	{
		if (Rank != 2)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_Need2DArray);
		}
		Span<int> span = stackalloc int[2] { index1, index2 };
		InternalSetValue(value, GetFlattenedIndex(span));
	}

	public void SetValue(object? value, int index1, int index2, int index3)
	{
		if (Rank != 3)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_Need3DArray);
		}
		Span<int> span = stackalloc int[3] { index1, index2, index3 };
		InternalSetValue(value, GetFlattenedIndex(span));
	}

	public void SetValue(object? value, params int[] indices)
	{
		if (indices == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indices);
		}
		if (Rank != indices.Length)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);
		}
		InternalSetValue(value, GetFlattenedIndex(new ReadOnlySpan<int>(indices)));
	}

	public object? GetValue(long index)
	{
		int num = (int)index;
		if (index != num)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
		}
		return GetValue(num);
	}

	public object? GetValue(long index1, long index2)
	{
		int num = (int)index1;
		int num2 = (int)index2;
		if (index1 != num)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index1, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
		}
		if (index2 != num2)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index2, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
		}
		return GetValue(num, num2);
	}

	public object? GetValue(long index1, long index2, long index3)
	{
		int num = (int)index1;
		int num2 = (int)index2;
		int num3 = (int)index3;
		if (index1 != num)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index1, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
		}
		if (index2 != num2)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index2, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
		}
		if (index3 != num3)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index3, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
		}
		return GetValue(num, num2, num3);
	}

	public object? GetValue(params long[] indices)
	{
		if (indices == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indices);
		}
		if (Rank != indices.Length)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);
		}
		int[] array = new int[indices.Length];
		for (int i = 0; i < indices.Length; i++)
		{
			long num = indices[i];
			int num2 = (int)num;
			if (num != num2)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
			}
			array[i] = num2;
		}
		return GetValue(array);
	}

	public void SetValue(object? value, long index)
	{
		int num = (int)index;
		if (index != num)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
		}
		SetValue(value, num);
	}

	public void SetValue(object? value, long index1, long index2)
	{
		int num = (int)index1;
		int num2 = (int)index2;
		if (index1 != num)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index1, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
		}
		if (index2 != num2)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index2, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
		}
		SetValue(value, num, num2);
	}

	public void SetValue(object? value, long index1, long index2, long index3)
	{
		int num = (int)index1;
		int num2 = (int)index2;
		int num3 = (int)index3;
		if (index1 != num)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index1, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
		}
		if (index2 != num2)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index2, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
		}
		if (index3 != num3)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index3, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
		}
		SetValue(value, num, num2, num3);
	}

	public void SetValue(object? value, params long[] indices)
	{
		if (indices == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indices);
		}
		if (Rank != indices.Length)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);
		}
		int[] array = new int[indices.Length];
		for (int i = 0; i < indices.Length; i++)
		{
			long num = indices[i];
			int num2 = (int)num;
			if (num != num2)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
			}
			array[i] = num2;
		}
		SetValue(value, array);
	}

	private static int GetMedian(int low, int hi)
	{
		return low + (hi - low >> 1);
	}

	public long GetLongLength(int dimension)
	{
		return GetLength(dimension);
	}

	int IList.Add(object value)
	{
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
		return 0;
	}

	bool IList.Contains(object value)
	{
		return IndexOf(this, value) >= GetLowerBound(0);
	}

	void IList.Clear()
	{
		Clear(this);
	}

	int IList.IndexOf(object value)
	{
		return IndexOf(this, value);
	}

	void IList.Insert(int index, object value)
	{
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
	}

	void IList.Remove(object value)
	{
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
	}

	void IList.RemoveAt(int index)
	{
		ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
	}

	[Intrinsic]
	public object Clone()
	{
		return MemberwiseClone();
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		if (other == null)
		{
			return 1;
		}
		Array array = other as Array;
		if (array == null || Length != array.Length)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.ArgumentException_OtherNotArrayOfCorrectLength, ExceptionArgument.other);
		}
		int i = 0;
		int num = 0;
		for (; i < array.Length; i++)
		{
			if (num != 0)
			{
				break;
			}
			object value = GetValue(i);
			object value2 = array.GetValue(i);
			num = comparer.Compare(value, value2);
		}
		return num;
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		if (other == null)
		{
			return false;
		}
		if (this == other)
		{
			return true;
		}
		if (!(other is Array array) || array.Length != Length)
		{
			return false;
		}
		for (int i = 0; i < array.Length; i++)
		{
			object value = GetValue(i);
			object value2 = array.GetValue(i);
			if (!comparer.Equals(value, value2))
			{
				return false;
			}
		}
		return true;
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		if (comparer == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparer);
		}
		HashCode hashCode = default(HashCode);
		for (int i = ((Length >= 8) ? (Length - 8) : 0); i < Length; i++)
		{
			hashCode.Add(comparer.GetHashCode(GetValue(i)));
		}
		return hashCode.ToHashCode();
	}

	public static int BinarySearch(Array array, object? value)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		return BinarySearch(array, array.GetLowerBound(0), array.Length, value, null);
	}

	public static int BinarySearch(Array array, int index, int length, object? value)
	{
		return BinarySearch(array, index, length, value, null);
	}

	public static int BinarySearch(Array array, object? value, IComparer? comparer)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		return BinarySearch(array, array.GetLowerBound(0), array.Length, value, comparer);
	}

	public static int BinarySearch(Array array, int index, int length, object? value, IComparer? comparer)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		int lowerBound = array.GetLowerBound(0);
		if (index < lowerBound)
		{
			ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
		}
		if (length < 0)
		{
			ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
		}
		if (array.Length - (index - lowerBound) < length)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
		}
		if (array.Rank != 1)
		{
			ThrowHelper.ThrowRankException(ExceptionResource.Rank_MultiDimNotSupported);
		}
		if (comparer == null)
		{
			comparer = Comparer.Default;
		}
		int num = index;
		int num2 = index + length - 1;
		if (array is object[] array2)
		{
			while (num <= num2)
			{
				int median = GetMedian(num, num2);
				int num3;
				try
				{
					num3 = comparer.Compare(array2[median], value);
				}
				catch (Exception e)
				{
					ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
					return 0;
				}
				if (num3 == 0)
				{
					return median;
				}
				if (num3 < 0)
				{
					num = median + 1;
				}
				else
				{
					num2 = median - 1;
				}
			}
			return ~num;
		}
		if (comparer == Comparer.Default)
		{
			CorElementType corElementTypeOfElementType = array.GetCorElementTypeOfElementType();
			if (corElementTypeOfElementType.IsPrimitiveType())
			{
				if (value == null)
				{
					return ~index;
				}
				if (array.IsValueOfElementType(value))
				{
					int adjustedIndex2 = index - lowerBound;
					int num4 = -1;
					switch (corElementTypeOfElementType)
					{
					case CorElementType.ELEMENT_TYPE_I1:
						num4 = GenericBinarySearch<sbyte>(array, adjustedIndex2, length, value);
						break;
					case CorElementType.ELEMENT_TYPE_BOOLEAN:
					case CorElementType.ELEMENT_TYPE_U1:
						num4 = GenericBinarySearch<byte>(array, adjustedIndex2, length, value);
						break;
					case CorElementType.ELEMENT_TYPE_I2:
						num4 = GenericBinarySearch<short>(array, adjustedIndex2, length, value);
						break;
					case CorElementType.ELEMENT_TYPE_CHAR:
					case CorElementType.ELEMENT_TYPE_U2:
						num4 = GenericBinarySearch<ushort>(array, adjustedIndex2, length, value);
						break;
					case CorElementType.ELEMENT_TYPE_I4:
						num4 = GenericBinarySearch<int>(array, adjustedIndex2, length, value);
						break;
					case CorElementType.ELEMENT_TYPE_U4:
						num4 = GenericBinarySearch<uint>(array, adjustedIndex2, length, value);
						break;
					case CorElementType.ELEMENT_TYPE_I8:
					case CorElementType.ELEMENT_TYPE_I:
						num4 = GenericBinarySearch<long>(array, adjustedIndex2, length, value);
						break;
					case CorElementType.ELEMENT_TYPE_U8:
					case CorElementType.ELEMENT_TYPE_U:
						num4 = GenericBinarySearch<ulong>(array, adjustedIndex2, length, value);
						break;
					case CorElementType.ELEMENT_TYPE_R4:
						num4 = GenericBinarySearch<float>(array, adjustedIndex2, length, value);
						break;
					case CorElementType.ELEMENT_TYPE_R8:
						num4 = GenericBinarySearch<double>(array, adjustedIndex2, length, value);
						break;
					}
					if (num4 < 0)
					{
						return ~(index + ~num4);
					}
					return index + num4;
				}
			}
		}
		while (num <= num2)
		{
			int median2 = GetMedian(num, num2);
			int num5;
			try
			{
				num5 = comparer.Compare(array.GetValue(median2), value);
			}
			catch (Exception e2)
			{
				ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e2);
				return 0;
			}
			if (num5 == 0)
			{
				return median2;
			}
			if (num5 < 0)
			{
				num = median2 + 1;
			}
			else
			{
				num2 = median2 - 1;
			}
		}
		return ~num;
		static int GenericBinarySearch<T>(Array array, int adjustedIndex, int length, object value) where T : struct, IComparable<T>
		{
			return UnsafeArrayAsSpan<T>(array, adjustedIndex, length).BinarySearch(Unsafe.As<byte, T>(ref value.GetRawData()));
		}
	}

	public static int BinarySearch<T>(T[] array, T value)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		return BinarySearch(array, 0, array.Length, value, null);
	}

	public static int BinarySearch<T>(T[] array, T value, IComparer<T>? comparer)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		return BinarySearch(array, 0, array.Length, value, comparer);
	}

	public static int BinarySearch<T>(T[] array, int index, int length, T value)
	{
		return BinarySearch(array, index, length, value, null);
	}

	public static int BinarySearch<T>(T[] array, int index, int length, T value, IComparer<T>? comparer)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (index < 0)
		{
			ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
		}
		if (length < 0)
		{
			ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
		}
		if (array.Length - index < length)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
		}
		return ArraySortHelper<T>.Default.BinarySearch(array, index, length, value, comparer);
	}

	public static TOutput[] ConvertAll<TInput, TOutput>(TInput[] array, Converter<TInput, TOutput> converter)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (converter == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.converter);
		}
		TOutput[] array2 = new TOutput[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = converter(array[i]);
		}
		return array2;
	}

	public void CopyTo(Array array, int index)
	{
		if (array != null && array.Rank != 1)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
		}
		Copy(this, GetLowerBound(0), array, index, Length);
	}

	public void CopyTo(Array array, long index)
	{
		int num = (int)index;
		if (index != num)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
		}
		CopyTo(array, num);
	}

	public static T[] Empty<T>()
	{
		return EmptyArray<T>.Value;
	}

	public static bool Exists<T>(T[] array, Predicate<T> match)
	{
		return FindIndex(array, match) != -1;
	}

	public static void Fill<T>(T[] array, T value)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = value;
			}
		}
		else
		{
			new Span<T>(array).Fill(value);
		}
	}

	public static void Fill<T>(T[] array, T value, int startIndex, int count)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if ((uint)startIndex > (uint)array.Length)
		{
			ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_Index();
		}
		if ((uint)count > (uint)(array.Length - startIndex))
		{
			ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
		}
		if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
		{
			for (int i = startIndex; i < startIndex + count; i++)
			{
				array[i] = value;
			}
		}
		else
		{
			new Span<T>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)startIndex), count).Fill(value);
		}
	}

	public static T? Find<T>(T[] array, Predicate<T> match)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (match == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (match(array[i]))
			{
				return array[i];
			}
		}
		return default(T);
	}

	public static T[] FindAll<T>(T[] array, Predicate<T> match)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (match == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
		}
		List<T> list = new List<T>();
		for (int i = 0; i < array.Length; i++)
		{
			if (match(array[i]))
			{
				list.Add(array[i]);
			}
		}
		return list.ToArray();
	}

	public static int FindIndex<T>(T[] array, Predicate<T> match)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		return FindIndex(array, 0, array.Length, match);
	}

	public static int FindIndex<T>(T[] array, int startIndex, Predicate<T> match)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		return FindIndex(array, startIndex, array.Length - startIndex, match);
	}

	public static int FindIndex<T>(T[] array, int startIndex, int count, Predicate<T> match)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (startIndex < 0 || startIndex > array.Length)
		{
			ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_Index();
		}
		if (count < 0 || startIndex > array.Length - count)
		{
			ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
		}
		if (match == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
		}
		int num = startIndex + count;
		for (int i = startIndex; i < num; i++)
		{
			if (match(array[i]))
			{
				return i;
			}
		}
		return -1;
	}

	public static T? FindLast<T>(T[] array, Predicate<T> match)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (match == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
		}
		for (int num = array.Length - 1; num >= 0; num--)
		{
			if (match(array[num]))
			{
				return array[num];
			}
		}
		return default(T);
	}

	public static int FindLastIndex<T>(T[] array, Predicate<T> match)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		return FindLastIndex(array, array.Length - 1, array.Length, match);
	}

	public static int FindLastIndex<T>(T[] array, int startIndex, Predicate<T> match)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		return FindLastIndex(array, startIndex, startIndex + 1, match);
	}

	public static int FindLastIndex<T>(T[] array, int startIndex, int count, Predicate<T> match)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (match == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
		}
		if (array.Length == 0)
		{
			if (startIndex != -1)
			{
				ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_Index();
			}
		}
		else if (startIndex < 0 || startIndex >= array.Length)
		{
			ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_Index();
		}
		if (count < 0 || startIndex - count + 1 < 0)
		{
			ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
		}
		int num = startIndex - count;
		for (int num2 = startIndex; num2 > num; num2--)
		{
			if (match(array[num2]))
			{
				return num2;
			}
		}
		return -1;
	}

	public static void ForEach<T>(T[] array, Action<T> action)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (action == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.action);
		}
		for (int i = 0; i < array.Length; i++)
		{
			action(array[i]);
		}
	}

	public static int IndexOf(Array array, object? value)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		return IndexOf(array, value, array.GetLowerBound(0), array.Length);
	}

	public static int IndexOf(Array array, object? value, int startIndex)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		int lowerBound = array.GetLowerBound(0);
		return IndexOf(array, value, startIndex, array.Length - startIndex + lowerBound);
	}

	public static int IndexOf(Array array, object? value, int startIndex, int count)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (array.Rank != 1)
		{
			ThrowHelper.ThrowRankException(ExceptionResource.Rank_MultiDimNotSupported);
		}
		int lowerBound = array.GetLowerBound(0);
		if (startIndex < lowerBound || startIndex > array.Length + lowerBound)
		{
			ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_Index();
		}
		if (count < 0 || count > array.Length - startIndex + lowerBound)
		{
			ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
		}
		int num = startIndex + count;
		if (array is object[] array2)
		{
			if (value == null)
			{
				for (int i = startIndex; i < num; i++)
				{
					if (array2[i] == null)
					{
						return i;
					}
				}
			}
			else
			{
				for (int j = startIndex; j < num; j++)
				{
					object obj = array2[j];
					if (obj != null && obj.Equals(value))
					{
						return j;
					}
				}
			}
			return -1;
		}
		CorElementType corElementTypeOfElementType = array.GetCorElementTypeOfElementType();
		if (corElementTypeOfElementType.IsPrimitiveType())
		{
			if (value == null)
			{
				return lowerBound - 1;
			}
			if (array.IsValueOfElementType(value))
			{
				int adjustedIndex2 = startIndex - lowerBound;
				int num2 = -1;
				switch (corElementTypeOfElementType)
				{
				case CorElementType.ELEMENT_TYPE_BOOLEAN:
				case CorElementType.ELEMENT_TYPE_I1:
				case CorElementType.ELEMENT_TYPE_U1:
					num2 = GenericIndexOf<byte>(array, value, adjustedIndex2, count);
					break;
				case CorElementType.ELEMENT_TYPE_CHAR:
				case CorElementType.ELEMENT_TYPE_I2:
				case CorElementType.ELEMENT_TYPE_U2:
					num2 = GenericIndexOf<char>(array, value, adjustedIndex2, count);
					break;
				case CorElementType.ELEMENT_TYPE_I4:
				case CorElementType.ELEMENT_TYPE_U4:
					num2 = GenericIndexOf<int>(array, value, adjustedIndex2, count);
					break;
				case CorElementType.ELEMENT_TYPE_I8:
				case CorElementType.ELEMENT_TYPE_U8:
				case CorElementType.ELEMENT_TYPE_I:
				case CorElementType.ELEMENT_TYPE_U:
					num2 = GenericIndexOf<long>(array, value, adjustedIndex2, count);
					break;
				case CorElementType.ELEMENT_TYPE_R4:
					num2 = GenericIndexOf<float>(array, value, adjustedIndex2, count);
					break;
				case CorElementType.ELEMENT_TYPE_R8:
					num2 = GenericIndexOf<double>(array, value, adjustedIndex2, count);
					break;
				}
				return ((num2 >= 0) ? startIndex : lowerBound) + num2;
			}
		}
		for (int k = startIndex; k < num; k++)
		{
			object value2 = array.GetValue(k);
			if (value2 == null)
			{
				if (value == null)
				{
					return k;
				}
			}
			else if (value2.Equals(value))
			{
				return k;
			}
		}
		return lowerBound - 1;
		static int GenericIndexOf<T>(Array array, object value, int adjustedIndex, int length) where T : struct, IEquatable<T>
		{
			return UnsafeArrayAsSpan<T>(array, adjustedIndex, length).IndexOf(Unsafe.As<byte, T>(ref value.GetRawData()));
		}
	}

	public static int IndexOf<T>(T[] array, T value)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		return IndexOf(array, value, 0, array.Length);
	}

	public static int IndexOf<T>(T[] array, T value, int startIndex)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		return IndexOf(array, value, startIndex, array.Length - startIndex);
	}

	public static int IndexOf<T>(T[] array, T value, int startIndex, int count)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if ((uint)startIndex > (uint)array.Length)
		{
			ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_Index();
		}
		if ((uint)count > (uint)(array.Length - startIndex))
		{
			ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
		}
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			if (Unsafe.SizeOf<T>() == 1)
			{
				int num = SpanHelpers.IndexOf(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Unsafe.As<byte[]>(array)), startIndex), Unsafe.As<T, byte>(ref value), count);
				return ((num >= 0) ? startIndex : 0) + num;
			}
			if (Unsafe.SizeOf<T>() == 2)
			{
				int num2 = SpanHelpers.IndexOf(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Unsafe.As<char[]>(array)), startIndex), Unsafe.As<T, char>(ref value), count);
				return ((num2 >= 0) ? startIndex : 0) + num2;
			}
			if (Unsafe.SizeOf<T>() == 4)
			{
				int num3 = SpanHelpers.IndexOf(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Unsafe.As<int[]>(array)), startIndex), Unsafe.As<T, int>(ref value), count);
				return ((num3 >= 0) ? startIndex : 0) + num3;
			}
			if (Unsafe.SizeOf<T>() == 8)
			{
				int num4 = SpanHelpers.IndexOf(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Unsafe.As<long[]>(array)), startIndex), Unsafe.As<T, long>(ref value), count);
				return ((num4 >= 0) ? startIndex : 0) + num4;
			}
		}
		return EqualityComparer<T>.Default.IndexOf(array, value, startIndex, count);
	}

	public static int LastIndexOf(Array array, object? value)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		int lowerBound = array.GetLowerBound(0);
		return LastIndexOf(array, value, array.Length - 1 + lowerBound, array.Length);
	}

	public static int LastIndexOf(Array array, object? value, int startIndex)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		int lowerBound = array.GetLowerBound(0);
		return LastIndexOf(array, value, startIndex, startIndex + 1 - lowerBound);
	}

	public static int LastIndexOf(Array array, object? value, int startIndex, int count)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		int lowerBound = array.GetLowerBound(0);
		if (array.Length == 0)
		{
			return lowerBound - 1;
		}
		if (startIndex < lowerBound || startIndex >= array.Length + lowerBound)
		{
			ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_Index();
		}
		if (count < 0)
		{
			ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
		}
		if (count > startIndex - lowerBound + 1)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.endIndex, ExceptionResource.ArgumentOutOfRange_EndIndexStartIndex);
		}
		if (array.Rank != 1)
		{
			ThrowHelper.ThrowRankException(ExceptionResource.Rank_MultiDimNotSupported);
		}
		int num = startIndex - count + 1;
		if (array is object[] array2)
		{
			if (value == null)
			{
				for (int num2 = startIndex; num2 >= num; num2--)
				{
					if (array2[num2] == null)
					{
						return num2;
					}
				}
			}
			else
			{
				for (int num3 = startIndex; num3 >= num; num3--)
				{
					object obj = array2[num3];
					if (obj != null && obj.Equals(value))
					{
						return num3;
					}
				}
			}
			return -1;
		}
		CorElementType corElementTypeOfElementType = array.GetCorElementTypeOfElementType();
		if (corElementTypeOfElementType.IsPrimitiveType())
		{
			if (value == null)
			{
				return lowerBound - 1;
			}
			if (array.IsValueOfElementType(value))
			{
				int adjustedIndex2 = num - lowerBound;
				int num4 = -1;
				switch (corElementTypeOfElementType)
				{
				case CorElementType.ELEMENT_TYPE_BOOLEAN:
				case CorElementType.ELEMENT_TYPE_I1:
				case CorElementType.ELEMENT_TYPE_U1:
					num4 = GenericLastIndexOf<byte>(array, value, adjustedIndex2, count);
					break;
				case CorElementType.ELEMENT_TYPE_CHAR:
				case CorElementType.ELEMENT_TYPE_I2:
				case CorElementType.ELEMENT_TYPE_U2:
					num4 = GenericLastIndexOf<char>(array, value, adjustedIndex2, count);
					break;
				case CorElementType.ELEMENT_TYPE_I4:
				case CorElementType.ELEMENT_TYPE_U4:
					num4 = GenericLastIndexOf<int>(array, value, adjustedIndex2, count);
					break;
				case CorElementType.ELEMENT_TYPE_I8:
				case CorElementType.ELEMENT_TYPE_U8:
				case CorElementType.ELEMENT_TYPE_I:
				case CorElementType.ELEMENT_TYPE_U:
					num4 = GenericLastIndexOf<long>(array, value, adjustedIndex2, count);
					break;
				case CorElementType.ELEMENT_TYPE_R4:
					num4 = GenericLastIndexOf<float>(array, value, adjustedIndex2, count);
					break;
				case CorElementType.ELEMENT_TYPE_R8:
					num4 = GenericLastIndexOf<double>(array, value, adjustedIndex2, count);
					break;
				}
				return ((num4 >= 0) ? num : lowerBound) + num4;
			}
		}
		for (int num5 = startIndex; num5 >= num; num5--)
		{
			object value2 = array.GetValue(num5);
			if (value2 == null)
			{
				if (value == null)
				{
					return num5;
				}
			}
			else if (value2.Equals(value))
			{
				return num5;
			}
		}
		return lowerBound - 1;
		static int GenericLastIndexOf<T>(Array array, object value, int adjustedIndex, int length) where T : struct, IEquatable<T>
		{
			return UnsafeArrayAsSpan<T>(array, adjustedIndex, length).LastIndexOf(Unsafe.As<byte, T>(ref value.GetRawData()));
		}
	}

	public static int LastIndexOf<T>(T[] array, T value)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		return LastIndexOf(array, value, array.Length - 1, array.Length);
	}

	public static int LastIndexOf<T>(T[] array, T value, int startIndex)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		return LastIndexOf(array, value, startIndex, (array.Length != 0) ? (startIndex + 1) : 0);
	}

	public static int LastIndexOf<T>(T[] array, T value, int startIndex, int count)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (array.Length == 0)
		{
			if (startIndex != -1 && startIndex != 0)
			{
				ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_Index();
			}
			if (count != 0)
			{
				ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
			}
			return -1;
		}
		if ((uint)startIndex >= (uint)array.Length)
		{
			ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_Index();
		}
		if (count < 0 || startIndex - count + 1 < 0)
		{
			ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
		}
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			if (Unsafe.SizeOf<T>() == 1)
			{
				int num = startIndex - count + 1;
				int num2 = SpanHelpers.LastIndexOf(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Unsafe.As<byte[]>(array)), num), Unsafe.As<T, byte>(ref value), count);
				return ((num2 >= 0) ? num : 0) + num2;
			}
			if (Unsafe.SizeOf<T>() == 2)
			{
				int num3 = startIndex - count + 1;
				int num4 = SpanHelpers.LastIndexOf(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Unsafe.As<char[]>(array)), num3), Unsafe.As<T, char>(ref value), count);
				return ((num4 >= 0) ? num3 : 0) + num4;
			}
			if (Unsafe.SizeOf<T>() == 4)
			{
				int num5 = startIndex - count + 1;
				int num6 = SpanHelpers.LastIndexOf(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Unsafe.As<int[]>(array)), num5), Unsafe.As<T, int>(ref value), count);
				return ((num6 >= 0) ? num5 : 0) + num6;
			}
			if (Unsafe.SizeOf<T>() == 8)
			{
				int num7 = startIndex - count + 1;
				int num8 = SpanHelpers.LastIndexOf(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Unsafe.As<long[]>(array)), num7), Unsafe.As<T, long>(ref value), count);
				return ((num8 >= 0) ? num7 : 0) + num8;
			}
		}
		return EqualityComparer<T>.Default.LastIndexOf(array, value, startIndex, count);
	}

	public static void Reverse(Array array)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		Reverse(array, array.GetLowerBound(0), array.Length);
	}

	public static void Reverse(Array array, int index, int length)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		int lowerBound = array.GetLowerBound(0);
		if (index < lowerBound)
		{
			ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
		}
		if (length < 0)
		{
			ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
		}
		if (array.Length - (index - lowerBound) < length)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
		}
		if (array.Rank != 1)
		{
			ThrowHelper.ThrowRankException(ExceptionResource.Rank_MultiDimNotSupported);
		}
		if (length <= 1)
		{
			return;
		}
		int adjustedIndex = index - lowerBound;
		switch (array.GetCorElementTypeOfElementType())
		{
		case CorElementType.ELEMENT_TYPE_BOOLEAN:
		case CorElementType.ELEMENT_TYPE_I1:
		case CorElementType.ELEMENT_TYPE_U1:
			UnsafeArrayAsSpan<byte>(array, adjustedIndex, length).Reverse();
			return;
		case CorElementType.ELEMENT_TYPE_CHAR:
		case CorElementType.ELEMENT_TYPE_I2:
		case CorElementType.ELEMENT_TYPE_U2:
			UnsafeArrayAsSpan<short>(array, adjustedIndex, length).Reverse();
			return;
		case CorElementType.ELEMENT_TYPE_I4:
		case CorElementType.ELEMENT_TYPE_U4:
		case CorElementType.ELEMENT_TYPE_R4:
			UnsafeArrayAsSpan<int>(array, adjustedIndex, length).Reverse();
			return;
		case CorElementType.ELEMENT_TYPE_I8:
		case CorElementType.ELEMENT_TYPE_U8:
		case CorElementType.ELEMENT_TYPE_R8:
		case CorElementType.ELEMENT_TYPE_I:
		case CorElementType.ELEMENT_TYPE_U:
			UnsafeArrayAsSpan<long>(array, adjustedIndex, length).Reverse();
			return;
		case CorElementType.ELEMENT_TYPE_ARRAY:
		case CorElementType.ELEMENT_TYPE_OBJECT:
		case CorElementType.ELEMENT_TYPE_SZARRAY:
			UnsafeArrayAsSpan<object>(array, adjustedIndex, length).Reverse();
			return;
		}
		int num = index;
		int num2 = index + length - 1;
		while (num < num2)
		{
			object value = array.GetValue(num);
			array.SetValue(array.GetValue(num2), num);
			array.SetValue(value, num2);
			num++;
			num2--;
		}
	}

	public static void Reverse<T>(T[] array)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		Reverse(array, 0, array.Length);
	}

	public static void Reverse<T>(T[] array, int index, int length)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (index < 0)
		{
			ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
		}
		if (length < 0)
		{
			ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
		}
		if (array.Length - index < length)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
		}
		if (length > 1)
		{
			ref T reference = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
			ref T reference2 = ref Unsafe.Add(ref Unsafe.Add(ref reference, length), -1);
			do
			{
				T val = reference;
				reference = reference2;
				reference2 = val;
				reference = ref Unsafe.Add(ref reference, 1);
				reference2 = ref Unsafe.Add(ref reference2, -1);
			}
			while (Unsafe.IsAddressLessThan(ref reference, ref reference2));
		}
	}

	public static void Sort(Array array)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		Sort(array, null, array.GetLowerBound(0), array.Length, null);
	}

	public static void Sort(Array keys, Array? items)
	{
		if (keys == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keys);
		}
		Sort(keys, items, keys.GetLowerBound(0), keys.Length, null);
	}

	public static void Sort(Array array, int index, int length)
	{
		Sort(array, null, index, length, null);
	}

	public static void Sort(Array keys, Array? items, int index, int length)
	{
		Sort(keys, items, index, length, null);
	}

	public static void Sort(Array array, IComparer? comparer)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		Sort(array, null, array.GetLowerBound(0), array.Length, comparer);
	}

	public static void Sort(Array keys, Array? items, IComparer? comparer)
	{
		if (keys == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keys);
		}
		Sort(keys, items, keys.GetLowerBound(0), keys.Length, comparer);
	}

	public static void Sort(Array array, int index, int length, IComparer? comparer)
	{
		Sort(array, null, index, length, comparer);
	}

	public static void Sort(Array keys, Array? items, int index, int length, IComparer? comparer)
	{
		if (keys == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keys);
		}
		if (keys.Rank != 1 || (items != null && items.Rank != 1))
		{
			ThrowHelper.ThrowRankException(ExceptionResource.Rank_MultiDimNotSupported);
		}
		int lowerBound = keys.GetLowerBound(0);
		if (items != null && lowerBound != items.GetLowerBound(0))
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_LowerBoundsMustMatch);
		}
		if (index < lowerBound)
		{
			ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
		}
		if (length < 0)
		{
			ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
		}
		if (keys.Length - (index - lowerBound) < length || (items != null && index - lowerBound > items.Length - length))
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
		}
		if (length <= 1)
		{
			return;
		}
		if (comparer == null)
		{
			comparer = Comparer.Default;
		}
		if (keys is object[] keys2)
		{
			object[] array = items as object[];
			if (items == null || array != null)
			{
				new SorterObjectArray(keys2, array, comparer).Sort(index, length);
				return;
			}
		}
		if (comparer == Comparer.Default)
		{
			CorElementType corElementTypeOfElementType = keys.GetCorElementTypeOfElementType();
			if (items == null || items.GetCorElementTypeOfElementType() == corElementTypeOfElementType)
			{
				int adjustedIndex2 = index - lowerBound;
				switch (corElementTypeOfElementType)
				{
				case CorElementType.ELEMENT_TYPE_I1:
					GenericSort<sbyte>(keys, items, adjustedIndex2, length);
					return;
				case CorElementType.ELEMENT_TYPE_BOOLEAN:
				case CorElementType.ELEMENT_TYPE_U1:
					GenericSort<byte>(keys, items, adjustedIndex2, length);
					return;
				case CorElementType.ELEMENT_TYPE_I2:
					GenericSort<short>(keys, items, adjustedIndex2, length);
					return;
				case CorElementType.ELEMENT_TYPE_CHAR:
				case CorElementType.ELEMENT_TYPE_U2:
					GenericSort<ushort>(keys, items, adjustedIndex2, length);
					return;
				case CorElementType.ELEMENT_TYPE_I4:
					GenericSort<int>(keys, items, adjustedIndex2, length);
					return;
				case CorElementType.ELEMENT_TYPE_U4:
					GenericSort<uint>(keys, items, adjustedIndex2, length);
					return;
				case CorElementType.ELEMENT_TYPE_I8:
				case CorElementType.ELEMENT_TYPE_I:
					GenericSort<long>(keys, items, adjustedIndex2, length);
					return;
				case CorElementType.ELEMENT_TYPE_U8:
				case CorElementType.ELEMENT_TYPE_U:
					GenericSort<ulong>(keys, items, adjustedIndex2, length);
					return;
				case CorElementType.ELEMENT_TYPE_R4:
					GenericSort<float>(keys, items, adjustedIndex2, length);
					return;
				case CorElementType.ELEMENT_TYPE_R8:
					GenericSort<double>(keys, items, adjustedIndex2, length);
					return;
				}
			}
		}
		new SorterGenericArray(keys, items, comparer).Sort(index, length);
		static void GenericSort<T>(Array keys, Array items, int adjustedIndex, int length) where T : struct
		{
			Span<T> span = UnsafeArrayAsSpan<T>(keys, adjustedIndex, length);
			if (items != null)
			{
				span.Sort(UnsafeArrayAsSpan<T>(items, adjustedIndex, length));
			}
			else
			{
				span.Sort();
			}
		}
	}

	public static void Sort<T>(T[] array)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (array.Length > 1)
		{
			Span<T> keys = new Span<T>(ref MemoryMarshal.GetArrayDataReference(array), array.Length);
			ArraySortHelper<T>.Default.Sort(keys, null);
		}
	}

	public static void Sort<TKey, TValue>(TKey[] keys, TValue[]? items)
	{
		if (keys == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keys);
		}
		Sort(keys, items, 0, keys.Length, null);
	}

	public static void Sort<T>(T[] array, int index, int length)
	{
		Sort(array, index, length, null);
	}

	public static void Sort<TKey, TValue>(TKey[] keys, TValue[]? items, int index, int length)
	{
		Sort(keys, items, index, length, null);
	}

	public static void Sort<T>(T[] array, IComparer<T>? comparer)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		Sort(array, 0, array.Length, comparer);
	}

	public static void Sort<TKey, TValue>(TKey[] keys, TValue[]? items, IComparer<TKey>? comparer)
	{
		if (keys == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keys);
		}
		Sort(keys, items, 0, keys.Length, comparer);
	}

	public static void Sort<T>(T[] array, int index, int length, IComparer<T>? comparer)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (index < 0)
		{
			ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
		}
		if (length < 0)
		{
			ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
		}
		if (array.Length - index < length)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
		}
		if (length > 1)
		{
			Span<T> keys = new Span<T>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index), length);
			ArraySortHelper<T>.Default.Sort(keys, comparer);
		}
	}

	public static void Sort<TKey, TValue>(TKey[] keys, TValue[]? items, int index, int length, IComparer<TKey>? comparer)
	{
		if (keys == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keys);
		}
		if (index < 0)
		{
			ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
		}
		if (length < 0)
		{
			ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
		}
		if (keys.Length - index < length || (items != null && index > items.Length - length))
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
		}
		if (length > 1)
		{
			if (items == null)
			{
				Sort(keys, index, length, comparer);
				return;
			}
			Span<TKey> keys2 = new Span<TKey>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(keys), index), length);
			Span<TValue> values = new Span<TValue>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(items), index), length);
			ArraySortHelper<TKey, TValue>.Default.Sort(keys2, values, comparer);
		}
	}

	public static void Sort<T>(T[] array, Comparison<T> comparison)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (comparison == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparison);
		}
		Span<T> keys = new Span<T>(ref MemoryMarshal.GetArrayDataReference(array), array.Length);
		ArraySortHelper<T>.Sort(keys, comparison);
	}

	public static bool TrueForAll<T>(T[] array, Predicate<T> match)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		if (match == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (!match(array[i]))
			{
				return false;
			}
		}
		return true;
	}

	private static Span<T> UnsafeArrayAsSpan<T>(Array array, int adjustedIndex, int length)
	{
		return new Span<T>(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array)), array.Length).Slice(adjustedIndex, length);
	}

	public IEnumerator GetEnumerator()
	{
		return new ArrayEnumerator(this);
	}
}
