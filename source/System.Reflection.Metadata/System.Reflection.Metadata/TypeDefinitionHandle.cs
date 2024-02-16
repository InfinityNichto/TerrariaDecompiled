namespace System.Reflection.Metadata;

public readonly struct TypeDefinitionHandle : IEquatable<TypeDefinitionHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private TypeDefinitionHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static TypeDefinitionHandle FromRowId(int rowId)
	{
		return new TypeDefinitionHandle(rowId);
	}

	public static implicit operator Handle(TypeDefinitionHandle handle)
	{
		return new Handle(2, handle._rowId);
	}

	public static implicit operator EntityHandle(TypeDefinitionHandle handle)
	{
		return new EntityHandle((uint)(0x2000000uL | (ulong)handle._rowId));
	}

	public static explicit operator TypeDefinitionHandle(Handle handle)
	{
		if (handle.VType != 2)
		{
			Throw.InvalidCast();
		}
		return new TypeDefinitionHandle(handle.RowId);
	}

	public static explicit operator TypeDefinitionHandle(EntityHandle handle)
	{
		if (handle.VType != 33554432)
		{
			Throw.InvalidCast();
		}
		return new TypeDefinitionHandle(handle.RowId);
	}

	public static bool operator ==(TypeDefinitionHandle left, TypeDefinitionHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is TypeDefinitionHandle)
		{
			return ((TypeDefinitionHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(TypeDefinitionHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(TypeDefinitionHandle left, TypeDefinitionHandle right)
	{
		return left._rowId != right._rowId;
	}
}
