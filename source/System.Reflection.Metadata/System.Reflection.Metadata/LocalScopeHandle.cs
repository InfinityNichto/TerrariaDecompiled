namespace System.Reflection.Metadata;

public readonly struct LocalScopeHandle : IEquatable<LocalScopeHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private LocalScopeHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static LocalScopeHandle FromRowId(int rowId)
	{
		return new LocalScopeHandle(rowId);
	}

	public static implicit operator Handle(LocalScopeHandle handle)
	{
		return new Handle(50, handle._rowId);
	}

	public static implicit operator EntityHandle(LocalScopeHandle handle)
	{
		return new EntityHandle((uint)(0x32000000uL | (ulong)handle._rowId));
	}

	public static explicit operator LocalScopeHandle(Handle handle)
	{
		if (handle.VType != 50)
		{
			Throw.InvalidCast();
		}
		return new LocalScopeHandle(handle.RowId);
	}

	public static explicit operator LocalScopeHandle(EntityHandle handle)
	{
		if (handle.VType != 838860800)
		{
			Throw.InvalidCast();
		}
		return new LocalScopeHandle(handle.RowId);
	}

	public static bool operator ==(LocalScopeHandle left, LocalScopeHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is LocalScopeHandle localScopeHandle)
		{
			return localScopeHandle._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(LocalScopeHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(LocalScopeHandle left, LocalScopeHandle right)
	{
		return left._rowId != right._rowId;
	}
}
