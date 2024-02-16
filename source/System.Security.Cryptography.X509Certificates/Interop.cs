using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;
using Internal.Cryptography.Pal.Native;
using Microsoft.Win32.SafeHandles;

internal static class Interop
{
	public static class cryptoapi
	{
		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "CryptAcquireContextW", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public unsafe static extern bool CryptAcquireContext(out IntPtr psafeProvHandle, char* pszContainer, char* pszProvider, int dwProvType, CryptAcquireContextFlags dwFlags);
	}

	public static class crypt32
	{
		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool CryptQueryObject(CertQueryObjectType dwObjectType, void* pvObject, ExpectedContentTypeFlags dwExpectedContentTypeFlags, ExpectedFormatTypeFlags dwExpectedFormatTypeFlags, int dwFlags, out CertEncodingType pdwMsgAndCertEncodingType, out ContentType pdwContentType, out FormatType pdwFormatType, out SafeCertStoreHandle phCertStore, out SafeCryptMsgHandle phMsg, out SafeCertContextHandle ppvContext);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool CryptQueryObject(CertQueryObjectType dwObjectType, void* pvObject, ExpectedContentTypeFlags dwExpectedContentTypeFlags, ExpectedFormatTypeFlags dwExpectedFormatTypeFlags, int dwFlags, IntPtr pdwMsgAndCertEncodingType, out ContentType pdwContentType, IntPtr pdwFormatType, IntPtr phCertStore, IntPtr phMsg, IntPtr ppvContext);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool CryptQueryObject(CertQueryObjectType dwObjectType, void* pvObject, ExpectedContentTypeFlags dwExpectedContentTypeFlags, ExpectedFormatTypeFlags dwExpectedFormatTypeFlags, int dwFlags, IntPtr pdwMsgAndCertEncodingType, out ContentType pdwContentType, IntPtr pdwFormatType, out SafeCertStoreHandle phCertStore, IntPtr phMsg, IntPtr ppvContext);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CertGetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, [Out] byte[] pvData, [In][Out] ref int pcbData);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CertGetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, out CRYPTOAPI_BLOB pvData, [In][Out] ref int pcbData);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CertGetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, out IntPtr pvData, [In][Out] ref int pcbData);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, EntryPoint = "CertGetCertificateContextProperty", SetLastError = true)]
		public unsafe static extern bool CertGetCertificateContextPropertyString(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, byte* pvData, ref int pcbData);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool CertSetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, CertSetPropertyFlags dwFlags, [In] CRYPTOAPI_BLOB* pvData);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool CertSetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, CertSetPropertyFlags dwFlags, [In] CRYPT_KEY_PROV_INFO* pvData);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CertSetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, CertSetPropertyFlags dwFlags, [In] SafeNCryptKeyHandle keyHandle);

		public unsafe static string CertGetNameString(SafeCertContextHandle certContext, CertNameType certNameType, CertNameFlags certNameFlags, CertNameStringType strType)
		{
			int num = CertGetNameString(certContext, certNameType, certNameFlags, in strType, null, 0);
			if (num == 0)
			{
				throw Marshal.GetLastWin32Error().ToCryptographicException();
			}
			Span<char> span = ((num > 256) ? ((Span<char>)new char[num]) : stackalloc char[num]);
			Span<char> span2 = span;
			fixed (char* pszNameString = &MemoryMarshal.GetReference(span2))
			{
				if (CertGetNameString(certContext, certNameType, certNameFlags, in strType, pszNameString, num) == 0)
				{
					throw Marshal.GetLastWin32Error().ToCryptographicException();
				}
				return new string(span2.Slice(0, num - 1));
			}
		}

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, EntryPoint = "CertGetNameStringW", SetLastError = true)]
		private unsafe static extern int CertGetNameString(SafeCertContextHandle pCertContext, CertNameType dwType, CertNameFlags dwFlags, in CertNameStringType pvTypePara, char* pszNameString, int cchNameString);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern SafeCertContextHandle CertDuplicateCertificateContext(IntPtr pCertContext);

		[DllImport("crypt32.dll", SetLastError = true)]
		public static extern SafeX509ChainHandle CertDuplicateCertificateChain(IntPtr pChainContext);

		[DllImport("crypt32.dll", SetLastError = true)]
		internal static extern SafeCertStoreHandle CertDuplicateStore(IntPtr hCertStore);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, EntryPoint = "CertDuplicateCertificateContext", SetLastError = true)]
		public static extern SafeCertContextHandleWithKeyContainerDeletion CertDuplicateCertificateContextWithKeyContainerDeletion(IntPtr pCertContext);

		public static SafeCertStoreHandle CertOpenStore(CertStoreProvider lpszStoreProvider, CertEncodingType dwMsgAndCertEncodingType, IntPtr hCryptProv, CertStoreFlags dwFlags, string pvPara)
		{
			return CertOpenStore((IntPtr)(int)lpszStoreProvider, dwMsgAndCertEncodingType, hCryptProv, dwFlags, pvPara);
		}

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern SafeCertStoreHandle CertOpenStore(IntPtr lpszStoreProvider, CertEncodingType dwMsgAndCertEncodingType, IntPtr hCryptProv, CertStoreFlags dwFlags, [MarshalAs(UnmanagedType.LPWStr)] string pvPara);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CertAddCertificateContextToStore(SafeCertStoreHandle hCertStore, SafeCertContextHandle pCertContext, CertStoreAddDisposition dwAddDisposition, IntPtr ppStoreContext);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CertAddCertificateLinkToStore(SafeCertStoreHandle hCertStore, SafeCertContextHandle pCertContext, CertStoreAddDisposition dwAddDisposition, IntPtr ppStoreContext);

		public unsafe static bool CertEnumCertificatesInStore(SafeCertStoreHandle hCertStore, [NotNull] ref SafeCertContextHandle pCertContext)
		{
			CERT_CONTEXT* pPrevCertContext;
			if (pCertContext == null)
			{
				pCertContext = new SafeCertContextHandle();
				pPrevCertContext = null;
			}
			else
			{
				pPrevCertContext = pCertContext.Disconnect();
			}
			pCertContext.SetHandle((IntPtr)CertEnumCertificatesInStore(hCertStore, pPrevCertContext));
			if (!pCertContext.IsInvalid)
			{
				return true;
			}
			pCertContext.Dispose();
			return false;
		}

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private unsafe static extern CERT_CONTEXT* CertEnumCertificatesInStore(SafeCertStoreHandle hCertStore, CERT_CONTEXT* pPrevCertContext);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern SafeCertStoreHandle PFXImportCertStore([In] ref CRYPTOAPI_BLOB pPFX, SafePasswordHandle password, PfxCertStoreFlags dwFlags);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool CryptMsgGetParam(SafeCryptMsgHandle hCryptMsg, CryptMessageParameterType dwParamType, int dwIndex, byte* pvData, [In][Out] ref int pcbData);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CryptMsgGetParam(SafeCryptMsgHandle hCryptMsg, CryptMessageParameterType dwParamType, int dwIndex, out int pvData, [In][Out] ref int pcbData);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CertSerializeCertificateStoreElement(SafeCertContextHandle pCertContext, int dwFlags, [Out] byte[] pbElement, [In][Out] ref int pcbElement);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool PFXExportCertStore(SafeCertStoreHandle hStore, [In][Out] ref CRYPTOAPI_BLOB pPFX, SafePasswordHandle szPassword, PFXExportFlags dwFlags);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, EntryPoint = "CertStrToNameW", SetLastError = true)]
		public static extern bool CertStrToName(CertEncodingType dwCertEncodingType, string pszX500, CertNameStrTypeAndFlags dwStrType, IntPtr pvReserved, [Out] byte[] pbEncoded, [In][Out] ref int pcbEncoded, IntPtr ppszError);

		public static bool CryptDecodeObject(CertEncodingType dwCertEncodingType, CryptDecodeObjectStructType lpszStructType, byte[] pbEncoded, int cbEncoded, CryptDecodeObjectFlags dwFlags, byte[] pvStructInfo, ref int pcbStructInfo)
		{
			return CryptDecodeObject(dwCertEncodingType, (IntPtr)(int)lpszStructType, pbEncoded, cbEncoded, dwFlags, pvStructInfo, ref pcbStructInfo);
		}

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern bool CryptDecodeObject(CertEncodingType dwCertEncodingType, IntPtr lpszStructType, [In] byte[] pbEncoded, int cbEncoded, CryptDecodeObjectFlags dwFlags, [Out] byte[] pvStructInfo, [In][Out] ref int pcbStructInfo);

		public unsafe static bool CryptDecodeObjectPointer(CertEncodingType dwCertEncodingType, CryptDecodeObjectStructType lpszStructType, byte[] pbEncoded, int cbEncoded, CryptDecodeObjectFlags dwFlags, void* pvStructInfo, ref int pcbStructInfo)
		{
			return CryptDecodeObjectPointer(dwCertEncodingType, (IntPtr)(int)lpszStructType, pbEncoded, cbEncoded, dwFlags, pvStructInfo, ref pcbStructInfo);
		}

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, EntryPoint = "CryptDecodeObject", SetLastError = true)]
		private unsafe static extern bool CryptDecodeObjectPointer(CertEncodingType dwCertEncodingType, IntPtr lpszStructType, [In] byte[] pbEncoded, int cbEncoded, CryptDecodeObjectFlags dwFlags, [Out] void* pvStructInfo, [In][Out] ref int pcbStructInfo);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, EntryPoint = "CryptDecodeObject", SetLastError = true)]
		public unsafe static extern bool CryptDecodeObjectPointer(CertEncodingType dwCertEncodingType, [MarshalAs(UnmanagedType.LPStr)] string lpszStructType, [In] byte[] pbEncoded, int cbEncoded, CryptDecodeObjectFlags dwFlags, [Out] void* pvStructInfo, [In][Out] ref int pcbStructInfo);

		public unsafe static bool CryptEncodeObject(CertEncodingType dwCertEncodingType, CryptDecodeObjectStructType lpszStructType, void* pvStructInfo, byte[] pbEncoded, ref int pcbEncoded)
		{
			return CryptEncodeObject(dwCertEncodingType, (IntPtr)(int)lpszStructType, pvStructInfo, pbEncoded, ref pcbEncoded);
		}

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private unsafe static extern bool CryptEncodeObject(CertEncodingType dwCertEncodingType, IntPtr lpszStructType, void* pvStructInfo, [Out] byte[] pbEncoded, [In][Out] ref int pcbEncoded);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool CryptEncodeObject(CertEncodingType dwCertEncodingType, [MarshalAs(UnmanagedType.LPStr)] string lpszStructType, void* pvStructInfo, [Out] byte[] pbEncoded, [In][Out] ref int pcbEncoded);

		public unsafe static byte[] EncodeObject(CryptDecodeObjectStructType lpszStructType, void* decoded)
		{
			int pcbEncoded = 0;
			if (!CryptEncodeObject(CertEncodingType.All, lpszStructType, decoded, null, ref pcbEncoded))
			{
				throw Marshal.GetLastWin32Error().ToCryptographicException();
			}
			byte[] array = new byte[pcbEncoded];
			if (!CryptEncodeObject(CertEncodingType.All, lpszStructType, decoded, array, ref pcbEncoded))
			{
				throw Marshal.GetLastWin32Error().ToCryptographicException();
			}
			return array;
		}

		public unsafe static byte[] EncodeObject(string lpszStructType, void* decoded)
		{
			int pcbEncoded = 0;
			if (!CryptEncodeObject(CertEncodingType.All, lpszStructType, decoded, null, ref pcbEncoded))
			{
				throw Marshal.GetLastWin32Error().ToCryptographicException();
			}
			byte[] array = new byte[pcbEncoded];
			if (!CryptEncodeObject(CertEncodingType.All, lpszStructType, decoded, array, ref pcbEncoded))
			{
				throw Marshal.GetLastWin32Error().ToCryptographicException();
			}
			return array;
		}

		internal static SafeChainEngineHandle CertCreateCertificateChainEngine(ref CERT_CHAIN_ENGINE_CONFIG config)
		{
			if (!CertCreateCertificateChainEngine(ref config, out var hChainEngineHandle))
			{
				int lastWin32Error = Marshal.GetLastWin32Error();
				throw lastWin32Error.ToCryptographicException();
			}
			return hChainEngineHandle;
		}

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern bool CertCreateCertificateChainEngine(ref CERT_CHAIN_ENGINE_CONFIG pConfig, out SafeChainEngineHandle hChainEngineHandle);

		[DllImport("crypt32.dll")]
		public static extern void CertFreeCertificateChainEngine(IntPtr hChainEngine);

		[DllImport("crypt32.dll", SetLastError = true)]
		public unsafe static extern bool CertGetCertificateChain(IntPtr hChainEngine, SafeCertContextHandle pCertContext, FILETIME* pTime, SafeCertStoreHandle hStore, [In] ref CERT_CHAIN_PARA pChainPara, CertChainFlags dwFlags, IntPtr pvReserved, out SafeX509ChainHandle ppChainContext);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CryptHashPublicKeyInfo(IntPtr hCryptProv, int algId, int dwFlags, CertEncodingType dwCertEncodingType, [In] ref CERT_PUBLIC_KEY_INFO pInfo, [Out] byte[] pbComputedHash, [In][Out] ref int pcbComputedHash);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CertSaveStore(SafeCertStoreHandle hCertStore, CertEncodingType dwMsgAndCertEncodingType, CertStoreSaveAs dwSaveAs, CertStoreSaveTo dwSaveTo, ref CRYPTOAPI_BLOB pvSaveToPara, int dwFlags);

		public unsafe static bool CertFindCertificateInStore(SafeCertStoreHandle hCertStore, CertFindType dwFindType, void* pvFindPara, [NotNull] ref SafeCertContextHandle pCertContext)
		{
			CERT_CONTEXT* pPrevCertContext = ((pCertContext == null) ? null : pCertContext.Disconnect());
			pCertContext = CertFindCertificateInStore(hCertStore, CertEncodingType.All, CertFindFlags.None, dwFindType, pvFindPara, pPrevCertContext);
			return !pCertContext.IsInvalid;
		}

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private unsafe static extern SafeCertContextHandle CertFindCertificateInStore(SafeCertStoreHandle hCertStore, CertEncodingType dwCertEncodingType, CertFindFlags dwFindFlags, CertFindType dwFindType, void* pvFindPara, CERT_CONTEXT* pPrevCertContext);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern int CertVerifyTimeValidity([In] ref FILETIME pTimeToVerify, [In] CERT_INFO* pCertInfo);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern CERT_EXTENSION* CertFindExtension([MarshalAs(UnmanagedType.LPStr)] string pszObjId, int cExtensions, CERT_EXTENSION* rgExtensions);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool CertGetIntendedKeyUsage(CertEncodingType dwCertEncodingType, CERT_INFO* pCertInfo, out X509KeyUsageFlags pbKeyUsage, int cbKeyUsage);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool CertGetValidUsages(int cCerts, [In] ref SafeCertContextHandle rghCerts, out int cNumOIDs, [Out] void* rghOIDs, [In][Out] ref int pcbOIDs);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CertControlStore(SafeCertStoreHandle hCertStore, CertControlStoreFlags dwFlags, CertControlStoreType dwControlType, IntPtr pvCtrlPara);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool CertDeleteCertificateFromStore(CERT_CONTEXT* pCertContext);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern void CertFreeCertificateChain(IntPtr pChainContext);

		public static bool CertVerifyCertificateChainPolicy(ChainPolicy pszPolicyOID, SafeX509ChainHandle pChainContext, ref CERT_CHAIN_POLICY_PARA pPolicyPara, ref CERT_CHAIN_POLICY_STATUS pPolicyStatus)
		{
			return CertVerifyCertificateChainPolicy((IntPtr)(int)pszPolicyOID, pChainContext, ref pPolicyPara, ref pPolicyStatus);
		}

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern bool CertVerifyCertificateChainPolicy(IntPtr pszPolicyOID, SafeX509ChainHandle pChainContext, [In] ref CERT_CHAIN_POLICY_PARA pPolicyPara, [In][Out] ref CERT_CHAIN_POLICY_STATUS pPolicyStatus);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool CryptImportPublicKeyInfoEx2(CertEncodingType dwCertEncodingType, CERT_PUBLIC_KEY_INFO* pInfo, CryptImportPublicKeyInfoFlags dwFlags, void* pvAuxInfo, out Microsoft.Win32.SafeHandles.SafeBCryptKeyHandle phKey);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CryptAcquireCertificatePrivateKey(SafeCertContextHandle pCert, CryptAcquireFlags dwFlags, IntPtr pvParameters, out SafeNCryptKeyHandle phCryptProvOrNCryptKey, out int pdwKeySpec, out bool pfCallerFreeProvOrNCryptKey);
	}

	internal static class Crypt32
	{
		internal struct CRYPT_OID_INFO
		{
			public int cbSize;

			public IntPtr pszOID;

			public IntPtr pwszName;

			public OidGroup dwGroupId;

			public int AlgId;

			public int cbData;

			public IntPtr pbData;

			public string OID => Marshal.PtrToStringAnsi(pszOID);
		}

		internal enum CryptOidInfoKeyType
		{
			CRYPT_OID_INFO_OID_KEY = 1,
			CRYPT_OID_INFO_NAME_KEY,
			CRYPT_OID_INFO_ALGID_KEY,
			CRYPT_OID_INFO_SIGN_KEY,
			CRYPT_OID_INFO_CNG_ALGID_KEY,
			CRYPT_OID_INFO_CNG_SIGN_KEY
		}

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool CertCloseStore(IntPtr hCertStore, uint dwFlags);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, EntryPoint = "CertNameToStrW", SetLastError = true)]
		internal unsafe static extern int CertNameToStr(int dwCertEncodingType, void* pName, int dwStrType, char* psz, int csz);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool CryptMsgClose(IntPtr hCryptMsg);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool CertFreeCertificateContext(IntPtr pCertContext);

		[DllImport("crypt32.dll", BestFitMapping = false, SetLastError = true)]
		internal unsafe static extern bool CryptFormatObject([In] int dwCertEncodingType, [In] int dwFormatType, [In] int dwFormatStrType, [In] IntPtr pFormatStruct, [In] byte* lpszStructType, [In] byte[] pbEncoded, [In] int cbEncoded, [Out] void* pbFormat, [In][Out] ref int pcbFormat);

		internal static CRYPT_OID_INFO FindOidInfo(CryptOidInfoKeyType keyType, string key, OidGroup group, bool fallBackToAllGroups)
		{
			IntPtr intPtr = IntPtr.Zero;
			try
			{
				intPtr = keyType switch
				{
					CryptOidInfoKeyType.CRYPT_OID_INFO_OID_KEY => Marshal.StringToCoTaskMemAnsi(key), 
					CryptOidInfoKeyType.CRYPT_OID_INFO_NAME_KEY => Marshal.StringToCoTaskMemUni(key), 
					_ => throw new NotSupportedException(), 
				};
				if (!OidGroupWillNotUseActiveDirectory(group))
				{
					OidGroup group2 = group | (OidGroup)(-2147483648);
					IntPtr intPtr2 = CryptFindOIDInfo(keyType, intPtr, group2);
					if (intPtr2 != IntPtr.Zero)
					{
						return Marshal.PtrToStructure<CRYPT_OID_INFO>(intPtr2);
					}
				}
				IntPtr intPtr3 = CryptFindOIDInfo(keyType, intPtr, group);
				if (intPtr3 != IntPtr.Zero)
				{
					return Marshal.PtrToStructure<CRYPT_OID_INFO>(intPtr3);
				}
				if (fallBackToAllGroups && group != 0)
				{
					IntPtr intPtr4 = CryptFindOIDInfo(keyType, intPtr, OidGroup.All);
					if (intPtr4 != IntPtr.Zero)
					{
						return Marshal.PtrToStructure<CRYPT_OID_INFO>(intPtr4);
					}
				}
				CRYPT_OID_INFO result = default(CRYPT_OID_INFO);
				result.AlgId = -1;
				return result;
			}
			finally
			{
				if (intPtr != IntPtr.Zero)
				{
					Marshal.FreeCoTaskMem(intPtr);
				}
			}
		}

		private static bool OidGroupWillNotUseActiveDirectory(OidGroup group)
		{
			if (group != OidGroup.HashAlgorithm && group != OidGroup.EncryptionAlgorithm && group != OidGroup.PublicKeyAlgorithm && group != OidGroup.SignatureAlgorithm && group != OidGroup.Attribute && group != OidGroup.ExtensionOrAttribute)
			{
				return group == OidGroup.KeyDerivationFunction;
			}
			return true;
		}

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr CryptFindOIDInfo(CryptOidInfoKeyType dwKeyType, IntPtr pvKey, OidGroup group);
	}

	internal static class Kernel32
	{
		[DllImport("kernel32.dll", BestFitMapping = true, CharSet = CharSet.Unicode, EntryPoint = "FormatMessageW", ExactSpelling = true, SetLastError = true)]
		private unsafe static extern int FormatMessage(int dwFlags, IntPtr lpSource, uint dwMessageId, int dwLanguageId, void* lpBuffer, int nSize, IntPtr arguments);

		internal static string GetMessage(int errorCode)
		{
			return GetMessage(errorCode, IntPtr.Zero);
		}

		internal unsafe static string GetMessage(int errorCode, IntPtr moduleHandle)
		{
			int num = 12800;
			if (moduleHandle != IntPtr.Zero)
			{
				num |= 0x800;
			}
			Span<char> span = stackalloc char[256];
			fixed (char* lpBuffer = span)
			{
				int num2 = FormatMessage(num, moduleHandle, (uint)errorCode, 0, lpBuffer, span.Length, IntPtr.Zero);
				if (num2 > 0)
				{
					return GetAndTrimString(span.Slice(0, num2));
				}
			}
			if (Marshal.GetLastWin32Error() == 122)
			{
				IntPtr intPtr = default(IntPtr);
				try
				{
					int num3 = FormatMessage(num | 0x100, moduleHandle, (uint)errorCode, 0, &intPtr, 0, IntPtr.Zero);
					if (num3 > 0)
					{
						return GetAndTrimString(new Span<char>((void*)intPtr, num3));
					}
				}
				finally
				{
					Marshal.FreeHGlobal(intPtr);
				}
			}
			return $"Unknown error (0x{errorCode:x})";
		}

		private static string GetAndTrimString(Span<char> buffer)
		{
			int num = buffer.Length;
			while (num > 0 && buffer[num - 1] <= ' ')
			{
				num--;
			}
			return buffer.Slice(0, num).ToString();
		}
	}

	internal static class BCrypt
	{
		internal enum KeyBlobMagicNumber
		{
			BCRYPT_DSA_PUBLIC_MAGIC = 1112560452,
			BCRYPT_DSA_PRIVATE_MAGIC = 1448104772,
			BCRYPT_DSA_PUBLIC_MAGIC_V2 = 843206724,
			BCRYPT_DSA_PRIVATE_MAGIC_V2 = 844517444,
			BCRYPT_ECDH_PUBLIC_P256_MAGIC = 827016005,
			BCRYPT_ECDH_PRIVATE_P256_MAGIC = 843793221,
			BCRYPT_ECDH_PUBLIC_P384_MAGIC = 860570437,
			BCRYPT_ECDH_PRIVATE_P384_MAGIC = 877347653,
			BCRYPT_ECDH_PUBLIC_P521_MAGIC = 894124869,
			BCRYPT_ECDH_PRIVATE_P521_MAGIC = 910902085,
			BCRYPT_ECDH_PUBLIC_GENERIC_MAGIC = 1347109701,
			BCRYPT_ECDH_PRIVATE_GENERIC_MAGIC = 1447772997,
			BCRYPT_ECDSA_PUBLIC_P256_MAGIC = 827540293,
			BCRYPT_ECDSA_PRIVATE_P256_MAGIC = 844317509,
			BCRYPT_ECDSA_PUBLIC_P384_MAGIC = 861094725,
			BCRYPT_ECDSA_PRIVATE_P384_MAGIC = 877871941,
			BCRYPT_ECDSA_PUBLIC_P521_MAGIC = 894649157,
			BCRYPT_ECDSA_PRIVATE_P521_MAGIC = 911426373,
			BCRYPT_ECDSA_PUBLIC_GENERIC_MAGIC = 1346650949,
			BCRYPT_ECDSA_PRIVATE_GENERIC_MAGIC = 1447314245,
			BCRYPT_RSAPUBLIC_MAGIC = 826364754,
			BCRYPT_RSAPRIVATE_MAGIC = 843141970,
			BCRYPT_RSAFULLPRIVATE_MAGIC = 859919186,
			BCRYPT_KEY_DATA_BLOB_MAGIC = 1296188491
		}

		internal struct BCRYPT_ECCKEY_BLOB
		{
			internal KeyBlobMagicNumber Magic;

			internal int cbKey;
		}

		internal enum NTSTATUS : uint
		{
			STATUS_SUCCESS = 0u,
			STATUS_NOT_FOUND = 3221226021u,
			STATUS_INVALID_PARAMETER = 3221225485u,
			STATUS_NO_MEMORY = 3221225495u,
			STATUS_AUTH_TAG_MISMATCH = 3221266434u
		}

		[DllImport("BCrypt.dll", CharSet = CharSet.Unicode)]
		internal static extern NTSTATUS BCryptDestroyKey(IntPtr hKey);

		[DllImport("BCrypt.dll", CharSet = CharSet.Unicode)]
		internal static extern NTSTATUS BCryptExportKey(Microsoft.Win32.SafeHandles.SafeBCryptKeyHandle hKey, IntPtr hExportKey, string pszBlobType, [Out] byte[] pbOutput, int cbOutput, out int pcbResult, int dwFlags);

		[DllImport("BCrypt.dll", CharSet = CharSet.Unicode)]
		internal unsafe static extern NTSTATUS BCryptGetProperty(Microsoft.Win32.SafeHandles.SafeBCryptHandle hObject, string pszProperty, void* pbOutput, int cbOutput, out int pcbResult, int dwFlags);

		internal static byte[] Consume(byte[] blob, ref int offset, int count)
		{
			byte[] array = new byte[count];
			Buffer.BlockCopy(blob, offset, array, 0, count);
			offset += count;
			return array;
		}
	}
}
