using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm;

[Intrinsic]
[CLSCompliant(false)]
public abstract class Crc32 : ArmBase
{
	[Intrinsic]
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

		public static uint ComputeCrc32(uint crc, ulong data)
		{
			throw new PlatformNotSupportedException();
		}

		public static uint ComputeCrc32C(uint crc, ulong data)
		{
			throw new PlatformNotSupportedException();
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

	public static uint ComputeCrc32(uint crc, byte data)
	{
		throw new PlatformNotSupportedException();
	}

	public static uint ComputeCrc32(uint crc, ushort data)
	{
		throw new PlatformNotSupportedException();
	}

	public static uint ComputeCrc32(uint crc, uint data)
	{
		throw new PlatformNotSupportedException();
	}

	public static uint ComputeCrc32C(uint crc, byte data)
	{
		throw new PlatformNotSupportedException();
	}

	public static uint ComputeCrc32C(uint crc, ushort data)
	{
		throw new PlatformNotSupportedException();
	}

	public static uint ComputeCrc32C(uint crc, uint data)
	{
		throw new PlatformNotSupportedException();
	}
}
