using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.IO;
using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public abstract class ECDsa : AsymmetricAlgorithm
{
	private static readonly string[] s_validOids = new string[1] { "1.2.840.10045.2.1" };

	public override string? KeyExchangeAlgorithm => null;

	public override string SignatureAlgorithm => "ECDsa";

	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public new static ECDsa? Create(string algorithm)
	{
		if (algorithm == null)
		{
			throw new ArgumentNullException("algorithm");
		}
		return CryptoConfig.CreateFromName(algorithm) as ECDsa;
	}

	public virtual ECParameters ExportParameters(bool includePrivateParameters)
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual ECParameters ExportExplicitParameters(bool includePrivateParameters)
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual void ImportParameters(ECParameters parameters)
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual void GenerateKey(ECCurve curve)
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		return SignData(data, 0, data.Length, hashAlgorithm);
	}

	public virtual byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (offset < 0 || offset > data.Length)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (count < 0 || count > data.Length - offset)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		byte[] hash = HashData(data, offset, count, hashAlgorithm);
		return SignHash(hash);
	}

	public byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (offset < 0 || offset > data.Length)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (count < 0 || count > data.Length - offset)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return SignDataCore(new ReadOnlySpan<byte>(data, offset, count), hashAlgorithm, signatureFormat);
	}

	protected virtual byte[] SignDataCore(ReadOnlySpan<byte> data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		Span<byte> destination = stackalloc byte[256];
		int maxSignatureSize = GetMaxSignatureSize(signatureFormat);
		byte[] array = null;
		bool flag = false;
		int bytesWritten = 0;
		if (maxSignatureSize > destination.Length)
		{
			array = ArrayPool<byte>.Shared.Rent(maxSignatureSize);
			destination = array;
		}
		try
		{
			if (!TrySignDataCore(data, destination, hashAlgorithm, signatureFormat, out bytesWritten))
			{
				throw new CryptographicException();
			}
			byte[] result = destination.Slice(0, bytesWritten).ToArray();
			flag = true;
			return result;
		}
		finally
		{
			if (array != null)
			{
				CryptographicOperations.ZeroMemory(array.AsSpan(0, bytesWritten));
				if (flag)
				{
					ArrayPool<byte>.Shared.Return(array);
				}
			}
		}
	}

	public byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return SignDataCore(data, hashAlgorithm, signatureFormat);
	}

	public byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return SignDataCore(data, hashAlgorithm, signatureFormat);
	}

	protected virtual byte[] SignDataCore(Stream data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		byte[] array = HashData(data, hashAlgorithm);
		return SignHashCore(array, signatureFormat);
	}

	public byte[] SignHash(byte[] hash, DSASignatureFormat signatureFormat)
	{
		if (hash == null)
		{
			throw new ArgumentNullException("hash");
		}
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return SignHashCore(hash, signatureFormat);
	}

	protected virtual byte[] SignHashCore(ReadOnlySpan<byte> hash, DSASignatureFormat signatureFormat)
	{
		Span<byte> destination = stackalloc byte[256];
		int maxSignatureSize = GetMaxSignatureSize(signatureFormat);
		byte[] array = null;
		bool flag = false;
		int bytesWritten = 0;
		if (maxSignatureSize > destination.Length)
		{
			array = ArrayPool<byte>.Shared.Rent(maxSignatureSize);
			destination = array;
		}
		try
		{
			if (!TrySignHashCore(hash, destination, signatureFormat, out bytesWritten))
			{
				throw new CryptographicException();
			}
			byte[] result = destination.Slice(0, bytesWritten).ToArray();
			flag = true;
			return result;
		}
		finally
		{
			if (array != null)
			{
				CryptographicOperations.ZeroMemory(array.AsSpan(0, bytesWritten));
				if (flag)
				{
					ArrayPool<byte>.Shared.Return(array);
				}
			}
		}
	}

	public virtual bool TrySignData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
	{
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		Span<byte> tmp = stackalloc byte[128];
		ReadOnlySpan<byte> hash = HashSpanToTmp(data, hashAlgorithm, tmp);
		return TrySignHash(hash, destination, out bytesWritten);
	}

	public bool TrySignData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat, out int bytesWritten)
	{
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return TrySignDataCore(data, destination, hashAlgorithm, signatureFormat, out bytesWritten);
	}

	protected virtual bool TrySignDataCore(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat, out int bytesWritten)
	{
		Span<byte> tmp = stackalloc byte[128];
		ReadOnlySpan<byte> hash = HashSpanToTmp(data, hashAlgorithm, tmp);
		return TrySignHashCore(hash, destination, signatureFormat, out bytesWritten);
	}

	public virtual byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		byte[] hash = HashData(data, hashAlgorithm);
		return SignHash(hash);
	}

	public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		return VerifyData(data, 0, data.Length, signature, hashAlgorithm);
	}

	public virtual bool VerifyData(byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (offset < 0 || offset > data.Length)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (count < 0 || count > data.Length - offset)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (signature == null)
		{
			throw new ArgumentNullException("signature");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		byte[] hash = HashData(data, offset, count, hashAlgorithm);
		return VerifyHash(hash, signature);
	}

	public bool VerifyData(byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (offset < 0 || offset > data.Length)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (count < 0 || count > data.Length - offset)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (signature == null)
		{
			throw new ArgumentNullException("signature");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return VerifyDataCore(new ReadOnlySpan<byte>(data, offset, count), signature, hashAlgorithm, signatureFormat);
	}

	public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (signature == null)
		{
			throw new ArgumentNullException("signature");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return VerifyDataCore(data, signature, hashAlgorithm, signatureFormat);
	}

	public virtual bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm)
	{
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		Span<byte> tmp = stackalloc byte[128];
		ReadOnlySpan<byte> hash = HashSpanToTmp(data, hashAlgorithm, tmp);
		return VerifyHash(hash, signature);
	}

	public bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return VerifyDataCore(data, signature, hashAlgorithm, signatureFormat);
	}

	protected virtual bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		Span<byte> span = stackalloc byte[64];
		span = ((!TryHashData(data, span, hashAlgorithm, out var bytesWritten)) ? ((Span<byte>)HashData(data.ToArray(), 0, data.Length, hashAlgorithm)) : span.Slice(0, bytesWritten));
		return VerifyHashCore(span, signature, signatureFormat);
	}

	public bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (signature == null)
		{
			throw new ArgumentNullException("signature");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		byte[] hash = HashData(data, hashAlgorithm);
		return VerifyHash(hash, signature);
	}

	public bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (signature == null)
		{
			throw new ArgumentNullException("signature");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return VerifyDataCore(data, signature, hashAlgorithm, signatureFormat);
	}

	protected virtual bool VerifyDataCore(Stream data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		byte[] array = HashData(data, hashAlgorithm);
		return VerifyHashCore(array, signature, signatureFormat);
	}

	public abstract byte[] SignHash(byte[] hash);

	public abstract bool VerifyHash(byte[] hash, byte[] signature);

	protected virtual byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	protected virtual byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	protected virtual bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
	{
		byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
		bool flag = false;
		try
		{
			data.CopyTo(array);
			byte[] array2 = HashData(array, 0, data.Length, hashAlgorithm);
			flag = true;
			if (array2.Length <= destination.Length)
			{
				new ReadOnlySpan<byte>(array2).CopyTo(destination);
				bytesWritten = array2.Length;
				return true;
			}
			bytesWritten = 0;
			return false;
		}
		finally
		{
			Array.Clear(array, 0, data.Length);
			if (flag)
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}
	}

	public virtual bool TrySignHash(ReadOnlySpan<byte> hash, Span<byte> destination, out int bytesWritten)
	{
		return TrySignHashCore(hash, destination, DSASignatureFormat.IeeeP1363FixedFieldConcatenation, out bytesWritten);
	}

	public bool TrySignHash(ReadOnlySpan<byte> hash, Span<byte> destination, DSASignatureFormat signatureFormat, out int bytesWritten)
	{
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return TrySignHashCore(hash, destination, signatureFormat, out bytesWritten);
	}

	protected virtual bool TrySignHashCore(ReadOnlySpan<byte> hash, Span<byte> destination, DSASignatureFormat signatureFormat, out int bytesWritten)
	{
		byte[] signature = SignHash(hash.ToArray());
		byte[] array = AsymmetricAlgorithmHelpers.ConvertFromIeeeP1363Signature(signature, signatureFormat);
		return Internal.Cryptography.Helpers.TryCopyToDestination(array, destination, out bytesWritten);
	}

	public virtual bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
	{
		return VerifyHashCore(hash, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
	}

	public bool VerifyHash(byte[] hash, byte[] signature, DSASignatureFormat signatureFormat)
	{
		if (hash == null)
		{
			throw new ArgumentNullException("hash");
		}
		if (signature == null)
		{
			throw new ArgumentNullException("signature");
		}
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return VerifyHashCore(hash, signature, signatureFormat);
	}

	public bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, DSASignatureFormat signatureFormat)
	{
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return VerifyHashCore(hash, signature, signatureFormat);
	}

	protected virtual bool VerifyHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, DSASignatureFormat signatureFormat)
	{
		byte[] array = this.ConvertSignatureToIeeeP1363(signatureFormat, signature);
		if (array == null)
		{
			return false;
		}
		return VerifyHash(hash.ToArray(), array);
	}

	private ReadOnlySpan<byte> HashSpanToTmp(ReadOnlySpan<byte> data, HashAlgorithmName hashAlgorithm, Span<byte> tmp)
	{
		if (TryHashData(data, tmp, hashAlgorithm, out var bytesWritten))
		{
			return tmp.Slice(0, bytesWritten);
		}
		return HashSpanToArray(data, hashAlgorithm);
	}

	private byte[] HashSpanToArray(ReadOnlySpan<byte> data, HashAlgorithmName hashAlgorithm)
	{
		byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
		bool flag = false;
		try
		{
			data.CopyTo(array);
			byte[] result = HashData(array, 0, data.Length, hashAlgorithm);
			flag = true;
			return result;
		}
		finally
		{
			Array.Clear(array, 0, data.Length);
			if (flag)
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}
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

	public int GetMaxSignatureSize(DSASignatureFormat signatureFormat)
	{
		int keySize = KeySize;
		if (keySize == 0)
		{
			ExportParameters(includePrivateParameters: false);
			keySize = KeySize;
			if (keySize == 0)
			{
				throw new NotSupportedException(System.SR.Cryptography_InvalidKeySize);
			}
		}
		return signatureFormat switch
		{
			DSASignatureFormat.IeeeP1363FixedFieldConcatenation => AsymmetricAlgorithmHelpers.BitsToBytes(keySize) * 2, 
			DSASignatureFormat.Rfc3279DerSequence => AsymmetricAlgorithmHelpers.GetMaxDerSignatureSize(keySize), 
			_ => throw new ArgumentOutOfRangeException("signatureFormat"), 
		};
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

	public new static ECDsa Create()
	{
		return new ECDsaImplementation.ECDsaCng();
	}

	public static ECDsa Create(ECCurve curve)
	{
		return new ECDsaImplementation.ECDsaCng(curve);
	}

	public static ECDsa Create(ECParameters parameters)
	{
		ECDsa eCDsa = new ECDsaImplementation.ECDsaCng();
		eCDsa.ImportParameters(parameters);
		return eCDsa;
	}
}
