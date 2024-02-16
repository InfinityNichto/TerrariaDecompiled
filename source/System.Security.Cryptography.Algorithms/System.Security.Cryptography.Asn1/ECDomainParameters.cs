using System.Formats.Asn1;

namespace System.Security.Cryptography.Asn1;

internal struct ECDomainParameters
{
	internal SpecifiedECDomain? Specified;

	internal string Named;

	internal void Encode(AsnWriter writer)
	{
		bool flag = false;
		if (Specified.HasValue)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			Specified.Value.Encode(writer);
			flag = true;
		}
		if (Named != null)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			try
			{
				writer.WriteObjectIdentifier(Named);
			}
			catch (ArgumentException inner)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
			}
			flag = true;
		}
		if (!flag)
		{
			throw new CryptographicException();
		}
	}

	internal static ECDomainParameters Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
	{
		try
		{
			AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);
			DecodeCore(ref reader, encoded, out var decoded);
			reader.ThrowIfNotEmpty();
			return decoded;
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out ECDomainParameters decoded)
	{
		try
		{
			DecodeCore(ref reader, rebind, out decoded);
		}
		catch (AsnContentException inner)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
		}
	}

	private static void DecodeCore(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out ECDomainParameters decoded)
	{
		decoded = default(ECDomainParameters);
		Asn1Tag asn1Tag = reader.PeekTag();
		if (asn1Tag.HasSameClassAndValue(Asn1Tag.Sequence))
		{
			SpecifiedECDomain.Decode(ref reader, rebind, out var decoded2);
			decoded.Specified = decoded2;
			return;
		}
		if (asn1Tag.HasSameClassAndValue(Asn1Tag.ObjectIdentifier))
		{
			decoded.Named = reader.ReadObjectIdentifier();
			return;
		}
		throw new CryptographicException();
	}
}
