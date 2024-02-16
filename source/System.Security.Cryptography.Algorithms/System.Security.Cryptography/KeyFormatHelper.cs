using System.Buffers;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography;

internal static class KeyFormatHelper
{
	internal delegate void KeyReader<TRet>(ReadOnlyMemory<byte> key, in AlgorithmIdentifierAsn algId, out TRet ret);

	internal unsafe static void ReadSubjectPublicKeyInfo<TRet>(string[] validOids, ReadOnlySpan<byte> source, KeyReader<TRet> keyReader, out int bytesRead, out TRet ret)
	{
		fixed (byte* pointer = &MemoryMarshal.GetReference(source))
		{
			using MemoryManager<byte> memoryManager = new PointerMemoryManager<byte>(pointer, source.Length);
			ReadSubjectPublicKeyInfo(validOids, memoryManager.Memory, keyReader, out bytesRead, out ret);
		}
	}

	internal static ReadOnlyMemory<byte> ReadSubjectPublicKeyInfo(string[] validOids, ReadOnlyMemory<byte> source, out int bytesRead)
	{
		int length;
		SubjectPublicKeyInfoAsn decoded;
		try
		{
			AsnValueReader reader = new AsnValueReader(source.Span, AsnEncodingRules.DER);
			length = reader.PeekEncodedValue().Length;
			SubjectPublicKeyInfoAsn.Decode(ref reader, source, out decoded);
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		if (Array.IndexOf(validOids, decoded.Algorithm.Algorithm) < 0)
		{
			throw new CryptographicException(System.SR.Cryptography_NotValidPublicOrPrivateKey);
		}
		bytesRead = length;
		return decoded.SubjectPublicKey;
	}

	private static void ReadSubjectPublicKeyInfo<TRet>(string[] validOids, ReadOnlyMemory<byte> source, KeyReader<TRet> keyReader, out int bytesRead, out TRet ret)
	{
		int length;
		SubjectPublicKeyInfoAsn decoded;
		try
		{
			AsnValueReader reader = new AsnValueReader(source.Span, AsnEncodingRules.DER);
			length = reader.PeekEncodedValue().Length;
			SubjectPublicKeyInfoAsn.Decode(ref reader, source, out decoded);
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		if (Array.IndexOf(validOids, decoded.Algorithm.Algorithm) < 0)
		{
			throw new CryptographicException(System.SR.Cryptography_NotValidPublicOrPrivateKey);
		}
		keyReader(decoded.SubjectPublicKey, in decoded.Algorithm, out ret);
		bytesRead = length;
	}

	internal unsafe static void ReadPkcs8<TRet>(string[] validOids, ReadOnlySpan<byte> source, KeyReader<TRet> keyReader, out int bytesRead, out TRet ret)
	{
		fixed (byte* pointer = &MemoryMarshal.GetReference(source))
		{
			using MemoryManager<byte> memoryManager = new PointerMemoryManager<byte>(pointer, source.Length);
			ReadPkcs8(validOids, memoryManager.Memory, keyReader, out bytesRead, out ret);
		}
	}

	internal static ReadOnlyMemory<byte> ReadPkcs8(string[] validOids, ReadOnlyMemory<byte> source, out int bytesRead)
	{
		try
		{
			AsnValueReader reader = new AsnValueReader(source.Span, AsnEncodingRules.BER);
			int length = reader.PeekEncodedValue().Length;
			PrivateKeyInfoAsn.Decode(ref reader, source, out var decoded);
			if (Array.IndexOf(validOids, decoded.PrivateKeyAlgorithm.Algorithm) < 0)
			{
				throw new CryptographicException(System.SR.Cryptography_NotValidPublicOrPrivateKey);
			}
			bytesRead = length;
			return decoded.PrivateKey;
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	private static void ReadPkcs8<TRet>(string[] validOids, ReadOnlyMemory<byte> source, KeyReader<TRet> keyReader, out int bytesRead, out TRet ret)
	{
		try
		{
			AsnValueReader reader = new AsnValueReader(source.Span, AsnEncodingRules.BER);
			int length = reader.PeekEncodedValue().Length;
			PrivateKeyInfoAsn.Decode(ref reader, source, out var decoded);
			if (Array.IndexOf(validOids, decoded.PrivateKeyAlgorithm.Algorithm) < 0)
			{
				throw new CryptographicException(System.SR.Cryptography_NotValidPublicOrPrivateKey);
			}
			keyReader(decoded.PrivateKey, in decoded.PrivateKeyAlgorithm, out ret);
			bytesRead = length;
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	internal static AsnWriter WritePkcs8(AsnWriter algorithmIdentifierWriter, AsnWriter privateKeyWriter, AsnWriter attributesWriter = null)
	{
		int encodedLength = algorithmIdentifierWriter.GetEncodedLength();
		int encodedLength2 = privateKeyWriter.GetEncodedLength();
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		asnWriter.PushSequence();
		asnWriter.WriteInteger(0L);
		algorithmIdentifierWriter.CopyTo(asnWriter);
		using (asnWriter.PushOctetString())
		{
			privateKeyWriter.CopyTo(asnWriter);
		}
		attributesWriter?.CopyTo(asnWriter);
		asnWriter.PopSequence();
		return asnWriter;
	}

	internal unsafe static void ReadEncryptedPkcs8<TRet>(string[] validOids, ReadOnlySpan<byte> source, ReadOnlySpan<char> password, KeyReader<TRet> keyReader, out int bytesRead, out TRet ret)
	{
		fixed (byte* pointer = &MemoryMarshal.GetReference(source))
		{
			using MemoryManager<byte> memoryManager = new PointerMemoryManager<byte>(pointer, source.Length);
			ReadEncryptedPkcs8(validOids, memoryManager.Memory, password, keyReader, out bytesRead, out ret);
		}
	}

	internal unsafe static void ReadEncryptedPkcs8<TRet>(string[] validOids, ReadOnlySpan<byte> source, ReadOnlySpan<byte> passwordBytes, KeyReader<TRet> keyReader, out int bytesRead, out TRet ret)
	{
		fixed (byte* pointer = &MemoryMarshal.GetReference(source))
		{
			using MemoryManager<byte> memoryManager = new PointerMemoryManager<byte>(pointer, source.Length);
			ReadEncryptedPkcs8(validOids, memoryManager.Memory, passwordBytes, keyReader, out bytesRead, out ret);
		}
	}

	private static void ReadEncryptedPkcs8<TRet>(string[] validOids, ReadOnlyMemory<byte> source, ReadOnlySpan<char> password, KeyReader<TRet> keyReader, out int bytesRead, out TRet ret)
	{
		ReadEncryptedPkcs8(validOids, source, password, ReadOnlySpan<byte>.Empty, keyReader, out bytesRead, out ret);
	}

	private static void ReadEncryptedPkcs8<TRet>(string[] validOids, ReadOnlyMemory<byte> source, ReadOnlySpan<byte> passwordBytes, KeyReader<TRet> keyReader, out int bytesRead, out TRet ret)
	{
		ReadEncryptedPkcs8(validOids, source, ReadOnlySpan<char>.Empty, passwordBytes, keyReader, out bytesRead, out ret);
	}

	private static void ReadEncryptedPkcs8<TRet>(string[] validOids, ReadOnlyMemory<byte> source, ReadOnlySpan<char> password, ReadOnlySpan<byte> passwordBytes, KeyReader<TRet> keyReader, out int bytesRead, out TRet ret)
	{
		int length;
		EncryptedPrivateKeyInfoAsn decoded;
		try
		{
			AsnValueReader reader = new AsnValueReader(source.Span, AsnEncodingRules.BER);
			length = reader.PeekEncodedValue().Length;
			EncryptedPrivateKeyInfoAsn.Decode(ref reader, source, out decoded);
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(decoded.EncryptedData.Length);
		Memory<byte> memory = array;
		try
		{
			memory = memory[..PasswordBasedEncryption.Decrypt(in decoded.EncryptionAlgorithm, password, passwordBytes, decoded.EncryptedData.Span, array)];
			ReadPkcs8(validOids, memory, keyReader, out var bytesRead2, out ret);
			if (bytesRead2 != memory.Length)
			{
				ret = default(TRet);
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			bytesRead = length;
		}
		catch (CryptographicException inner2)
		{
			throw new CryptographicException(System.SR.Cryptography_Pkcs8_EncryptedReadFailed, inner2);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(memory.Span);
			System.Security.Cryptography.CryptoPool.Return(array, 0);
		}
	}

	internal static AsnWriter WriteEncryptedPkcs8(ReadOnlySpan<char> password, AsnWriter pkcs8Writer, PbeParameters pbeParameters)
	{
		return WriteEncryptedPkcs8(password, ReadOnlySpan<byte>.Empty, pkcs8Writer, pbeParameters);
	}

	internal static AsnWriter WriteEncryptedPkcs8(ReadOnlySpan<byte> passwordBytes, AsnWriter pkcs8Writer, PbeParameters pbeParameters)
	{
		return WriteEncryptedPkcs8(ReadOnlySpan<char>.Empty, passwordBytes, pkcs8Writer, pbeParameters);
	}

	private static AsnWriter WriteEncryptedPkcs8(ReadOnlySpan<char> password, ReadOnlySpan<byte> passwordBytes, AsnWriter pkcs8Writer, PbeParameters pbeParameters)
	{
		PasswordBasedEncryption.InitiateEncryption(pbeParameters, out var cipher, out var hmacOid, out var encryptionAlgorithmOid, out var isPkcs);
		Span<byte> span = default(Span<byte>);
		AsnWriter asnWriter = null;
		Span<byte> span2 = stackalloc byte[cipher.BlockSize / 8];
		Span<byte> span3 = stackalloc byte[16];
		checked
		{
			byte[] array = System.Security.Cryptography.CryptoPool.Rent(pkcs8Writer.GetEncodedLength() + unchecked(cipher.BlockSize / 8));
			try
			{
				RandomNumberGenerator.Fill(span3);
				int length = PasswordBasedEncryption.Encrypt(password, passwordBytes, cipher, isPkcs, pkcs8Writer, pbeParameters, span3, array, span2);
				span = array.AsSpan(0, length);
				asnWriter = new AsnWriter(AsnEncodingRules.DER);
				asnWriter.PushSequence();
				PasswordBasedEncryption.WritePbeAlgorithmIdentifier(asnWriter, isPkcs, encryptionAlgorithmOid, span3, pbeParameters.IterationCount, hmacOid, span2);
				asnWriter.WriteOctetString(span);
				asnWriter.PopSequence();
				return asnWriter;
			}
			finally
			{
				CryptographicOperations.ZeroMemory(span);
				System.Security.Cryptography.CryptoPool.Return(array, 0);
				cipher.Dispose();
			}
		}
	}

	internal static ArraySegment<byte> DecryptPkcs8(ReadOnlySpan<char> inputPassword, ReadOnlyMemory<byte> source, out int bytesRead)
	{
		return DecryptPkcs8(inputPassword, ReadOnlySpan<byte>.Empty, source, out bytesRead);
	}

	internal static ArraySegment<byte> DecryptPkcs8(ReadOnlySpan<byte> inputPasswordBytes, ReadOnlyMemory<byte> source, out int bytesRead)
	{
		return DecryptPkcs8(ReadOnlySpan<char>.Empty, inputPasswordBytes, source, out bytesRead);
	}

	private static ArraySegment<byte> DecryptPkcs8(ReadOnlySpan<char> inputPassword, ReadOnlySpan<byte> inputPasswordBytes, ReadOnlyMemory<byte> source, out int bytesRead)
	{
		int length;
		EncryptedPrivateKeyInfoAsn decoded;
		try
		{
			AsnValueReader reader = new AsnValueReader(source.Span, AsnEncodingRules.BER);
			length = reader.PeekEncodedValue().Length;
			EncryptedPrivateKeyInfoAsn.Decode(ref reader, source, out decoded);
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(decoded.EncryptedData.Length);
		try
		{
			int count = PasswordBasedEncryption.Decrypt(in decoded.EncryptionAlgorithm, inputPassword, inputPasswordBytes, decoded.EncryptedData.Span, array);
			bytesRead = length;
			return new ArraySegment<byte>(array, 0, count);
		}
		catch (CryptographicException inner2)
		{
			System.Security.Cryptography.CryptoPool.Return(array);
			throw new CryptographicException(System.SR.Cryptography_Pkcs8_EncryptedReadFailed, inner2);
		}
	}

	internal static AsnWriter ReencryptPkcs8(ReadOnlySpan<char> inputPassword, ReadOnlyMemory<byte> current, ReadOnlySpan<char> newPassword, PbeParameters pbeParameters)
	{
		int bytesRead;
		ArraySegment<byte> arraySegment = DecryptPkcs8(inputPassword, current, out bytesRead);
		try
		{
			if (bytesRead != current.Length)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.BER);
			asnWriter.WriteEncodedValueForCrypto(arraySegment);
			return WriteEncryptedPkcs8(newPassword, asnWriter, pbeParameters);
		}
		catch (CryptographicException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Pkcs8_EncryptedReadFailed, inner);
		}
		finally
		{
			System.Security.Cryptography.CryptoPool.Return(arraySegment);
		}
	}

	internal static AsnWriter ReencryptPkcs8(ReadOnlySpan<char> inputPassword, ReadOnlyMemory<byte> current, ReadOnlySpan<byte> newPasswordBytes, PbeParameters pbeParameters)
	{
		int bytesRead;
		ArraySegment<byte> arraySegment = DecryptPkcs8(inputPassword, current, out bytesRead);
		try
		{
			if (bytesRead != current.Length)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.BER);
			asnWriter.WriteEncodedValueForCrypto(arraySegment);
			return WriteEncryptedPkcs8(newPasswordBytes, asnWriter, pbeParameters);
		}
		catch (CryptographicException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Pkcs8_EncryptedReadFailed, inner);
		}
		finally
		{
			System.Security.Cryptography.CryptoPool.Return(arraySegment);
		}
	}
}
