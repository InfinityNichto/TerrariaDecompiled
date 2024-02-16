using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86;

[Intrinsic]
[CLSCompliant(false)]
public abstract class Bmi2 : X86Base
{
	[Intrinsic]
	public new abstract class X64 : X86Base.X64
	{
		public new static bool IsSupported => IsSupported;

		public static ulong ZeroHighBits(ulong value, ulong index)
		{
			return ZeroHighBits(value, index);
		}

		public static ulong MultiplyNoFlags(ulong left, ulong right)
		{
			return MultiplyNoFlags(left, right);
		}

		public unsafe static ulong MultiplyNoFlags(ulong left, ulong right, ulong* low)
		{
			return MultiplyNoFlags(left, right, low);
		}

		public static ulong ParallelBitDeposit(ulong value, ulong mask)
		{
			return ParallelBitDeposit(value, mask);
		}

		public static ulong ParallelBitExtract(ulong value, ulong mask)
		{
			return ParallelBitExtract(value, mask);
		}
	}

	public new static bool IsSupported => IsSupported;

	public static uint ZeroHighBits(uint value, uint index)
	{
		return ZeroHighBits(value, index);
	}

	public static uint MultiplyNoFlags(uint left, uint right)
	{
		return MultiplyNoFlags(left, right);
	}

	public unsafe static uint MultiplyNoFlags(uint left, uint right, uint* low)
	{
		return MultiplyNoFlags(left, right, low);
	}

	public static uint ParallelBitDeposit(uint value, uint mask)
	{
		return ParallelBitDeposit(value, mask);
	}

	public static uint ParallelBitExtract(uint value, uint mask)
	{
		return ParallelBitExtract(value, mask);
	}
}
