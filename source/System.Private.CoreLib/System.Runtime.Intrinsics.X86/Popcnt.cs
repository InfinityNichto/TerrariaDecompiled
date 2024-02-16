using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86;

[Intrinsic]
[CLSCompliant(false)]
public abstract class Popcnt : Sse42
{
	[Intrinsic]
	public new abstract class X64 : Sse42.X64
	{
		public new static bool IsSupported => IsSupported;

		public static ulong PopCount(ulong value)
		{
			return PopCount(value);
		}
	}

	public new static bool IsSupported => IsSupported;

	public static uint PopCount(uint value)
	{
		return PopCount(value);
	}
}
