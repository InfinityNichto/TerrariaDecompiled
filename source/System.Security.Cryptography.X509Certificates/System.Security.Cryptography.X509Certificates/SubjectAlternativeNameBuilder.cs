using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.Net;
using System.Security.Cryptography.Asn1;
using System.Text;

namespace System.Security.Cryptography.X509Certificates;

public sealed class SubjectAlternativeNameBuilder
{
	private static readonly IdnMapping s_idnMapping = new IdnMapping();

	private readonly List<byte[]> _encodedNames = new List<byte[]>();

	public void AddEmailAddress(string emailAddress)
	{
		if (string.IsNullOrEmpty(emailAddress))
		{
			throw new ArgumentOutOfRangeException("emailAddress", System.SR.Arg_EmptyOrNullString);
		}
		AddGeneralName(new GeneralNameAsn
		{
			Rfc822Name = emailAddress
		});
	}

	public void AddDnsName(string dnsName)
	{
		if (string.IsNullOrEmpty(dnsName))
		{
			throw new ArgumentOutOfRangeException("dnsName", System.SR.Arg_EmptyOrNullString);
		}
		AddGeneralName(new GeneralNameAsn
		{
			DnsName = s_idnMapping.GetAscii(dnsName)
		});
	}

	public void AddUri(Uri uri)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		AddGeneralName(new GeneralNameAsn
		{
			Uri = uri.AbsoluteUri.ToString()
		});
	}

	public void AddIpAddress(IPAddress ipAddress)
	{
		if (ipAddress == null)
		{
			throw new ArgumentNullException("ipAddress");
		}
		AddGeneralName(new GeneralNameAsn
		{
			IPAddress = ipAddress.GetAddressBytes()
		});
	}

	public void AddUserPrincipalName(string upn)
	{
		if (string.IsNullOrEmpty(upn))
		{
			throw new ArgumentOutOfRangeException("upn", System.SR.Arg_EmptyOrNullString);
		}
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		asnWriter.WriteCharacterString(UniversalTagNumber.UTF8String, upn);
		byte[] array = asnWriter.Encode();
		OtherNameAsn otherNameAsn = default(OtherNameAsn);
		otherNameAsn.TypeId = "1.3.6.1.4.1.311.20.2.3";
		otherNameAsn.Value = array;
		OtherNameAsn value = otherNameAsn;
		AddGeneralName(new GeneralNameAsn
		{
			OtherName = value
		});
	}

	public X509Extension Build(bool critical = false)
	{
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		using (asnWriter.PushSequence())
		{
			foreach (byte[] encodedName in _encodedNames)
			{
				asnWriter.WriteEncodedValue(encodedName);
			}
		}
		return new X509Extension("2.5.29.17", asnWriter.Encode(), critical);
	}

	private void AddGeneralName(GeneralNameAsn generalName)
	{
		try
		{
			AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
			generalName.Encode(asnWriter);
			_encodedNames.Add(asnWriter.Encode());
		}
		catch (EncoderFallbackException)
		{
			throw new CryptographicException(System.SR.Cryptography_Invalid_IA5String);
		}
	}
}
