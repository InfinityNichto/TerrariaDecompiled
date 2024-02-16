using System.Buffers;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

internal static class CngPkcs8
{
	internal struct Pkcs8Response
	{
		internal SafeNCryptKeyHandle KeyHandle;

		internal string GetAlgorithmGroup()
		{
			return CngKeyLite.GetPropertyAsString(KeyHandle, "Algorithm Group", CngPropertyOptions.None);
		}

		internal void FreeKey()
		{
			KeyHandle.Dispose();
		}
	}

	private static readonly PbeParameters s_platformParameters = new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 1);

	private static Pkcs8Response ImportPkcs8(ReadOnlySpan<byte> keyBlob)
	{
		SafeNCryptKeyHandle keyHandle = CngKeyLite.ImportKeyBlob("PKCS8_PRIVATEKEY", keyBlob);
		Pkcs8Response result = default(Pkcs8Response);
		result.KeyHandle = keyHandle;
		return result;
	}

	private static Pkcs8Response ImportPkcs8(ReadOnlySpan<byte> keyBlob, ReadOnlySpan<char> password)
	{
		SafeNCryptKeyHandle keyHandle = CngKeyLite.ImportKeyBlob("PKCS8_PRIVATEKEY", keyBlob, encrypted: true, password);
		Pkcs8Response result = default(Pkcs8Response);
		result.KeyHandle = keyHandle;
		return result;
	}

	internal static bool IsPlatformScheme(PbeParameters pbeParameters)
	{
		if (pbeParameters.EncryptionAlgorithm == s_platformParameters.EncryptionAlgorithm)
		{
			return pbeParameters.HashAlgorithm == s_platformParameters.HashAlgorithm;
		}
		return false;
	}

	internal static byte[] ExportEncryptedPkcs8PrivateKey(AsymmetricAlgorithm key, ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
	{
		if (pbeParameters == null)
		{
			throw new ArgumentNullException("pbeParameters");
		}
		PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
		if (passwordBytes.Length == 0)
		{
			return key.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, pbeParameters);
		}
		AsnWriter asnWriter = RewriteEncryptedPkcs8PrivateKey(key, passwordBytes, pbeParameters);
		return asnWriter.Encode();
	}

	internal static bool TryExportEncryptedPkcs8PrivateKey(AsymmetricAlgorithm key, ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		if (passwordBytes.Length == 0)
		{
			return key.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, pbeParameters, destination, out bytesWritten);
		}
		AsnWriter asnWriter = RewriteEncryptedPkcs8PrivateKey(key, passwordBytes, pbeParameters);
		return asnWriter.TryEncode(destination, out bytesWritten);
	}

	internal static byte[] ExportEncryptedPkcs8PrivateKey(AsymmetricAlgorithm key, ReadOnlySpan<char> password, PbeParameters pbeParameters)
	{
		AsnWriter asnWriter = RewriteEncryptedPkcs8PrivateKey(key, password, pbeParameters);
		return asnWriter.Encode();
	}

	internal static bool TryExportEncryptedPkcs8PrivateKey(AsymmetricAlgorithm key, ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		AsnWriter asnWriter = RewriteEncryptedPkcs8PrivateKey(key, password, pbeParameters);
		return asnWriter.TryEncode(destination, out bytesWritten);
	}

	internal static Pkcs8Response ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
	{
		int bytesConsumed;
		try
		{
			AsnDecoder.ReadEncodedValue(source, AsnEncodingRules.BER, out var _, out var _, out bytesConsumed);
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		bytesRead = bytesConsumed;
		ReadOnlySpan<byte> readOnlySpan = source.Slice(0, bytesConsumed);
		try
		{
			return ImportPkcs8(readOnlySpan);
		}
		catch (CryptographicException)
		{
			AsnWriter asnWriter = RewritePkcs8ECPrivateKeyWithZeroPublicKey(readOnlySpan);
			if (asnWriter == null)
			{
				throw;
			}
			return ImportPkcs8(asnWriter);
		}
		catch (AsnContentException inner2)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner2);
		}
	}

	private static Pkcs8Response ImportPkcs8(AsnWriter pkcs8Writer)
	{
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(pkcs8Writer.GetEncodedLength());
		if (!pkcs8Writer.TryEncode(array, out var bytesWritten))
		{
			throw new CryptographicException();
		}
		Pkcs8Response result = ImportPkcs8(array.AsSpan(0, bytesWritten));
		System.Security.Cryptography.CryptoPool.Return(array, bytesWritten);
		return result;
	}

	internal unsafe static Pkcs8Response ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source, out int bytesRead)
	{
		fixed (byte* pointer = &MemoryMarshal.GetReference(source))
		{
			using MemoryManager<byte> memoryManager = new PointerMemoryManager<byte>(pointer, source.Length);
			try
			{
				ArraySegment<byte> arraySegment = KeyFormatHelper.DecryptPkcs8(passwordBytes, memoryManager.Memory, out bytesRead);
				Span<byte> span = arraySegment;
				try
				{
					return ImportPkcs8(span);
				}
				catch (CryptographicException inner)
				{
					AsnWriter asnWriter = RewritePkcs8ECPrivateKeyWithZeroPublicKey(span);
					if (asnWriter == null)
					{
						throw new CryptographicException(System.SR.Cryptography_Pkcs8_EncryptedReadFailed, inner);
					}
					try
					{
						return ImportPkcs8(asnWriter);
					}
					catch (CryptographicException)
					{
						throw new CryptographicException(System.SR.Cryptography_Pkcs8_EncryptedReadFailed, inner);
					}
				}
				finally
				{
					System.Security.Cryptography.CryptoPool.Return(arraySegment);
				}
			}
			catch (AsnContentException inner2)
			{
				throw new CryptographicException(System.SR.Cryptography_Pkcs8_EncryptedReadFailed, inner2);
			}
		}
	}

	internal unsafe static Pkcs8Response ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source, out int bytesRead)
	{
		try
		{
			AsnDecoder.ReadEncodedValue(source, AsnEncodingRules.BER, out var _, out var _, out var bytesConsumed);
			source = source.Slice(0, bytesConsumed);
			fixed (byte* pointer = &MemoryMarshal.GetReference(source))
			{
				using MemoryManager<byte> memoryManager = new PointerMemoryManager<byte>(pointer, source.Length);
				try
				{
					bytesRead = bytesConsumed;
					return ImportPkcs8(source, password);
				}
				catch (CryptographicException)
				{
				}
				int bytesRead2;
				ArraySegment<byte> arraySegment = KeyFormatHelper.DecryptPkcs8(password, memoryManager.Memory.Slice(0, bytesConsumed), out bytesRead2);
				Span<byte> span = arraySegment;
				try
				{
					if (bytesRead2 != bytesConsumed)
					{
						throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
					}
					bytesRead = bytesConsumed;
					return ImportPkcs8(span);
				}
				catch (CryptographicException inner)
				{
					AsnWriter asnWriter = RewritePkcs8ECPrivateKeyWithZeroPublicKey(span);
					if (asnWriter == null)
					{
						throw new CryptographicException(System.SR.Cryptography_Pkcs8_EncryptedReadFailed, inner);
					}
					try
					{
						bytesRead = bytesConsumed;
						return ImportPkcs8(asnWriter);
					}
					catch (CryptographicException)
					{
						throw new CryptographicException(System.SR.Cryptography_Pkcs8_EncryptedReadFailed, inner);
					}
				}
				finally
				{
					System.Security.Cryptography.CryptoPool.Return(arraySegment);
				}
			}
		}
		catch (AsnContentException inner2)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner2);
		}
	}

	private static AsnWriter RewriteEncryptedPkcs8PrivateKey(AsymmetricAlgorithm key, ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
	{
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(key.KeySize);
		int bytesWritten = 0;
		Span<char> span = stackalloc char[22];
		try
		{
			FillRandomAsciiString(span);
			while (!key.TryExportEncryptedPkcs8PrivateKey(span, s_platformParameters, array, out bytesWritten))
			{
				int num = array.Length;
				byte[] array2 = array;
				array = System.Security.Cryptography.CryptoPool.Rent(checked(num * 2));
				System.Security.Cryptography.CryptoPool.Return(array2, bytesWritten);
			}
			return KeyFormatHelper.ReencryptPkcs8(span, array.AsMemory(0, bytesWritten), passwordBytes, pbeParameters);
		}
		finally
		{
			span.Clear();
			System.Security.Cryptography.CryptoPool.Return(array, bytesWritten);
		}
	}

	private static AsnWriter RewriteEncryptedPkcs8PrivateKey(AsymmetricAlgorithm key, ReadOnlySpan<char> password, PbeParameters pbeParameters)
	{
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(key.KeySize);
		int bytesWritten = 0;
		try
		{
			while (!key.TryExportEncryptedPkcs8PrivateKey(password, s_platformParameters, array, out bytesWritten))
			{
				int num = array.Length;
				byte[] array2 = array;
				array = System.Security.Cryptography.CryptoPool.Rent(checked(num * 2));
				System.Security.Cryptography.CryptoPool.Return(array2, bytesWritten);
			}
			return KeyFormatHelper.ReencryptPkcs8(password, array.AsMemory(0, bytesWritten), password, pbeParameters);
		}
		finally
		{
			System.Security.Cryptography.CryptoPool.Return(array, bytesWritten);
		}
	}

	private unsafe static AsnWriter RewritePkcs8ECPrivateKeyWithZeroPublicKey(ReadOnlySpan<byte> source)
	{
		fixed (byte* pointer = &MemoryMarshal.GetReference(source))
		{
			using MemoryManager<byte> memoryManager = new PointerMemoryManager<byte>(pointer, source.Length);
			PrivateKeyInfoAsn privateKeyInfoAsn = PrivateKeyInfoAsn.Decode(memoryManager.Memory, AsnEncodingRules.BER);
			AlgorithmIdentifierAsn algId = privateKeyInfoAsn.PrivateKeyAlgorithm;
			if (algId.Algorithm != "1.2.840.10045.2.1")
			{
				return null;
			}
			ECPrivateKey key = ECPrivateKey.Decode(privateKeyInfoAsn.PrivateKey, AsnEncodingRules.BER);
			EccKeyFormatHelper.FromECPrivateKey(key, in algId, out var ret);
			fixed (byte* ptr = ret.D)
			{
				try
				{
					if (!ret.Curve.IsExplicit || ret.Q.X != null || ret.Q.Y != null)
					{
						return null;
					}
					byte[] array = new byte[ret.D.Length];
					ret.Q.Y = array;
					ret.Q.X = array;
					return EccKeyFormatHelper.WritePkcs8PrivateKey(ret, privateKeyInfoAsn.Attributes);
				}
				finally
				{
					Array.Clear(ret.D);
				}
			}
		}
	}

	private static void FillRandomAsciiString(Span<char> destination)
	{
		Span<byte> data = stackalloc byte[destination.Length];
		RandomNumberGenerator.Fill(data);
		for (int i = 0; i < data.Length; i++)
		{
			destination[i] = (char)(33 + (data[i] & 0x3F));
		}
	}
}
