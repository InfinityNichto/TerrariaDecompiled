namespace System.Reflection.Metadata;

public readonly struct FieldDefinitionHandle : IEquatable<FieldDefinitionHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private FieldDefinitionHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static FieldDefinitionHandle FromRowId(int rowId)
	{
		return new FieldDefinitionHandle(rowId);
	}

	public static implicit operator Handle(FieldDefinitionHandle handle)
	{
		return new Handle(4, handle._rowId);
	}

	public static implicit operator EntityHandle(FieldDefinitionHandle handle)
	{
		return new EntityHandle((uint)(0x4000000uL | (ulong)handle._rowId));
	}

	public static explicit operator FieldDefinitionHandle(Handle handle)
	{
		if (handle.VType != 4)
		{
			Throw.InvalidCast();
		}
		return new FieldDefinitionHandle(handle.RowId);
	}

	public static explicit operator FieldDefinitionHandle(EntityHandle handle)
	{
		if (handle.VType != 67108864)
		{
			Throw.InvalidCast();
		}
		return new FieldDefinitionHandle(handle.RowId);
	}

	public static bool operator ==(FieldDefinitionHandle left, FieldDefinitionHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is FieldDefinitionHandle)
		{
			return ((FieldDefinitionHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(FieldDefinitionHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(FieldDefinitionHandle left, FieldDefinitionHandle right)
	{
		return left._rowId != right._rowId;
	}
}
