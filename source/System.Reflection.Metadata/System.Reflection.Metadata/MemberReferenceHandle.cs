namespace System.Reflection.Metadata;

public readonly struct MemberReferenceHandle : IEquatable<MemberReferenceHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private MemberReferenceHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static MemberReferenceHandle FromRowId(int rowId)
	{
		return new MemberReferenceHandle(rowId);
	}

	public static implicit operator Handle(MemberReferenceHandle handle)
	{
		return new Handle(10, handle._rowId);
	}

	public static implicit operator EntityHandle(MemberReferenceHandle handle)
	{
		return new EntityHandle((uint)(0xA000000uL | (ulong)handle._rowId));
	}

	public static explicit operator MemberReferenceHandle(Handle handle)
	{
		if (handle.VType != 10)
		{
			Throw.InvalidCast();
		}
		return new MemberReferenceHandle(handle.RowId);
	}

	public static explicit operator MemberReferenceHandle(EntityHandle handle)
	{
		if (handle.VType != 167772160)
		{
			Throw.InvalidCast();
		}
		return new MemberReferenceHandle(handle.RowId);
	}

	public static bool operator ==(MemberReferenceHandle left, MemberReferenceHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is MemberReferenceHandle)
		{
			return ((MemberReferenceHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(MemberReferenceHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(MemberReferenceHandle left, MemberReferenceHandle right)
	{
		return left._rowId != right._rowId;
	}
}
