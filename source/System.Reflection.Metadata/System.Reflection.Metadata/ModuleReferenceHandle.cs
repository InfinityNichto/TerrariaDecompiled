namespace System.Reflection.Metadata;

public readonly struct ModuleReferenceHandle : IEquatable<ModuleReferenceHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private ModuleReferenceHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static ModuleReferenceHandle FromRowId(int rowId)
	{
		return new ModuleReferenceHandle(rowId);
	}

	public static implicit operator Handle(ModuleReferenceHandle handle)
	{
		return new Handle(26, handle._rowId);
	}

	public static implicit operator EntityHandle(ModuleReferenceHandle handle)
	{
		return new EntityHandle((uint)(0x1A000000uL | (ulong)handle._rowId));
	}

	public static explicit operator ModuleReferenceHandle(Handle handle)
	{
		if (handle.VType != 26)
		{
			Throw.InvalidCast();
		}
		return new ModuleReferenceHandle(handle.RowId);
	}

	public static explicit operator ModuleReferenceHandle(EntityHandle handle)
	{
		if (handle.VType != 436207616)
		{
			Throw.InvalidCast();
		}
		return new ModuleReferenceHandle(handle.RowId);
	}

	public static bool operator ==(ModuleReferenceHandle left, ModuleReferenceHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is ModuleReferenceHandle)
		{
			return ((ModuleReferenceHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(ModuleReferenceHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(ModuleReferenceHandle left, ModuleReferenceHandle right)
	{
		return left._rowId != right._rowId;
	}
}
