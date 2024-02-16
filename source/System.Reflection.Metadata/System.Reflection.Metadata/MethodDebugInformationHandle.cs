namespace System.Reflection.Metadata;

public readonly struct MethodDebugInformationHandle : IEquatable<MethodDebugInformationHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private MethodDebugInformationHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static MethodDebugInformationHandle FromRowId(int rowId)
	{
		return new MethodDebugInformationHandle(rowId);
	}

	public static implicit operator Handle(MethodDebugInformationHandle handle)
	{
		return new Handle(49, handle._rowId);
	}

	public static implicit operator EntityHandle(MethodDebugInformationHandle handle)
	{
		return new EntityHandle((uint)(0x31000000uL | (ulong)handle._rowId));
	}

	public static explicit operator MethodDebugInformationHandle(Handle handle)
	{
		if (handle.VType != 49)
		{
			Throw.InvalidCast();
		}
		return new MethodDebugInformationHandle(handle.RowId);
	}

	public static explicit operator MethodDebugInformationHandle(EntityHandle handle)
	{
		if (handle.VType != 822083584)
		{
			Throw.InvalidCast();
		}
		return new MethodDebugInformationHandle(handle.RowId);
	}

	public static bool operator ==(MethodDebugInformationHandle left, MethodDebugInformationHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is MethodDebugInformationHandle methodDebugInformationHandle)
		{
			return methodDebugInformationHandle._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(MethodDebugInformationHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(MethodDebugInformationHandle left, MethodDebugInformationHandle right)
	{
		return left._rowId != right._rowId;
	}

	public MethodDefinitionHandle ToDefinitionHandle()
	{
		return MethodDefinitionHandle.FromRowId(_rowId);
	}
}
