using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata;

public readonly struct BlobHandle : IEquatable<BlobHandle>
{
	internal enum VirtualIndex : byte
	{
		Nil,
		ContractPublicKeyToken,
		ContractPublicKey,
		AttributeUsage_AllowSingle,
		AttributeUsage_AllowMultiple,
		Count
	}

	private readonly uint _value;

	internal const int TemplateParameterOffset_AttributeUsageTarget = 2;

	internal uint RawValue => _value;

	public bool IsNil => _value == 0;

	internal bool IsVirtual => (_value & 0x80000000u) != 0;

	private ushort VirtualValue => (ushort)(_value >> 8);

	private BlobHandle(uint value)
	{
		_value = value;
	}

	internal static BlobHandle FromOffset(int heapOffset)
	{
		return new BlobHandle((uint)heapOffset);
	}

	internal static BlobHandle FromVirtualIndex(VirtualIndex virtualIndex, ushort virtualValue)
	{
		return new BlobHandle(0x80000000u | (uint)(virtualValue << 8) | (uint)virtualIndex);
	}

	internal unsafe void SubstituteTemplateParameters(byte[] blob)
	{
		fixed (byte* ptr = &blob[2])
		{
			*(int*)ptr = VirtualValue;
		}
	}

	public static implicit operator Handle(BlobHandle handle)
	{
		return new Handle((byte)(((handle._value & 0x80000000u) >> 24) | 0x71u), (int)(handle._value & 0x1FFFFFFF));
	}

	public static explicit operator BlobHandle(Handle handle)
	{
		if ((handle.VType & 0x7F) != 113)
		{
			Throw.InvalidCast();
		}
		return new BlobHandle((uint)(((handle.VType & 0x80) << 24) | handle.Offset));
	}

	internal int GetHeapOffset()
	{
		return (int)_value;
	}

	internal VirtualIndex GetVirtualIndex()
	{
		return (VirtualIndex)(_value & 0xFFu);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is BlobHandle other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(BlobHandle other)
	{
		return _value == other._value;
	}

	public override int GetHashCode()
	{
		return (int)_value;
	}

	public static bool operator ==(BlobHandle left, BlobHandle right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(BlobHandle left, BlobHandle right)
	{
		return !left.Equals(right);
	}
}
