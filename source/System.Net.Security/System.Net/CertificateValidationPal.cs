using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace System.Net;

internal static class CertificateValidationPal
{
	private static readonly object s_syncObject = new object();

	private static volatile X509Store s_myCertStoreEx;

	private static volatile X509Store s_myMachineCertStoreEx;

	internal static X509Store EnsureStoreOpened(bool isMachineStore)
	{
		X509Store x509Store = (isMachineStore ? s_myMachineCertStoreEx : s_myCertStoreEx);
		if (x509Store == null)
		{
			StoreLocation storeLocation = ((!isMachineStore) ? StoreLocation.CurrentUser : StoreLocation.LocalMachine);
			if (1 == 0)
			{
				return null;
			}
			lock (s_syncObject)
			{
				x509Store = (isMachineStore ? s_myMachineCertStoreEx : s_myCertStoreEx);
				if (x509Store == null)
				{
					try
					{
						x509Store = OpenStore(storeLocation);
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(null, $"storeLocation: {storeLocation} returned store {x509Store}", "EnsureStoreOpened");
						}
						if (isMachineStore)
						{
							s_myMachineCertStoreEx = x509Store;
						}
						else
						{
							s_myCertStoreEx = x509Store;
						}
					}
					catch (Exception ex)
					{
						if (ex is CryptographicException || ex is SecurityException)
						{
							return null;
						}
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Error(null, System.SR.Format(System.SR.net_log_open_store_failed, storeLocation, ex), "EnsureStoreOpened");
						}
						throw;
					}
				}
			}
		}
		return x509Store;
	}

	internal static SslPolicyErrors VerifyCertificateProperties(SafeDeleteContext securityContext, X509Chain chain, X509Certificate2 remoteCertificate, bool checkCertName, bool isServer, string hostName)
	{
		return CertificateValidation.BuildChainAndVerifyProperties(chain, remoteCertificate, checkCertName, isServer, hostName);
	}

	internal static X509Certificate2 GetRemoteCertificate(SafeDeleteContext securityContext)
	{
		X509Certificate2Collection remoteCertificateCollection;
		return GetRemoteCertificate(securityContext, retrieveCollection: false, out remoteCertificateCollection);
	}

	internal static X509Certificate2 GetRemoteCertificate(SafeDeleteContext securityContext, out X509Certificate2Collection remoteCertificateCollection)
	{
		return GetRemoteCertificate(securityContext, retrieveCollection: true, out remoteCertificateCollection);
	}

	private static X509Certificate2 GetRemoteCertificate(SafeDeleteContext securityContext, bool retrieveCollection, out X509Certificate2Collection remoteCertificateCollection)
	{
		remoteCertificateCollection = null;
		if (securityContext == null)
		{
			return null;
		}
		X509Certificate2 x509Certificate = null;
		SafeFreeCertContext safeFreeCertContext = null;
		try
		{
			safeFreeCertContext = SSPIWrapper.QueryContextAttributes_SECPKG_ATTR_REMOTE_CERT_CONTEXT(GlobalSSPI.SSPISecureChannel, securityContext);
			if (safeFreeCertContext != null && !safeFreeCertContext.IsInvalid)
			{
				x509Certificate = new X509Certificate2(safeFreeCertContext.DangerousGetHandle());
			}
		}
		finally
		{
			if (safeFreeCertContext != null && !safeFreeCertContext.IsInvalid)
			{
				if (retrieveCollection)
				{
					remoteCertificateCollection = UnmanagedCertificateContext.GetRemoteCertificatesFromStoreContext(safeFreeCertContext);
				}
				safeFreeCertContext.Dispose();
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Log.RemoteCertificate(x509Certificate);
		}
		return x509Certificate;
	}

	internal unsafe static string[] GetRequestCertificateAuthorities(SafeDeleteContext securityContext)
	{
		global::Interop.SspiCli.SecPkgContext_IssuerListInfoEx ctx = default(global::Interop.SspiCli.SecPkgContext_IssuerListInfoEx);
		SafeHandle sspiHandle;
		bool flag = SSPIWrapper.QueryContextAttributes_SECPKG_ATTR_ISSUER_LIST_EX(GlobalSSPI.SSPISecureChannel, securityContext, ref ctx, out sspiHandle);
		string[] array = Array.Empty<string>();
		try
		{
			if (flag && ctx.cIssuers != 0)
			{
				array = new string[ctx.cIssuers];
				Span<global::Interop.SspiCli.CERT_CHAIN_ELEMENT> span = new Span<global::Interop.SspiCli.CERT_CHAIN_ELEMENT>((void*)sspiHandle.DangerousGetHandle(), array.Length);
				for (int i = 0; i < span.Length; i++)
				{
					if (span[i].cbSize != 0)
					{
						byte[] encodedDistinguishedName = new Span<byte>((void*)span[i].pCertContext, checked((int)span[i].cbSize)).ToArray();
						X500DistinguishedName x500DistinguishedName = new X500DistinguishedName(encodedDistinguishedName);
						array[i] = x500DistinguishedName.Name;
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(securityContext, $"IssuerListEx[{array[i]}]", "GetRequestCertificateAuthorities");
						}
					}
				}
			}
		}
		finally
		{
			sspiHandle?.Dispose();
		}
		return array;
	}

	internal static X509Store OpenStore(StoreLocation storeLocation)
	{
		X509Store store = new X509Store(StoreName.My, storeLocation);
		try
		{
			WindowsIdentity.RunImpersonated(SafeAccessTokenHandle.InvalidHandle, delegate
			{
				store.Open(OpenFlags.OpenExistingOnly);
			});
		}
		catch
		{
			throw;
		}
		return store;
	}
}
