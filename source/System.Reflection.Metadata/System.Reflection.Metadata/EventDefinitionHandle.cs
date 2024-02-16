namespace System.Reflection.Metadata;

public readonly struct EventDefinitionHandle : IEquatable<EventDefinitionHandle>
{
	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private EventDefinitionHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static EventDefinitionHandle FromRowId(int rowId)
	{
		return new EventDefinitionHandle(rowId);
	}

	public static implicit operator Handle(EventDefinitionHandle handle)
	{
		return new Handle(20, handle._rowId);
	}

	public static implicit operator EntityHandle(EventDefinitionHandle handle)
	{
		return new EntityHandle((uint)(0x14000000uL | (ulong)handle._rowId));
	}

	public static explicit operator EventDefinitionHandle(Handle handle)
	{
		if (handle.VType != 20)
		{
			Throw.InvalidCast();
		}
		return new EventDefinitionHandle(handle.RowId);
	}

	public static explicit operator EventDefinitionHandle(EntityHandle handle)
	{
		if (handle.VType != 335544320)
		{
			Throw.InvalidCast();
		}
		return new EventDefinitionHandle(handle.RowId);
	}

	public static bool operator ==(EventDefinitionHandle left, EventDefinitionHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is EventDefinitionHandle)
		{
			return ((EventDefinitionHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(EventDefinitionHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(EventDefinitionHandle left, EventDefinitionHandle right)
	{
		return left._rowId != right._rowId;
	}
}
