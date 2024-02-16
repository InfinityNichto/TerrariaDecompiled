using System.Runtime.InteropServices;

namespace System.Security.Cryptography;

public sealed class SafeEvpPKeyHandle : SafeHandle
{
	public static long OpenSslVersion
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
		}
	}

	public override bool IsInvalid
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
		}
	}

	public SafeEvpPKeyHandle()
		: base((IntPtr)0, ownsHandle: false)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public SafeEvpPKeyHandle(IntPtr handle, bool ownsHandle)
		: base((IntPtr)0, ownsHandle: false)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public SafeEvpPKeyHandle DuplicateHandle()
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	protected override bool ReleaseHandle()
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}
}
