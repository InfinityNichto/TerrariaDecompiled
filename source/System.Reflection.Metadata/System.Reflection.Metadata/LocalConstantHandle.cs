using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata;

public readonly struct LocalConstantHandle : IEquatable<LocalConstantHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private LocalConstantHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static LocalConstantHandle FromRowId(int rowId)
	{
		return new LocalConstantHandle(rowId);
	}

	public static implicit operator Handle(LocalConstantHandle handle)
	{
		return new Handle(52, handle._rowId);
	}

	public static implicit operator EntityHandle(LocalConstantHandle handle)
	{
		return new EntityHandle((uint)(0x34000000uL | (ulong)handle._rowId));
	}

	public static explicit operator LocalConstantHandle(Handle handle)
	{
		if (handle.VType != 52)
		{
			Throw.InvalidCast();
		}
		return new LocalConstantHandle(handle.RowId);
	}

	public static explicit operator LocalConstantHandle(EntityHandle handle)
	{
		if (handle.VType != 872415232)
		{
			Throw.InvalidCast();
		}
		return new LocalConstantHandle(handle.RowId);
	}

	public static bool operator ==(LocalConstantHandle left, LocalConstantHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is LocalConstantHandle localConstantHandle)
		{
			return localConstantHandle._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(LocalConstantHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(LocalConstantHandle left, LocalConstantHandle right)
	{
		return left._rowId != right._rowId;
	}
}
