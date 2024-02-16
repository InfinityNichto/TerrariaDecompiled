using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86;

[Intrinsic]
[CLSCompliant(false)]
public abstract class Lzcnt : X86Base
{
	[Intrinsic]
	public new abstract class X64 : X86Base.X64
	{
		public new static bool IsSupported => IsSupported;

		public static ulong LeadingZeroCount(ulong value)
		{
			return LeadingZeroCount(value);
		}
	}

	public new static bool IsSupported => IsSupported;

	public static uint LeadingZeroCount(uint value)
	{
		return LeadingZeroCount(value);
	}
}
