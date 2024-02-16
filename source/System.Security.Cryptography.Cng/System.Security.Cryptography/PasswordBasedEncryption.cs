using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs;
using System.Text;
using Internal.Cryptography;

namespace System.Security.Cryptography;

internal static class PasswordBasedEncryption
{
	private static CryptographicException AlgorithmKdfRequiresChars(string algId)
	{
		return new CryptographicException(System.SR.Cryptography_AlgKdfRequiresChars, algId);
	}

	internal static void ValidatePbeParameters(PbeParameters pbeParameters, ReadOnlySpan<char> password, ReadOnlySpan<byte> passwordBytes)
	{
		PbeEncryptionAlgorithm encryptionAlgorithm = pbeParameters.EncryptionAlgorithm;
		switch (encryptionAlgorithm)
		{
		case PbeEncryptionAlgorithm.Aes128Cbc:
		case PbeEncryptionAlgorithm.Aes192Cbc:
		case PbeEncryptionAlgorithm.Aes256Cbc:
			break;
		case PbeEncryptionAlgorithm.TripleDes3KeyPkcs12:
			if (pbeParameters.HashAlgorithm != HashAlgorithmName.SHA1)
			{
				throw new CryptographicException(System.SR.Cryptography_UnknownHashAlgorithm, pbeParameters.HashAlgorithm.Name);
			}
			if (passwordBytes.Length > 0 && password.Length == 0)
			{
				throw AlgorithmKdfRequiresChars(encryptionAlgorithm.ToString());
			}
			break;
		default:
			throw new CryptographicException(System.SR.Cryptography_UnknownAlgorithmIdentifier, encryptionAlgorithm.ToString());
		}
	}

	internal unsafe static int Decrypt(in System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn algorithmIdentifier, ReadOnlySpan<char> password, ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> encryptedData, Span<byte> destination)
	{
		SymmetricAlgorithm symmetricAlgorithm = null;
		bool flag = false;
		HashAlgorithmName hashAlgorithm;
		switch (algorithmIdentifier.Algorithm)
		{
		case "1.2.840.113549.1.5.3":
			hashAlgorithm = HashAlgorithmName.MD5;
			symmetricAlgorithm = DES.Create();
			break;
		case "1.2.840.113549.1.5.6":
			hashAlgorithm = HashAlgorithmName.MD5;
			symmetricAlgorithm = CreateRC2();
			break;
		case "1.2.840.113549.1.5.10":
			hashAlgorithm = HashAlgorithmName.SHA1;
			symmetricAlgorithm = DES.Create();
			break;
		case "1.2.840.113549.1.5.11":
			hashAlgorithm = HashAlgorithmName.SHA1;
			symmetricAlgorithm = CreateRC2();
			break;
		case "1.2.840.113549.1.12.1.3":
			hashAlgorithm = HashAlgorithmName.SHA1;
			symmetricAlgorithm = TripleDES.Create();
			flag = true;
			break;
		case "1.2.840.113549.1.12.1.4":
			hashAlgorithm = HashAlgorithmName.SHA1;
			symmetricAlgorithm = TripleDES.Create();
			symmetricAlgorithm.KeySize = 128;
			flag = true;
			break;
		case "1.2.840.113549.1.12.1.5":
			hashAlgorithm = HashAlgorithmName.SHA1;
			symmetricAlgorithm = CreateRC2();
			symmetricAlgorithm.KeySize = 128;
			flag = true;
			break;
		case "1.2.840.113549.1.12.1.6":
			hashAlgorithm = HashAlgorithmName.SHA1;
			symmetricAlgorithm = CreateRC2();
			symmetricAlgorithm.KeySize = 40;
			flag = true;
			break;
		case "1.2.840.113549.1.5.13":
			return Pbes2Decrypt(algorithmIdentifier.Parameters, password, passwordBytes, encryptedData, destination);
		default:
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownAlgorithmIdentifier, algorithmIdentifier.Algorithm));
		}
		using (symmetricAlgorithm)
		{
			if (flag)
			{
				if (password.IsEmpty && passwordBytes.Length > 0)
				{
					throw AlgorithmKdfRequiresChars(algorithmIdentifier.Algorithm);
				}
				return Pkcs12PbeDecrypt(algorithmIdentifier, password, hashAlgorithm, symmetricAlgorithm, encryptedData, destination);
			}
			using IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithm);
			Span<byte> span = stackalloc byte[128];
			ReadOnlySpan<byte> password2 = default(Span<byte>);
			byte[] array = null;
			Encoding encoding = null;
			if (passwordBytes.Length > 0 || password.Length == 0)
			{
				password2 = passwordBytes;
			}
			else
			{
				encoding = Encoding.UTF8;
				int byteCount = encoding.GetByteCount(password);
				if (byteCount > span.Length)
				{
					array = System.Security.Cryptography.CryptoPool.Rent(byteCount);
					span = array.AsSpan(0, byteCount);
				}
				else
				{
					span = span.Slice(0, byteCount);
				}
			}
			fixed (byte* ptr = &MemoryMarshal.GetReference(span))
			{
				if (encoding != null)
				{
					span = span[..encoding.GetBytes(password, span)];
					password2 = span;
				}
				try
				{
					return Pbes1Decrypt(algorithmIdentifier.Parameters, password2, hasher, symmetricAlgorithm, encryptedData, destination);
				}
				finally
				{
					CryptographicOperations.ZeroMemory(span);
					if (array != null)
					{
						System.Security.Cryptography.CryptoPool.Return(array, 0);
					}
				}
			}
		}
	}

	internal static void InitiateEncryption(PbeParameters pbeParameters, out SymmetricAlgorithm cipher, out string hmacOid, out string encryptionAlgorithmOid, out bool isPkcs12)
	{
		isPkcs12 = false;
		switch (pbeParameters.EncryptionAlgorithm)
		{
		case PbeEncryptionAlgorithm.Aes128Cbc:
			cipher = Aes.Create();
			cipher.KeySize = 128;
			encryptionAlgorithmOid = "2.16.840.1.101.3.4.1.2";
			break;
		case PbeEncryptionAlgorithm.Aes192Cbc:
			cipher = Aes.Create();
			cipher.KeySize = 192;
			encryptionAlgorithmOid = "2.16.840.1.101.3.4.1.22";
			break;
		case PbeEncryptionAlgorithm.Aes256Cbc:
			cipher = Aes.Create();
			cipher.KeySize = 256;
			encryptionAlgorithmOid = "2.16.840.1.101.3.4.1.42";
			break;
		case PbeEncryptionAlgorithm.TripleDes3KeyPkcs12:
			cipher = TripleDES.Create();
			cipher.KeySize = 192;
			encryptionAlgorithmOid = "1.2.840.113549.1.12.1.3";
			isPkcs12 = true;
			break;
		default:
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownAlgorithmIdentifier, pbeParameters.HashAlgorithm.Name));
		}
		HashAlgorithmName hashAlgorithm = pbeParameters.HashAlgorithm;
		if (hashAlgorithm == HashAlgorithmName.SHA256)
		{
			hmacOid = "1.2.840.113549.2.9";
			return;
		}
		if (hashAlgorithm == HashAlgorithmName.SHA384)
		{
			hmacOid = "1.2.840.113549.2.10";
			return;
		}
		if (hashAlgorithm == HashAlgorithmName.SHA512)
		{
			hmacOid = "1.2.840.113549.2.11";
			return;
		}
		if (hashAlgorithm == HashAlgorithmName.SHA1)
		{
			hmacOid = "1.2.840.113549.2.7";
			return;
		}
		cipher.Dispose();
		throw new CryptographicException(System.SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name);
	}

	internal unsafe static int Encrypt(ReadOnlySpan<char> password, ReadOnlySpan<byte> passwordBytes, SymmetricAlgorithm cipher, bool isPkcs12, AsnWriter source, PbeParameters pbeParameters, ReadOnlySpan<byte> salt, byte[] destination, Span<byte> ivDest)
	{
		byte[] array = null;
		byte[] iV = cipher.IV;
		int encodedLength = source.GetEncodedLength();
		byte[] array2 = System.Security.Cryptography.CryptoPool.Rent(encodedLength);
		int num = cipher.KeySize / 8;
		int iterationCount = pbeParameters.IterationCount;
		HashAlgorithmName hashAlgorithm = pbeParameters.HashAlgorithm;
		Encoding uTF = Encoding.UTF8;
		if (!isPkcs12)
		{
			array = ((passwordBytes.Length == 0 && password.Length > 0) ? new byte[uTF.GetByteCount(password)] : ((passwordBytes.Length != 0) ? new byte[passwordBytes.Length] : Array.Empty<byte>()));
		}
		fixed (byte* ptr3 = array2)
		{
			fixed (byte* ptr2 = array)
			{
				byte[] array3;
				if (isPkcs12)
				{
					array3 = new byte[num];
					System.Security.Cryptography.Pkcs.Pkcs12Kdf.DeriveCipherKey(password, hashAlgorithm, iterationCount, salt, array3);
					System.Security.Cryptography.Pkcs.Pkcs12Kdf.DeriveIV(password, hashAlgorithm, iterationCount, salt, iV);
					ivDest.Clear();
				}
				else
				{
					if (passwordBytes.Length > 0)
					{
						passwordBytes.CopyTo(array);
					}
					else if (password.Length > 0)
					{
						int bytes = uTF.GetBytes(password, array);
						if (bytes != array.Length)
						{
							throw new CryptographicException();
						}
					}
					using (Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(array, salt.ToArray(), iterationCount, hashAlgorithm))
					{
						array3 = rfc2898DeriveBytes.GetBytes(num);
					}
					iV.CopyTo(ivDest);
				}
				fixed (byte* ptr = array3)
				{
					CryptographicOperations.ZeroMemory(array);
					using ICryptoTransform cryptoTransform = cipher.CreateEncryptor(array3, iV);
					int num2 = cipher.BlockSize / 8;
					int num3 = encodedLength % num2;
					int num4 = encodedLength - num3;
					try
					{
						if (!source.TryEncode(array2, out var _))
						{
							throw new CryptographicException();
						}
						int num5 = 0;
						if (num4 != 0)
						{
							num5 = cryptoTransform.TransformBlock(array2, 0, num4, destination, 0);
						}
						byte[] array4 = cryptoTransform.TransformFinalBlock(array2, num5, num3);
						array4.AsSpan().CopyTo(destination.AsSpan(num5));
						return num5 + array4.Length;
					}
					finally
					{
						System.Security.Cryptography.CryptoPool.Return(array2, encodedLength);
					}
				}
			}
		}
	}

	private unsafe static int Pbes2Decrypt(ReadOnlyMemory<byte>? algorithmParameters, ReadOnlySpan<char> password, ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> encryptedData, Span<byte> destination)
	{
		Span<byte> span = stackalloc byte[128];
		ReadOnlySpan<byte> password2 = default(Span<byte>);
		byte[] array = null;
		Encoding encoding = null;
		if (passwordBytes.Length > 0 || password.Length == 0)
		{
			password2 = passwordBytes;
		}
		else
		{
			encoding = Encoding.UTF8;
			int byteCount = encoding.GetByteCount(password);
			if (byteCount > span.Length)
			{
				array = System.Security.Cryptography.CryptoPool.Rent(byteCount);
				span = array.AsSpan(0, byteCount);
			}
			else
			{
				span = span.Slice(0, byteCount);
			}
		}
		fixed (byte* ptr = &MemoryMarshal.GetReference(span))
		{
			if (encoding != null)
			{
				span = span[..encoding.GetBytes(password, span)];
				password2 = span;
			}
			try
			{
				return Pbes2Decrypt(algorithmParameters, password2, encryptedData, destination);
			}
			finally
			{
				if (array != null)
				{
					System.Security.Cryptography.CryptoPool.Return(array, span.Length);
				}
			}
		}
	}

	private unsafe static int Pbes2Decrypt(ReadOnlyMemory<byte>? algorithmParameters, ReadOnlySpan<byte> password, ReadOnlySpan<byte> encryptedData, Span<byte> destination)
	{
		if (!algorithmParameters.HasValue)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		System.Security.Cryptography.Asn1.PBES2Params pBES2Params = System.Security.Cryptography.Asn1.PBES2Params.Decode(algorithmParameters.Value, AsnEncodingRules.BER);
		if (pBES2Params.KeyDerivationFunc.Algorithm != "1.2.840.113549.1.5.12")
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownAlgorithmIdentifier, pBES2Params.EncryptionScheme.Algorithm));
		}
		int? requestedKeyLength;
		Rfc2898DeriveBytes rfc2898DeriveBytes = OpenPbkdf2(password, pBES2Params.KeyDerivationFunc.Parameters, out requestedKeyLength);
		using (rfc2898DeriveBytes)
		{
			Span<byte> iv = stackalloc byte[16];
			SymmetricAlgorithm symmetricAlgorithm = OpenCipher(pBES2Params.EncryptionScheme, requestedKeyLength, ref iv);
			using (symmetricAlgorithm)
			{
				byte[] bytes = rfc2898DeriveBytes.GetBytes(symmetricAlgorithm.KeySize / 8);
				fixed (byte* ptr = bytes)
				{
					try
					{
						return Decrypt(symmetricAlgorithm, bytes, iv, encryptedData, destination);
					}
					finally
					{
						CryptographicOperations.ZeroMemory(bytes);
					}
				}
			}
		}
	}

	private static SymmetricAlgorithm OpenCipher(System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn encryptionScheme, int? requestedKeyLength, ref Span<byte> iv)
	{
		string algorithm = encryptionScheme.Algorithm;
		switch (algorithm)
		{
		case "2.16.840.1.101.3.4.1.2":
		case "2.16.840.1.101.3.4.1.22":
		case "2.16.840.1.101.3.4.1.42":
		{
			int num = algorithm switch
			{
				"2.16.840.1.101.3.4.1.2" => 16, 
				"2.16.840.1.101.3.4.1.22" => 24, 
				"2.16.840.1.101.3.4.1.42" => 32, 
				_ => throw new CryptographicException(), 
			};
			if (requestedKeyLength.HasValue && requestedKeyLength != num)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			ReadIvParameter(encryptionScheme.Parameters, 16, ref iv);
			Aes aes = Aes.Create();
			aes.KeySize = num * 8;
			return aes;
		}
		case "1.2.840.113549.3.7":
			if (requestedKeyLength.HasValue && requestedKeyLength != 24)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			ReadIvParameter(encryptionScheme.Parameters, 8, ref iv);
			return TripleDES.Create();
		case "1.2.840.113549.3.2":
		{
			if (!encryptionScheme.Parameters.HasValue)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			if (!requestedKeyLength.HasValue)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			System.Security.Cryptography.Asn1.Rc2CbcParameters rc2CbcParameters = System.Security.Cryptography.Asn1.Rc2CbcParameters.Decode(encryptionScheme.Parameters.Value, AsnEncodingRules.BER);
			if (rc2CbcParameters.Iv.Length != 8)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			RC2 rC = CreateRC2();
			rC.KeySize = requestedKeyLength.Value * 8;
			rC.EffectiveKeySize = rc2CbcParameters.GetEffectiveKeyBits();
			rc2CbcParameters.Iv.Span.CopyTo(iv);
			iv = iv.Slice(0, rc2CbcParameters.Iv.Length);
			return rC;
		}
		case "1.3.14.3.2.7":
			if (requestedKeyLength.HasValue && requestedKeyLength != 8)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			ReadIvParameter(encryptionScheme.Parameters, 8, ref iv);
			return DES.Create();
		default:
			throw new CryptographicException(System.SR.Cryptography_UnknownAlgorithmIdentifier, algorithm);
		}
	}

	private static void ReadIvParameter(ReadOnlyMemory<byte>? encryptionSchemeParameters, int length, ref Span<byte> iv)
	{
		if (!encryptionSchemeParameters.HasValue)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		try
		{
			ReadOnlySpan<byte> span = encryptionSchemeParameters.Value.Span;
			if (!AsnDecoder.TryReadOctetString(span, iv, AsnEncodingRules.BER, out var bytesConsumed, out var bytesWritten) || bytesWritten != length || bytesConsumed != span.Length)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			iv = iv.Slice(0, bytesWritten);
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	private unsafe static Rfc2898DeriveBytes OpenPbkdf2(ReadOnlySpan<byte> password, ReadOnlyMemory<byte>? parameters, out int? requestedKeyLength)
	{
		if (!parameters.HasValue)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		System.Security.Cryptography.Asn1.Pbkdf2Params pbkdf2Params = System.Security.Cryptography.Asn1.Pbkdf2Params.Decode(parameters.Value, AsnEncodingRules.BER);
		if (pbkdf2Params.Salt.OtherSource.HasValue)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownAlgorithmIdentifier, pbkdf2Params.Salt.OtherSource.Value.Algorithm));
		}
		if (!pbkdf2Params.Salt.Specified.HasValue)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		HashAlgorithmName hashAlgorithm = pbkdf2Params.Prf.Algorithm switch
		{
			"1.2.840.113549.2.7" => HashAlgorithmName.SHA1, 
			"1.2.840.113549.2.9" => HashAlgorithmName.SHA256, 
			"1.2.840.113549.2.10" => HashAlgorithmName.SHA384, 
			"1.2.840.113549.2.11" => HashAlgorithmName.SHA512, 
			_ => throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownAlgorithmIdentifier, pbkdf2Params.Prf.Algorithm)), 
		};
		if (!pbkdf2Params.Prf.HasNullEquivalentParameters())
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		int iterations = NormalizeIterationCount(pbkdf2Params.IterationCount);
		ReadOnlyMemory<byte> value = pbkdf2Params.Salt.Specified.Value;
		byte[] array = new byte[password.Length];
		byte[] array2 = new byte[value.Length];
		fixed (byte* ptr2 = array)
		{
			fixed (byte* ptr = array2)
			{
				password.CopyTo(array);
				value.CopyTo(array2);
				try
				{
					requestedKeyLength = pbkdf2Params.KeyLength;
					return new Rfc2898DeriveBytes(array, array2, iterations, hashAlgorithm);
				}
				catch (ArgumentException inner)
				{
					throw new CryptographicException(System.SR.Argument_InvalidValue, inner);
				}
				finally
				{
					CryptographicOperations.ZeroMemory(array);
					CryptographicOperations.ZeroMemory(array2);
				}
			}
		}
	}

	private static int Pbes1Decrypt(ReadOnlyMemory<byte>? algorithmParameters, ReadOnlySpan<byte> password, IncrementalHash hasher, SymmetricAlgorithm cipher, ReadOnlySpan<byte> encryptedData, Span<byte> destination)
	{
		if (!algorithmParameters.HasValue)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		System.Security.Cryptography.Asn1.PBEParameter pBEParameter = System.Security.Cryptography.Asn1.PBEParameter.Decode(algorithmParameters.Value, AsnEncodingRules.BER);
		if (pBEParameter.Salt.Length != 8)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		if (pBEParameter.IterationCount < 1)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		int iterationCount = NormalizeIterationCount(pBEParameter.IterationCount);
		Span<byte> span = stackalloc byte[16];
		try
		{
			Pbkdf1(hasher, password, pBEParameter.Salt.Span, iterationCount, span);
			Span<byte> span2 = span.Slice(0, 8);
			Span<byte> span3 = span.Slice(8, 8);
			return Decrypt(cipher, span2, span3, encryptedData, destination);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(span);
		}
	}

	private static int Pkcs12PbeDecrypt(System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn algorithmIdentifier, ReadOnlySpan<char> password, HashAlgorithmName hashAlgorithm, SymmetricAlgorithm cipher, ReadOnlySpan<byte> encryptedData, Span<byte> destination)
	{
		if (!algorithmIdentifier.Parameters.HasValue)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		if (cipher.KeySize > 256 || cipher.BlockSize > 256)
		{
			throw new CryptographicException();
		}
		System.Security.Cryptography.Asn1.PBEParameter pBEParameter = System.Security.Cryptography.Asn1.PBEParameter.Decode(algorithmIdentifier.Parameters.Value, AsnEncodingRules.BER);
		int iterationCount = NormalizeIterationCount(pBEParameter.IterationCount, 600000);
		Span<byte> span = stackalloc byte[cipher.BlockSize / 8];
		Span<byte> span2 = stackalloc byte[cipher.KeySize / 8];
		ReadOnlySpan<byte> span3 = pBEParameter.Salt.Span;
		try
		{
			System.Security.Cryptography.Pkcs.Pkcs12Kdf.DeriveIV(password, hashAlgorithm, iterationCount, span3, span);
			System.Security.Cryptography.Pkcs.Pkcs12Kdf.DeriveCipherKey(password, hashAlgorithm, iterationCount, span3, span2);
			return Decrypt(cipher, span2, span, encryptedData, destination);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(span2);
			CryptographicOperations.ZeroMemory(span);
		}
	}

	private unsafe static int Decrypt(SymmetricAlgorithm cipher, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> encryptedData, Span<byte> destination)
	{
		byte[] array = new byte[key.Length];
		byte[] array2 = new byte[iv.Length];
		byte[] array3 = System.Security.Cryptography.CryptoPool.Rent(encryptedData.Length);
		byte[] array4 = System.Security.Cryptography.CryptoPool.Rent(destination.Length);
		fixed (byte* ptr5 = array)
		{
			fixed (byte* ptr4 = array2)
			{
				fixed (byte* ptr3 = array3)
				{
					fixed (byte* ptr2 = array4)
					{
						try
						{
							key.CopyTo(array);
							iv.CopyTo(array2);
							using ICryptoTransform cryptoTransform = cipher.CreateDecryptor(array, array2);
							encryptedData.CopyTo(array3);
							int num = cryptoTransform.TransformBlock(array3, 0, encryptedData.Length, array4, 0);
							array4.AsSpan(0, num).CopyTo(destination);
							byte[] array5 = cryptoTransform.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
							fixed (byte* ptr = array5)
							{
								Span<byte> buffer = array5.AsSpan();
								buffer.CopyTo(destination.Slice(num));
								CryptographicOperations.ZeroMemory(buffer);
							}
							return num + array5.Length;
						}
						finally
						{
							CryptographicOperations.ZeroMemory(array);
							CryptographicOperations.ZeroMemory(array2);
							System.Security.Cryptography.CryptoPool.Return(array3, encryptedData.Length);
							System.Security.Cryptography.CryptoPool.Return(array4, destination.Length);
						}
					}
				}
			}
		}
	}

	private static void Pbkdf1(IncrementalHash hasher, ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterationCount, Span<byte> dk)
	{
		Span<byte> span = stackalloc byte[20];
		hasher.AppendData(password);
		hasher.AppendData(salt);
		if (!hasher.TryGetHashAndReset(span, out var bytesWritten))
		{
			throw new CryptographicException();
		}
		span = span.Slice(0, bytesWritten);
		for (int i = 1; i < iterationCount; i++)
		{
			hasher.AppendData(span);
			if (!hasher.TryGetHashAndReset(span, out bytesWritten) || bytesWritten != span.Length)
			{
				throw new CryptographicException();
			}
		}
		span.Slice(0, dk.Length).CopyTo(dk);
		CryptographicOperations.ZeroMemory(span);
	}

	internal static void WritePbeAlgorithmIdentifier(AsnWriter writer, bool isPkcs12, string encryptionAlgorithmOid, Span<byte> salt, int iterationCount, string hmacOid, Span<byte> iv)
	{
		writer.PushSequence();
		if (isPkcs12)
		{
			writer.WriteObjectIdentifierForCrypto(encryptionAlgorithmOid);
			writer.PushSequence();
			writer.WriteOctetString(salt);
			writer.WriteInteger((long)iterationCount, (Asn1Tag?)null);
			writer.PopSequence();
		}
		else
		{
			writer.WriteObjectIdentifierForCrypto("1.2.840.113549.1.5.13");
			writer.PushSequence();
			writer.PushSequence();
			writer.WriteObjectIdentifierForCrypto("1.2.840.113549.1.5.12");
			writer.PushSequence();
			writer.WriteOctetString(salt);
			writer.WriteInteger((long)iterationCount, (Asn1Tag?)null);
			if (hmacOid != "1.2.840.113549.2.7")
			{
				writer.PushSequence();
				writer.WriteObjectIdentifierForCrypto(hmacOid);
				writer.WriteNull();
				writer.PopSequence();
			}
			writer.PopSequence();
			writer.PopSequence();
			writer.PushSequence();
			writer.WriteObjectIdentifierForCrypto(encryptionAlgorithmOid);
			writer.WriteOctetString(iv);
			writer.PopSequence();
			writer.PopSequence();
		}
		writer.PopSequence();
	}

	internal static int NormalizeIterationCount(int iterationCount, int? iterationLimit = null)
	{
		if (iterationCount <= 0 || (iterationLimit.HasValue && iterationCount > iterationLimit.Value))
		{
			throw new CryptographicException(System.SR.Argument_InvalidValue);
		}
		return iterationCount;
	}

	private static RC2 CreateRC2()
	{
		if (!Internal.Cryptography.Helpers.IsRC2Supported)
		{
			throw new PlatformNotSupportedException(System.SR.Format(System.SR.Cryptography_AlgorithmNotSupported, "RC2"));
		}
		return RC2.Create();
	}
}
