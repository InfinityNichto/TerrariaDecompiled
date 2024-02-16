using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography;

internal static class RSAKeyFormatHelper
{
	private static readonly string[] s_validOids = new string[1] { "1.2.840.113549.1.1.1" };

	internal static void FromPkcs1PrivateKey(ReadOnlyMemory<byte> keyData, in AlgorithmIdentifierAsn algId, out RSAParameters ret)
	{
		RSAPrivateKeyAsn rSAPrivateKeyAsn = RSAPrivateKeyAsn.Decode(keyData, AsnEncodingRules.BER);
		if (!algId.HasNullEquivalentParameters())
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		if (rSAPrivateKeyAsn.Version > 0)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_RSAPrivateKey_VersionTooNew, rSAPrivateKeyAsn.Version, 0));
		}
		byte[] array = rSAPrivateKeyAsn.Modulus.ToByteArray(isUnsigned: true, isBigEndian: true);
		int length = (array.Length + 1) / 2;
		ret = new RSAParameters
		{
			Modulus = array,
			Exponent = rSAPrivateKeyAsn.PublicExponent.ToByteArray(isUnsigned: true, isBigEndian: true),
			D = rSAPrivateKeyAsn.PrivateExponent.ExportKeyParameter(array.Length),
			P = rSAPrivateKeyAsn.Prime1.ExportKeyParameter(length),
			Q = rSAPrivateKeyAsn.Prime2.ExportKeyParameter(length),
			DP = rSAPrivateKeyAsn.Exponent1.ExportKeyParameter(length),
			DQ = rSAPrivateKeyAsn.Exponent2.ExportKeyParameter(length),
			InverseQ = rSAPrivateKeyAsn.Coefficient.ExportKeyParameter(length)
		};
	}

	internal static void ReadRsaPublicKey(ReadOnlyMemory<byte> keyData, in AlgorithmIdentifierAsn algId, out RSAParameters ret)
	{
		RSAPublicKeyAsn rSAPublicKeyAsn = RSAPublicKeyAsn.Decode(keyData, AsnEncodingRules.BER);
		ret = new RSAParameters
		{
			Modulus = rSAPublicKeyAsn.Modulus.ToByteArray(isUnsigned: true, isBigEndian: true),
			Exponent = rSAPublicKeyAsn.PublicExponent.ToByteArray(isUnsigned: true, isBigEndian: true)
		};
	}

	internal static ReadOnlyMemory<byte> ReadSubjectPublicKeyInfo(ReadOnlyMemory<byte> source, out int bytesRead)
	{
		return KeyFormatHelper.ReadSubjectPublicKeyInfo(s_validOids, source, out bytesRead);
	}

	internal static ReadOnlyMemory<byte> ReadPkcs8(ReadOnlyMemory<byte> source, out int bytesRead)
	{
		return KeyFormatHelper.ReadPkcs8(s_validOids, source, out bytesRead);
	}

	internal static AsnWriter WriteSubjectPublicKeyInfo(ReadOnlySpan<byte> pkcs1PublicKey)
	{
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		asnWriter.PushSequence();
		WriteAlgorithmIdentifier(asnWriter);
		asnWriter.WriteBitString(pkcs1PublicKey);
		asnWriter.PopSequence();
		return asnWriter;
	}

	internal static AsnWriter WritePkcs8PrivateKey(ReadOnlySpan<byte> pkcs1PrivateKey, AsnWriter copyFrom = null)
	{
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.BER);
		using (asnWriter.PushSequence())
		{
			asnWriter.WriteInteger(0L);
			WriteAlgorithmIdentifier(asnWriter);
			if (copyFrom != null)
			{
				using (asnWriter.PushOctetString())
				{
					copyFrom.CopyTo(asnWriter);
				}
			}
			else
			{
				asnWriter.WriteOctetString(pkcs1PrivateKey);
			}
		}
		return asnWriter;
	}

	private static void WriteAlgorithmIdentifier(AsnWriter writer)
	{
		writer.PushSequence();
		writer.WriteObjectIdentifier("1.2.840.113549.1.1.1");
		writer.WriteNull();
		writer.PopSequence();
	}

	internal static AsnWriter WritePkcs1PublicKey(in RSAParameters rsaParameters)
	{
		if (rsaParameters.Modulus == null || rsaParameters.Exponent == null)
		{
			throw new CryptographicException(System.SR.Cryptography_InvalidRsaParameters);
		}
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		asnWriter.PushSequence();
		asnWriter.WriteKeyParameterInteger(rsaParameters.Modulus);
		asnWriter.WriteKeyParameterInteger(rsaParameters.Exponent);
		asnWriter.PopSequence();
		return asnWriter;
	}

	internal static AsnWriter WritePkcs1PrivateKey(in RSAParameters rsaParameters)
	{
		if (rsaParameters.Modulus == null || rsaParameters.Exponent == null)
		{
			throw new CryptographicException(System.SR.Cryptography_InvalidRsaParameters);
		}
		if (rsaParameters.D == null || rsaParameters.P == null || rsaParameters.Q == null || rsaParameters.DP == null || rsaParameters.DQ == null || rsaParameters.InverseQ == null)
		{
			throw new CryptographicException(System.SR.Cryptography_NotValidPrivateKey);
		}
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		asnWriter.PushSequence();
		asnWriter.WriteInteger(0L);
		asnWriter.WriteKeyParameterInteger(rsaParameters.Modulus);
		asnWriter.WriteKeyParameterInteger(rsaParameters.Exponent);
		asnWriter.WriteKeyParameterInteger(rsaParameters.D);
		asnWriter.WriteKeyParameterInteger(rsaParameters.P);
		asnWriter.WriteKeyParameterInteger(rsaParameters.Q);
		asnWriter.WriteKeyParameterInteger(rsaParameters.DP);
		asnWriter.WriteKeyParameterInteger(rsaParameters.DQ);
		asnWriter.WriteKeyParameterInteger(rsaParameters.InverseQ);
		asnWriter.PopSequence();
		return asnWriter;
	}

	internal static void ReadEncryptedPkcs8(ReadOnlySpan<byte> source, ReadOnlySpan<char> password, out int bytesRead, out RSAParameters key)
	{
		KeyFormatHelper.ReadEncryptedPkcs8(s_validOids, source, password, (KeyFormatHelper.KeyReader<RSAParameters>)FromPkcs1PrivateKey, out bytesRead, out key);
	}

	internal static void ReadEncryptedPkcs8(ReadOnlySpan<byte> source, ReadOnlySpan<byte> passwordBytes, out int bytesRead, out RSAParameters key)
	{
		KeyFormatHelper.ReadEncryptedPkcs8(s_validOids, source, passwordBytes, (KeyFormatHelper.KeyReader<RSAParameters>)FromPkcs1PrivateKey, out bytesRead, out key);
	}
}
