using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86;

[Intrinsic]
[CLSCompliant(false)]
public abstract class Pclmulqdq : Sse2
{
	[Intrinsic]
	public new abstract class X64 : Sse2.X64
	{
		public new static bool IsSupported => IsSupported;
	}

	public new static bool IsSupported => IsSupported;

	public static Vector128<long> CarrylessMultiply(Vector128<long> left, Vector128<long> right, byte control)
	{
		return CarrylessMultiply(left, right, control);
	}

	public static Vector128<ulong> CarrylessMultiply(Vector128<ulong> left, Vector128<ulong> right, byte control)
	{
		return CarrylessMultiply(left, right, control);
	}
}
