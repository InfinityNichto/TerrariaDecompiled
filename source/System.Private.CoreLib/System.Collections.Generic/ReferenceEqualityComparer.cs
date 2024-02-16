using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

public sealed class ReferenceEqualityComparer : IEqualityComparer<object?>, IEqualityComparer
{
	public static ReferenceEqualityComparer Instance { get; } = new ReferenceEqualityComparer();


	private ReferenceEqualityComparer()
	{
	}

	public new bool Equals(object? x, object? y)
	{
		return x == y;
	}

	public int GetHashCode(object? obj)
	{
		return RuntimeHelpers.GetHashCode(obj);
	}
}
