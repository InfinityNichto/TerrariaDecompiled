using System.Formats.Asn1;

namespace System.Security.Cryptography.X509Certificates.Asn1;

internal struct TimeAsn
{
	internal DateTimeOffset? UtcTime;

	internal DateTimeOffset? GeneralTime;

	internal void Encode(AsnWriter writer)
	{
		bool flag = false;
		if (UtcTime.HasValue)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.WriteUtcTime(UtcTime.Value);
			flag = true;
		}
		if (GeneralTime.HasValue)
		{
			if (flag)
			{
				throw new CryptographicException();
			}
			writer.WriteGeneralizedTime(GeneralTime.Value, omitFractionalSeconds: true);
			flag = true;
		}
		if (!flag)
		{
			throw new CryptographicException();
		}
	}

	internal static void Decode(ref System.Formats.Asn1.AsnValueReader reader, ReadOnlyMemory<byte> rebind, out TimeAsn decoded)
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

	private static void DecodeCore(ref System.Formats.Asn1.AsnValueReader reader, ReadOnlyMemory<byte> rebind, out TimeAsn decoded)
	{
		decoded = default(TimeAsn);
		Asn1Tag asn1Tag = reader.PeekTag();
		if (asn1Tag.HasSameClassAndValue(Asn1Tag.UtcTime))
		{
			decoded.UtcTime = reader.ReadUtcTime();
			return;
		}
		if (asn1Tag.HasSameClassAndValue(Asn1Tag.GeneralizedTime))
		{
			decoded.GeneralTime = reader.ReadGeneralizedTime();
			if (decoded.GeneralTime.Value.Ticks % 10000000 == 0L)
			{
				return;
			}
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		throw new CryptographicException();
	}

	public TimeAsn(DateTimeOffset dateTimeOffset)
	{
		DateTime utcDateTime = dateTimeOffset.UtcDateTime;
		if (utcDateTime.Year >= 1950 && utcDateTime.Year < 2050)
		{
			UtcTime = utcDateTime;
			GeneralTime = null;
		}
		else
		{
			UtcTime = null;
			GeneralTime = utcDateTime;
		}
	}
}
