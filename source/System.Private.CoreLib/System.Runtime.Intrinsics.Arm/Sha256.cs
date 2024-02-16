using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm;

[CLSCompliant(false)]
public abstract class Sha256 : ArmBase
{
	public new abstract class Arm64 : ArmBase.Arm64
	{
		public new static bool IsSupported
		{
			[Intrinsic]
			get
			{
				return false;
			}
		}
	}

	public new static bool IsSupported
	{
		[Intrinsic]
		get
		{
			return false;
		}
	}

	public static Vector128<uint> HashUpdate1(Vector128<uint> hash_abcd, Vector128<uint> hash_efgh, Vector128<uint> wk)
	{
		throw new PlatformNotSupportedException();
	}

	public static Vector128<uint> HashUpdate2(Vector128<uint> hash_efgh, Vector128<uint> hash_abcd, Vector128<uint> wk)
	{
		throw new PlatformNotSupportedException();
	}

	public static Vector128<uint> ScheduleUpdate0(Vector128<uint> w0_3, Vector128<uint> w4_7)
	{
		throw new PlatformNotSupportedException();
	}

	public static Vector128<uint> ScheduleUpdate1(Vector128<uint> w0_3, Vector128<uint> w8_11, Vector128<uint> w12_15)
	{
		throw new PlatformNotSupportedException();
	}
}
