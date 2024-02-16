using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm;

[CLSCompliant(false)]
public abstract class ArmBase
{
	public abstract class Arm64
	{
		public static bool IsSupported
		{
			[Intrinsic]
			get
			{
				return false;
			}
		}

		public static int LeadingSignCount(int value)
		{
			throw new PlatformNotSupportedException();
		}

		public static int LeadingSignCount(long value)
		{
			throw new PlatformNotSupportedException();
		}

		public static int LeadingZeroCount(long value)
		{
			throw new PlatformNotSupportedException();
		}

		public static int LeadingZeroCount(ulong value)
		{
			throw new PlatformNotSupportedException();
		}

		public static long MultiplyHigh(long left, long right)
		{
			throw new PlatformNotSupportedException();
		}

		public static ulong MultiplyHigh(ulong left, ulong right)
		{
			throw new PlatformNotSupportedException();
		}

		public static long ReverseElementBits(long value)
		{
			throw new PlatformNotSupportedException();
		}

		public static ulong ReverseElementBits(ulong value)
		{
			throw new PlatformNotSupportedException();
		}
	}

	public static bool IsSupported
	{
		[Intrinsic]
		get
		{
			return false;
		}
	}

	public static int LeadingZeroCount(int value)
	{
		throw new PlatformNotSupportedException();
	}

	public static int LeadingZeroCount(uint value)
	{
		throw new PlatformNotSupportedException();
	}

	public static int ReverseElementBits(int value)
	{
		throw new PlatformNotSupportedException();
	}

	public static uint ReverseElementBits(uint value)
	{
		throw new PlatformNotSupportedException();
	}
}
