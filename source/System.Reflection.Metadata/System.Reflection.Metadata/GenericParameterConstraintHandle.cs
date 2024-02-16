namespace System.Reflection.Metadata;

public readonly struct GenericParameterConstraintHandle : IEquatable<GenericParameterConstraintHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private GenericParameterConstraintHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static GenericParameterConstraintHandle FromRowId(int rowId)
	{
		return new GenericParameterConstraintHandle(rowId);
	}

	public static implicit operator Handle(GenericParameterConstraintHandle handle)
	{
		return new Handle(44, handle._rowId);
	}

	public static implicit operator EntityHandle(GenericParameterConstraintHandle handle)
	{
		return new EntityHandle((uint)(0x2C000000uL | (ulong)handle._rowId));
	}

	public static explicit operator GenericParameterConstraintHandle(Handle handle)
	{
		if (handle.VType != 44)
		{
			Throw.InvalidCast();
		}
		return new GenericParameterConstraintHandle(handle.RowId);
	}

	public static explicit operator GenericParameterConstraintHandle(EntityHandle handle)
	{
		if (handle.VType != 738197504)
		{
			Throw.InvalidCast();
		}
		return new GenericParameterConstraintHandle(handle.RowId);
	}

	public static bool operator ==(GenericParameterConstraintHandle left, GenericParameterConstraintHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is GenericParameterConstraintHandle)
		{
			return ((GenericParameterConstraintHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(GenericParameterConstraintHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(GenericParameterConstraintHandle left, GenericParameterConstraintHandle right)
	{
		return left._rowId != right._rowId;
	}
}
