using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal;

internal static class CertificateExtensionsCommon
{
	public static T GetPublicKey<T>(this X509Certificate2 certificate, Predicate<X509Certificate2> matchesConstraints = null) where T : AsymmetricAlgorithm
	{
		if (certificate == null)
		{
			throw new ArgumentNullException("certificate");
		}
		string expectedOidValue = GetExpectedOidValue<T>();
		PublicKey publicKey = certificate.PublicKey;
		Oid oid = publicKey.Oid;
		if (expectedOidValue != oid.Value)
		{
			return null;
		}
		if (matchesConstraints != null && !matchesConstraints(certificate))
		{
			return null;
		}
		if (typeof(T) == typeof(RSA) || typeof(T) == typeof(DSA))
		{
			byte[] rawData = publicKey.EncodedKeyValue.RawData;
			byte[] rawData2 = publicKey.EncodedParameters.RawData;
			return (T)X509Pal.Instance.DecodePublicKey(oid, rawData, rawData2, certificate.Pal);
		}
		if (typeof(T) == typeof(ECDsa))
		{
			return (T)(AsymmetricAlgorithm)X509Pal.Instance.DecodeECDsaPublicKey(certificate.Pal);
		}
		if (typeof(T) == typeof(ECDiffieHellman))
		{
			return (T)(AsymmetricAlgorithm)X509Pal.Instance.DecodeECDiffieHellmanPublicKey(certificate.Pal);
		}
		throw new NotSupportedException(System.SR.NotSupported_KeyAlgorithm);
	}

	public static T GetPrivateKey<T>(this X509Certificate2 certificate, Predicate<X509Certificate2> matchesConstraints = null) where T : AsymmetricAlgorithm
	{
		if (certificate == null)
		{
			throw new ArgumentNullException("certificate");
		}
		string expectedOidValue = GetExpectedOidValue<T>();
		if (!certificate.HasPrivateKey || expectedOidValue != certificate.PublicKey.Oid.Value)
		{
			return null;
		}
		if (matchesConstraints != null && !matchesConstraints(certificate))
		{
			return null;
		}
		if (typeof(T) == typeof(RSA))
		{
			return (T)(AsymmetricAlgorithm)certificate.Pal.GetRSAPrivateKey();
		}
		if (typeof(T) == typeof(ECDsa))
		{
			return (T)(AsymmetricAlgorithm)certificate.Pal.GetECDsaPrivateKey();
		}
		if (typeof(T) == typeof(DSA))
		{
			return (T)(AsymmetricAlgorithm)certificate.Pal.GetDSAPrivateKey();
		}
		if (typeof(T) == typeof(ECDiffieHellman))
		{
			return (T)(AsymmetricAlgorithm)certificate.Pal.GetECDiffieHellmanPrivateKey();
		}
		throw new NotSupportedException(System.SR.NotSupported_KeyAlgorithm);
	}

	private static string GetExpectedOidValue<T>() where T : AsymmetricAlgorithm
	{
		if (typeof(T) == typeof(RSA))
		{
			return "1.2.840.113549.1.1.1";
		}
		if (typeof(T) == typeof(ECDsa) || typeof(T) == typeof(ECDiffieHellman))
		{
			return "1.2.840.10045.2.1";
		}
		if (typeof(T) == typeof(DSA))
		{
			return "1.2.840.10040.4.1";
		}
		throw new NotSupportedException(System.SR.NotSupported_KeyAlgorithm);
	}
}
