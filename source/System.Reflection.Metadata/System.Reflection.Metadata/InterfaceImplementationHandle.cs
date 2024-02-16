namespace System.Reflection.Metadata;

public readonly struct InterfaceImplementationHandle : IEquatable<InterfaceImplementationHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	internal InterfaceImplementationHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static InterfaceImplementationHandle FromRowId(int rowId)
	{
		return new InterfaceImplementationHandle(rowId);
	}

	public static implicit operator Handle(InterfaceImplementationHandle handle)
	{
		return new Handle(9, handle._rowId);
	}

	public static implicit operator EntityHandle(InterfaceImplementationHandle handle)
	{
		return new EntityHandle((uint)(0x9000000uL | (ulong)handle._rowId));
	}

	public static explicit operator InterfaceImplementationHandle(Handle handle)
	{
		if (handle.VType != 9)
		{
			Throw.InvalidCast();
		}
		return new InterfaceImplementationHandle(handle.RowId);
	}

	public static explicit operator InterfaceImplementationHandle(EntityHandle handle)
	{
		if (handle.VType != 150994944)
		{
			Throw.InvalidCast();
		}
		return new InterfaceImplementationHandle(handle.RowId);
	}

	public static bool operator ==(InterfaceImplementationHandle left, InterfaceImplementationHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is InterfaceImplementationHandle)
		{
			return ((InterfaceImplementationHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(InterfaceImplementationHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(InterfaceImplementationHandle left, InterfaceImplementationHandle right)
	{
		return left._rowId != right._rowId;
	}
}
