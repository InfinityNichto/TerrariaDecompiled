namespace System.Reflection.Metadata;

public readonly struct CustomAttributeHandle : IEquatable<CustomAttributeHandle>
{
	private readonly int _rowId;

	public bool IsNil => _rowId == 0;

	internal int RowId => _rowId;

	private CustomAttributeHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static CustomAttributeHandle FromRowId(int rowId)
	{
		return new CustomAttributeHandle(rowId);
	}

	public static implicit operator Handle(CustomAttributeHandle handle)
	{
		return new Handle(12, handle._rowId);
	}

	public static implicit operator EntityHandle(CustomAttributeHandle handle)
	{
		return new EntityHandle((uint)(0xC000000uL | (ulong)handle._rowId));
	}

	public static explicit operator CustomAttributeHandle(Handle handle)
	{
		if (handle.VType != 12)
		{
			Throw.InvalidCast();
		}
		return new CustomAttributeHandle(handle.RowId);
	}

	public static explicit operator CustomAttributeHandle(EntityHandle handle)
	{
		if (handle.VType != 201326592)
		{
			Throw.InvalidCast();
		}
		return new CustomAttributeHandle(handle.RowId);
	}

	public static bool operator ==(CustomAttributeHandle left, CustomAttributeHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is CustomAttributeHandle)
		{
			return ((CustomAttributeHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(CustomAttributeHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(CustomAttributeHandle left, CustomAttributeHandle right)
	{
		return left._rowId != right._rowId;
	}
}
