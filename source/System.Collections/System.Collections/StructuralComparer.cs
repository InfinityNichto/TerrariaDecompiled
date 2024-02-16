using System.Collections.Generic;

namespace System.Collections;

internal sealed class StructuralComparer : IComparer
{
	public int Compare(object x, object y)
	{
		if (x == null)
		{
			if (y != null)
			{
				return -1;
			}
			return 0;
		}
		if (y == null)
		{
			return 1;
		}
		if (x is IStructuralComparable structuralComparable)
		{
			return structuralComparable.CompareTo(y, this);
		}
		return Comparer<object>.Default.Compare(x, y);
	}
}
