using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography;

internal static class KeyFormatHelper
{
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
		System.Security.Cryptography.PasswordBasedEncryption.InitiateEncryption(pbeParameters, out var cipher, out var hmacOid, out var encryptionAlgorithmOid, out var isPkcs);
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
				int length = System.Security.Cryptography.PasswordBasedEncryption.Encrypt(password, passwordBytes, cipher, isPkcs, pkcs8Writer, pbeParameters, span3, array, span2);
				span = array.AsSpan(0, length);
				asnWriter = new AsnWriter(AsnEncodingRules.DER);
				asnWriter.PushSequence();
				System.Security.Cryptography.PasswordBasedEncryption.WritePbeAlgorithmIdentifier(asnWriter, isPkcs, encryptionAlgorithmOid, span3, pbeParameters.IterationCount, hmacOid, span2);
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
		System.Security.Cryptography.Asn1.EncryptedPrivateKeyInfoAsn decoded;
		try
		{
			System.Formats.Asn1.AsnValueReader reader = new System.Formats.Asn1.AsnValueReader(source.Span, AsnEncodingRules.BER);
			length = reader.PeekEncodedValue().Length;
			System.Security.Cryptography.Asn1.EncryptedPrivateKeyInfoAsn.Decode(ref reader, source, out decoded);
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(decoded.EncryptedData.Length);
		try
		{
			int count = System.Security.Cryptography.PasswordBasedEncryption.Decrypt(in decoded.EncryptionAlgorithm, inputPassword, inputPasswordBytes, decoded.EncryptedData.Span, array);
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
