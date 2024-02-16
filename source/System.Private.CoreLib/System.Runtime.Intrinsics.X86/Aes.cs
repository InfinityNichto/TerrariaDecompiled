using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86;

[Intrinsic]
[CLSCompliant(false)]
public abstract class Aes : Sse2
{
	[Intrinsic]
	public new abstract class X64 : Sse2.X64
	{
		public new static bool IsSupported => IsSupported;
	}

	public new static bool IsSupported => IsSupported;

	public static Vector128<byte> Decrypt(Vector128<byte> value, Vector128<byte> roundKey)
	{
		return Decrypt(value, roundKey);
	}

	public static Vector128<byte> DecryptLast(Vector128<byte> value, Vector128<byte> roundKey)
	{
		return DecryptLast(value, roundKey);
	}

	public static Vector128<byte> Encrypt(Vector128<byte> value, Vector128<byte> roundKey)
	{
		return Encrypt(value, roundKey);
	}

	public static Vector128<byte> EncryptLast(Vector128<byte> value, Vector128<byte> roundKey)
	{
		return EncryptLast(value, roundKey);
	}

	public static Vector128<byte> InverseMixColumns(Vector128<byte> value)
	{
		return InverseMixColumns(value);
	}

	public static Vector128<byte> KeygenAssist(Vector128<byte> value, byte control)
	{
		return KeygenAssist(value, control);
	}
}
