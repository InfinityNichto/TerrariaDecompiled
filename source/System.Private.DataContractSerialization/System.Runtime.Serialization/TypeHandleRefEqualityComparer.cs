using System.Collections.Generic;

namespace System.Runtime.Serialization;

internal sealed class TypeHandleRefEqualityComparer : IEqualityComparer<TypeHandleRef>
{
	public bool Equals(TypeHandleRef x, TypeHandleRef y)
	{
		return x.Value.Equals(y.Value);
	}

	public int GetHashCode(TypeHandleRef obj)
	{
		return obj.Value.GetHashCode();
	}
}
