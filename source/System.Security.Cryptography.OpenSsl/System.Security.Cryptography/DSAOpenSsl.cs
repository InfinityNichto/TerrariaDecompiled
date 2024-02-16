using System.IO;

namespace System.Security.Cryptography;

public sealed class DSAOpenSsl : DSA
{
	public override int KeySize
	{
		set
		{
			throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
		}
	}

	public override KeySizes[] LegalKeySizes
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
		}
	}

	public DSAOpenSsl()
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public DSAOpenSsl(int keySize)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public DSAOpenSsl(IntPtr handle)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public DSAOpenSsl(DSAParameters parameters)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public DSAOpenSsl(SafeEvpPKeyHandle pkeyHandle)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public override byte[] CreateSignature(byte[] rgbHash)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	protected override void Dispose(bool disposing)
	{
	}

	public SafeEvpPKeyHandle DuplicateKeyHandle()
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public override DSAParameters ExportParameters(bool includePrivateParameters)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public override void ImportParameters(DSAParameters parameters)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}
}
