using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security;

internal static class CertificateHelper
{
	internal static X509Certificate2 GetEligibleClientCertificate(X509CertificateCollection candidateCerts)
	{
		if (candidateCerts.Count == 0)
		{
			return null;
		}
		X509Certificate2Collection x509Certificate2Collection = new X509Certificate2Collection();
		x509Certificate2Collection.AddRange(candidateCerts);
		return GetEligibleClientCertificate(x509Certificate2Collection);
	}

	internal static X509Certificate2 GetEligibleClientCertificate(X509Certificate2Collection candidateCerts)
	{
		if (candidateCerts.Count == 0)
		{
			return null;
		}
		foreach (X509Certificate2 candidateCert in candidateCerts)
		{
			if (!candidateCert.HasPrivateKey)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(candidateCerts, $"Skipping current X509Certificate2 {candidateCert.GetHashCode()} since it doesn't have private key. Certificate Subject: {candidateCert.Subject}, Thumbprint: {candidateCert.Thumbprint}.", "GetEligibleClientCertificate");
				}
			}
			else if (IsValidClientCertificate(candidateCert))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(candidateCerts, $"Choosing X509Certificate2 {candidateCert.GetHashCode()} as the Client Certificate. Certificate Subject: {candidateCert.Subject}, Thumbprint: {candidateCert.Thumbprint}.", "GetEligibleClientCertificate");
				}
				return candidateCert;
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(candidateCerts, "No eligible client certificate found.", "GetEligibleClientCertificate");
		}
		return null;
	}

	private static bool IsValidClientCertificate(X509Certificate2 cert)
	{
		foreach (X509Extension extension in cert.Extensions)
		{
			if (extension is X509EnhancedKeyUsageExtension x509EnhancedKeyUsageExtension && !IsValidForClientAuthenticationEKU(x509EnhancedKeyUsageExtension))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(cert, $"For Certificate {cert.GetHashCode()} - current X509EnhancedKeyUsageExtension {x509EnhancedKeyUsageExtension.GetHashCode()} is not valid for Client Authentication.", "IsValidClientCertificate");
				}
				return false;
			}
			if (extension is X509KeyUsageExtension x509KeyUsageExtension && !IsValidForDigitalSignatureUsage(x509KeyUsageExtension))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(cert, $"For Certificate {cert.GetHashCode()} - current X509KeyUsageExtension {x509KeyUsageExtension.GetHashCode()} is not valid for Digital Signature.", "IsValidClientCertificate");
				}
				return false;
			}
		}
		return true;
	}

	private static bool IsValidForClientAuthenticationEKU(X509EnhancedKeyUsageExtension eku)
	{
		OidEnumerator enumerator = eku.EnhancedKeyUsages.GetEnumerator();
		while (enumerator.MoveNext())
		{
			Oid current = enumerator.Current;
			if (current.Value == "1.3.6.1.5.5.7.3.2")
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsValidForDigitalSignatureUsage(X509KeyUsageExtension ku)
	{
		return (ku.KeyUsages & X509KeyUsageFlags.DigitalSignature) == X509KeyUsageFlags.DigitalSignature;
	}

	internal static X509Certificate2 GetEligibleClientCertificate()
	{
		X509Certificate2Collection certificates;
		using (X509Store x509Store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
		{
			x509Store.Open(OpenFlags.OpenExistingOnly);
			certificates = x509Store.Certificates;
		}
		return GetEligibleClientCertificate(certificates);
	}
}
