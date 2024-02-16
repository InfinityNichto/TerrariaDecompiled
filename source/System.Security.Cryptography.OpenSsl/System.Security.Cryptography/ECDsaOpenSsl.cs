using System.IO;

namespace System.Security.Cryptography;

public sealed class ECDsaOpenSsl : ECDsa
{
	public override int KeySize
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
		}
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

	public ECDsaOpenSsl()
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public ECDsaOpenSsl(int keySize)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public ECDsaOpenSsl(IntPtr handle)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public ECDsaOpenSsl(ECCurve curve)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public ECDsaOpenSsl(SafeEvpPKeyHandle pkeyHandle)
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

	public override ECParameters ExportExplicitParameters(bool includePrivateParameters)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public override ECParameters ExportParameters(bool includePrivateParameters)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public override void GenerateKey(ECCurve curve)
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

	public override void ImportParameters(ECParameters parameters)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public override byte[] SignHash(byte[] hash)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public override bool VerifyHash(byte[] hash, byte[] signature)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}
}
