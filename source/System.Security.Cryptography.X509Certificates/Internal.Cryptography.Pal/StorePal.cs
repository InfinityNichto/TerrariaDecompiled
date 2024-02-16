using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography.Pal.Native;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal;

internal sealed class StorePal : IDisposable, IStorePal, IExportPal, ILoaderPal
{
	private SafeCertStoreHandle _certStore;

	internal SafeCertStoreHandle SafeCertStoreHandle => _certStore;

	SafeHandle IStorePal.SafeHandle
	{
		get
		{
			if (_certStore == null || _certStore.IsInvalid || _certStore.IsClosed)
			{
				throw new CryptographicException(System.SR.Cryptography_X509_StoreNotOpen);
			}
			return _certStore;
		}
	}

	public static IStorePal FromHandle(IntPtr storeHandle)
	{
		if (storeHandle == IntPtr.Zero)
		{
			throw new ArgumentNullException("storeHandle");
		}
		SafeCertStoreHandle safeCertStoreHandle = global::Interop.crypt32.CertDuplicateStore(storeHandle);
		if (safeCertStoreHandle == null || safeCertStoreHandle.IsInvalid)
		{
			throw new CryptographicException(System.SR.Cryptography_InvalidStoreHandle, "storeHandle");
		}
		return new StorePal(safeCertStoreHandle);
	}

	public void CloneTo(X509Certificate2Collection collection)
	{
		CopyTo(collection);
	}

	public void CopyTo(X509Certificate2Collection collection)
	{
		SafeCertContextHandle pCertContext = null;
		while (global::Interop.crypt32.CertEnumCertificatesInStore(_certStore, ref pCertContext))
		{
			X509Certificate2 certificate = new X509Certificate2(pCertContext.DangerousGetHandle());
			collection.Add(certificate);
		}
	}

	public void Add(ICertificatePal certificate)
	{
		if (!global::Interop.crypt32.CertAddCertificateContextToStore(_certStore, ((CertificatePal)certificate).CertContext, CertStoreAddDisposition.CERT_STORE_ADD_REPLACE_EXISTING_INHERIT_PROPERTIES, IntPtr.Zero))
		{
			throw Marshal.GetLastWin32Error().ToCryptographicException();
		}
	}

	public unsafe void Remove(ICertificatePal certificate)
	{
		SafeCertContextHandle certContext = ((CertificatePal)certificate).CertContext;
		SafeCertContextHandle pCertContext = null;
		CERT_CONTEXT* certContext2 = certContext.CertContext;
		if (global::Interop.crypt32.CertFindCertificateInStore(_certStore, CertFindType.CERT_FIND_EXISTING, certContext2, ref pCertContext))
		{
			CERT_CONTEXT* pCertContext2 = pCertContext.Disconnect();
			if (!global::Interop.crypt32.CertDeleteCertificateFromStore(pCertContext2))
			{
				throw Marshal.GetLastWin32Error().ToCryptographicException();
			}
			GC.KeepAlive(certContext);
		}
	}

	public void Dispose()
	{
		SafeCertStoreHandle certStore = _certStore;
		_certStore = null;
		certStore?.Dispose();
	}

	internal StorePal(SafeCertStoreHandle certStore)
	{
		_certStore = certStore;
	}

	public void MoveTo(X509Certificate2Collection collection)
	{
		CopyTo(collection);
		Dispose();
	}

	public unsafe byte[] Export(X509ContentType contentType, SafePasswordHandle password)
	{
		switch (contentType)
		{
		case X509ContentType.Cert:
		{
			SafeCertContextHandle pCertContext = null;
			if (!global::Interop.crypt32.CertEnumCertificatesInStore(_certStore, ref pCertContext))
			{
				return null;
			}
			try
			{
				byte[] array2 = new byte[pCertContext.CertContext->cbCertEncoded];
				Marshal.Copy((IntPtr)pCertContext.CertContext->pbCertEncoded, array2, 0, array2.Length);
				GC.KeepAlive(pCertContext);
				return array2;
			}
			finally
			{
				pCertContext.Dispose();
			}
		}
		case X509ContentType.SerializedCert:
		{
			SafeCertContextHandle pCertContext2 = null;
			if (!global::Interop.crypt32.CertEnumCertificatesInStore(_certStore, ref pCertContext2))
			{
				return null;
			}
			try
			{
				int pcbElement = 0;
				if (!global::Interop.crypt32.CertSerializeCertificateStoreElement(pCertContext2, 0, null, ref pcbElement))
				{
					throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
				}
				byte[] array3 = new byte[pcbElement];
				if (!global::Interop.crypt32.CertSerializeCertificateStoreElement(pCertContext2, 0, array3, ref pcbElement))
				{
					throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
				}
				return array3;
			}
			finally
			{
				pCertContext2.Dispose();
			}
		}
		case X509ContentType.Pfx:
		{
			CRYPTOAPI_BLOB pPFX = new CRYPTOAPI_BLOB(0, null);
			if (!global::Interop.crypt32.PFXExportCertStore(_certStore, ref pPFX, password, PFXExportFlags.REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY | PFXExportFlags.EXPORT_PRIVATE_KEYS))
			{
				throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
			}
			byte[] array = new byte[pPFX.cbData];
			fixed (byte* pbData = array)
			{
				pPFX.pbData = pbData;
				if (!global::Interop.crypt32.PFXExportCertStore(_certStore, ref pPFX, password, PFXExportFlags.REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY | PFXExportFlags.EXPORT_PRIVATE_KEYS))
				{
					throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
				}
			}
			return array;
		}
		case X509ContentType.SerializedStore:
			return SaveToMemoryStore(CertStoreSaveAs.CERT_STORE_SAVE_AS_STORE);
		case X509ContentType.Pkcs7:
			return SaveToMemoryStore(CertStoreSaveAs.CERT_STORE_SAVE_AS_PKCS7);
		default:
			throw new CryptographicException(System.SR.Cryptography_X509_InvalidContentType);
		}
	}

	private unsafe byte[] SaveToMemoryStore(CertStoreSaveAs dwSaveAs)
	{
		CRYPTOAPI_BLOB pvSaveToPara = new CRYPTOAPI_BLOB(0, null);
		if (!global::Interop.crypt32.CertSaveStore(_certStore, CertEncodingType.All, dwSaveAs, CertStoreSaveTo.CERT_STORE_SAVE_TO_MEMORY, ref pvSaveToPara, 0))
		{
			throw Marshal.GetLastWin32Error().ToCryptographicException();
		}
		byte[] array = new byte[pvSaveToPara.cbData];
		fixed (byte* pbData = array)
		{
			pvSaveToPara.pbData = pbData;
			if (!global::Interop.crypt32.CertSaveStore(_certStore, CertEncodingType.All, dwSaveAs, CertStoreSaveTo.CERT_STORE_SAVE_TO_MEMORY, ref pvSaveToPara, 0))
			{
				throw Marshal.GetLastWin32Error().ToCryptographicException();
			}
		}
		return array;
	}

	public static ILoaderPal FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
	{
		return FromBlobOrFile(rawData, null, password, keyStorageFlags);
	}

	public static ILoaderPal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
	{
		return FromBlobOrFile(null, fileName, password, keyStorageFlags);
	}

	private unsafe static StorePal FromBlobOrFile(ReadOnlySpan<byte> rawData, string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
	{
		bool flag = fileName != null;
		fixed (byte* pbData = rawData)
		{
			fixed (char* ptr = fileName)
			{
				CRYPTOAPI_BLOB cRYPTOAPI_BLOB = new CRYPTOAPI_BLOB((!flag) ? rawData.Length : 0, pbData);
				bool flag2 = (keyStorageFlags & X509KeyStorageFlags.PersistKeySet) != 0;
				PfxCertStoreFlags dwFlags = MapKeyStorageFlags(keyStorageFlags);
				void* pvObject = (flag ? ((void*)ptr) : ((void*)(&cRYPTOAPI_BLOB)));
				if (!global::Interop.crypt32.CryptQueryObject(flag ? CertQueryObjectType.CERT_QUERY_OBJECT_FILE : CertQueryObjectType.CERT_QUERY_OBJECT_BLOB, pvObject, ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_CERT | ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_SERIALIZED_STORE | ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT | ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED | ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_UNSIGNED | ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED | ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PFX, ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_ALL, 0, IntPtr.Zero, out var pdwContentType, IntPtr.Zero, out var phCertStore, IntPtr.Zero, IntPtr.Zero))
				{
					throw Marshal.GetLastWin32Error().ToCryptographicException();
				}
				if (pdwContentType == ContentType.CERT_QUERY_CONTENT_PFX)
				{
					phCertStore.Dispose();
					if (flag)
					{
						rawData = File.ReadAllBytes(fileName);
					}
					fixed (byte* pbData2 = rawData)
					{
						CRYPTOAPI_BLOB pPFX = new CRYPTOAPI_BLOB(rawData.Length, pbData2);
						phCertStore = global::Interop.crypt32.PFXImportCertStore(ref pPFX, password, dwFlags);
						if (phCertStore == null || phCertStore.IsInvalid)
						{
							throw Marshal.GetLastWin32Error().ToCryptographicException();
						}
					}
					if (!flag2)
					{
						SafeCertContextHandle pCertContext = null;
						while (global::Interop.crypt32.CertEnumCertificatesInStore(phCertStore, ref pCertContext))
						{
							CRYPTOAPI_BLOB cRYPTOAPI_BLOB2 = new CRYPTOAPI_BLOB(0, null);
							if (!global::Interop.crypt32.CertSetCertificateContextProperty(pCertContext, CertContextPropId.CERT_CLR_DELETE_KEY_PROP_ID, CertSetPropertyFlags.CERT_SET_PROPERTY_INHIBIT_PERSIST_FLAG, &cRYPTOAPI_BLOB2))
							{
								throw Marshal.GetLastWin32Error().ToCryptographicException();
							}
						}
					}
				}
				return new StorePal(phCertStore);
			}
		}
	}

	public static IExportPal FromCertificate(ICertificatePalCore cert)
	{
		CertificatePal certificatePal = (CertificatePal)cert;
		SafeCertStoreHandle safeCertStoreHandle = global::Interop.crypt32.CertOpenStore(CertStoreProvider.CERT_STORE_PROV_MEMORY, CertEncodingType.All, IntPtr.Zero, CertStoreFlags.CERT_STORE_DEFER_CLOSE_UNTIL_LAST_FREE_FLAG | CertStoreFlags.CERT_STORE_ENUM_ARCHIVED_FLAG | CertStoreFlags.CERT_STORE_CREATE_NEW_FLAG, null);
		if (safeCertStoreHandle.IsInvalid)
		{
			throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
		}
		if (!global::Interop.crypt32.CertAddCertificateLinkToStore(safeCertStoreHandle, certificatePal.CertContext, CertStoreAddDisposition.CERT_STORE_ADD_ALWAYS, IntPtr.Zero))
		{
			throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
		}
		return new StorePal(safeCertStoreHandle);
	}

	public static IExportPal LinkFromCertificateCollection(X509Certificate2Collection certificates)
	{
		SafeCertStoreHandle safeCertStoreHandle = global::Interop.crypt32.CertOpenStore(CertStoreProvider.CERT_STORE_PROV_MEMORY, CertEncodingType.All, IntPtr.Zero, CertStoreFlags.CERT_STORE_ENUM_ARCHIVED_FLAG | CertStoreFlags.CERT_STORE_CREATE_NEW_FLAG, null);
		if (safeCertStoreHandle.IsInvalid)
		{
			throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
		}
		for (int i = 0; i < certificates.Count; i++)
		{
			SafeCertContextHandle certContext = ((CertificatePal)certificates[i].Pal).CertContext;
			if (!global::Interop.crypt32.CertAddCertificateLinkToStore(safeCertStoreHandle, certContext, CertStoreAddDisposition.CERT_STORE_ADD_ALWAYS, IntPtr.Zero))
			{
				throw Marshal.GetLastWin32Error().ToCryptographicException();
			}
		}
		return new StorePal(safeCertStoreHandle);
	}

	public static IStorePal FromSystemStore(string storeName, StoreLocation storeLocation, OpenFlags openFlags)
	{
		CertStoreFlags dwFlags = MapX509StoreFlags(storeLocation, openFlags);
		SafeCertStoreHandle safeCertStoreHandle = global::Interop.crypt32.CertOpenStore(CertStoreProvider.CERT_STORE_PROV_SYSTEM_W, CertEncodingType.All, IntPtr.Zero, dwFlags, storeName);
		if (safeCertStoreHandle.IsInvalid)
		{
			throw Marshal.GetLastWin32Error().ToCryptographicException();
		}
		global::Interop.crypt32.CertControlStore(safeCertStoreHandle, CertControlStoreFlags.None, CertControlStoreType.CERT_STORE_CTRL_AUTO_RESYNC, IntPtr.Zero);
		return new StorePal(safeCertStoreHandle);
	}

	private static PfxCertStoreFlags MapKeyStorageFlags(X509KeyStorageFlags keyStorageFlags)
	{
		PfxCertStoreFlags pfxCertStoreFlags = PfxCertStoreFlags.None;
		if ((keyStorageFlags & X509KeyStorageFlags.UserKeySet) == X509KeyStorageFlags.UserKeySet)
		{
			pfxCertStoreFlags |= PfxCertStoreFlags.CRYPT_USER_KEYSET;
		}
		else if ((keyStorageFlags & X509KeyStorageFlags.MachineKeySet) == X509KeyStorageFlags.MachineKeySet)
		{
			pfxCertStoreFlags |= PfxCertStoreFlags.CRYPT_MACHINE_KEYSET;
		}
		if ((keyStorageFlags & X509KeyStorageFlags.Exportable) == X509KeyStorageFlags.Exportable)
		{
			pfxCertStoreFlags |= PfxCertStoreFlags.CRYPT_EXPORTABLE;
		}
		if ((keyStorageFlags & X509KeyStorageFlags.UserProtected) == X509KeyStorageFlags.UserProtected)
		{
			pfxCertStoreFlags |= PfxCertStoreFlags.CRYPT_USER_PROTECTED;
		}
		if ((keyStorageFlags & X509KeyStorageFlags.EphemeralKeySet) == X509KeyStorageFlags.EphemeralKeySet)
		{
			pfxCertStoreFlags |= PfxCertStoreFlags.PKCS12_ALWAYS_CNG_KSP | PfxCertStoreFlags.PKCS12_NO_PERSIST_KEY;
		}
		return pfxCertStoreFlags;
	}

	private static CertStoreFlags MapX509StoreFlags(StoreLocation storeLocation, OpenFlags flags)
	{
		CertStoreFlags certStoreFlags = CertStoreFlags.None;
		switch ((uint)(flags & (OpenFlags.ReadWrite | OpenFlags.MaxAllowed)))
		{
		case 0u:
			certStoreFlags |= CertStoreFlags.CERT_STORE_READONLY_FLAG;
			break;
		case 2u:
			certStoreFlags |= CertStoreFlags.CERT_STORE_MAXIMUM_ALLOWED_FLAG;
			break;
		}
		if ((flags & OpenFlags.OpenExistingOnly) == OpenFlags.OpenExistingOnly)
		{
			certStoreFlags |= CertStoreFlags.CERT_STORE_OPEN_EXISTING_FLAG;
		}
		if ((flags & OpenFlags.IncludeArchived) == OpenFlags.IncludeArchived)
		{
			certStoreFlags |= CertStoreFlags.CERT_STORE_ENUM_ARCHIVED_FLAG;
		}
		switch (storeLocation)
		{
		case StoreLocation.LocalMachine:
			certStoreFlags |= CertStoreFlags.CERT_SYSTEM_STORE_LOCAL_MACHINE;
			break;
		case StoreLocation.CurrentUser:
			certStoreFlags |= CertStoreFlags.CERT_SYSTEM_STORE_CURRENT_USER;
			break;
		}
		return certStoreFlags;
	}
}
