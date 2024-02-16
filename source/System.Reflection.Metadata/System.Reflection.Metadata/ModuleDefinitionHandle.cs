namespace System.Reflection.Metadata;

public readonly struct ModuleDefinitionHandle : IEquatable<ModuleDefinitionHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	internal ModuleDefinitionHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static ModuleDefinitionHandle FromRowId(int rowId)
	{
		return new ModuleDefinitionHandle(rowId);
	}

	public static implicit operator Handle(ModuleDefinitionHandle handle)
	{
		return new Handle(0, handle._rowId);
	}

	public static implicit operator EntityHandle(ModuleDefinitionHandle handle)
	{
		return new EntityHandle((uint)(0uL | (ulong)handle._rowId));
	}

	public static explicit operator ModuleDefinitionHandle(Handle handle)
	{
		if (handle.VType != 0)
		{
			Throw.InvalidCast();
		}
		return new ModuleDefinitionHandle(handle.RowId);
	}

	public static explicit operator ModuleDefinitionHandle(EntityHandle handle)
	{
		if (handle.VType != 0)
		{
			Throw.InvalidCast();
		}
		return new ModuleDefinitionHandle(handle.RowId);
	}

	public static bool operator ==(ModuleDefinitionHandle left, ModuleDefinitionHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is ModuleDefinitionHandle moduleDefinitionHandle)
		{
			return moduleDefinitionHandle._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(ModuleDefinitionHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(ModuleDefinitionHandle left, ModuleDefinitionHandle right)
	{
		return left._rowId != right._rowId;
	}
}
