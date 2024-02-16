using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.Intrinsics.X86;

[Intrinsic]
public abstract class X86Base
{
	[Intrinsic]
	public abstract class X64
	{
		public static bool IsSupported => IsSupported;

		internal static ulong BitScanForward(ulong value)
		{
			return BitScanForward(value);
		}

		internal static ulong BitScanReverse(ulong value)
		{
			return BitScanReverse(value);
		}
	}

	public static bool IsSupported => IsSupported;

	[DllImport("QCall")]
	private unsafe static extern void __cpuidex(int* cpuInfo, int functionId, int subFunctionId);

	internal static uint BitScanForward(uint value)
	{
		return BitScanForward(value);
	}

	internal static uint BitScanReverse(uint value)
	{
		return BitScanReverse(value);
	}

	public unsafe static (int Eax, int Ebx, int Ecx, int Edx) CpuId(int functionId, int subFunctionId)
	{
		int* ptr = stackalloc int[4];
		__cpuidex(ptr, functionId, subFunctionId);
		return (Eax: *ptr, Ebx: ptr[1], Ecx: ptr[2], Edx: ptr[3]);
	}
}
