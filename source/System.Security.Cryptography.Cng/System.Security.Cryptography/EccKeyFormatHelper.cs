using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography;

internal static class EccKeyFormatHelper
{
	internal static void FromECPrivateKey(System.Security.Cryptography.Asn1.ECPrivateKey key, in System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn algId, out ECParameters ret)
	{
		ValidateParameters(key.Parameters, in algId);
		if (key.Version != 1)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		byte[] x = null;
		byte[] y = null;
		if (key.PublicKey.HasValue)
		{
			ReadOnlySpan<byte> span = key.PublicKey.Value.Span;
			if (span.Length == 0)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			if (span[0] != 4)
			{
				throw new CryptographicException(System.SR.Cryptography_NotValidPublicOrPrivateKey);
			}
			if (span.Length != 2 * key.PrivateKey.Length + 1)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			x = span.Slice(1, key.PrivateKey.Length).ToArray();
			y = span.Slice(1 + key.PrivateKey.Length).ToArray();
		}
		System.Security.Cryptography.Asn1.ECDomainParameters domainParameters = ((!key.Parameters.HasValue) ? System.Security.Cryptography.Asn1.ECDomainParameters.Decode(algId.Parameters.Value, AsnEncodingRules.DER) : key.Parameters.Value);
		ret = new ECParameters
		{
			Curve = GetCurve(domainParameters),
			Q = 
			{
				X = x,
				Y = y
			},
			D = key.PrivateKey.ToArray()
		};
		ret.Validate();
	}

	private static void ValidateParameters(System.Security.Cryptography.Asn1.ECDomainParameters? keyParameters, in System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn algId)
	{
		if (!keyParameters.HasValue && !algId.Parameters.HasValue)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		if (keyParameters.HasValue && algId.Parameters.HasValue)
		{
			ReadOnlySpan<byte> span = algId.Parameters.Value.Span;
			AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
			keyParameters.Value.Encode(asnWriter);
			if (!asnWriter.EncodedValueEquals(span))
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
		}
	}

	private static ECCurve GetCurve(System.Security.Cryptography.Asn1.ECDomainParameters domainParameters)
	{
		if (domainParameters.Specified.HasValue)
		{
			return GetSpecifiedECCurve(domainParameters.Specified.Value);
		}
		if (domainParameters.Named == null)
		{
			throw new CryptographicException(System.SR.Cryptography_ECC_NamedCurvesOnly);
		}
		return ECCurve.CreateFromOid(domainParameters.Named switch
		{
			"1.2.840.10045.3.1.7" => System.Security.Cryptography.Oids.secp256r1Oid, 
			"1.3.132.0.34" => System.Security.Cryptography.Oids.secp384r1Oid, 
			"1.3.132.0.35" => System.Security.Cryptography.Oids.secp521r1Oid, 
			_ => new Oid(domainParameters.Named, null), 
		});
	}

	private static ECCurve GetSpecifiedECCurve(System.Security.Cryptography.Asn1.SpecifiedECDomain specifiedParameters)
	{
		try
		{
			return GetSpecifiedECCurveCore(specifiedParameters);
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	private static ECCurve GetSpecifiedECCurveCore(System.Security.Cryptography.Asn1.SpecifiedECDomain specifiedParameters)
	{
		if (specifiedParameters.Version < 1 || specifiedParameters.Version > 3)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		if (specifiedParameters.Version > 1 && !specifiedParameters.Curve.Seed.HasValue)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		string fieldType = specifiedParameters.FieldID.FieldType;
		bool flag;
		byte[] array;
		if (!(fieldType == "1.2.840.10045.1.1"))
		{
			if (!(fieldType == "1.2.840.10045.1.2"))
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			flag = false;
			AsnReader asnReader = new AsnReader(specifiedParameters.FieldID.Parameters, AsnEncodingRules.BER);
			AsnReader asnReader2 = asnReader.ReadSequence();
			asnReader.ThrowIfNotEmpty();
			if (!asnReader2.TryReadInt32(out var value) || value > 661 || value < 0)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			int value2 = -1;
			int value3 = -1;
			string text = asnReader2.ReadObjectIdentifier();
			int value4;
			if (!(text == "1.2.840.10045.1.2.3.2"))
			{
				if (!(text == "1.2.840.10045.1.2.3.3"))
				{
					throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
				}
				AsnReader asnReader3 = asnReader2.ReadSequence();
				if (!asnReader3.TryReadInt32(out value4) || !asnReader3.TryReadInt32(out value2) || !asnReader3.TryReadInt32(out value3) || value4 < 1 || value2 <= value4 || value3 <= value2 || value3 >= value)
				{
					throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
				}
				asnReader3.ThrowIfNotEmpty();
			}
			else if (!asnReader2.TryReadInt32(out value4) || value4 >= value || value4 < 1)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			asnReader2.ThrowIfNotEmpty();
			BitArray bitArray = new BitArray(value + 1);
			bitArray.Set(value, value: true);
			bitArray.Set(value4, value: true);
			bitArray.Set(0, value: true);
			if (value2 > 0)
			{
				bitArray.Set(value2, value: true);
				bitArray.Set(value3, value: true);
			}
			array = new byte[(value + 7) / 8];
			bitArray.CopyTo(array, 0);
			Array.Reverse(array);
		}
		else
		{
			flag = true;
			AsnReader asnReader4 = new AsnReader(specifiedParameters.FieldID.Parameters, AsnEncodingRules.BER);
			ReadOnlySpan<byte> readOnlySpan = asnReader4.ReadIntegerBytes().Span;
			asnReader4.ThrowIfNotEmpty();
			if (readOnlySpan[0] == 0)
			{
				readOnlySpan = readOnlySpan.Slice(1);
			}
			if (readOnlySpan.Length > 82)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
			}
			array = readOnlySpan.ToArray();
		}
		ECCurve result;
		if (flag)
		{
			ECCurve eCCurve = default(ECCurve);
			eCCurve.CurveType = ECCurve.ECCurveType.PrimeShortWeierstrass;
			eCCurve.Prime = array;
			result = eCCurve;
		}
		else
		{
			ECCurve eCCurve = default(ECCurve);
			eCCurve.CurveType = ECCurve.ECCurveType.Characteristic2;
			eCCurve.Polynomial = array;
			result = eCCurve;
		}
		result.A = specifiedParameters.Curve.A.ToUnsignedIntegerBytes(array.Length);
		result.B = specifiedParameters.Curve.B.ToUnsignedIntegerBytes(array.Length);
		result.Order = specifiedParameters.Order.ToUnsignedIntegerBytes(array.Length);
		ReadOnlySpan<byte> span = specifiedParameters.Base.Span;
		if (span[0] != 4 || span.Length != 2 * array.Length + 1)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		result.G.X = span.Slice(1, array.Length).ToArray();
		result.G.Y = span.Slice(1 + array.Length).ToArray();
		if (specifiedParameters.Cofactor.HasValue)
		{
			result.Cofactor = specifiedParameters.Cofactor.Value.ToUnsignedIntegerBytes();
		}
		return result;
	}

	private static AsnWriter WriteAlgorithmIdentifier(in ECParameters ecParameters)
	{
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		WriteAlgorithmIdentifier(in ecParameters, asnWriter);
		return asnWriter;
	}

	private static void WriteAlgorithmIdentifier(in ECParameters ecParameters, AsnWriter writer)
	{
		writer.PushSequence();
		writer.WriteObjectIdentifier("1.2.840.10045.2.1");
		WriteEcParameters(ecParameters, writer);
		writer.PopSequence();
	}

	internal static AsnWriter WritePkcs8PrivateKey(ECParameters ecParameters, System.Security.Cryptography.Asn1.AttributeAsn[] attributes = null)
	{
		ecParameters.Validate();
		if (ecParameters.D == null)
		{
			throw new CryptographicException(System.SR.Cryptography_CSP_NoPrivateKey);
		}
		AsnWriter privateKeyWriter = WriteEcPrivateKey(in ecParameters, includeDomainParameters: false);
		AsnWriter algorithmIdentifierWriter = WriteAlgorithmIdentifier(in ecParameters);
		AsnWriter attributesWriter = WritePrivateKeyInfoAttributes(attributes);
		return System.Security.Cryptography.KeyFormatHelper.WritePkcs8(algorithmIdentifierWriter, privateKeyWriter, attributesWriter);
	}

	[return: NotNullIfNotNull("attributes")]
	private static AsnWriter WritePrivateKeyInfoAttributes(System.Security.Cryptography.Asn1.AttributeAsn[] attributes)
	{
		if (attributes == null)
		{
			return null;
		}
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		Asn1Tag value = new Asn1Tag(TagClass.ContextSpecific, 0);
		asnWriter.PushSetOf(value);
		for (int i = 0; i < attributes.Length; i++)
		{
			attributes[i].Encode(asnWriter);
		}
		asnWriter.PopSetOf(value);
		return asnWriter;
	}

	private static void WriteEcParameters(ECParameters ecParameters, AsnWriter writer)
	{
		if (ecParameters.Curve.IsNamed)
		{
			Oid oid = ecParameters.Curve.Oid;
			if (string.IsNullOrEmpty(oid.Value))
			{
				oid = Oid.FromFriendlyName(oid.FriendlyName, OidGroup.All);
			}
			writer.WriteObjectIdentifier(oid.Value);
		}
		else
		{
			if (!ecParameters.Curve.IsExplicit)
			{
				throw new CryptographicException(System.SR.Format(System.SR.Cryptography_CurveNotSupported, ecParameters.Curve.CurveType.ToString()));
			}
			WriteSpecifiedECDomain(ecParameters, writer);
		}
	}

	private static void WriteSpecifiedECDomain(ECParameters ecParameters, AsnWriter writer)
	{
		int k3;
		int k2;
		int k;
		int m = (k3 = (k2 = (k = -1)));
		if (ecParameters.Curve.IsCharacteristic2)
		{
			DetermineChar2Parameters(in ecParameters, ref m, ref k3, ref k2, ref k);
		}
		writer.PushSequence();
		writer.WriteInteger(1L);
		writer.PushSequence();
		if (ecParameters.Curve.IsPrime)
		{
			writer.WriteObjectIdentifier("1.2.840.10045.1.1");
			writer.WriteIntegerUnsigned(ecParameters.Curve.Prime);
		}
		else
		{
			writer.WriteObjectIdentifier("1.2.840.10045.1.2");
			writer.PushSequence();
			writer.WriteInteger((long)m, (Asn1Tag?)null);
			if (k > 0)
			{
				writer.WriteObjectIdentifier("1.2.840.10045.1.2.3.3");
				writer.PushSequence();
				writer.WriteInteger((long)k3, (Asn1Tag?)null);
				writer.WriteInteger((long)k2, (Asn1Tag?)null);
				writer.WriteInteger((long)k, (Asn1Tag?)null);
				writer.PopSequence();
			}
			else
			{
				writer.WriteObjectIdentifier("1.2.840.10045.1.2.3.2");
				writer.WriteInteger((long)k3, (Asn1Tag?)null);
			}
			writer.PopSequence();
		}
		writer.PopSequence();
		WriteCurve(in ecParameters.Curve, writer);
		WriteUncompressedBasePoint(in ecParameters, writer);
		writer.WriteIntegerUnsigned(ecParameters.Curve.Order);
		if (ecParameters.Curve.Cofactor != null)
		{
			writer.WriteIntegerUnsigned(ecParameters.Curve.Cofactor);
		}
		writer.PopSequence();
	}

	private static void DetermineChar2Parameters(in ECParameters ecParameters, ref int m, ref int k1, ref int k2, ref int k3)
	{
		byte[] polynomial = ecParameters.Curve.Polynomial;
		int num = polynomial.Length - 1;
		if (polynomial[0] == 0 || (polynomial[num] & 1) != 1)
		{
			throw new CryptographicException(System.SR.Cryptography_InvalidECCharacteristic2Curve);
		}
		for (int num2 = 7; num2 >= 0; num2--)
		{
			int num3 = 1 << num2;
			if ((polynomial[0] & num3) == num3)
			{
				m = checked(8 * num + num2);
			}
		}
		for (int i = 0; i < polynomial.Length; i++)
		{
			int num4 = num - i;
			byte b = polynomial[num4];
			for (int j = 0; j < 8; j++)
			{
				int num5 = 1 << j;
				if ((b & num5) != num5)
				{
					continue;
				}
				int num6 = 8 * i + j;
				if (num6 == 0)
				{
					continue;
				}
				if (num6 == m)
				{
					break;
				}
				if (k1 < 0)
				{
					k1 = num6;
					continue;
				}
				if (k2 < 0)
				{
					k2 = num6;
					continue;
				}
				if (k3 < 0)
				{
					k3 = num6;
					continue;
				}
				throw new CryptographicException(System.SR.Cryptography_InvalidECCharacteristic2Curve);
			}
		}
		if (k3 <= 0)
		{
			if (k2 > 0)
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidECCharacteristic2Curve);
			}
			if (k1 <= 0)
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidECCharacteristic2Curve);
			}
		}
	}

	private static void WriteCurve(in ECCurve curve, AsnWriter writer)
	{
		writer.PushSequence();
		WriteFieldElement(curve.A, writer);
		WriteFieldElement(curve.B, writer);
		if (curve.Seed != null)
		{
			writer.WriteBitString(curve.Seed);
		}
		writer.PopSequence();
	}

	private static void WriteFieldElement(byte[] fieldElement, AsnWriter writer)
	{
		int i;
		for (i = 0; i < fieldElement.Length - 1 && fieldElement[i] == 0; i++)
		{
		}
		writer.WriteOctetString(fieldElement.AsSpan(i));
	}

	private static void WriteUncompressedBasePoint(in ECParameters ecParameters, AsnWriter writer)
	{
		int num = ecParameters.Curve.G.X.Length * 2 + 1;
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(num);
		array[0] = 4;
		ecParameters.Curve.G.X.CopyTo(array.AsSpan(1));
		ecParameters.Curve.G.Y.CopyTo(array.AsSpan(1 + ecParameters.Curve.G.X.Length));
		writer.WriteOctetString(array.AsSpan(0, num));
		System.Security.Cryptography.CryptoPool.Return(array, 0);
	}

	private static void WriteUncompressedPublicKey(in ECParameters ecParameters, AsnWriter writer)
	{
		int num = ecParameters.Q.X.Length * 2 + 1;
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(num);
		array[0] = 4;
		ecParameters.Q.X.AsSpan().CopyTo(array.AsSpan(1));
		ecParameters.Q.Y.AsSpan().CopyTo(array.AsSpan(1 + ecParameters.Q.X.Length));
		writer.WriteBitString(array.AsSpan(0, num));
	}

	private static AsnWriter WriteEcPrivateKey(in ECParameters ecParameters, bool includeDomainParameters)
	{
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		asnWriter.PushSequence();
		asnWriter.WriteInteger(1L);
		asnWriter.WriteOctetString(ecParameters.D);
		if (includeDomainParameters)
		{
			Asn1Tag value = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);
			asnWriter.PushSequence(value);
			WriteEcParameters(ecParameters, asnWriter);
			asnWriter.PopSequence(value);
		}
		if (ecParameters.Q.X != null)
		{
			Asn1Tag value2 = new Asn1Tag(TagClass.ContextSpecific, 1, isConstructed: true);
			asnWriter.PushSequence(value2);
			WriteUncompressedPublicKey(in ecParameters, asnWriter);
			asnWriter.PopSequence(value2);
		}
		asnWriter.PopSequence();
		return asnWriter;
	}
}
