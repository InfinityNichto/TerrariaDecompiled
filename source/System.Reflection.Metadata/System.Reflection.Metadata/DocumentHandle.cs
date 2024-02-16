namespace System.Reflection.Metadata;

public readonly struct DocumentHandle : IEquatable<DocumentHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private DocumentHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static DocumentHandle FromRowId(int rowId)
	{
		return new DocumentHandle(rowId);
	}

	public static implicit operator Handle(DocumentHandle handle)
	{
		return new Handle(48, handle._rowId);
	}

	public static implicit operator EntityHandle(DocumentHandle handle)
	{
		return new EntityHandle((uint)(0x30000000uL | (ulong)handle._rowId));
	}

	public static explicit operator DocumentHandle(Handle handle)
	{
		if (handle.VType != 48)
		{
			Throw.InvalidCast();
		}
		return new DocumentHandle(handle.RowId);
	}

	public static explicit operator DocumentHandle(EntityHandle handle)
	{
		if (handle.VType != 805306368)
		{
			Throw.InvalidCast();
		}
		return new DocumentHandle(handle.RowId);
	}

	public static bool operator ==(DocumentHandle left, DocumentHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is DocumentHandle documentHandle)
		{
			return documentHandle._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(DocumentHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(DocumentHandle left, DocumentHandle right)
	{
		return left._rowId != right._rowId;
	}
}
