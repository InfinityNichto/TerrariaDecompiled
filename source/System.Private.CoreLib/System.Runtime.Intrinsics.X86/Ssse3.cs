using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86;

[Intrinsic]
[CLSCompliant(false)]
public abstract class Ssse3 : Sse3
{
	[Intrinsic]
	public new abstract class X64 : Sse3.X64
	{
		public new static bool IsSupported => IsSupported;
	}

	public new static bool IsSupported => IsSupported;

	public static Vector128<byte> Abs(Vector128<sbyte> value)
	{
		return Abs(value);
	}

	public static Vector128<ushort> Abs(Vector128<short> value)
	{
		return Abs(value);
	}

	public static Vector128<uint> Abs(Vector128<int> value)
	{
		return Abs(value);
	}

	public static Vector128<sbyte> AlignRight(Vector128<sbyte> left, Vector128<sbyte> right, byte mask)
	{
		return AlignRight(left, right, mask);
	}

	public static Vector128<byte> AlignRight(Vector128<byte> left, Vector128<byte> right, byte mask)
	{
		return AlignRight(left, right, mask);
	}

	public static Vector128<short> AlignRight(Vector128<short> left, Vector128<short> right, byte mask)
	{
		return AlignRight(left, right, mask);
	}

	public static Vector128<ushort> AlignRight(Vector128<ushort> left, Vector128<ushort> right, byte mask)
	{
		return AlignRight(left, right, mask);
	}

	public static Vector128<int> AlignRight(Vector128<int> left, Vector128<int> right, byte mask)
	{
		return AlignRight(left, right, mask);
	}

	public static Vector128<uint> AlignRight(Vector128<uint> left, Vector128<uint> right, byte mask)
	{
		return AlignRight(left, right, mask);
	}

	public static Vector128<long> AlignRight(Vector128<long> left, Vector128<long> right, byte mask)
	{
		return AlignRight(left, right, mask);
	}

	public static Vector128<ulong> AlignRight(Vector128<ulong> left, Vector128<ulong> right, byte mask)
	{
		return AlignRight(left, right, mask);
	}

	public static Vector128<short> HorizontalAdd(Vector128<short> left, Vector128<short> right)
	{
		return HorizontalAdd(left, right);
	}

	public static Vector128<int> HorizontalAdd(Vector128<int> left, Vector128<int> right)
	{
		return HorizontalAdd(left, right);
	}

	public static Vector128<short> HorizontalAddSaturate(Vector128<short> left, Vector128<short> right)
	{
		return HorizontalAddSaturate(left, right);
	}

	public static Vector128<short> HorizontalSubtract(Vector128<short> left, Vector128<short> right)
	{
		return HorizontalSubtract(left, right);
	}

	public static Vector128<int> HorizontalSubtract(Vector128<int> left, Vector128<int> right)
	{
		return HorizontalSubtract(left, right);
	}

	public static Vector128<short> HorizontalSubtractSaturate(Vector128<short> left, Vector128<short> right)
	{
		return HorizontalSubtractSaturate(left, right);
	}

	public static Vector128<short> MultiplyAddAdjacent(Vector128<byte> left, Vector128<sbyte> right)
	{
		return MultiplyAddAdjacent(left, right);
	}

	public static Vector128<short> MultiplyHighRoundScale(Vector128<short> left, Vector128<short> right)
	{
		return MultiplyHighRoundScale(left, right);
	}

	public static Vector128<sbyte> Shuffle(Vector128<sbyte> value, Vector128<sbyte> mask)
	{
		return Shuffle(value, mask);
	}

	public static Vector128<byte> Shuffle(Vector128<byte> value, Vector128<byte> mask)
	{
		return Shuffle(value, mask);
	}

	public static Vector128<sbyte> Sign(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return Sign(left, right);
	}

	public static Vector128<short> Sign(Vector128<short> left, Vector128<short> right)
	{
		return Sign(left, right);
	}

	public static Vector128<int> Sign(Vector128<int> left, Vector128<int> right)
	{
		return Sign(left, right);
	}
}
