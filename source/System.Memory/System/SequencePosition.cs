using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System;

public readonly struct SequencePosition : IEquatable<SequencePosition>
{
	private readonly object _object;

	private readonly int _integer;

	public SequencePosition(object? @object, int integer)
	{
		_object = @object;
		_integer = integer;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public object? GetObject()
	{
		return _object;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public int GetInteger()
	{
		return _integer;
	}

	public bool Equals(SequencePosition other)
	{
		if (_integer == other._integer)
		{
			return object.Equals(_object, other._object);
		}
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is SequencePosition other)
		{
			return Equals(other);
		}
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public override int GetHashCode()
	{
		return HashCode.Combine(_object?.GetHashCode() ?? 0, _integer);
	}
}
