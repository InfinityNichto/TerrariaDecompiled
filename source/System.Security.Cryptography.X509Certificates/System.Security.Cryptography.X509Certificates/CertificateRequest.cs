using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates;

public sealed class CertificateRequest
{
	private readonly AsymmetricAlgorithm _key;

	private readonly X509SignatureGenerator _generator;

	private readonly RSASignaturePadding _rsaPadding;

	public X500DistinguishedName SubjectName { get; }

	public Collection<X509Extension> CertificateExtensions { get; } = new Collection<X509Extension>();


	public PublicKey PublicKey { get; }

	public HashAlgorithmName HashAlgorithm { get; }

	public CertificateRequest(string subjectName, ECDsa key, HashAlgorithmName hashAlgorithm)
	{
		if (subjectName == null)
		{
			throw new ArgumentNullException("subjectName");
		}
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		SubjectName = new X500DistinguishedName(subjectName);
		_key = key;
		_generator = X509SignatureGenerator.CreateForECDsa(key);
		PublicKey = _generator.PublicKey;
		HashAlgorithm = hashAlgorithm;
	}

	public CertificateRequest(X500DistinguishedName subjectName, ECDsa key, HashAlgorithmName hashAlgorithm)
	{
		if (subjectName == null)
		{
			throw new ArgumentNullException("subjectName");
		}
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		SubjectName = subjectName;
		_key = key;
		_generator = X509SignatureGenerator.CreateForECDsa(key);
		PublicKey = _generator.PublicKey;
		HashAlgorithm = hashAlgorithm;
	}

	public CertificateRequest(string subjectName, RSA key, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		if (subjectName == null)
		{
			throw new ArgumentNullException("subjectName");
		}
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		if (padding == null)
		{
			throw new ArgumentNullException("padding");
		}
		SubjectName = new X500DistinguishedName(subjectName);
		_key = key;
		_generator = X509SignatureGenerator.CreateForRSA(key, padding);
		_rsaPadding = padding;
		PublicKey = _generator.PublicKey;
		HashAlgorithm = hashAlgorithm;
	}

	public CertificateRequest(X500DistinguishedName subjectName, RSA key, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		if (subjectName == null)
		{
			throw new ArgumentNullException("subjectName");
		}
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		if (padding == null)
		{
			throw new ArgumentNullException("padding");
		}
		SubjectName = subjectName;
		_key = key;
		_generator = X509SignatureGenerator.CreateForRSA(key, padding);
		_rsaPadding = padding;
		PublicKey = _generator.PublicKey;
		HashAlgorithm = hashAlgorithm;
	}

	public CertificateRequest(X500DistinguishedName subjectName, PublicKey publicKey, HashAlgorithmName hashAlgorithm)
	{
		if (subjectName == null)
		{
			throw new ArgumentNullException("subjectName");
		}
		if (publicKey == null)
		{
			throw new ArgumentNullException("publicKey");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		SubjectName = subjectName;
		PublicKey = publicKey;
		HashAlgorithm = hashAlgorithm;
	}

	public byte[] CreateSigningRequest()
	{
		if (_generator == null)
		{
			throw new InvalidOperationException(System.SR.Cryptography_CertReq_NoKeyProvided);
		}
		return CreateSigningRequest(_generator);
	}

	public byte[] CreateSigningRequest(X509SignatureGenerator signatureGenerator)
	{
		if (signatureGenerator == null)
		{
			throw new ArgumentNullException("signatureGenerator");
		}
		X501Attribute[] attributes = Array.Empty<X501Attribute>();
		if (CertificateExtensions.Count > 0)
		{
			attributes = new X501Attribute[1]
			{
				new Pkcs9ExtensionRequest(CertificateExtensions)
			};
		}
		Pkcs10CertificationRequestInfo pkcs10CertificationRequestInfo = new Pkcs10CertificationRequestInfo(SubjectName, PublicKey, attributes);
		return pkcs10CertificationRequestInfo.ToPkcs10Request(signatureGenerator, HashAlgorithm);
	}

	public X509Certificate2 CreateSelfSigned(DateTimeOffset notBefore, DateTimeOffset notAfter)
	{
		if (notAfter < notBefore)
		{
			throw new ArgumentException(System.SR.Cryptography_CertReq_DatesReversed);
		}
		if (_key == null)
		{
			throw new InvalidOperationException(System.SR.Cryptography_CertReq_NoKeyProvided);
		}
		Span<byte> span = stackalloc byte[8];
		RandomNumberGenerator.Fill(span);
		using (X509Certificate2 certificate = Create(SubjectName, _generator, notBefore, notAfter, span))
		{
			if (_key is RSA privateKey)
			{
				return certificate.CopyWithPrivateKey(privateKey);
			}
			if (_key is ECDsa privateKey2)
			{
				return certificate.CopyWithPrivateKey(privateKey2);
			}
		}
		throw new CryptographicException();
	}

	public X509Certificate2 Create(X509Certificate2 issuerCertificate, DateTimeOffset notBefore, DateTimeOffset notAfter, byte[] serialNumber)
	{
		return Create(issuerCertificate, notBefore, notAfter, new ReadOnlySpan<byte>(serialNumber));
	}

	public X509Certificate2 Create(X509Certificate2 issuerCertificate, DateTimeOffset notBefore, DateTimeOffset notAfter, ReadOnlySpan<byte> serialNumber)
	{
		if (issuerCertificate == null)
		{
			throw new ArgumentNullException("issuerCertificate");
		}
		if (!issuerCertificate.HasPrivateKey)
		{
			throw new ArgumentException(System.SR.Cryptography_CertReq_IssuerRequiresPrivateKey, "issuerCertificate");
		}
		if (notAfter < notBefore)
		{
			throw new ArgumentException(System.SR.Cryptography_CertReq_DatesReversed);
		}
		if (serialNumber.IsEmpty)
		{
			throw new ArgumentException(System.SR.Arg_EmptyOrNullArray, "serialNumber");
		}
		if (issuerCertificate.PublicKey.Oid.Value != PublicKey.Oid.Value)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Cryptography_CertReq_AlgorithmMustMatch, issuerCertificate.PublicKey.Oid.Value, PublicKey.Oid.Value), "issuerCertificate");
		}
		DateTime localDateTime = notBefore.LocalDateTime;
		if (localDateTime < issuerCertificate.NotBefore)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Cryptography_CertReq_NotBeforeNotNested, localDateTime, issuerCertificate.NotBefore), "notBefore");
		}
		DateTime localDateTime2 = notAfter.LocalDateTime;
		long ticks = localDateTime2.Ticks;
		long num = ticks % 10000000;
		ticks -= num;
		localDateTime2 = new DateTime(ticks, localDateTime2.Kind);
		if (localDateTime2 > issuerCertificate.NotAfter)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Cryptography_CertReq_NotAfterNotNested, localDateTime2, issuerCertificate.NotAfter), "notAfter");
		}
		X509BasicConstraintsExtension x509BasicConstraintsExtension = (X509BasicConstraintsExtension)issuerCertificate.Extensions["2.5.29.19"];
		X509KeyUsageExtension x509KeyUsageExtension = (X509KeyUsageExtension)issuerCertificate.Extensions["2.5.29.15"];
		if (x509BasicConstraintsExtension == null)
		{
			throw new ArgumentException(System.SR.Cryptography_CertReq_BasicConstraintsRequired, "issuerCertificate");
		}
		if (!x509BasicConstraintsExtension.CertificateAuthority)
		{
			throw new ArgumentException(System.SR.Cryptography_CertReq_IssuerBasicConstraintsInvalid, "issuerCertificate");
		}
		if (x509KeyUsageExtension != null && (x509KeyUsageExtension.KeyUsages & X509KeyUsageFlags.KeyCertSign) == 0)
		{
			throw new ArgumentException(System.SR.Cryptography_CertReq_IssuerKeyUsageInvalid, "issuerCertificate");
		}
		AsymmetricAlgorithm asymmetricAlgorithm = null;
		string keyAlgorithm = issuerCertificate.GetKeyAlgorithm();
		try
		{
			X509SignatureGenerator generator;
			if (!(keyAlgorithm == "1.2.840.113549.1.1.1"))
			{
				if (!(keyAlgorithm == "1.2.840.10045.2.1"))
				{
					throw new ArgumentException(System.SR.Format(System.SR.Cryptography_UnknownKeyAlgorithm, keyAlgorithm), "issuerCertificate");
				}
				ECDsa eCDsaPrivateKey = issuerCertificate.GetECDsaPrivateKey();
				asymmetricAlgorithm = eCDsaPrivateKey;
				generator = X509SignatureGenerator.CreateForECDsa(eCDsaPrivateKey);
			}
			else
			{
				if (_rsaPadding == null)
				{
					throw new InvalidOperationException(System.SR.Cryptography_CertReq_RSAPaddingRequired);
				}
				RSA rSAPrivateKey = issuerCertificate.GetRSAPrivateKey();
				asymmetricAlgorithm = rSAPrivateKey;
				generator = X509SignatureGenerator.CreateForRSA(rSAPrivateKey, _rsaPadding);
			}
			return Create(issuerCertificate.SubjectName, generator, notBefore, notAfter, serialNumber);
		}
		finally
		{
			asymmetricAlgorithm?.Dispose();
		}
	}

	public X509Certificate2 Create(X500DistinguishedName issuerName, X509SignatureGenerator generator, DateTimeOffset notBefore, DateTimeOffset notAfter, byte[] serialNumber)
	{
		return Create(issuerName, generator, notBefore, notAfter, new ReadOnlySpan<byte>(serialNumber));
	}

	public X509Certificate2 Create(X500DistinguishedName issuerName, X509SignatureGenerator generator, DateTimeOffset notBefore, DateTimeOffset notAfter, ReadOnlySpan<byte> serialNumber)
	{
		if (issuerName == null)
		{
			throw new ArgumentNullException("issuerName");
		}
		if (generator == null)
		{
			throw new ArgumentNullException("generator");
		}
		if (notAfter < notBefore)
		{
			throw new ArgumentException(System.SR.Cryptography_CertReq_DatesReversed);
		}
		if (serialNumber == null || serialNumber.Length < 1)
		{
			throw new ArgumentException(System.SR.Arg_EmptyOrNullArray, "serialNumber");
		}
		byte[] signatureAlgorithmIdentifier = generator.GetSignatureAlgorithmIdentifier(HashAlgorithm);
		System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn signatureAlgorithm = System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(signatureAlgorithmIdentifier, AsnEncodingRules.DER);
		if (signatureAlgorithm.Parameters.HasValue)
		{
			Internal.Cryptography.Helpers.ValidateDer(signatureAlgorithm.Parameters.Value);
		}
		ArraySegment<byte> arraySegment = NormalizeSerialNumber(serialNumber);
		TbsCertificateAsn tbsCertificateAsn = default(TbsCertificateAsn);
		tbsCertificateAsn.Version = 2;
		tbsCertificateAsn.SerialNumber = arraySegment;
		tbsCertificateAsn.SignatureAlgorithm = signatureAlgorithm;
		tbsCertificateAsn.Issuer = issuerName.RawData;
		tbsCertificateAsn.SubjectPublicKeyInfo = new System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn
		{
			Algorithm = new System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn
			{
				Algorithm = PublicKey.Oid.Value,
				Parameters = PublicKey.EncodedParameters.RawData
			},
			SubjectPublicKey = PublicKey.EncodedKeyValue.RawData
		};
		tbsCertificateAsn.Validity = new ValidityAsn(notBefore, notAfter);
		tbsCertificateAsn.Subject = SubjectName.RawData;
		TbsCertificateAsn tbsCertificate = tbsCertificateAsn;
		if (CertificateExtensions.Count > 0)
		{
			HashSet<string> hashSet = new HashSet<string>(CertificateExtensions.Count);
			List<X509ExtensionAsn> list = new List<X509ExtensionAsn>(CertificateExtensions.Count);
			foreach (X509Extension certificateExtension in CertificateExtensions)
			{
				if (certificateExtension != null)
				{
					if (!hashSet.Add(certificateExtension.Oid.Value))
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.Cryptography_CertReq_DuplicateExtension, certificateExtension.Oid.Value));
					}
					list.Add(new X509ExtensionAsn(certificateExtension));
				}
			}
			if (list.Count > 0)
			{
				tbsCertificate.Extensions = list.ToArray();
			}
		}
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		tbsCertificate.Encode(asnWriter);
		byte[] data = asnWriter.Encode();
		asnWriter.Reset();
		CertificateAsn certificateAsn = default(CertificateAsn);
		certificateAsn.TbsCertificate = tbsCertificate;
		certificateAsn.SignatureAlgorithm = signatureAlgorithm;
		certificateAsn.SignatureValue = generator.SignData(data, HashAlgorithm);
		CertificateAsn certificateAsn2 = certificateAsn;
		certificateAsn2.Encode(asnWriter);
		X509Certificate2 result = new X509Certificate2(asnWriter.Encode());
		System.Security.Cryptography.CryptoPool.Return(arraySegment);
		return result;
	}

	private ArraySegment<byte> NormalizeSerialNumber(ReadOnlySpan<byte> serialNumber)
	{
		byte[] array;
		if (serialNumber[0] >= 128)
		{
			array = System.Security.Cryptography.CryptoPool.Rent(serialNumber.Length + 1);
			array[0] = 0;
			serialNumber.CopyTo(array.AsSpan(1));
			return new ArraySegment<byte>(array, 0, serialNumber.Length + 1);
		}
		int i;
		for (i = 0; i < serialNumber.Length - 1 && serialNumber[i] == 0 && serialNumber[i + 1] < 128; i++)
		{
		}
		int num = serialNumber.Length - i;
		array = System.Security.Cryptography.CryptoPool.Rent(num);
		serialNumber.Slice(i).CopyTo(array);
		return new ArraySegment<byte>(array, 0, num);
	}
}
