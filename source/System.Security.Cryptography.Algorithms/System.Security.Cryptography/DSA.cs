using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using Internal.Cryptography;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public abstract class DSA : AsymmetricAlgorithm
{
	public abstract DSAParameters ExportParameters(bool includePrivateParameters);

	public abstract void ImportParameters(DSAParameters parameters);

	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public new static DSA? Create(string algName)
	{
		return (DSA)CryptoConfig.CreateFromName(algName);
	}

	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public new static DSA Create()
	{
		return CreateCore();
	}

	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static DSA Create(int keySizeInBits)
	{
		DSA dSA = CreateCore();
		try
		{
			dSA.KeySize = keySizeInBits;
			return dSA;
		}
		catch
		{
			dSA.Dispose();
			throw;
		}
	}

	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static DSA Create(DSAParameters parameters)
	{
		DSA dSA = CreateCore();
		try
		{
			dSA.ImportParameters(parameters);
			return dSA;
		}
		catch
		{
			dSA.Dispose();
			throw;
		}
	}

	public abstract byte[] CreateSignature(byte[] rgbHash);

	public abstract bool VerifySignature(byte[] rgbHash, byte[] rgbSignature);

	protected virtual byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
	{
		throw DerivedClassMustOverride();
	}

	protected virtual byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
	{
		throw DerivedClassMustOverride();
	}

	public byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		return SignData(data, 0, data.Length, hashAlgorithm);
	}

	public byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw HashAlgorithmNameNullOrEmpty();
		}
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return SignDataCore(data, hashAlgorithm, signatureFormat);
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
			throw HashAlgorithmNameNullOrEmpty();
		}
		byte[] rgbHash = HashData(data, offset, count, hashAlgorithm);
		return CreateSignature(rgbHash);
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
			throw HashAlgorithmNameNullOrEmpty();
		}
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return SignDataCore(new ReadOnlySpan<byte>(data, offset, count), hashAlgorithm, signatureFormat);
	}

	protected virtual byte[] SignDataCore(ReadOnlySpan<byte> data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		Span<byte> destination = stackalloc byte[128];
		if (TrySignDataCore(data, destination, hashAlgorithm, signatureFormat, out var bytesWritten))
		{
			return destination.Slice(0, bytesWritten).ToArray();
		}
		byte[] rgbHash = HashSpanToArray(data, hashAlgorithm);
		byte[] signature = CreateSignature(rgbHash);
		return AsymmetricAlgorithmHelpers.ConvertFromIeeeP1363Signature(signature, signatureFormat);
	}

	public virtual byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw HashAlgorithmNameNullOrEmpty();
		}
		byte[] rgbHash = HashData(data, hashAlgorithm);
		return CreateSignature(rgbHash);
	}

	public byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw HashAlgorithmNameNullOrEmpty();
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
		return CreateSignatureCore(array, signatureFormat);
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
			throw HashAlgorithmNameNullOrEmpty();
		}
		byte[] rgbHash = HashData(data, offset, count, hashAlgorithm);
		return VerifySignature(rgbHash, signature);
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
			throw HashAlgorithmNameNullOrEmpty();
		}
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return VerifyDataCore(new ReadOnlySpan<byte>(data, offset, count), signature, hashAlgorithm, signatureFormat);
	}

	public virtual bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm)
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
			throw HashAlgorithmNameNullOrEmpty();
		}
		byte[] rgbHash = HashData(data, hashAlgorithm);
		return VerifySignature(rgbHash, signature);
	}

	public byte[] CreateSignature(byte[] rgbHash, DSASignatureFormat signatureFormat)
	{
		if (rgbHash == null)
		{
			throw new ArgumentNullException("rgbHash");
		}
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return CreateSignatureCore(rgbHash, signatureFormat);
	}

	protected virtual byte[] CreateSignatureCore(ReadOnlySpan<byte> hash, DSASignatureFormat signatureFormat)
	{
		Span<byte> destination = stackalloc byte[128];
		if (TryCreateSignatureCore(hash, destination, signatureFormat, out var bytesWritten))
		{
			return destination.Slice(0, bytesWritten).ToArray();
		}
		byte[] signature = CreateSignature(hash.ToArray());
		return AsymmetricAlgorithmHelpers.ConvertFromIeeeP1363Signature(signature, signatureFormat);
	}

	public virtual bool TryCreateSignature(ReadOnlySpan<byte> hash, Span<byte> destination, out int bytesWritten)
	{
		return TryCreateSignatureCore(hash, destination, DSASignatureFormat.IeeeP1363FixedFieldConcatenation, out bytesWritten);
	}

	public bool TryCreateSignature(ReadOnlySpan<byte> hash, Span<byte> destination, DSASignatureFormat signatureFormat, out int bytesWritten)
	{
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return TryCreateSignatureCore(hash, destination, signatureFormat, out bytesWritten);
	}

	protected virtual bool TryCreateSignatureCore(ReadOnlySpan<byte> hash, Span<byte> destination, DSASignatureFormat signatureFormat, out int bytesWritten)
	{
		byte[] array = CreateSignature(hash.ToArray());
		if (signatureFormat != 0)
		{
			array = AsymmetricAlgorithmHelpers.ConvertFromIeeeP1363Signature(array, signatureFormat);
		}
		return Internal.Cryptography.Helpers.TryCopyToDestination(array, destination, out bytesWritten);
	}

	protected virtual bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
	{
		byte[] array = HashSpanToArray(data, hashAlgorithm);
		return Internal.Cryptography.Helpers.TryCopyToDestination(array, destination, out bytesWritten);
	}

	public virtual bool TrySignData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
	{
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw HashAlgorithmNameNullOrEmpty();
		}
		if (TryHashData(data, destination, hashAlgorithm, out var bytesWritten2) && TryCreateSignature(destination.Slice(0, bytesWritten2), destination, out bytesWritten))
		{
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public bool TrySignData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat, out int bytesWritten)
	{
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw HashAlgorithmNameNullOrEmpty();
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
		return TryCreateSignatureCore(hash, destination, signatureFormat, out bytesWritten);
	}

	public virtual bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm)
	{
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw HashAlgorithmNameNullOrEmpty();
		}
		return VerifyDataCore(data, signature, hashAlgorithm, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
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
			throw HashAlgorithmNameNullOrEmpty();
		}
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return VerifyDataCore(data, signature, hashAlgorithm, signatureFormat);
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
			throw HashAlgorithmNameNullOrEmpty();
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
		return VerifySignatureCore(array, signature, signatureFormat);
	}

	public bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw HashAlgorithmNameNullOrEmpty();
		}
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return VerifyDataCore(data, signature, hashAlgorithm, signatureFormat);
	}

	protected virtual bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
	{
		Span<byte> tmp = stackalloc byte[128];
		ReadOnlySpan<byte> hash = HashSpanToTmp(data, hashAlgorithm, tmp);
		return VerifySignatureCore(hash, signature, signatureFormat);
	}

	public bool VerifySignature(byte[] rgbHash, byte[] rgbSignature, DSASignatureFormat signatureFormat)
	{
		if (rgbHash == null)
		{
			throw new ArgumentNullException("rgbHash");
		}
		if (rgbSignature == null)
		{
			throw new ArgumentNullException("rgbSignature");
		}
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return VerifySignatureCore(rgbHash, rgbSignature, signatureFormat);
	}

	public virtual bool VerifySignature(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
	{
		return VerifySignature(hash.ToArray(), signature.ToArray());
	}

	public bool VerifySignature(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, DSASignatureFormat signatureFormat)
	{
		if (!signatureFormat.IsKnownValue())
		{
			throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);
		}
		return VerifySignatureCore(hash, signature, signatureFormat);
	}

	protected virtual bool VerifySignatureCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, DSASignatureFormat signatureFormat)
	{
		byte[] array = this.ConvertSignatureToIeeeP1363(signatureFormat, signature);
		if (array == null)
		{
			return false;
		}
		return VerifySignature(hash, array);
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

	private static Exception DerivedClassMustOverride()
	{
		return new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	internal static Exception HashAlgorithmNameNullOrEmpty()
	{
		return new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
	}

	public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		if (pbeParameters == null)
		{
			throw new ArgumentNullException("pbeParameters");
		}
		PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
		AsnWriter pkcs8Writer = WritePkcs8();
		AsnWriter asnWriter = KeyFormatHelper.WriteEncryptedPkcs8(passwordBytes, pkcs8Writer, pbeParameters);
		return asnWriter.TryEncode(destination, out bytesWritten);
	}

	public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		if (pbeParameters == null)
		{
			throw new ArgumentNullException("pbeParameters");
		}
		PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
		AsnWriter pkcs8Writer = WritePkcs8();
		AsnWriter asnWriter = KeyFormatHelper.WriteEncryptedPkcs8(password, pkcs8Writer, pbeParameters);
		return asnWriter.TryEncode(destination, out bytesWritten);
	}

	public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
	{
		AsnWriter asnWriter = WritePkcs8();
		return asnWriter.TryEncode(destination, out bytesWritten);
	}

	public override bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
	{
		AsnWriter asnWriter = WriteSubjectPublicKeyInfo();
		return asnWriter.TryEncode(destination, out bytesWritten);
	}

	private unsafe AsnWriter WritePkcs8()
	{
		DSAParameters dsaParameters = ExportParameters(includePrivateParameters: true);
		fixed (byte* ptr = dsaParameters.X)
		{
			try
			{
				return DSAKeyFormatHelper.WritePkcs8(in dsaParameters);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(dsaParameters.X);
			}
		}
	}

	private AsnWriter WriteSubjectPublicKeyInfo()
	{
		DSAParameters dsaParameters = ExportParameters(includePrivateParameters: false);
		return DSAKeyFormatHelper.WriteSubjectPublicKeyInfo(in dsaParameters);
	}

	public unsafe override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source, out int bytesRead)
	{
		DSAKeyFormatHelper.ReadEncryptedPkcs8(source, passwordBytes, out var bytesRead2, out var key);
		fixed (byte* ptr = key.X)
		{
			try
			{
				ImportParameters(key);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(key.X);
			}
		}
		bytesRead = bytesRead2;
	}

	public unsafe override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source, out int bytesRead)
	{
		DSAKeyFormatHelper.ReadEncryptedPkcs8(source, password, out var bytesRead2, out var key);
		fixed (byte* ptr = key.X)
		{
			try
			{
				ImportParameters(key);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(key.X);
			}
		}
		bytesRead = bytesRead2;
	}

	public unsafe override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
	{
		DSAKeyFormatHelper.ReadPkcs8(source, out var bytesRead2, out var key);
		fixed (byte* ptr = key.X)
		{
			try
			{
				ImportParameters(key);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(key.X);
			}
		}
		bytesRead = bytesRead2;
	}

	public override void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead)
	{
		DSAKeyFormatHelper.ReadSubjectPublicKeyInfo(source, out var bytesRead2, out var key);
		ImportParameters(key);
		bytesRead = bytesRead2;
	}

	public int GetMaxSignatureSize(DSASignatureFormat signatureFormat)
	{
		int num = ExportParameters(includePrivateParameters: false).Q.Length;
		return signatureFormat switch
		{
			DSASignatureFormat.IeeeP1363FixedFieldConcatenation => num * 2, 
			DSASignatureFormat.Rfc3279DerSequence => AsymmetricAlgorithmHelpers.GetMaxDerSignatureSize(num * 8), 
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
			return label.SequenceEqual("PUBLIC KEY") ? new PemKeyImportHelpers.ImportKeyAction(ImportSubjectPublicKeyInfo) : null;
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

	private static byte[] ReadRequiredElement(ref XmlKeyHelper.ParseState state, string name, int sizeHint = -1)
	{
		byte[] array = XmlKeyHelper.ReadCryptoBinary(ref state, name, sizeHint);
		if (array == null)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_InvalidFromXmlString, "DSA", name));
		}
		return array;
	}

	public override void FromXmlString(string xmlString)
	{
		XmlKeyHelper.ParseState state = XmlKeyHelper.ParseDocument(xmlString);
		byte[] array = ReadRequiredElement(ref state, "P");
		byte[] array2 = ReadRequiredElement(ref state, "Q");
		byte[] g = ReadRequiredElement(ref state, "G", array.Length);
		byte[] y = ReadRequiredElement(ref state, "Y", array.Length);
		byte[] j = XmlKeyHelper.ReadCryptoBinary(ref state, "J");
		byte[] array3 = XmlKeyHelper.ReadCryptoBinary(ref state, "Seed");
		int counter = 0;
		byte[] x = XmlKeyHelper.ReadCryptoBinary(ref state, "X", array2.Length);
		if (array3 != null)
		{
			byte[] buf = ReadRequiredElement(ref state, "PgenCounter");
			counter = XmlKeyHelper.ReadCryptoBinaryInt32(buf);
		}
		DSAParameters dSAParameters = default(DSAParameters);
		dSAParameters.P = array;
		dSAParameters.Q = array2;
		dSAParameters.G = g;
		dSAParameters.Y = y;
		dSAParameters.J = j;
		dSAParameters.Seed = array3;
		dSAParameters.Counter = counter;
		dSAParameters.X = x;
		DSAParameters parameters = dSAParameters;
		if (parameters.Seed == null && XmlKeyHelper.HasElement(ref state, "PgenCounter"))
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_InvalidFromXmlString, "DSA", "Seed"));
		}
		ImportParameters(parameters);
	}

	public override string ToXmlString(bool includePrivateParameters)
	{
		DSAParameters dSAParameters = ExportParameters(includePrivateParameters);
		StringBuilder stringBuilder = new StringBuilder((dSAParameters.P.Length << 1) / 3);
		stringBuilder.Append("<DSAKeyValue>");
		XmlKeyHelper.WriteCryptoBinary("P", dSAParameters.P, stringBuilder);
		XmlKeyHelper.WriteCryptoBinary("Q", dSAParameters.Q, stringBuilder);
		XmlKeyHelper.WriteCryptoBinary("G", dSAParameters.G, stringBuilder);
		XmlKeyHelper.WriteCryptoBinary("Y", dSAParameters.Y, stringBuilder);
		if (dSAParameters.J != null)
		{
			XmlKeyHelper.WriteCryptoBinary("J", dSAParameters.J, stringBuilder);
		}
		if (dSAParameters.Seed != null)
		{
			XmlKeyHelper.WriteCryptoBinary("Seed", dSAParameters.Seed, stringBuilder);
			XmlKeyHelper.WriteCryptoBinary("PgenCounter", dSAParameters.Counter, stringBuilder);
		}
		if (includePrivateParameters)
		{
			if (dSAParameters.X == null)
			{
				throw new ArgumentNullException("inArray");
			}
			XmlKeyHelper.WriteCryptoBinary("X", dSAParameters.X, stringBuilder);
		}
		stringBuilder.Append("</DSAKeyValue>");
		return stringBuilder.ToString();
	}

	private static DSA CreateCore()
	{
		return new DSAImplementation.DSACng();
	}
}
