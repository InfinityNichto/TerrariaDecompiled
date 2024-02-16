using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata;

public readonly struct NamespaceDefinitionHandle : IEquatable<NamespaceDefinitionHandle>
{
	private readonly uint _value;

	public bool IsNil => _value == 0;

	internal bool IsVirtual => (_value & 0x80000000u) != 0;

	internal bool HasFullName => !IsVirtual;

	private NamespaceDefinitionHandle(uint value)
	{
		_value = value;
	}

	internal static NamespaceDefinitionHandle FromFullNameOffset(int stringHeapOffset)
	{
		return new NamespaceDefinitionHandle((uint)stringHeapOffset);
	}

	internal static NamespaceDefinitionHandle FromVirtualIndex(uint virtualIndex)
	{
		if (!HeapHandleType.IsValidHeapOffset(virtualIndex))
		{
			Throw.TooManySubnamespaces();
		}
		return new NamespaceDefinitionHandle(0x80000000u | virtualIndex);
	}

	public static implicit operator Handle(NamespaceDefinitionHandle handle)
	{
		return new Handle((byte)(((handle._value & 0x80000000u) >> 24) | 0x7Cu), (int)(handle._value & 0x1FFFFFFF));
	}

	public static explicit operator NamespaceDefinitionHandle(Handle handle)
	{
		if ((handle.VType & 0x7F) != 124)
		{
			Throw.InvalidCast();
		}
		return new NamespaceDefinitionHandle((uint)(((handle.VType & 0x80) << 24) | handle.Offset));
	}

	internal int GetHeapOffset()
	{
		return (int)(_value & 0x1FFFFFFF);
	}

	internal StringHandle GetFullName()
	{
		return StringHandle.FromOffset(GetHeapOffset());
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is NamespaceDefinitionHandle other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(NamespaceDefinitionHandle other)
	{
		return _value == other._value;
	}

	public override int GetHashCode()
	{
		return (int)_value;
	}

	public static bool operator ==(NamespaceDefinitionHandle left, NamespaceDefinitionHandle right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(NamespaceDefinitionHandle left, NamespaceDefinitionHandle right)
	{
		return !left.Equals(right);
	}
}
