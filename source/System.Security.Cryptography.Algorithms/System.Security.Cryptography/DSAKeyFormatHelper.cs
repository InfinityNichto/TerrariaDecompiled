using System.Formats.Asn1;
using System.Numerics;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography;

internal static class DSAKeyFormatHelper
{
	private static readonly string[] s_validOids = new string[1] { "1.2.840.10040.4.1" };

	internal static void ReadDsaPrivateKey(ReadOnlyMemory<byte> xBytes, in AlgorithmIdentifierAsn algId, out DSAParameters ret)
	{
		if (!algId.Parameters.HasValue)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		DssParms dssParms = DssParms.Decode(algId.Parameters.Value, AsnEncodingRules.BER);
		ret = new DSAParameters
		{
			P = dssParms.P.ToByteArray(isUnsigned: true, isBigEndian: true),
			Q = dssParms.Q.ToByteArray(isUnsigned: true, isBigEndian: true)
		};
		ret.G = dssParms.G.ExportKeyParameter(ret.P.Length);
		BigInteger bigInteger;
		try
		{
			int bytesConsumed;
			ReadOnlySpan<byte> value = AsnDecoder.ReadIntegerBytes(xBytes.Span, AsnEncodingRules.DER, out bytesConsumed);
			if (bytesConsumed != xBytes.Length)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			bigInteger = new BigInteger(value, isUnsigned: true, isBigEndian: true);
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		ret.X = bigInteger.ExportKeyParameter(ret.Q.Length);
		BigInteger value2 = BigInteger.ModPow(dssParms.G, bigInteger, dssParms.P);
		ret.Y = value2.ExportKeyParameter(ret.P.Length);
	}

	internal static void ReadDsaPublicKey(ReadOnlyMemory<byte> yBytes, in AlgorithmIdentifierAsn algId, out DSAParameters ret)
	{
		BigInteger value;
		try
		{
			value = AsnDecoder.ReadInteger(yBytes.Span, AsnEncodingRules.DER, out var bytesConsumed);
			if (bytesConsumed != yBytes.Length)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
		if (!algId.Parameters.HasValue)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		DssParms dssParms = DssParms.Decode(algId.Parameters.Value, AsnEncodingRules.BER);
		ret = new DSAParameters
		{
			P = dssParms.P.ToByteArray(isUnsigned: true, isBigEndian: true),
			Q = dssParms.Q.ToByteArray(isUnsigned: true, isBigEndian: true)
		};
		ret.G = dssParms.G.ExportKeyParameter(ret.P.Length);
		ret.Y = value.ExportKeyParameter(ret.P.Length);
	}

	internal static void ReadSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead, out DSAParameters key)
	{
		KeyFormatHelper.ReadSubjectPublicKeyInfo(s_validOids, source, (KeyFormatHelper.KeyReader<DSAParameters>)ReadDsaPublicKey, out bytesRead, out key);
	}

	internal static void ReadPkcs8(ReadOnlySpan<byte> source, out int bytesRead, out DSAParameters key)
	{
		KeyFormatHelper.ReadPkcs8(s_validOids, source, (KeyFormatHelper.KeyReader<DSAParameters>)ReadDsaPrivateKey, out bytesRead, out key);
	}

	internal static void ReadEncryptedPkcs8(ReadOnlySpan<byte> source, ReadOnlySpan<char> password, out int bytesRead, out DSAParameters key)
	{
		KeyFormatHelper.ReadEncryptedPkcs8(s_validOids, source, password, (KeyFormatHelper.KeyReader<DSAParameters>)ReadDsaPrivateKey, out bytesRead, out key);
	}

	internal static void ReadEncryptedPkcs8(ReadOnlySpan<byte> source, ReadOnlySpan<byte> passwordBytes, out int bytesRead, out DSAParameters key)
	{
		KeyFormatHelper.ReadEncryptedPkcs8(s_validOids, source, passwordBytes, (KeyFormatHelper.KeyReader<DSAParameters>)ReadDsaPrivateKey, out bytesRead, out key);
	}

	internal static AsnWriter WriteSubjectPublicKeyInfo(in DSAParameters dsaParameters)
	{
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		asnWriter.PushSequence();
		WriteAlgorithmId(asnWriter, in dsaParameters);
		WriteKeyComponent(asnWriter, dsaParameters.Y, bitString: true);
		asnWriter.PopSequence();
		return asnWriter;
	}

	internal static AsnWriter WritePkcs8(in DSAParameters dsaParameters)
	{
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		asnWriter.PushSequence();
		asnWriter.WriteInteger(0L);
		WriteAlgorithmId(asnWriter, in dsaParameters);
		WriteKeyComponent(asnWriter, dsaParameters.X, bitString: false);
		asnWriter.PopSequence();
		return asnWriter;
	}

	private static void WriteAlgorithmId(AsnWriter writer, in DSAParameters dsaParameters)
	{
		writer.PushSequence();
		writer.WriteObjectIdentifier("1.2.840.10040.4.1");
		writer.PushSequence();
		writer.WriteKeyParameterInteger(dsaParameters.P);
		writer.WriteKeyParameterInteger(dsaParameters.Q);
		writer.WriteKeyParameterInteger(dsaParameters.G);
		writer.PopSequence();
		writer.PopSequence();
	}

	private static void WriteKeyComponent(AsnWriter writer, byte[] component, bool bitString)
	{
		if (bitString)
		{
			AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
			asnWriter.WriteKeyParameterInteger(component);
			byte[] array = System.Security.Cryptography.CryptoPool.Rent(asnWriter.GetEncodedLength());
			if (!asnWriter.TryEncode(array, out var bytesWritten))
			{
				throw new CryptographicException();
			}
			writer.WriteBitString(array.AsSpan(0, bytesWritten));
			System.Security.Cryptography.CryptoPool.Return(array, bytesWritten);
			return;
		}
		using (writer.PushOctetString())
		{
			writer.WriteKeyParameterInteger(component);
		}
	}
}
