namespace System.Collections;

internal sealed class StructuralEqualityComparer : IEqualityComparer
{
	public new bool Equals(object x, object y)
	{
		if (x != null)
		{
			if (x is IStructuralEquatable structuralEquatable)
			{
				return structuralEquatable.Equals(y, this);
			}
			if (y != null)
			{
				return x.Equals(y);
			}
			return false;
		}
		if (y != null)
		{
			return false;
		}
		return true;
	}

	public int GetHashCode(object obj)
	{
		if (obj == null)
		{
			return 0;
		}
		if (obj is IStructuralEquatable structuralEquatable)
		{
			return structuralEquatable.GetHashCode(this);
		}
		return obj.GetHashCode();
	}
}
