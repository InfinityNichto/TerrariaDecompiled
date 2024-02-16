using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata;

public readonly struct GuidHandle : IEquatable<GuidHandle>
{
	private readonly int _index;

	public bool IsNil => _index == 0;

	internal int Index => _index;

	private GuidHandle(int index)
	{
		_index = index;
	}

	internal static GuidHandle FromIndex(int heapIndex)
	{
		return new GuidHandle(heapIndex);
	}

	public static implicit operator Handle(GuidHandle handle)
	{
		return new Handle(114, handle._index);
	}

	public static explicit operator GuidHandle(Handle handle)
	{
		if (handle.VType != 114)
		{
			Throw.InvalidCast();
		}
		return new GuidHandle(handle.Offset);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is GuidHandle other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(GuidHandle other)
	{
		return _index == other._index;
	}

	public override int GetHashCode()
	{
		return _index;
	}

	public static bool operator ==(GuidHandle left, GuidHandle right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(GuidHandle left, GuidHandle right)
	{
		return !left.Equals(right);
	}
}
