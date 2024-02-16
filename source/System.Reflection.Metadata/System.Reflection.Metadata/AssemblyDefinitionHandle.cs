namespace System.Reflection.Metadata;

public readonly struct AssemblyDefinitionHandle : IEquatable<AssemblyDefinitionHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	internal AssemblyDefinitionHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static AssemblyDefinitionHandle FromRowId(int rowId)
	{
		return new AssemblyDefinitionHandle(rowId);
	}

	public static implicit operator Handle(AssemblyDefinitionHandle handle)
	{
		return new Handle(32, handle._rowId);
	}

	public static implicit operator EntityHandle(AssemblyDefinitionHandle handle)
	{
		return new EntityHandle((uint)(0x20000000uL | (ulong)handle._rowId));
	}

	public static explicit operator AssemblyDefinitionHandle(Handle handle)
	{
		if (handle.VType != 32)
		{
			Throw.InvalidCast();
		}
		return new AssemblyDefinitionHandle(handle.RowId);
	}

	public static explicit operator AssemblyDefinitionHandle(EntityHandle handle)
	{
		if (handle.VType != 536870912)
		{
			Throw.InvalidCast();
		}
		return new AssemblyDefinitionHandle(handle.RowId);
	}

	public static bool operator ==(AssemblyDefinitionHandle left, AssemblyDefinitionHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is AssemblyDefinitionHandle)
		{
			return ((AssemblyDefinitionHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(AssemblyDefinitionHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(AssemblyDefinitionHandle left, AssemblyDefinitionHandle right)
	{
		return left._rowId != right._rowId;
	}
}
