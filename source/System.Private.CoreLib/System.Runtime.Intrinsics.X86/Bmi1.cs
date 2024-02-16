using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86;

[Intrinsic]
[CLSCompliant(false)]
public abstract class Bmi1 : X86Base
{
	[Intrinsic]
	public new abstract class X64 : X86Base.X64
	{
		public new static bool IsSupported => IsSupported;

		public static ulong AndNot(ulong left, ulong right)
		{
			return AndNot(left, right);
		}

		public static ulong BitFieldExtract(ulong value, byte start, byte length)
		{
			return BitFieldExtract(value, (ushort)(start | (length << 8)));
		}

		public static ulong BitFieldExtract(ulong value, ushort control)
		{
			return BitFieldExtract(value, control);
		}

		public static ulong ExtractLowestSetBit(ulong value)
		{
			return ExtractLowestSetBit(value);
		}

		public static ulong GetMaskUpToLowestSetBit(ulong value)
		{
			return GetMaskUpToLowestSetBit(value);
		}

		public static ulong ResetLowestSetBit(ulong value)
		{
			return ResetLowestSetBit(value);
		}

		public static ulong TrailingZeroCount(ulong value)
		{
			return TrailingZeroCount(value);
		}
	}

	public new static bool IsSupported => IsSupported;

	public static uint AndNot(uint left, uint right)
	{
		return AndNot(left, right);
	}

	public static uint BitFieldExtract(uint value, byte start, byte length)
	{
		return BitFieldExtract(value, (ushort)(start | (length << 8)));
	}

	public static uint BitFieldExtract(uint value, ushort control)
	{
		return BitFieldExtract(value, control);
	}

	public static uint ExtractLowestSetBit(uint value)
	{
		return ExtractLowestSetBit(value);
	}

	public static uint GetMaskUpToLowestSetBit(uint value)
	{
		return GetMaskUpToLowestSetBit(value);
	}

	public static uint ResetLowestSetBit(uint value)
	{
		return ResetLowestSetBit(value);
	}

	public static uint TrailingZeroCount(uint value)
	{
		return TrailingZeroCount(value);
	}
}
