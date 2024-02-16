using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm;

[CLSCompliant(false)]
public abstract class Aes : ArmBase
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

	public static Vector128<byte> Decrypt(Vector128<byte> value, Vector128<byte> roundKey)
	{
		throw new PlatformNotSupportedException();
	}

	public static Vector128<byte> Encrypt(Vector128<byte> value, Vector128<byte> roundKey)
	{
		throw new PlatformNotSupportedException();
	}

	public static Vector128<byte> InverseMixColumns(Vector128<byte> value)
	{
		throw new PlatformNotSupportedException();
	}

	public static Vector128<byte> MixColumns(Vector128<byte> value)
	{
		throw new PlatformNotSupportedException();
	}

	public static Vector128<long> PolynomialMultiplyWideningLower(Vector64<long> left, Vector64<long> right)
	{
		throw new PlatformNotSupportedException();
	}

	public static Vector128<ulong> PolynomialMultiplyWideningLower(Vector64<ulong> left, Vector64<ulong> right)
	{
		throw new PlatformNotSupportedException();
	}

	public static Vector128<long> PolynomialMultiplyWideningUpper(Vector128<long> left, Vector128<long> right)
	{
		throw new PlatformNotSupportedException();
	}

	public static Vector128<ulong> PolynomialMultiplyWideningUpper(Vector128<ulong> left, Vector128<ulong> right)
	{
		throw new PlatformNotSupportedException();
	}
}
