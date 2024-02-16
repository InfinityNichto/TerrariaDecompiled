using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace System.Net;

internal static class UnmanagedCertificateContext
{
	internal static X509Certificate2Collection GetRemoteCertificatesFromStoreContext(SafeFreeCertContext certContext)
	{
		if (certContext.IsInvalid)
		{
			return new X509Certificate2Collection();
		}
		return GetRemoteCertificatesFromStoreContext(certContext.DangerousGetHandle());
	}

	internal unsafe static X509Certificate2Collection GetRemoteCertificatesFromStoreContext(IntPtr certContext)
	{
		X509Certificate2Collection x509Certificate2Collection = new X509Certificate2Collection();
		if (certContext == IntPtr.Zero)
		{
			return x509Certificate2Collection;
		}
		global::Interop.Crypt32.CERT_CONTEXT cERT_CONTEXT = *(global::Interop.Crypt32.CERT_CONTEXT*)(void*)certContext;
		if (cERT_CONTEXT.hCertStore != IntPtr.Zero)
		{
			global::Interop.Crypt32.CERT_CONTEXT* pPrevCertContext = null;
			while (true)
			{
				global::Interop.Crypt32.CERT_CONTEXT* ptr = global::Interop.Crypt32.CertEnumCertificatesInStore(cERT_CONTEXT.hCertStore, pPrevCertContext);
				if (ptr == null)
				{
					break;
				}
				X509Certificate2 x509Certificate = new X509Certificate2(new IntPtr(ptr));
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(certContext, $"Adding remote certificate:{x509Certificate}", "GetRemoteCertificatesFromStoreContext");
				}
				x509Certificate2Collection.Add(x509Certificate);
				pPrevCertContext = ptr;
			}
		}
		return x509Certificate2Collection;
	}
}
