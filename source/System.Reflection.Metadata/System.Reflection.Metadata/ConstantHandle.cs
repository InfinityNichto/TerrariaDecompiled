namespace System.Reflection.Metadata;

public readonly struct ConstantHandle : IEquatable<ConstantHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private ConstantHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static ConstantHandle FromRowId(int rowId)
	{
		return new ConstantHandle(rowId);
	}

	public static implicit operator Handle(ConstantHandle handle)
	{
		return new Handle(11, handle._rowId);
	}

	public static implicit operator EntityHandle(ConstantHandle handle)
	{
		return new EntityHandle((uint)(0xB000000uL | (ulong)handle._rowId));
	}

	public static explicit operator ConstantHandle(Handle handle)
	{
		if (handle.VType != 11)
		{
			Throw.InvalidCast();
		}
		return new ConstantHandle(handle.RowId);
	}

	public static explicit operator ConstantHandle(EntityHandle handle)
	{
		if (handle.VType != 184549376)
		{
			Throw.InvalidCast();
		}
		return new ConstantHandle(handle.RowId);
	}

	public static bool operator ==(ConstantHandle left, ConstantHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is ConstantHandle)
		{
			return ((ConstantHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(ConstantHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(ConstantHandle left, ConstantHandle right)
	{
		return left._rowId != right._rowId;
	}
}
