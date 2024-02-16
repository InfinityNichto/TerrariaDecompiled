using System.Collections.Generic;

namespace System.Collections;

internal static class CollectionHelpers
{
	internal static void ValidateCopyToArguments(int sourceCount, Array array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (array.Rank != 1)
		{
			throw new ArgumentException(System.SR.Arg_RankMultiDimNotSupported, "array");
		}
		if (array.GetLowerBound(0) != 0)
		{
			throw new ArgumentException(System.SR.Arg_NonZeroLowerBound, "array");
		}
		if (index < 0 || index > array.Length)
		{
			throw new ArgumentOutOfRangeException("index", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (array.Length - index < sourceCount)
		{
			throw new ArgumentException(System.SR.Arg_ArrayPlusOffTooSmall);
		}
	}

	internal static void CopyTo<T>(ICollection<T> collection, Array array, int index)
	{
		ValidateCopyToArguments(collection.Count, array, index);
		if (collection is ICollection collection2)
		{
			collection2.CopyTo(array, index);
			return;
		}
		if (array is T[] array2)
		{
			collection.CopyTo(array2, index);
			return;
		}
		if (!(array is object[] array3))
		{
			throw new ArgumentException(System.SR.Argument_InvalidArrayType, "array");
		}
		try
		{
			foreach (T item in collection)
			{
				array3[index++] = item;
			}
		}
		catch (ArrayTypeMismatchException)
		{
			throw new ArgumentException(System.SR.Argument_InvalidArrayType, "array");
		}
	}
}
