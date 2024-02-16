namespace System.Reflection.Metadata;

public readonly struct TypeReferenceHandle : IEquatable<TypeReferenceHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private TypeReferenceHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static TypeReferenceHandle FromRowId(int rowId)
	{
		return new TypeReferenceHandle(rowId);
	}

	public static implicit operator Handle(TypeReferenceHandle handle)
	{
		return new Handle(1, handle._rowId);
	}

	public static implicit operator EntityHandle(TypeReferenceHandle handle)
	{
		return new EntityHandle((uint)(0x1000000uL | (ulong)handle._rowId));
	}

	public static explicit operator TypeReferenceHandle(Handle handle)
	{
		if (handle.VType != 1)
		{
			Throw.InvalidCast();
		}
		return new TypeReferenceHandle(handle.RowId);
	}

	public static explicit operator TypeReferenceHandle(EntityHandle handle)
	{
		if (handle.VType != 16777216)
		{
			Throw.InvalidCast();
		}
		return new TypeReferenceHandle(handle.RowId);
	}

	public static bool operator ==(TypeReferenceHandle left, TypeReferenceHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is TypeReferenceHandle)
		{
			return ((TypeReferenceHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(TypeReferenceHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(TypeReferenceHandle left, TypeReferenceHandle right)
	{
		return left._rowId != right._rowId;
	}
}
