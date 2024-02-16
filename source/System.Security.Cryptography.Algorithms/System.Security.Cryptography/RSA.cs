using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography.Asn1;
using System.Text;
using Internal.Cryptography;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public abstract class RSA : AsymmetricAlgorithm
{
	public override string? KeyExchangeAlgorithm => "RSA";

	public override string SignatureAlgorithm => "RSA";

	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public new static RSA? Create(string algName)
	{
		return (RSA)CryptoConfig.CreateFromName(algName);
	}

	public static RSA Create(int keySizeInBits)
	{
		RSA rSA = Create();
		try
		{
			rSA.KeySize = keySizeInBits;
			return rSA;
		}
		catch
		{
			rSA.Dispose();
			throw;
		}
	}

	public static RSA Create(RSAParameters parameters)
	{
		RSA rSA = Create();
		try
		{
			rSA.ImportParameters(parameters);
			return rSA;
		}
		catch
		{
			rSA.Dispose();
			throw;
		}
	}

	public abstract RSAParameters ExportParameters(bool includePrivateParameters);

	public abstract void ImportParameters(RSAParameters parameters);

	public virtual byte[] Encrypt(byte[] data, RSAEncryptionPadding padding)
	{
		throw DerivedClassMustOverride();
	}

	public virtual byte[] Decrypt(byte[] data, RSAEncryptionPadding padding)
	{
		throw DerivedClassMustOverride();
	}

	public virtual byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		throw DerivedClassMustOverride();
	}

	public virtual bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		throw DerivedClassMustOverride();
	}

	protected virtual byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
	{
		throw DerivedClassMustOverride();
	}

	protected virtual byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
	{
		throw DerivedClassMustOverride();
	}

	public virtual bool TryDecrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
	{
		byte[] array = Decrypt(data.ToArray(), padding);
		if (destination.Length >= array.Length)
		{
			new ReadOnlySpan<byte>(array).CopyTo(destination);
			bytesWritten = array.Length;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public virtual bool TryEncrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
	{
		byte[] array = Encrypt(data.ToArray(), padding);
		if (destination.Length >= array.Length)
		{
			new ReadOnlySpan<byte>(array).CopyTo(destination);
			bytesWritten = array.Length;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	protected virtual bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
	{
		byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
		byte[] array2;
		try
		{
			data.CopyTo(array);
			array2 = HashData(array, 0, data.Length, hashAlgorithm);
		}
		finally
		{
			Array.Clear(array, 0, data.Length);
			ArrayPool<byte>.Shared.Return(array);
		}
		if (destination.Length >= array2.Length)
		{
			new ReadOnlySpan<byte>(array2).CopyTo(destination);
			bytesWritten = array2.Length;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public virtual bool TrySignHash(ReadOnlySpan<byte> hash, Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, out int bytesWritten)
	{
		byte[] array = SignHash(hash.ToArray(), hashAlgorithm, padding);
		if (destination.Length >= array.Length)
		{
			new ReadOnlySpan<byte>(array).CopyTo(destination);
			bytesWritten = array.Length;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public virtual bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		return VerifyHash(hash.ToArray(), signature.ToArray(), hashAlgorithm, padding);
	}

	private static Exception DerivedClassMustOverride()
	{
		return new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual byte[] DecryptValue(byte[] rgb)
	{
		throw new NotSupportedException(System.SR.NotSupported_Method);
	}

	public virtual byte[] EncryptValue(byte[] rgb)
	{
		throw new NotSupportedException(System.SR.NotSupported_Method);
	}

	public byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		return SignData(data, 0, data.Length, hashAlgorithm, padding);
	}

	public virtual byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
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
		if (padding == null)
		{
			throw new ArgumentNullException("padding");
		}
		byte[] hash = HashData(data, offset, count, hashAlgorithm);
		return SignHash(hash, hashAlgorithm, padding);
	}

	public virtual byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw HashAlgorithmNameNullOrEmpty();
		}
		if (padding == null)
		{
			throw new ArgumentNullException("padding");
		}
		byte[] hash = HashData(data, hashAlgorithm);
		return SignHash(hash, hashAlgorithm, padding);
	}

	public virtual bool TrySignData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, out int bytesWritten)
	{
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw HashAlgorithmNameNullOrEmpty();
		}
		if (padding == null)
		{
			throw new ArgumentNullException("padding");
		}
		if (TryHashData(data, destination, hashAlgorithm, out var bytesWritten2) && TrySignHash(destination.Slice(0, bytesWritten2), destination, hashAlgorithm, padding, out bytesWritten))
		{
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		return VerifyData(data, 0, data.Length, signature, hashAlgorithm, padding);
	}

	public virtual bool VerifyData(byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
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
		if (padding == null)
		{
			throw new ArgumentNullException("padding");
		}
		byte[] hash = HashData(data, offset, count, hashAlgorithm);
		return VerifyHash(hash, signature, hashAlgorithm, padding);
	}

	public bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
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
		if (padding == null)
		{
			throw new ArgumentNullException("padding");
		}
		byte[] hash = HashData(data, hashAlgorithm);
		return VerifyHash(hash, signature, hashAlgorithm, padding);
	}

	public virtual bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw HashAlgorithmNameNullOrEmpty();
		}
		if (padding == null)
		{
			throw new ArgumentNullException("padding");
		}
		int num = 256;
		while (true)
		{
			int bytesWritten = 0;
			byte[] array = System.Security.Cryptography.CryptoPool.Rent(num);
			try
			{
				if (TryHashData(data, array, hashAlgorithm, out bytesWritten))
				{
					return VerifyHash(new ReadOnlySpan<byte>(array, 0, bytesWritten), signature, hashAlgorithm, padding);
				}
			}
			finally
			{
				System.Security.Cryptography.CryptoPool.Return(array, bytesWritten);
			}
			num = checked(num * 2);
		}
	}

	public virtual byte[] ExportRSAPrivateKey()
	{
		AsnWriter asnWriter = WritePkcs1PrivateKey();
		return asnWriter.Encode();
	}

	public virtual bool TryExportRSAPrivateKey(Span<byte> destination, out int bytesWritten)
	{
		AsnWriter asnWriter = WritePkcs1PrivateKey();
		return asnWriter.TryEncode(destination, out bytesWritten);
	}

	public virtual byte[] ExportRSAPublicKey()
	{
		AsnWriter asnWriter = WritePkcs1PublicKey();
		return asnWriter.Encode();
	}

	public virtual bool TryExportRSAPublicKey(Span<byte> destination, out int bytesWritten)
	{
		AsnWriter asnWriter = WritePkcs1PublicKey();
		return asnWriter.TryEncode(destination, out bytesWritten);
	}

	public unsafe override bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
	{
		int num = KeySize / 4;
		while (true)
		{
			byte[] array = System.Security.Cryptography.CryptoPool.Rent(num);
			num = array.Length;
			int bytesWritten2 = 0;
			fixed (byte* ptr = array)
			{
				try
				{
					if (!TryExportRSAPublicKey(array, out bytesWritten2))
					{
						num = checked(num * 2);
						continue;
					}
					AsnWriter asnWriter = RSAKeyFormatHelper.WriteSubjectPublicKeyInfo(array.AsSpan(0, bytesWritten2));
					return asnWriter.TryEncode(destination, out bytesWritten);
				}
				finally
				{
					System.Security.Cryptography.CryptoPool.Return(array, bytesWritten2);
				}
			}
		}
	}

	public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
	{
		AsnWriter asnWriter = WritePkcs8PrivateKey();
		return asnWriter.TryEncode(destination, out bytesWritten);
	}

	private unsafe AsnWriter WritePkcs8PrivateKey()
	{
		int num = checked(5 * KeySize) / 8;
		while (true)
		{
			byte[] array = System.Security.Cryptography.CryptoPool.Rent(num);
			num = array.Length;
			int bytesWritten = 0;
			fixed (byte* ptr = array)
			{
				try
				{
					if (!TryExportRSAPrivateKey(array, out bytesWritten))
					{
						num = checked(num * 2);
						continue;
					}
					return RSAKeyFormatHelper.WritePkcs8PrivateKey(new ReadOnlySpan<byte>(array, 0, bytesWritten));
				}
				finally
				{
					System.Security.Cryptography.CryptoPool.Return(array, bytesWritten);
				}
			}
		}
	}

	public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		if (pbeParameters == null)
		{
			throw new ArgumentNullException("pbeParameters");
		}
		PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
		AsnWriter pkcs8Writer = WritePkcs8PrivateKey();
		AsnWriter asnWriter = KeyFormatHelper.WriteEncryptedPkcs8(password, pkcs8Writer, pbeParameters);
		return asnWriter.TryEncode(destination, out bytesWritten);
	}

	public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		if (pbeParameters == null)
		{
			throw new ArgumentNullException("pbeParameters");
		}
		PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
		AsnWriter pkcs8Writer = WritePkcs8PrivateKey();
		AsnWriter asnWriter = KeyFormatHelper.WriteEncryptedPkcs8(passwordBytes, pkcs8Writer, pbeParameters);
		return asnWriter.TryEncode(destination, out bytesWritten);
	}

	private AsnWriter WritePkcs1PublicKey()
	{
		RSAParameters rsaParameters = ExportParameters(includePrivateParameters: false);
		return RSAKeyFormatHelper.WritePkcs1PublicKey(in rsaParameters);
	}

	private unsafe AsnWriter WritePkcs1PrivateKey()
	{
		RSAParameters rsaParameters = ExportParameters(includePrivateParameters: true);
		fixed (byte* ptr6 = rsaParameters.D)
		{
			fixed (byte* ptr5 = rsaParameters.P)
			{
				fixed (byte* ptr4 = rsaParameters.Q)
				{
					fixed (byte* ptr3 = rsaParameters.DP)
					{
						fixed (byte* ptr2 = rsaParameters.DQ)
						{
							fixed (byte* ptr = rsaParameters.InverseQ)
							{
								try
								{
									return RSAKeyFormatHelper.WritePkcs1PrivateKey(in rsaParameters);
								}
								finally
								{
									ClearPrivateParameters(in rsaParameters);
								}
							}
						}
					}
				}
			}
		}
	}

	public unsafe override void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead)
	{
		fixed (byte* pointer = &MemoryMarshal.GetReference(source))
		{
			using MemoryManager<byte> memoryManager = new PointerMemoryManager<byte>(pointer, source.Length);
			ImportRSAPublicKey(RSAKeyFormatHelper.ReadSubjectPublicKeyInfo(memoryManager.Memory, out var bytesRead2).Span, out var _);
			bytesRead = bytesRead2;
		}
	}

	public unsafe virtual void ImportRSAPublicKey(ReadOnlySpan<byte> source, out int bytesRead)
	{
		try
		{
			AsnDecoder.ReadEncodedValue(source, AsnEncodingRules.BER, out var _, out var _, out var bytesConsumed);
			fixed (byte* pointer = &MemoryMarshal.GetReference(source))
			{
				using MemoryManager<byte> memoryManager = new PointerMemoryManager<byte>(pointer, bytesConsumed);
				AlgorithmIdentifierAsn algId = default(AlgorithmIdentifierAsn);
				RSAKeyFormatHelper.ReadRsaPublicKey(memoryManager.Memory, in algId, out var ret);
				ImportParameters(ret);
				bytesRead = bytesConsumed;
			}
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	public unsafe virtual void ImportRSAPrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
	{
		try
		{
			AsnDecoder.ReadEncodedValue(source, AsnEncodingRules.BER, out var _, out var _, out var bytesConsumed);
			fixed (byte* pointer = &MemoryMarshal.GetReference(source))
			{
				using MemoryManager<byte> memoryManager = new PointerMemoryManager<byte>(pointer, bytesConsumed);
				ReadOnlyMemory<byte> keyData = memoryManager.Memory;
				int length = keyData.Length;
				AlgorithmIdentifierAsn algId = default(AlgorithmIdentifierAsn);
				RSAKeyFormatHelper.FromPkcs1PrivateKey(keyData, in algId, out var ret);
				fixed (byte* ptr6 = ret.D)
				{
					fixed (byte* ptr5 = ret.P)
					{
						fixed (byte* ptr4 = ret.Q)
						{
							fixed (byte* ptr3 = ret.DP)
							{
								fixed (byte* ptr2 = ret.DQ)
								{
									fixed (byte* ptr = ret.InverseQ)
									{
										try
										{
											ImportParameters(ret);
										}
										finally
										{
											ClearPrivateParameters(in ret);
										}
									}
								}
							}
						}
					}
				}
				bytesRead = length;
			}
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	public unsafe override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
	{
		fixed (byte* pointer = &MemoryMarshal.GetReference(source))
		{
			using MemoryManager<byte> memoryManager = new PointerMemoryManager<byte>(pointer, source.Length);
			ImportRSAPrivateKey(RSAKeyFormatHelper.ReadPkcs8(memoryManager.Memory, out var bytesRead2).Span, out var _);
			bytesRead = bytesRead2;
		}
	}

	public unsafe override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source, out int bytesRead)
	{
		RSAKeyFormatHelper.ReadEncryptedPkcs8(source, passwordBytes, out var bytesRead2, out var key);
		fixed (byte* ptr6 = key.D)
		{
			fixed (byte* ptr5 = key.P)
			{
				fixed (byte* ptr4 = key.Q)
				{
					fixed (byte* ptr3 = key.DP)
					{
						fixed (byte* ptr2 = key.DQ)
						{
							fixed (byte* ptr = key.InverseQ)
							{
								try
								{
									ImportParameters(key);
								}
								finally
								{
									ClearPrivateParameters(in key);
								}
							}
						}
					}
				}
			}
		}
		bytesRead = bytesRead2;
	}

	public unsafe override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source, out int bytesRead)
	{
		RSAKeyFormatHelper.ReadEncryptedPkcs8(source, password, out var bytesRead2, out var key);
		fixed (byte* ptr6 = key.D)
		{
			fixed (byte* ptr5 = key.P)
			{
				fixed (byte* ptr4 = key.Q)
				{
					fixed (byte* ptr3 = key.DP)
					{
						fixed (byte* ptr2 = key.DQ)
						{
							fixed (byte* ptr = key.InverseQ)
							{
								try
								{
									ImportParameters(key);
								}
								finally
								{
									ClearPrivateParameters(in key);
								}
							}
						}
					}
				}
			}
		}
		bytesRead = bytesRead2;
	}

	public override void ImportFromPem(ReadOnlySpan<char> input)
	{
		PemKeyImportHelpers.ImportPem(input, delegate(ReadOnlySpan<char> label)
		{
			if (label.SequenceEqual("RSA PRIVATE KEY"))
			{
				return ImportRSAPrivateKey;
			}
			if (label.SequenceEqual("PRIVATE KEY"))
			{
				return ImportPkcs8PrivateKey;
			}
			if (label.SequenceEqual("RSA PUBLIC KEY"))
			{
				return ImportRSAPublicKey;
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

	private static void ClearPrivateParameters(in RSAParameters rsaParameters)
	{
		CryptographicOperations.ZeroMemory(rsaParameters.D);
		CryptographicOperations.ZeroMemory(rsaParameters.P);
		CryptographicOperations.ZeroMemory(rsaParameters.Q);
		CryptographicOperations.ZeroMemory(rsaParameters.DP);
		CryptographicOperations.ZeroMemory(rsaParameters.DQ);
		CryptographicOperations.ZeroMemory(rsaParameters.InverseQ);
	}

	private static Exception HashAlgorithmNameNullOrEmpty()
	{
		return new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
	}

	private static byte[] ReadRequiredElement(ref XmlKeyHelper.ParseState state, string name, int sizeHint = -1)
	{
		byte[] array = XmlKeyHelper.ReadCryptoBinary(ref state, name, sizeHint);
		if (array == null)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_InvalidFromXmlString, "RSA", name));
		}
		return array;
	}

	public override void FromXmlString(string xmlString)
	{
		XmlKeyHelper.ParseState state = XmlKeyHelper.ParseDocument(xmlString);
		byte[] array = ReadRequiredElement(ref state, "Modulus");
		byte[] exponent = ReadRequiredElement(ref state, "Exponent");
		int sizeHint = (array.Length + 1) / 2;
		byte[] p = XmlKeyHelper.ReadCryptoBinary(ref state, "P", sizeHint);
		byte[] q = XmlKeyHelper.ReadCryptoBinary(ref state, "Q", sizeHint);
		byte[] dP = XmlKeyHelper.ReadCryptoBinary(ref state, "DP", sizeHint);
		byte[] dQ = XmlKeyHelper.ReadCryptoBinary(ref state, "DQ", sizeHint);
		byte[] inverseQ = XmlKeyHelper.ReadCryptoBinary(ref state, "InverseQ", sizeHint);
		byte[] d = XmlKeyHelper.ReadCryptoBinary(ref state, "D", array.Length);
		RSAParameters rSAParameters = default(RSAParameters);
		rSAParameters.Modulus = array;
		rSAParameters.Exponent = exponent;
		rSAParameters.D = d;
		rSAParameters.P = p;
		rSAParameters.Q = q;
		rSAParameters.DP = dP;
		rSAParameters.DQ = dQ;
		rSAParameters.InverseQ = inverseQ;
		RSAParameters parameters = rSAParameters;
		ImportParameters(parameters);
	}

	public override string ToXmlString(bool includePrivateParameters)
	{
		int num = KeySize / 6;
		int num2 = 100 + num;
		if (includePrivateParameters)
		{
			num2 += 76 + 5 * num / 2;
		}
		RSAParameters rSAParameters = ExportParameters(includePrivateParameters);
		StringBuilder stringBuilder = new StringBuilder(num2);
		stringBuilder.Append("<RSAKeyValue>");
		XmlKeyHelper.WriteCryptoBinary("Modulus", rSAParameters.Modulus, stringBuilder);
		XmlKeyHelper.WriteCryptoBinary("Exponent", rSAParameters.Exponent, stringBuilder);
		if (includePrivateParameters)
		{
			XmlKeyHelper.WriteCryptoBinary("P", rSAParameters.P, stringBuilder);
			XmlKeyHelper.WriteCryptoBinary("Q", rSAParameters.Q, stringBuilder);
			XmlKeyHelper.WriteCryptoBinary("DP", rSAParameters.DP, stringBuilder);
			XmlKeyHelper.WriteCryptoBinary("DQ", rSAParameters.DQ, stringBuilder);
			XmlKeyHelper.WriteCryptoBinary("InverseQ", rSAParameters.InverseQ, stringBuilder);
			XmlKeyHelper.WriteCryptoBinary("D", rSAParameters.D, stringBuilder);
		}
		stringBuilder.Append("</RSAKeyValue>");
		return stringBuilder.ToString();
	}

	public new static RSA Create()
	{
		return new RSAImplementation.RSACng();
	}
}
