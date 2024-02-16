using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata;

public readonly struct ImportScopeHandle : IEquatable<ImportScopeHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private ImportScopeHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static ImportScopeHandle FromRowId(int rowId)
	{
		return new ImportScopeHandle(rowId);
	}

	public static implicit operator Handle(ImportScopeHandle handle)
	{
		return new Handle(53, handle._rowId);
	}

	public static implicit operator EntityHandle(ImportScopeHandle handle)
	{
		return new EntityHandle((uint)(0x35000000uL | (ulong)handle._rowId));
	}

	public static explicit operator ImportScopeHandle(Handle handle)
	{
		if (handle.VType != 53)
		{
			Throw.InvalidCast();
		}
		return new ImportScopeHandle(handle.RowId);
	}

	public static explicit operator ImportScopeHandle(EntityHandle handle)
	{
		if (handle.VType != 889192448)
		{
			Throw.InvalidCast();
		}
		return new ImportScopeHandle(handle.RowId);
	}

	public static bool operator ==(ImportScopeHandle left, ImportScopeHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is ImportScopeHandle importScopeHandle)
		{
			return importScopeHandle._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(ImportScopeHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(ImportScopeHandle left, ImportScopeHandle right)
	{
		return left._rowId != right._rowId;
	}
}
