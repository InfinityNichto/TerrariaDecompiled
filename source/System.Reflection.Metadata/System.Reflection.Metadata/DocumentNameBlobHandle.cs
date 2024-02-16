using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata;

public readonly struct DocumentNameBlobHandle : IEquatable<DocumentNameBlobHandle>
{
	private readonly int _heapOffset;

	public bool IsNil => _heapOffset == 0;

	private DocumentNameBlobHandle(int heapOffset)
	{
		_heapOffset = heapOffset;
	}

	internal static DocumentNameBlobHandle FromOffset(int heapOffset)
	{
		return new DocumentNameBlobHandle(heapOffset);
	}

	public static implicit operator BlobHandle(DocumentNameBlobHandle handle)
	{
		return BlobHandle.FromOffset(handle._heapOffset);
	}

	public static explicit operator DocumentNameBlobHandle(BlobHandle handle)
	{
		if (handle.IsVirtual)
		{
			Throw.InvalidCast();
		}
		return FromOffset(handle.GetHeapOffset());
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is DocumentNameBlobHandle other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(DocumentNameBlobHandle other)
	{
		return _heapOffset == other._heapOffset;
	}

	public override int GetHashCode()
	{
		return _heapOffset;
	}

	public static bool operator ==(DocumentNameBlobHandle left, DocumentNameBlobHandle right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(DocumentNameBlobHandle left, DocumentNameBlobHandle right)
	{
		return !left.Equals(right);
	}
}
