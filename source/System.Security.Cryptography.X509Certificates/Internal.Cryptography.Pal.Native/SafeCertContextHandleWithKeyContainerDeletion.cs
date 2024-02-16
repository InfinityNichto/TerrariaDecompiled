using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Internal.Cryptography.Pal.Native;

internal sealed class SafeCertContextHandleWithKeyContainerDeletion : SafeCertContextHandle
{
	protected sealed override bool ReleaseHandle()
	{
		using (SafeCertContextHandle pCertContext = global::Interop.crypt32.CertDuplicateCertificateContext(handle))
		{
			DeleteKeyContainer(pCertContext);
		}
		base.ReleaseHandle();
		return true;
	}

	public unsafe static void DeleteKeyContainer(SafeCertContextHandle pCertContext)
	{
		if (pCertContext.IsInvalid)
		{
			return;
		}
		int pcbData = 0;
		if (!global::Interop.crypt32.CertGetCertificateContextProperty(pCertContext, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, null, ref pcbData))
		{
			return;
		}
		byte[] array = new byte[pcbData];
		if (!global::Interop.crypt32.CertGetCertificateContextProperty(pCertContext, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, array, ref pcbData))
		{
			return;
		}
		fixed (byte* ptr = array)
		{
			CRYPT_KEY_PROV_INFO* ptr2 = (CRYPT_KEY_PROV_INFO*)ptr;
			if (ptr2->dwProvType == 0)
			{
				string provider = Marshal.PtrToStringUni((IntPtr)ptr2->pwszProvName);
				string keyName = Marshal.PtrToStringUni((IntPtr)ptr2->pwszContainerName);
				try
				{
					using CngKey cngKey = CngKey.Open(keyName, new CngProvider(provider));
					cngKey.Delete();
				}
				catch (CryptographicException)
				{
				}
			}
			else
			{
				CryptAcquireContextFlags dwFlags = (ptr2->dwFlags & CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET) | CryptAcquireContextFlags.CRYPT_DELETEKEYSET;
				global::Interop.cryptoapi.CryptAcquireContext(out var _, ptr2->pwszContainerName, ptr2->pwszProvName, ptr2->dwProvType, dwFlags);
			}
		}
	}
}
