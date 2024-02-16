using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Reflection.Internal;

internal sealed class ByteSequenceComparer : IEqualityComparer<byte[]>, IEqualityComparer<ImmutableArray<byte>>
{
	internal static readonly ByteSequenceComparer Instance = new ByteSequenceComparer();

	private ByteSequenceComparer()
	{
	}

	internal static bool Equals(ImmutableArray<byte> x, ImmutableArray<byte> y)
	{
		if (x == y)
		{
			return true;
		}
		if (x.IsDefault || y.IsDefault || x.Length != y.Length)
		{
			return false;
		}
		for (int i = 0; i < x.Length; i++)
		{
			if (x[i] != y[i])
			{
				return false;
			}
		}
		return true;
	}

	internal static bool Equals(byte[] left, int leftStart, byte[] right, int rightStart, int length)
	{
		if (left == null || right == null)
		{
			return left == right;
		}
		if (left == right && leftStart == rightStart)
		{
			return true;
		}
		for (int i = 0; i < length; i++)
		{
			if (left[leftStart + i] != right[rightStart + i])
			{
				return false;
			}
		}
		return true;
	}

	internal static bool Equals(byte[]? left, byte[]? right)
	{
		if (left == right)
		{
			return true;
		}
		if (left == null || right == null || left.Length != right.Length)
		{
			return false;
		}
		for (int i = 0; i < left.Length; i++)
		{
			if (left[i] != right[i])
			{
				return false;
			}
		}
		return true;
	}

	internal static int GetHashCode(byte[] x)
	{
		return Hash.GetFNVHashCode(x);
	}

	internal static int GetHashCode(ImmutableArray<byte> x)
	{
		return Hash.GetFNVHashCode(x);
	}

	bool IEqualityComparer<byte[]>.Equals(byte[] x, byte[] y)
	{
		return Equals(x, y);
	}

	int IEqualityComparer<byte[]>.GetHashCode(byte[] x)
	{
		return GetHashCode(x);
	}

	bool IEqualityComparer<ImmutableArray<byte>>.Equals(ImmutableArray<byte> x, ImmutableArray<byte> y)
	{
		return Equals(x, y);
	}

	int IEqualityComparer<ImmutableArray<byte>>.GetHashCode(ImmutableArray<byte> x)
	{
		return GetHashCode(x);
	}
}
