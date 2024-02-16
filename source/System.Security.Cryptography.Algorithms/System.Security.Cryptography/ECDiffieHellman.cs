using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public abstract class ECDiffieHellman : AsymmetricAlgorithm
{
	private static readonly string[] s_validOids = new string[1] { "1.2.840.10045.2.1" };

	public override string KeyExchangeAlgorithm => "ECDiffieHellman";

	public override string? SignatureAlgorithm => null;

	public abstract ECDiffieHellmanPublicKey PublicKey { get; }

	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public new static ECDiffieHellman? Create(string algorithm)
	{
		if (algorithm == null)
		{
			throw new ArgumentNullException("algorithm");
		}
		return CryptoConfig.CreateFromName(algorithm) as ECDiffieHellman;
	}

	public virtual byte[] DeriveKeyMaterial(ECDiffieHellmanPublicKey otherPartyPublicKey)
	{
		throw DerivedClassMustOverride();
	}

	public byte[] DeriveKeyFromHash(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm)
	{
		return DeriveKeyFromHash(otherPartyPublicKey, hashAlgorithm, null, null);
	}

	public virtual byte[] DeriveKeyFromHash(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm, byte[]? secretPrepend, byte[]? secretAppend)
	{
		throw DerivedClassMustOverride();
	}

	public byte[] DeriveKeyFromHmac(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm, byte[]? hmacKey)
	{
		return DeriveKeyFromHmac(otherPartyPublicKey, hashAlgorithm, hmacKey, null, null);
	}

	public virtual byte[] DeriveKeyFromHmac(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm, byte[]? hmacKey, byte[]? secretPrepend, byte[]? secretAppend)
	{
		throw DerivedClassMustOverride();
	}

	public virtual byte[] DeriveKeyTls(ECDiffieHellmanPublicKey otherPartyPublicKey, byte[] prfLabel, byte[] prfSeed)
	{
		throw DerivedClassMustOverride();
	}

	private static Exception DerivedClassMustOverride()
	{
		return new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual ECParameters ExportParameters(bool includePrivateParameters)
	{
		throw DerivedClassMustOverride();
	}

	public virtual ECParameters ExportExplicitParameters(bool includePrivateParameters)
	{
		throw DerivedClassMustOverride();
	}

	public virtual void ImportParameters(ECParameters parameters)
	{
		throw DerivedClassMustOverride();
	}

	public virtual void GenerateKey(ECCurve curve)
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	public unsafe override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		if (pbeParameters == null)
		{
			throw new ArgumentNullException("pbeParameters");
		}
		PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
		ECParameters ecParameters = ExportParameters(includePrivateParameters: true);
		fixed (byte* ptr = ecParameters.D)
		{
			try
			{
				AsnWriter pkcs8Writer = EccKeyFormatHelper.WritePkcs8PrivateKey(ecParameters);
				AsnWriter asnWriter = KeyFormatHelper.WriteEncryptedPkcs8(passwordBytes, pkcs8Writer, pbeParameters);
				return asnWriter.TryEncode(destination, out bytesWritten);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(ecParameters.D);
			}
		}
	}

	public unsafe override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		if (pbeParameters == null)
		{
			throw new ArgumentNullException("pbeParameters");
		}
		PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
		ECParameters ecParameters = ExportParameters(includePrivateParameters: true);
		fixed (byte* ptr = ecParameters.D)
		{
			try
			{
				AsnWriter pkcs8Writer = EccKeyFormatHelper.WritePkcs8PrivateKey(ecParameters);
				AsnWriter asnWriter = KeyFormatHelper.WriteEncryptedPkcs8(password, pkcs8Writer, pbeParameters);
				return asnWriter.TryEncode(destination, out bytesWritten);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(ecParameters.D);
			}
		}
	}

	public unsafe override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
	{
		ECParameters ecParameters = ExportParameters(includePrivateParameters: true);
		fixed (byte* ptr = ecParameters.D)
		{
			try
			{
				AsnWriter asnWriter = EccKeyFormatHelper.WritePkcs8PrivateKey(ecParameters);
				return asnWriter.TryEncode(destination, out bytesWritten);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(ecParameters.D);
			}
		}
	}

	public override bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
	{
		ECParameters ecParameters = ExportParameters(includePrivateParameters: false);
		AsnWriter asnWriter = EccKeyFormatHelper.WriteSubjectPublicKeyInfo(ecParameters);
		return asnWriter.TryEncode(destination, out bytesWritten);
	}

	public unsafe override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source, out int bytesRead)
	{
		KeyFormatHelper.ReadEncryptedPkcs8(s_validOids, source, passwordBytes, (KeyFormatHelper.KeyReader<ECParameters>)EccKeyFormatHelper.FromECPrivateKey, out int bytesRead2, out ECParameters ret);
		fixed (byte* ptr = ret.D)
		{
			try
			{
				ImportParameters(ret);
				bytesRead = bytesRead2;
			}
			finally
			{
				CryptographicOperations.ZeroMemory(ret.D);
			}
		}
	}

	public unsafe override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source, out int bytesRead)
	{
		KeyFormatHelper.ReadEncryptedPkcs8(s_validOids, source, password, (KeyFormatHelper.KeyReader<ECParameters>)EccKeyFormatHelper.FromECPrivateKey, out int bytesRead2, out ECParameters ret);
		fixed (byte* ptr = ret.D)
		{
			try
			{
				ImportParameters(ret);
				bytesRead = bytesRead2;
			}
			finally
			{
				CryptographicOperations.ZeroMemory(ret.D);
			}
		}
	}

	public unsafe override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
	{
		KeyFormatHelper.ReadPkcs8(s_validOids, source, (KeyFormatHelper.KeyReader<ECParameters>)EccKeyFormatHelper.FromECPrivateKey, out int bytesRead2, out ECParameters ret);
		fixed (byte* ptr = ret.D)
		{
			try
			{
				ImportParameters(ret);
				bytesRead = bytesRead2;
			}
			finally
			{
				CryptographicOperations.ZeroMemory(ret.D);
			}
		}
	}

	public override void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead)
	{
		KeyFormatHelper.ReadSubjectPublicKeyInfo(s_validOids, source, (KeyFormatHelper.KeyReader<ECParameters>)EccKeyFormatHelper.FromECPublicKey, out int bytesRead2, out ECParameters ret);
		ImportParameters(ret);
		bytesRead = bytesRead2;
	}

	public unsafe virtual void ImportECPrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
	{
		int bytesRead2;
		ECParameters parameters = EccKeyFormatHelper.FromECPrivateKey(source, out bytesRead2);
		fixed (byte* ptr = parameters.D)
		{
			try
			{
				ImportParameters(parameters);
				bytesRead = bytesRead2;
			}
			finally
			{
				CryptographicOperations.ZeroMemory(parameters.D);
			}
		}
	}

	public unsafe virtual byte[] ExportECPrivateKey()
	{
		ECParameters ecParameters = ExportParameters(includePrivateParameters: true);
		fixed (byte* ptr = ecParameters.D)
		{
			try
			{
				AsnWriter asnWriter = EccKeyFormatHelper.WriteECPrivateKey(in ecParameters);
				return asnWriter.Encode();
			}
			finally
			{
				CryptographicOperations.ZeroMemory(ecParameters.D);
			}
		}
	}

	public unsafe virtual bool TryExportECPrivateKey(Span<byte> destination, out int bytesWritten)
	{
		ECParameters ecParameters = ExportParameters(includePrivateParameters: true);
		fixed (byte* ptr = ecParameters.D)
		{
			try
			{
				AsnWriter asnWriter = EccKeyFormatHelper.WriteECPrivateKey(in ecParameters);
				return asnWriter.TryEncode(destination, out bytesWritten);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(ecParameters.D);
			}
		}
	}

	public override void ImportFromPem(ReadOnlySpan<char> input)
	{
		PemKeyImportHelpers.ImportPem(input, delegate(ReadOnlySpan<char> label)
		{
			if (label.SequenceEqual("PRIVATE KEY"))
			{
				return ImportPkcs8PrivateKey;
			}
			if (label.SequenceEqual("PUBLIC KEY"))
			{
				return ImportSubjectPublicKeyInfo;
			}
			return label.SequenceEqual("EC PRIVATE KEY") ? new PemKeyImportHelpers.ImportKeyAction(ImportECPrivateKey) : null;
		});
	}

	public override void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<char> password)
	{
		PemKeyImportHelpers.ImportEncryptedPem(input, password, ImportEncryptedPkcs8PrivateKey);
	}

	public override void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<byte> passwordBytes)
	{
		PemKeyImportHelpers.ImportEncryptedPem(input, passwordBytes, ImportEncryptedPkcs8PrivateKey);
	}

	public override void FromXmlString(string xmlString)
	{
		throw new NotImplementedException(System.SR.Cryptography_ECXmlSerializationFormatRequired);
	}

	public override string ToXmlString(bool includePrivateParameters)
	{
		throw new NotImplementedException(System.SR.Cryptography_ECXmlSerializationFormatRequired);
	}

	public new static ECDiffieHellman Create()
	{
		return new ECDiffieHellmanImplementation.ECDiffieHellmanCng();
	}

	public static ECDiffieHellman Create(ECCurve curve)
	{
		return new ECDiffieHellmanImplementation.ECDiffieHellmanCng(curve);
	}

	public static ECDiffieHellman Create(ECParameters parameters)
	{
		ECDiffieHellman eCDiffieHellman = new ECDiffieHellmanImplementation.ECDiffieHellmanCng();
		try
		{
			eCDiffieHellman.ImportParameters(parameters);
			return eCDiffieHellman;
		}
		catch
		{
			eCDiffieHellman.Dispose();
			throw;
		}
	}
}
