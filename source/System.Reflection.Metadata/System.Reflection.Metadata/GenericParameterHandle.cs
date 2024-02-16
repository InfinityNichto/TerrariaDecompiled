namespace System.Reflection.Metadata;

public readonly struct GenericParameterHandle : IEquatable<GenericParameterHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private GenericParameterHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static GenericParameterHandle FromRowId(int rowId)
	{
		return new GenericParameterHandle(rowId);
	}

	public static implicit operator Handle(GenericParameterHandle handle)
	{
		return new Handle(42, handle._rowId);
	}

	public static implicit operator EntityHandle(GenericParameterHandle handle)
	{
		return new EntityHandle((uint)(0x2A000000uL | (ulong)handle._rowId));
	}

	public static explicit operator GenericParameterHandle(Handle handle)
	{
		if (handle.VType != 42)
		{
			Throw.InvalidCast();
		}
		return new GenericParameterHandle(handle.RowId);
	}

	public static explicit operator GenericParameterHandle(EntityHandle handle)
	{
		if (handle.VType != 704643072)
		{
			Throw.InvalidCast();
		}
		return new GenericParameterHandle(handle.RowId);
	}

	public static bool operator ==(GenericParameterHandle left, GenericParameterHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is GenericParameterHandle)
		{
			return ((GenericParameterHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(GenericParameterHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(GenericParameterHandle left, GenericParameterHandle right)
	{
		return left._rowId != right._rowId;
	}
}
