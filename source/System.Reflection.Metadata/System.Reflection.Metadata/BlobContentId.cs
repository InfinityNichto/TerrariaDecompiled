using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Internal;

namespace System.Reflection.Metadata;

public readonly struct BlobContentId : IEquatable<BlobContentId>
{
	public Guid Guid { get; }

	public uint Stamp { get; }

	public bool IsDefault
	{
		get
		{
			if (Guid == default(Guid))
			{
				return Stamp == 0;
			}
			return false;
		}
	}

	public BlobContentId(Guid guid, uint stamp)
	{
		Guid = guid;
		Stamp = stamp;
	}

	public BlobContentId(ImmutableArray<byte> id)
		: this(ImmutableByteArrayInterop.DangerousGetUnderlyingArray(id))
	{
	}

	public unsafe BlobContentId(byte[] id)
	{
		if (id == null)
		{
			throw new ArgumentNullException("id");
		}
		if (id.Length != 20)
		{
			throw new ArgumentException(System.SR.Format(System.SR.UnexpectedArrayLength, 20), "id");
		}
		fixed (byte* buffer = &id[0])
		{
			BlobReader blobReader = new BlobReader(buffer, id.Length);
			Guid = blobReader.ReadGuid();
			Stamp = blobReader.ReadUInt32();
		}
	}

	public static BlobContentId FromHash(ImmutableArray<byte> hashCode)
	{
		return FromHash(ImmutableByteArrayInterop.DangerousGetUnderlyingArray(hashCode));
	}

	public static BlobContentId FromHash(byte[] hashCode)
	{
		if (hashCode == null)
		{
			throw new ArgumentNullException("hashCode");
		}
		if (hashCode.Length < 20)
		{
			throw new ArgumentException(System.SR.Format(System.SR.HashTooShort, 20), "hashCode");
		}
		uint a = (uint)((hashCode[3] << 24) | (hashCode[2] << 16) | (hashCode[1] << 8) | hashCode[0]);
		ushort num = (ushort)((hashCode[5] << 8) | hashCode[4]);
		ushort num2 = (ushort)((hashCode[7] << 8) | hashCode[6]);
		byte b = hashCode[8];
		byte e = hashCode[9];
		byte f = hashCode[10];
		byte g = hashCode[11];
		byte h = hashCode[12];
		byte i = hashCode[13];
		byte j = hashCode[14];
		byte k = hashCode[15];
		num2 = (ushort)((num2 & 0xFFFu) | 0x4000u);
		b = (byte)((b & 0x3Fu) | 0x80u);
		Guid guid = new Guid((int)a, (short)num, (short)num2, b, e, f, g, h, i, j, k);
		uint stamp = 0x80000000u | (uint)((hashCode[19] << 24) | (hashCode[18] << 16) | (hashCode[17] << 8) | hashCode[16]);
		return new BlobContentId(guid, stamp);
	}

	public static Func<IEnumerable<Blob>, BlobContentId> GetTimeBasedProvider()
	{
		uint timestamp = (uint)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
		return (IEnumerable<Blob> content) => new BlobContentId(Guid.NewGuid(), timestamp);
	}

	public bool Equals(BlobContentId other)
	{
		if (Guid == other.Guid)
		{
			return Stamp == other.Stamp;
		}
		return false;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is BlobContentId other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Hash.Combine(Stamp, Guid.GetHashCode());
	}

	public static bool operator ==(BlobContentId left, BlobContentId right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(BlobContentId left, BlobContentId right)
	{
		return !left.Equals(right);
	}
}
