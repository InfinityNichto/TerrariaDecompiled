namespace System.Reflection.Metadata;

public readonly struct AssemblyFileHandle : IEquatable<AssemblyFileHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private AssemblyFileHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static AssemblyFileHandle FromRowId(int rowId)
	{
		return new AssemblyFileHandle(rowId);
	}

	public static implicit operator Handle(AssemblyFileHandle handle)
	{
		return new Handle(38, handle._rowId);
	}

	public static implicit operator EntityHandle(AssemblyFileHandle handle)
	{
		return new EntityHandle((uint)(0x26000000uL | (ulong)handle._rowId));
	}

	public static explicit operator AssemblyFileHandle(Handle handle)
	{
		if (handle.VType != 38)
		{
			Throw.InvalidCast();
		}
		return new AssemblyFileHandle(handle.RowId);
	}

	public static explicit operator AssemblyFileHandle(EntityHandle handle)
	{
		if (handle.VType != 637534208)
		{
			Throw.InvalidCast();
		}
		return new AssemblyFileHandle(handle.RowId);
	}

	public static bool operator ==(AssemblyFileHandle left, AssemblyFileHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is AssemblyFileHandle)
		{
			return ((AssemblyFileHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(AssemblyFileHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(AssemblyFileHandle left, AssemblyFileHandle right)
	{
		return left._rowId != right._rowId;
	}
}
