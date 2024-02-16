using Internal.Cryptography;
using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates;

public static class ECDsaCertificateExtensions
{
	public static ECDsa? GetECDsaPublicKey(this X509Certificate2 certificate)
	{
		return certificate.GetPublicKey<ECDsa>((X509Certificate2 cert) => HasECDsaKeyUsage(cert));
	}

	public static ECDsa? GetECDsaPrivateKey(this X509Certificate2 certificate)
	{
		return certificate.GetPrivateKey<ECDsa>((X509Certificate2 cert) => HasECDsaKeyUsage(cert));
	}

	public static X509Certificate2 CopyWithPrivateKey(this X509Certificate2 certificate, ECDsa privateKey)
	{
		if (certificate == null)
		{
			throw new ArgumentNullException("certificate");
		}
		if (privateKey == null)
		{
			throw new ArgumentNullException("privateKey");
		}
		if (certificate.HasPrivateKey)
		{
			throw new InvalidOperationException(System.SR.Cryptography_Cert_AlreadyHasPrivateKey);
		}
		using (ECDsa eCDsa = certificate.GetECDsaPublicKey())
		{
			if (eCDsa == null)
			{
				throw new ArgumentException(System.SR.Cryptography_PrivateKey_WrongAlgorithm);
			}
			if (!Internal.Cryptography.Helpers.AreSamePublicECParameters(eCDsa.ExportParameters(includePrivateParameters: false), privateKey.ExportParameters(includePrivateParameters: false)))
			{
				throw new ArgumentException(System.SR.Cryptography_PrivateKey_DoesNotMatch, "privateKey");
			}
		}
		ICertificatePal pal = certificate.Pal.CopyWithPrivateKey(privateKey);
		return new X509Certificate2(pal);
	}

	private static bool HasECDsaKeyUsage(X509Certificate2 certificate)
	{
		foreach (X509Extension extension in certificate.Extensions)
		{
			if (extension.Oid.Value == "2.5.29.15")
			{
				X509KeyUsageExtension x509KeyUsageExtension = (X509KeyUsageExtension)extension;
				if ((x509KeyUsageExtension.KeyUsages & X509KeyUsageFlags.KeyAgreement) == 0)
				{
					return true;
				}
				return (x509KeyUsageExtension.KeyUsages & (X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.NonRepudiation | X509KeyUsageFlags.DigitalSignature)) != 0;
			}
		}
		return true;
	}
}
