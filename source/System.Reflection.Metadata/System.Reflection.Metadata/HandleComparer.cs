using System.Collections.Generic;

namespace System.Reflection.Metadata;

public sealed class HandleComparer : IEqualityComparer<Handle>, IComparer<Handle>, IEqualityComparer<EntityHandle>, IComparer<EntityHandle>
{
	private static readonly HandleComparer s_default = new HandleComparer();

	public static HandleComparer Default => s_default;

	private HandleComparer()
	{
	}

	public bool Equals(Handle x, Handle y)
	{
		return x.Equals(y);
	}

	public bool Equals(EntityHandle x, EntityHandle y)
	{
		return x.Equals(y);
	}

	public int GetHashCode(Handle obj)
	{
		return obj.GetHashCode();
	}

	public int GetHashCode(EntityHandle obj)
	{
		return obj.GetHashCode();
	}

	public int Compare(Handle x, Handle y)
	{
		return Handle.Compare(x, y);
	}

	public int Compare(EntityHandle x, EntityHandle y)
	{
		return EntityHandle.Compare(x, y);
	}
}
