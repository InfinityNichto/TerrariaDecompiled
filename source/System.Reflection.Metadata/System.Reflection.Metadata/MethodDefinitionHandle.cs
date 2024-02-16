namespace System.Reflection.Metadata;

public readonly struct MethodDefinitionHandle : IEquatable<MethodDefinitionHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private MethodDefinitionHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static MethodDefinitionHandle FromRowId(int rowId)
	{
		return new MethodDefinitionHandle(rowId);
	}

	public static implicit operator Handle(MethodDefinitionHandle handle)
	{
		return new Handle(6, handle._rowId);
	}

	public static implicit operator EntityHandle(MethodDefinitionHandle handle)
	{
		return new EntityHandle((uint)(0x6000000uL | (ulong)handle._rowId));
	}

	public static explicit operator MethodDefinitionHandle(Handle handle)
	{
		if (handle.VType != 6)
		{
			Throw.InvalidCast();
		}
		return new MethodDefinitionHandle(handle.RowId);
	}

	public static explicit operator MethodDefinitionHandle(EntityHandle handle)
	{
		if (handle.VType != 100663296)
		{
			Throw.InvalidCast();
		}
		return new MethodDefinitionHandle(handle.RowId);
	}

	public static bool operator ==(MethodDefinitionHandle left, MethodDefinitionHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is MethodDefinitionHandle)
		{
			return ((MethodDefinitionHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(MethodDefinitionHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(MethodDefinitionHandle left, MethodDefinitionHandle right)
	{
		return left._rowId != right._rowId;
	}

	public MethodDebugInformationHandle ToDebugInformationHandle()
	{
		return MethodDebugInformationHandle.FromRowId(_rowId);
	}
}
