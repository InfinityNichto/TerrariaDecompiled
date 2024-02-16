using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86;

[Intrinsic]
[CLSCompliant(false)]
public abstract class Sse42 : Sse41
{
	[Intrinsic]
	public new abstract class X64 : Sse41.X64
	{
		public new static bool IsSupported => IsSupported;

		public static ulong Crc32(ulong crc, ulong data)
		{
			return Crc32(crc, data);
		}
	}

	public new static bool IsSupported => IsSupported;

	public static Vector128<long> CompareGreaterThan(Vector128<long> left, Vector128<long> right)
	{
		return CompareGreaterThan(left, right);
	}

	public static uint Crc32(uint crc, byte data)
	{
		return Crc32(crc, data);
	}

	public static uint Crc32(uint crc, ushort data)
	{
		return Crc32(crc, data);
	}

	public static uint Crc32(uint crc, uint data)
	{
		return Crc32(crc, data);
	}
}
