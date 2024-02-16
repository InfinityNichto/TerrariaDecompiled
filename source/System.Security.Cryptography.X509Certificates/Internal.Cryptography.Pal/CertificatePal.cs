using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Internal.Cryptography.Pal.Native;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal;

internal sealed class CertificatePal : IDisposable, ICertificatePal, ICertificatePalCore
{
	private SafeCertContextHandle _certContext;

	public IntPtr Handle => _certContext.DangerousGetHandle();

	public string Issuer => GetIssuerOrSubject(issuer: true, reverse: true);

	public string Subject => GetIssuerOrSubject(issuer: false, reverse: true);

	public string LegacyIssuer => GetIssuerOrSubject(issuer: true, reverse: false);

	public string LegacySubject => GetIssuerOrSubject(issuer: false, reverse: false);

	public byte[] Thumbprint
	{
		get
		{
			int pcbData = 0;
			if (!global::Interop.crypt32.CertGetCertificateContextProperty(_certContext, CertContextPropId.CERT_SHA1_HASH_PROP_ID, null, ref pcbData))
			{
				throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
			}
			byte[] array = new byte[pcbData];
			if (!global::Interop.crypt32.CertGetCertificateContextProperty(_certContext, CertContextPropId.CERT_SHA1_HASH_PROP_ID, array, ref pcbData))
			{
				throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
			}
			return array;
		}
	}

	public unsafe string KeyAlgorithm
	{
		get
		{
			CERT_CONTEXT* certContext = _certContext.CertContext;
			string result = Marshal.PtrToStringAnsi(certContext->pCertInfo->SubjectPublicKeyInfo.Algorithm.pszObjId);
			GC.KeepAlive(this);
			return result;
		}
	}

	public unsafe byte[] KeyAlgorithmParameters
	{
		get
		{
			CERT_CONTEXT* certContext = _certContext.CertContext;
			string text = Marshal.PtrToStringAnsi(certContext->pCertInfo->SubjectPublicKeyInfo.Algorithm.pszObjId);
			int num = ((!(text == "1.2.840.113549.1.1.1")) ? global::Interop.Crypt32.FindOidInfo(global::Interop.Crypt32.CryptOidInfoKeyType.CRYPT_OID_INFO_OID_KEY, text, OidGroup.PublicKeyAlgorithm, fallBackToAllGroups: true).AlgId : 41984);
			byte* ptr = (byte*)5;
			byte[] result = ((num != 8704 || certContext->pCertInfo->SubjectPublicKeyInfo.Algorithm.Parameters.cbData != 0 || certContext->pCertInfo->SubjectPublicKeyInfo.Algorithm.Parameters.pbData != ptr) ? certContext->pCertInfo->SubjectPublicKeyInfo.Algorithm.Parameters.ToByteArray() : PropagateKeyAlgorithmParametersFromChain());
			GC.KeepAlive(this);
			return result;
		}
	}

	public unsafe byte[] PublicKeyValue
	{
		get
		{
			CERT_CONTEXT* certContext = _certContext.CertContext;
			byte[] result = certContext->pCertInfo->SubjectPublicKeyInfo.PublicKey.ToByteArray();
			GC.KeepAlive(this);
			return result;
		}
	}

	public unsafe byte[] SerialNumber
	{
		get
		{
			CERT_CONTEXT* certContext = _certContext.CertContext;
			byte[] array = certContext->pCertInfo->SerialNumber.ToByteArray();
			Array.Reverse(array);
			GC.KeepAlive(this);
			return array;
		}
	}

	public unsafe string SignatureAlgorithm
	{
		get
		{
			CERT_CONTEXT* certContext = _certContext.CertContext;
			string result = Marshal.PtrToStringAnsi(certContext->pCertInfo->SignatureAlgorithm.pszObjId);
			GC.KeepAlive(this);
			return result;
		}
	}

	public unsafe DateTime NotAfter
	{
		get
		{
			CERT_CONTEXT* certContext = _certContext.CertContext;
			DateTime result = certContext->pCertInfo->NotAfter.ToDateTime();
			GC.KeepAlive(this);
			return result;
		}
	}

	public unsafe DateTime NotBefore
	{
		get
		{
			CERT_CONTEXT* certContext = _certContext.CertContext;
			DateTime result = certContext->pCertInfo->NotBefore.ToDateTime();
			GC.KeepAlive(this);
			return result;
		}
	}

	public unsafe byte[] RawData
	{
		get
		{
			CERT_CONTEXT* certContext = _certContext.CertContext;
			byte[] result = new Span<byte>(certContext->pbCertEncoded, certContext->cbCertEncoded).ToArray();
			GC.KeepAlive(this);
			return result;
		}
	}

	public unsafe int Version
	{
		get
		{
			CERT_CONTEXT* certContext = _certContext.CertContext;
			int result = certContext->pCertInfo->dwVersion + 1;
			GC.KeepAlive(this);
			return result;
		}
	}

	public unsafe bool Archived
	{
		get
		{
			int pcbData = 0;
			return global::Interop.crypt32.CertGetCertificateContextProperty(_certContext, CertContextPropId.CERT_ARCHIVED_PROP_ID, null, ref pcbData);
		}
		set
		{
			CRYPTOAPI_BLOB cRYPTOAPI_BLOB = new CRYPTOAPI_BLOB(0, null);
			CRYPTOAPI_BLOB* pvData = (value ? (&cRYPTOAPI_BLOB) : null);
			if (!global::Interop.crypt32.CertSetCertificateContextProperty(_certContext, CertContextPropId.CERT_ARCHIVED_PROP_ID, CertSetPropertyFlags.None, pvData))
			{
				throw Marshal.GetLastWin32Error().ToCryptographicException();
			}
		}
	}

	public unsafe string FriendlyName
	{
		get
		{
			int pcbData = 0;
			if (!global::Interop.crypt32.CertGetCertificateContextPropertyString(_certContext, CertContextPropId.CERT_FRIENDLY_NAME_PROP_ID, null, ref pcbData))
			{
				return string.Empty;
			}
			int num = (pcbData + 1) / 2;
			Span<char> span = ((num > 256) ? ((Span<char>)new char[num]) : stackalloc char[num]);
			Span<char> span2 = span;
			fixed (char* pvData = &MemoryMarshal.GetReference(span2))
			{
				if (!global::Interop.crypt32.CertGetCertificateContextPropertyString(_certContext, CertContextPropId.CERT_FRIENDLY_NAME_PROP_ID, (byte*)pvData, ref pcbData))
				{
					return string.Empty;
				}
			}
			return new string(span2.Slice(0, pcbData / 2 - 1));
		}
		set
		{
			string text = ((value == null) ? string.Empty : value);
			IntPtr intPtr = Marshal.StringToHGlobalUni(text);
			try
			{
				CRYPTOAPI_BLOB cRYPTOAPI_BLOB = new CRYPTOAPI_BLOB(checked(2 * (text.Length + 1)), (byte*)(void*)intPtr);
				if (!global::Interop.crypt32.CertSetCertificateContextProperty(_certContext, CertContextPropId.CERT_FRIENDLY_NAME_PROP_ID, CertSetPropertyFlags.None, &cRYPTOAPI_BLOB))
				{
					throw Marshal.GetLastWin32Error().ToCryptographicException();
				}
			}
			finally
			{
				Marshal.FreeHGlobal(intPtr);
			}
		}
	}

	public unsafe X500DistinguishedName SubjectName
	{
		get
		{
			byte[] encodedDistinguishedName = _certContext.CertContext->pCertInfo->Subject.ToByteArray();
			X500DistinguishedName result = new X500DistinguishedName(encodedDistinguishedName);
			GC.KeepAlive(this);
			return result;
		}
	}

	public unsafe X500DistinguishedName IssuerName
	{
		get
		{
			byte[] encodedDistinguishedName = _certContext.CertContext->pCertInfo->Issuer.ToByteArray();
			X500DistinguishedName result = new X500DistinguishedName(encodedDistinguishedName);
			GC.KeepAlive(this);
			return result;
		}
	}

	public unsafe IEnumerable<X509Extension> Extensions
	{
		get
		{
			CERT_INFO* pCertInfo = _certContext.CertContext->pCertInfo;
			int cExtension = pCertInfo->cExtension;
			X509Extension[] array = new X509Extension[cExtension];
			for (int i = 0; i < cExtension; i++)
			{
				CERT_EXTENSION* ptr = pCertInfo->rgExtension + i;
				string value = Marshal.PtrToStringAnsi(ptr->pszObjId);
				Oid oid = new Oid(value, null);
				bool critical = ptr->fCritical != 0;
				byte[] rawData = ptr->Value.ToByteArray();
				array[i] = new X509Extension(oid, rawData, critical);
			}
			GC.KeepAlive(this);
			return array;
		}
	}

	internal SafeCertContextHandle CertContext
	{
		get
		{
			SafeCertContextHandle result = global::Interop.crypt32.CertDuplicateCertificateContext(_certContext.DangerousGetHandle());
			GC.KeepAlive(_certContext);
			return result;
		}
	}

	public bool HasPrivateKey => _certContext.ContainsPrivateKey;

	public static ICertificatePal FromHandle(IntPtr handle)
	{
		if (handle == IntPtr.Zero)
		{
			throw new ArgumentException(System.SR.Arg_InvalidHandle, "handle");
		}
		SafeCertContextHandle safeCertContextHandle = global::Interop.crypt32.CertDuplicateCertificateContext(handle);
		if (safeCertContextHandle.IsInvalid)
		{
			throw (-2147024890).ToCryptographicException();
		}
		int pcbData = 0;
		CRYPTOAPI_BLOB pvData;
		bool deleteKeyContainer = global::Interop.crypt32.CertGetCertificateContextProperty(safeCertContextHandle, CertContextPropId.CERT_CLR_DELETE_KEY_PROP_ID, out pvData, ref pcbData);
		return new CertificatePal(safeCertContextHandle, deleteKeyContainer);
	}

	public static ICertificatePal FromOtherCert(X509Certificate copyFrom)
	{
		return new CertificatePal((CertificatePal)copyFrom.Pal);
	}

	private unsafe byte[] PropagateKeyAlgorithmParametersFromChain()
	{
		SafeX509ChainHandle ppChainContext = null;
		try
		{
			int pcbData = 0;
			if (!global::Interop.crypt32.CertGetCertificateContextProperty(_certContext, CertContextPropId.CERT_PUBKEY_ALG_PARA_PROP_ID, null, ref pcbData))
			{
				CERT_CHAIN_PARA pChainPara = default(CERT_CHAIN_PARA);
				pChainPara.cbSize = sizeof(CERT_CHAIN_PARA);
				if (!global::Interop.crypt32.CertGetCertificateChain((IntPtr)0, _certContext, null, SafePointerHandle<SafeCertStoreHandle>.InvalidHandle, ref pChainPara, CertChainFlags.None, IntPtr.Zero, out ppChainContext))
				{
					throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
				}
				if (!global::Interop.crypt32.CertGetCertificateContextProperty(_certContext, CertContextPropId.CERT_PUBKEY_ALG_PARA_PROP_ID, null, ref pcbData))
				{
					throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
				}
			}
			byte[] array = new byte[pcbData];
			if (!global::Interop.crypt32.CertGetCertificateContextProperty(_certContext, CertContextPropId.CERT_PUBKEY_ALG_PARA_PROP_ID, array, ref pcbData))
			{
				throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
			}
			return array;
		}
		finally
		{
			ppChainContext?.Dispose();
		}
	}

	public string GetNameInfo(X509NameType nameType, bool forIssuer)
	{
		return global::Interop.crypt32.CertGetNameString(_certContext, MapNameType(nameType), forIssuer ? CertNameFlags.CERT_NAME_ISSUER_FLAG : CertNameFlags.None, (CertNameStringType)33554435);
	}

	public void AppendPrivateKeyInfo(StringBuilder sb)
	{
		if (!HasPrivateKey)
		{
			return;
		}
		sb.AppendLine();
		sb.AppendLine();
		sb.AppendLine("[Private Key]");
		CspKeyContainerInfo cspKeyContainerInfo = null;
		try
		{
			CspParameters privateKeyCsp = GetPrivateKeyCsp();
			if (privateKeyCsp != null)
			{
				cspKeyContainerInfo = new CspKeyContainerInfo(privateKeyCsp);
			}
		}
		catch (CryptographicException)
		{
		}
		if (cspKeyContainerInfo == null)
		{
			return;
		}
		sb.AppendLine().Append("  Key Store: ").Append(cspKeyContainerInfo.MachineKeyStore ? "Machine" : "User");
		sb.AppendLine().Append("  Provider Name: ").Append(cspKeyContainerInfo.ProviderName);
		sb.AppendLine().Append("  Provider type: ").Append(cspKeyContainerInfo.ProviderType);
		sb.AppendLine().Append("  Key Spec: ").Append(cspKeyContainerInfo.KeyNumber);
		sb.AppendLine().Append("  Key Container Name: ").Append(cspKeyContainerInfo.KeyContainerName);
		try
		{
			string uniqueKeyContainerName = cspKeyContainerInfo.UniqueKeyContainerName;
			sb.AppendLine().Append("  Unique Key Container Name: ").Append(uniqueKeyContainerName);
		}
		catch (CryptographicException)
		{
		}
		catch (NotSupportedException)
		{
		}
		try
		{
			bool hardwareDevice = cspKeyContainerInfo.HardwareDevice;
			sb.AppendLine().Append("  Hardware Device: ").Append(hardwareDevice);
		}
		catch (CryptographicException)
		{
		}
		try
		{
			bool removable = cspKeyContainerInfo.Removable;
			sb.AppendLine().Append("  Removable: ").Append(removable);
		}
		catch (CryptographicException)
		{
		}
		try
		{
			bool @protected = cspKeyContainerInfo.Protected;
			sb.AppendLine().Append("  Protected: ").Append(@protected);
		}
		catch (CryptographicException)
		{
		}
		catch (NotSupportedException)
		{
		}
	}

	public void Dispose()
	{
		SafeCertContextHandle certContext = _certContext;
		_certContext = null;
		if (certContext != null && !certContext.IsInvalid)
		{
			certContext.Dispose();
		}
	}

	private static CertNameType MapNameType(X509NameType nameType)
	{
		switch (nameType)
		{
		case X509NameType.SimpleName:
			return CertNameType.CERT_NAME_SIMPLE_DISPLAY_TYPE;
		case X509NameType.EmailName:
			return CertNameType.CERT_NAME_EMAIL_TYPE;
		case X509NameType.UpnName:
			return CertNameType.CERT_NAME_UPN_TYPE;
		case X509NameType.DnsName:
		case X509NameType.DnsFromAlternativeName:
			return CertNameType.CERT_NAME_DNS_TYPE;
		case X509NameType.UrlName:
			return CertNameType.CERT_NAME_URL_TYPE;
		default:
			throw new ArgumentException(System.SR.Argument_InvalidNameType);
		}
	}

	private string GetIssuerOrSubject(bool issuer, bool reverse)
	{
		return global::Interop.crypt32.CertGetNameString(_certContext, CertNameType.CERT_NAME_RDN_TYPE, issuer ? CertNameFlags.CERT_NAME_ISSUER_FLAG : CertNameFlags.None, CertNameStringType.CERT_X500_NAME_STR | (reverse ? CertNameStringType.CERT_NAME_STR_REVERSE_FLAG : ((CertNameStringType)0)));
	}

	private CertificatePal(CertificatePal copyFrom)
	{
		_certContext = new SafeCertContextHandle(copyFrom._certContext);
	}

	private CertificatePal(SafeCertContextHandle certContext, bool deleteKeyContainer)
	{
		if (deleteKeyContainer)
		{
			SafeCertContextHandle safeCertContextHandle = certContext;
			certContext = global::Interop.crypt32.CertDuplicateCertificateContextWithKeyContainerDeletion(safeCertContextHandle.DangerousGetHandle());
			GC.KeepAlive(safeCertContextHandle);
		}
		_certContext = certContext;
	}

	public byte[] Export(X509ContentType contentType, SafePasswordHandle password)
	{
		using IExportPal exportPal = StorePal.FromCertificate(this);
		return exportPal.Export(contentType, password);
	}

	public static ICertificatePal FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
	{
		return FromBlobOrFile(rawData, null, password, keyStorageFlags);
	}

	public static ICertificatePal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
	{
		return FromBlobOrFile(ReadOnlySpan<byte>.Empty, fileName, password, keyStorageFlags);
	}

	private unsafe static ICertificatePal FromBlobOrFile(ReadOnlySpan<byte> rawData, string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
	{
		bool flag = fileName != null;
		PfxCertStoreFlags pfxCertStoreFlags = MapKeyStorageFlags(keyStorageFlags);
		bool deleteKeyContainer = false;
		SafeCertStoreHandle phCertStore = null;
		SafeCryptMsgHandle phMsg = null;
		SafeCertContextHandle ppvContext = null;
		try
		{
			ContentType pdwContentType;
			fixed (byte* pbData = rawData)
			{
				fixed (char* ptr = fileName)
				{
					CRYPTOAPI_BLOB cRYPTOAPI_BLOB = new CRYPTOAPI_BLOB((!flag) ? rawData.Length : 0, pbData);
					CertQueryObjectType dwObjectType = (flag ? CertQueryObjectType.CERT_QUERY_OBJECT_FILE : CertQueryObjectType.CERT_QUERY_OBJECT_BLOB);
					void* pvObject = (flag ? ((void*)ptr) : ((void*)(&cRYPTOAPI_BLOB)));
					if (!global::Interop.crypt32.CryptQueryObject(dwObjectType, pvObject, ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_CERT | ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT | ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED | ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED | ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PFX, ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_ALL, 0, out var _, out pdwContentType, out var _, out phCertStore, out phMsg, out ppvContext))
					{
						int hRForLastWin32Error = Marshal.GetHRForLastWin32Error();
						throw hRForLastWin32Error.ToCryptographicException();
					}
				}
			}
			switch (pdwContentType)
			{
			case ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED:
			case ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED:
				ppvContext = GetSignerInPKCS7Store(phCertStore, phMsg);
				break;
			case ContentType.CERT_QUERY_CONTENT_PFX:
				if (flag)
				{
					rawData = File.ReadAllBytes(fileName);
				}
				ppvContext = FilterPFXStore(rawData, password, pfxCertStoreFlags);
				deleteKeyContainer = (keyStorageFlags & (X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.EphemeralKeySet)) == 0;
				break;
			}
			CertificatePal result = new CertificatePal(ppvContext, deleteKeyContainer);
			ppvContext = null;
			return result;
		}
		finally
		{
			phCertStore?.Dispose();
			phMsg?.Dispose();
			ppvContext?.Dispose();
		}
	}

	private unsafe static SafeCertContextHandle GetSignerInPKCS7Store(SafeCertStoreHandle hCertStore, SafeCryptMsgHandle hCryptMsg)
	{
		int pcbData = 4;
		if (!global::Interop.crypt32.CryptMsgGetParam(hCryptMsg, CryptMessageParameterType.CMSG_SIGNER_COUNT_PARAM, 0, out var pvData, ref pcbData))
		{
			throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
		}
		if (pvData == 0)
		{
			throw (-2146889714).ToCryptographicException();
		}
		int pcbData2 = 0;
		if (!global::Interop.crypt32.CryptMsgGetParam(hCryptMsg, CryptMessageParameterType.CMSG_SIGNER_INFO_PARAM, 0, null, ref pcbData2))
		{
			throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
		}
		fixed (byte* ptr = new byte[pcbData2])
		{
			if (!global::Interop.crypt32.CryptMsgGetParam(hCryptMsg, CryptMessageParameterType.CMSG_SIGNER_INFO_PARAM, 0, ptr, ref pcbData2))
			{
				throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
			}
			CMSG_SIGNER_INFO_Partial* ptr2 = (CMSG_SIGNER_INFO_Partial*)ptr;
			CERT_INFO cERT_INFO = default(CERT_INFO);
			cERT_INFO.Issuer.cbData = ptr2->Issuer.cbData;
			cERT_INFO.Issuer.pbData = ptr2->Issuer.pbData;
			cERT_INFO.SerialNumber.cbData = ptr2->SerialNumber.cbData;
			cERT_INFO.SerialNumber.pbData = ptr2->SerialNumber.pbData;
			SafeCertContextHandle pCertContext = null;
			if (!global::Interop.crypt32.CertFindCertificateInStore(hCertStore, CertFindType.CERT_FIND_SUBJECT_CERT, &cERT_INFO, ref pCertContext))
			{
				throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
			}
			return pCertContext;
		}
	}

	private unsafe static SafeCertContextHandle FilterPFXStore(ReadOnlySpan<byte> rawData, SafePasswordHandle password, PfxCertStoreFlags pfxCertStoreFlags)
	{
		SafeCertStoreHandle safeCertStoreHandle;
		fixed (byte* pbData = rawData)
		{
			CRYPTOAPI_BLOB pPFX = new CRYPTOAPI_BLOB(rawData.Length, pbData);
			safeCertStoreHandle = global::Interop.crypt32.PFXImportCertStore(ref pPFX, password, pfxCertStoreFlags);
			if (safeCertStoreHandle.IsInvalid)
			{
				throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
			}
		}
		try
		{
			SafeCertContextHandle safeCertContextHandle = SafePointerHandle<SafeCertContextHandle>.InvalidHandle;
			SafeCertContextHandle pCertContext = null;
			while (global::Interop.crypt32.CertEnumCertificatesInStore(safeCertStoreHandle, ref pCertContext))
			{
				if (pCertContext.ContainsPrivateKey)
				{
					if (!safeCertContextHandle.IsInvalid && safeCertContextHandle.ContainsPrivateKey)
					{
						if (pCertContext.HasPersistedPrivateKey)
						{
							SafeCertContextHandleWithKeyContainerDeletion.DeleteKeyContainer(pCertContext);
						}
					}
					else
					{
						safeCertContextHandle.Dispose();
						safeCertContextHandle = pCertContext.Duplicate();
					}
				}
				else if (safeCertContextHandle.IsInvalid)
				{
					safeCertContextHandle = pCertContext.Duplicate();
				}
			}
			if (safeCertContextHandle.IsInvalid)
			{
				throw new CryptographicException(System.SR.Cryptography_Pfx_NoCertificates);
			}
			return safeCertContextHandle;
		}
		finally
		{
			safeCertStoreHandle.Dispose();
		}
	}

	private static PfxCertStoreFlags MapKeyStorageFlags(X509KeyStorageFlags keyStorageFlags)
	{
		if ((keyStorageFlags & (X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable | X509KeyStorageFlags.UserProtected | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.EphemeralKeySet)) != keyStorageFlags)
		{
			throw new ArgumentException(System.SR.Argument_InvalidFlag, "keyStorageFlags");
		}
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

	public RSA GetRSAPrivateKey()
	{
		return GetPrivateKey((Func<CspParameters, RSA>)((CspParameters csp) => new RSACryptoServiceProvider(csp)), (Func<CngKey, RSA>)((CngKey cngKey) => new RSACng(cngKey)));
	}

	public DSA GetDSAPrivateKey()
	{
		return GetPrivateKey((Func<CspParameters, DSA>)((CspParameters csp) => new DSACryptoServiceProvider(csp)), (Func<CngKey, DSA>)((CngKey cngKey) => new DSACng(cngKey)));
	}

	public ECDsa GetECDsaPrivateKey()
	{
		return GetPrivateKey<ECDsa>(delegate
		{
			throw new NotSupportedException(System.SR.NotSupported_ECDsa_Csp);
		}, (CngKey cngKey) => new ECDsaCng(cngKey));
	}

	public ECDiffieHellman GetECDiffieHellmanPrivateKey()
	{
		return GetPrivateKey<ECDiffieHellman>(delegate
		{
			throw new NotSupportedException(System.SR.NotSupported_ECDiffieHellman_Csp);
		}, (CngKey cngKey) => new ECDiffieHellmanCng(cngKey));
	}

	public ICertificatePal CopyWithPrivateKey(DSA dsa)
	{
		DSACng dSACng = dsa as DSACng;
		ICertificatePal certificatePal = null;
		if (dSACng != null)
		{
			certificatePal = CopyWithPersistedCngKey(dSACng.Key);
			if (certificatePal != null)
			{
				return certificatePal;
			}
		}
		if (dsa is DSACryptoServiceProvider dSACryptoServiceProvider)
		{
			certificatePal = CopyWithPersistedCapiKey(dSACryptoServiceProvider.CspKeyContainerInfo);
			if (certificatePal != null)
			{
				return certificatePal;
			}
		}
		DSAParameters parameters = dsa.ExportParameters(includePrivateParameters: true);
		using (PinAndClear.Track(parameters.X))
		{
			using DSACng dSACng2 = new DSACng();
			dSACng2.ImportParameters(parameters);
			return CopyWithEphemeralKey(dSACng2.Key);
		}
	}

	public ICertificatePal CopyWithPrivateKey(ECDsa ecdsa)
	{
		if (ecdsa is ECDsaCng eCDsaCng)
		{
			ICertificatePal certificatePal = CopyWithPersistedCngKey(eCDsaCng.Key);
			if (certificatePal != null)
			{
				return certificatePal;
			}
		}
		ECParameters parameters = ecdsa.ExportParameters(includePrivateParameters: true);
		using (PinAndClear.Track(parameters.D))
		{
			using ECDsaCng eCDsaCng2 = new ECDsaCng();
			eCDsaCng2.ImportParameters(parameters);
			return CopyWithEphemeralKey(eCDsaCng2.Key);
		}
	}

	public ICertificatePal CopyWithPrivateKey(ECDiffieHellman ecdh)
	{
		if (ecdh is ECDiffieHellmanCng eCDiffieHellmanCng)
		{
			ICertificatePal certificatePal = CopyWithPersistedCngKey(eCDiffieHellmanCng.Key);
			if (certificatePal != null)
			{
				return certificatePal;
			}
		}
		ECParameters parameters = ecdh.ExportParameters(includePrivateParameters: true);
		using (PinAndClear.Track(parameters.D))
		{
			using ECDiffieHellmanCng eCDiffieHellmanCng2 = new ECDiffieHellmanCng();
			eCDiffieHellmanCng2.ImportParameters(parameters);
			return CopyWithEphemeralKey(eCDiffieHellmanCng2.Key);
		}
	}

	public ICertificatePal CopyWithPrivateKey(RSA rsa)
	{
		RSACng rSACng = rsa as RSACng;
		ICertificatePal certificatePal = null;
		if (rSACng != null)
		{
			certificatePal = CopyWithPersistedCngKey(rSACng.Key);
			if (certificatePal != null)
			{
				return certificatePal;
			}
		}
		if (rsa is RSACryptoServiceProvider rSACryptoServiceProvider)
		{
			certificatePal = CopyWithPersistedCapiKey(rSACryptoServiceProvider.CspKeyContainerInfo);
			if (certificatePal != null)
			{
				return certificatePal;
			}
		}
		RSAParameters parameters = rsa.ExportParameters(includePrivateParameters: true);
		using (PinAndClear.Track(parameters.D))
		{
			using (PinAndClear.Track(parameters.P))
			{
				using (PinAndClear.Track(parameters.Q))
				{
					using (PinAndClear.Track(parameters.DP))
					{
						using (PinAndClear.Track(parameters.DQ))
						{
							using (PinAndClear.Track(parameters.InverseQ))
							{
								using RSACng rSACng2 = new RSACng();
								rSACng2.ImportParameters(parameters);
								return CopyWithEphemeralKey(rSACng2.Key);
							}
						}
					}
				}
			}
		}
	}

	private T GetPrivateKey<T>(Func<CspParameters, T> createCsp, Func<CngKey, T> createCng) where T : AsymmetricAlgorithm
	{
		CngKeyHandleOpenOptions handleOptions;
		SafeNCryptKeyHandle safeNCryptKeyHandle = TryAcquireCngPrivateKey(CertContext, out handleOptions);
		if (safeNCryptKeyHandle != null)
		{
			CngKey arg = CngKey.Open(safeNCryptKeyHandle, handleOptions);
			return createCng(arg);
		}
		CspParameters privateKeyCsp = GetPrivateKeyCsp();
		if (privateKeyCsp == null)
		{
			return null;
		}
		if (privateKeyCsp.ProviderType == 0)
		{
			string providerName = privateKeyCsp.ProviderName;
			string keyContainerName = privateKeyCsp.KeyContainerName;
			CngKey arg2 = CngKey.Open(keyContainerName, new CngProvider(providerName));
			return createCng(arg2);
		}
		privateKeyCsp.Flags |= CspProviderFlags.UseExistingKey;
		return createCsp(privateKeyCsp);
	}

	private static SafeNCryptKeyHandle TryAcquireCngPrivateKey(SafeCertContextHandle certificateContext, out CngKeyHandleOpenOptions handleOptions)
	{
		if (!certificateContext.HasPersistedPrivateKey)
		{
			int pcbData = IntPtr.Size;
			if (global::Interop.crypt32.CertGetCertificateContextProperty(certificateContext, CertContextPropId.CERT_NCRYPT_KEY_HANDLE_PROP_ID, out IntPtr pvData, ref pcbData))
			{
				handleOptions = CngKeyHandleOpenOptions.EphemeralKey;
				return new SafeNCryptKeyHandle(pvData, certificateContext);
			}
		}
		bool pfCallerFreeProvOrNCryptKey = true;
		SafeNCryptKeyHandle phCryptProvOrNCryptKey = null;
		handleOptions = CngKeyHandleOpenOptions.None;
		try
		{
			int pdwKeySpec = 0;
			if (!global::Interop.crypt32.CryptAcquireCertificatePrivateKey(certificateContext, CryptAcquireFlags.CRYPT_ACQUIRE_ONLY_NCRYPT_KEY_FLAG, IntPtr.Zero, out phCryptProvOrNCryptKey, out pdwKeySpec, out pfCallerFreeProvOrNCryptKey))
			{
				pfCallerFreeProvOrNCryptKey = false;
				phCryptProvOrNCryptKey?.SetHandleAsInvalid();
				return null;
			}
			if (!pfCallerFreeProvOrNCryptKey && phCryptProvOrNCryptKey != null && !phCryptProvOrNCryptKey.IsInvalid)
			{
				SafeNCryptKeyHandle safeNCryptKeyHandle = new SafeNCryptKeyHandle(phCryptProvOrNCryptKey.DangerousGetHandle(), certificateContext);
				phCryptProvOrNCryptKey.SetHandleAsInvalid();
				phCryptProvOrNCryptKey = safeNCryptKeyHandle;
				pfCallerFreeProvOrNCryptKey = true;
			}
			return phCryptProvOrNCryptKey;
		}
		catch
		{
			if (phCryptProvOrNCryptKey != null && !pfCallerFreeProvOrNCryptKey)
			{
				phCryptProvOrNCryptKey.SetHandleAsInvalid();
			}
			throw;
		}
	}

	private unsafe CspParameters GetPrivateKeyCsp()
	{
		int pcbData = 0;
		if (!global::Interop.crypt32.CertGetCertificateContextProperty(_certContext, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, null, ref pcbData))
		{
			int lastWin32Error = Marshal.GetLastWin32Error();
			if (lastWin32Error == -2146885628)
			{
				return null;
			}
			throw lastWin32Error.ToCryptographicException();
		}
		byte[] array = new byte[pcbData];
		fixed (byte* ptr = array)
		{
			if (!global::Interop.crypt32.CertGetCertificateContextProperty(_certContext, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, array, ref pcbData))
			{
				throw Marshal.GetLastWin32Error().ToCryptographicException();
			}
			CRYPT_KEY_PROV_INFO* ptr2 = (CRYPT_KEY_PROV_INFO*)ptr;
			CspParameters cspParameters = new CspParameters();
			cspParameters.ProviderName = Marshal.PtrToStringUni((IntPtr)ptr2->pwszProvName);
			cspParameters.KeyContainerName = Marshal.PtrToStringUni((IntPtr)ptr2->pwszContainerName);
			cspParameters.ProviderType = ptr2->dwProvType;
			cspParameters.KeyNumber = ptr2->dwKeySpec;
			cspParameters.Flags = (((ptr2->dwFlags & CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET) == CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET) ? CspProviderFlags.UseMachineKeyStore : CspProviderFlags.NoFlags);
			return cspParameters;
		}
	}

	private unsafe ICertificatePal CopyWithPersistedCngKey(CngKey cngKey)
	{
		//The blocks IL_0090, IL_00a9, IL_00ac, IL_00ae, IL_00ce are reachable both inside and outside the pinned region starting at IL_008b. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (string.IsNullOrEmpty(cngKey.KeyName))
		{
			return null;
		}
		CertificatePal certificatePal = (CertificatePal)FromBlob(RawData, SafePasswordHandle.InvalidHandle, X509KeyStorageFlags.PersistKeySet);
		CngProvider provider = cngKey.Provider;
		string keyName = cngKey.KeyName;
		bool isMachineKey = cngKey.IsMachineKey;
		int dwKeySpec = GuessKeySpec(provider, keyName, isMachineKey, cngKey.AlgorithmGroup);
		CRYPT_KEY_PROV_INFO cRYPT_KEY_PROV_INFO = default(CRYPT_KEY_PROV_INFO);
		fixed (char* pwszContainerName = cngKey.KeyName)
		{
			string provider2 = cngKey.Provider.Provider;
			char* intPtr;
			ref CRYPT_KEY_PROV_INFO reference;
			int dwFlags;
			if (provider2 == null)
			{
				char* pwszProvName;
				intPtr = (pwszProvName = null);
				cRYPT_KEY_PROV_INFO.pwszContainerName = pwszContainerName;
				cRYPT_KEY_PROV_INFO.pwszProvName = pwszProvName;
				reference = ref cRYPT_KEY_PROV_INFO;
				dwFlags = (isMachineKey ? 32 : 0);
				reference.dwFlags = (CryptAcquireContextFlags)dwFlags;
				cRYPT_KEY_PROV_INFO.dwKeySpec = dwKeySpec;
				if (!global::Interop.crypt32.CertSetCertificateContextProperty(certificatePal._certContext, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, CertSetPropertyFlags.None, &cRYPT_KEY_PROV_INFO))
				{
					certificatePal.Dispose();
					throw Marshal.GetLastWin32Error().ToCryptographicException();
				}
			}
			else
			{
				fixed (char* ptr = &provider2.GetPinnableReference())
				{
					char* pwszProvName;
					intPtr = (pwszProvName = ptr);
					cRYPT_KEY_PROV_INFO.pwszContainerName = pwszContainerName;
					cRYPT_KEY_PROV_INFO.pwszProvName = pwszProvName;
					reference = ref cRYPT_KEY_PROV_INFO;
					dwFlags = (int)(reference.dwFlags = (isMachineKey ? CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET : CryptAcquireContextFlags.None));
					cRYPT_KEY_PROV_INFO.dwKeySpec = dwKeySpec;
					if (!global::Interop.crypt32.CertSetCertificateContextProperty(certificatePal._certContext, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, CertSetPropertyFlags.None, &cRYPT_KEY_PROV_INFO))
					{
						certificatePal.Dispose();
						throw Marshal.GetLastWin32Error().ToCryptographicException();
					}
				}
			}
		}
		return certificatePal;
	}

	private static int GuessKeySpec(CngProvider provider, string keyName, bool machineKey, CngAlgorithmGroup algorithmGroup)
	{
		if (provider == CngProvider.MicrosoftSoftwareKeyStorageProvider || provider == CngProvider.MicrosoftSmartCardKeyStorageProvider)
		{
			return 0;
		}
		try
		{
			CngKeyOpenOptions openOptions = (machineKey ? CngKeyOpenOptions.MachineKey : CngKeyOpenOptions.None);
			using (CngKey.Open(keyName, provider, openOptions))
			{
				return 0;
			}
		}
		catch (CryptographicException)
		{
			CspParameters cspParameters = new CspParameters
			{
				ProviderName = provider.Provider,
				KeyContainerName = keyName,
				Flags = CspProviderFlags.UseExistingKey,
				KeyNumber = 2
			};
			if (machineKey)
			{
				cspParameters.Flags |= CspProviderFlags.UseMachineKeyStore;
			}
			if (TryGuessKeySpec(cspParameters, algorithmGroup, out var keySpec))
			{
				return keySpec;
			}
			throw;
		}
	}

	private static bool TryGuessKeySpec(CspParameters cspParameters, CngAlgorithmGroup algorithmGroup, out int keySpec)
	{
		if (algorithmGroup == CngAlgorithmGroup.Rsa)
		{
			return TryGuessRsaKeySpec(cspParameters, out keySpec);
		}
		if (algorithmGroup == CngAlgorithmGroup.Dsa)
		{
			return TryGuessDsaKeySpec(cspParameters, out keySpec);
		}
		keySpec = 0;
		return false;
	}

	private static bool TryGuessRsaKeySpec(CspParameters cspParameters, out int keySpec)
	{
		int[] array = new int[4] { 1, 24, 12, 2 };
		int[] array2 = array;
		foreach (int providerType in array2)
		{
			cspParameters.ProviderType = providerType;
			try
			{
				using (new RSACryptoServiceProvider(cspParameters))
				{
					keySpec = cspParameters.KeyNumber;
					return true;
				}
			}
			catch (CryptographicException)
			{
			}
		}
		keySpec = 0;
		return false;
	}

	private static bool TryGuessDsaKeySpec(CspParameters cspParameters, out int keySpec)
	{
		int[] array = new int[2] { 13, 3 };
		int[] array2 = array;
		foreach (int providerType in array2)
		{
			cspParameters.ProviderType = providerType;
			try
			{
				using (new DSACryptoServiceProvider(cspParameters))
				{
					keySpec = cspParameters.KeyNumber;
					return true;
				}
			}
			catch (CryptographicException)
			{
			}
		}
		keySpec = 0;
		return false;
	}

	private unsafe ICertificatePal CopyWithPersistedCapiKey(CspKeyContainerInfo keyContainerInfo)
	{
		//The blocks IL_0063, IL_0080, IL_0083, IL_0085, IL_00b6 are reachable both inside and outside the pinned region starting at IL_005e. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (string.IsNullOrEmpty(keyContainerInfo.KeyContainerName))
		{
			return null;
		}
		CertificatePal certificatePal = (CertificatePal)FromBlob(RawData, SafePasswordHandle.InvalidHandle, X509KeyStorageFlags.PersistKeySet);
		CRYPT_KEY_PROV_INFO cRYPT_KEY_PROV_INFO = default(CRYPT_KEY_PROV_INFO);
		fixed (char* pwszContainerName = keyContainerInfo.KeyContainerName)
		{
			string? providerName = keyContainerInfo.ProviderName;
			char* intPtr;
			ref CRYPT_KEY_PROV_INFO reference;
			int dwFlags;
			if (providerName == null)
			{
				char* pwszProvName;
				intPtr = (pwszProvName = null);
				cRYPT_KEY_PROV_INFO.pwszContainerName = pwszContainerName;
				cRYPT_KEY_PROV_INFO.pwszProvName = pwszProvName;
				reference = ref cRYPT_KEY_PROV_INFO;
				dwFlags = (keyContainerInfo.MachineKeyStore ? 32 : 0);
				reference.dwFlags = (CryptAcquireContextFlags)dwFlags;
				cRYPT_KEY_PROV_INFO.dwProvType = keyContainerInfo.ProviderType;
				cRYPT_KEY_PROV_INFO.dwKeySpec = (int)keyContainerInfo.KeyNumber;
				if (!global::Interop.crypt32.CertSetCertificateContextProperty(certificatePal._certContext, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, CertSetPropertyFlags.None, &cRYPT_KEY_PROV_INFO))
				{
					certificatePal.Dispose();
					throw Marshal.GetLastWin32Error().ToCryptographicException();
				}
			}
			else
			{
				fixed (char* ptr = &providerName.GetPinnableReference())
				{
					char* pwszProvName;
					intPtr = (pwszProvName = ptr);
					cRYPT_KEY_PROV_INFO.pwszContainerName = pwszContainerName;
					cRYPT_KEY_PROV_INFO.pwszProvName = pwszProvName;
					reference = ref cRYPT_KEY_PROV_INFO;
					dwFlags = (int)(reference.dwFlags = (keyContainerInfo.MachineKeyStore ? CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET : CryptAcquireContextFlags.None));
					cRYPT_KEY_PROV_INFO.dwProvType = keyContainerInfo.ProviderType;
					cRYPT_KEY_PROV_INFO.dwKeySpec = (int)keyContainerInfo.KeyNumber;
					if (!global::Interop.crypt32.CertSetCertificateContextProperty(certificatePal._certContext, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, CertSetPropertyFlags.None, &cRYPT_KEY_PROV_INFO))
					{
						certificatePal.Dispose();
						throw Marshal.GetLastWin32Error().ToCryptographicException();
					}
				}
			}
		}
		return certificatePal;
	}

	private ICertificatePal CopyWithEphemeralKey(CngKey cngKey)
	{
		SafeNCryptKeyHandle handle = cngKey.Handle;
		CertificatePal certificatePal = (CertificatePal)FromBlob(RawData, SafePasswordHandle.InvalidHandle, X509KeyStorageFlags.PersistKeySet);
		if (!global::Interop.crypt32.CertSetCertificateContextProperty(certificatePal._certContext, CertContextPropId.CERT_NCRYPT_KEY_HANDLE_PROP_ID, CertSetPropertyFlags.CERT_SET_PROPERTY_INHIBIT_PERSIST_FLAG, handle))
		{
			certificatePal.Dispose();
			throw Marshal.GetLastWin32Error().ToCryptographicException();
		}
		handle.SetHandleAsInvalid();
		return certificatePal;
	}
}
