using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Win32.SafeHandles;

internal static class Interop
{
	internal enum BOOL
	{
		FALSE,
		TRUE
	}

	internal struct UNICODE_STRING
	{
		internal ushort Length;

		internal ushort MaximumLength;

		internal IntPtr Buffer;
	}

	internal static class Crypt32
	{
		internal struct CERT_CONTEXT
		{
			internal MsgEncodingType dwCertEncodingType;

			internal unsafe byte* pbCertEncoded;

			internal int cbCertEncoded;

			internal unsafe CERT_INFO* pCertInfo;

			internal IntPtr hCertStore;
		}

		internal struct CERT_INFO
		{
			internal int dwVersion;

			internal DATA_BLOB SerialNumber;

			internal CRYPT_ALGORITHM_IDENTIFIER SignatureAlgorithm;

			internal DATA_BLOB Issuer;

			internal FILETIME NotBefore;

			internal FILETIME NotAfter;

			internal DATA_BLOB Subject;

			internal CERT_PUBLIC_KEY_INFO SubjectPublicKeyInfo;

			internal CRYPT_BIT_BLOB IssuerUniqueId;

			internal CRYPT_BIT_BLOB SubjectUniqueId;

			internal int cExtension;

			internal IntPtr rgExtension;
		}

		internal struct CERT_PUBLIC_KEY_INFO
		{
			internal CRYPT_ALGORITHM_IDENTIFIER Algorithm;

			internal CRYPT_BIT_BLOB PublicKey;
		}

		internal struct CRYPT_ALGORITHM_IDENTIFIER
		{
			internal IntPtr pszObjId;

			internal DATA_BLOB Parameters;
		}

		internal struct CRYPT_BIT_BLOB
		{
			internal int cbData;

			internal IntPtr pbData;

			internal int cUnusedBits;
		}

		internal struct DATA_BLOB
		{
			internal uint cbData;

			internal IntPtr pbData;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct SSL_EXTRA_CERT_CHAIN_POLICY_PARA
		{
			internal uint cbSize;

			internal uint dwAuthType;

			internal uint fdwChecks;

			internal unsafe char* pwszServerName;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct CERT_CHAIN_POLICY_PARA
		{
			public uint cbSize;

			public uint dwFlags;

			public unsafe SSL_EXTRA_CERT_CHAIN_POLICY_PARA* pvExtraPolicyPara;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct CERT_CHAIN_POLICY_STATUS
		{
			public uint cbSize;

			public uint dwError;

			public int lChainIndex;

			public int lElementIndex;

			public unsafe void* pvExtraPolicyStatus;
		}

		[Flags]
		internal enum MsgEncodingType
		{
			PKCS_7_ASN_ENCODING = 0x10000,
			X509_ASN_ENCODING = 1,
			All = 0x10001
		}

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool CertFreeCertificateContext(IntPtr pCertContext);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool CertVerifyCertificateChainPolicy(IntPtr pszPolicyOID, SafeX509ChainHandle pChainContext, [In] ref CERT_CHAIN_POLICY_PARA pPolicyPara, [In][Out] ref CERT_CHAIN_POLICY_STATUS pPolicyStatus);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern CERT_CONTEXT* CertEnumCertificatesInStore(IntPtr hCertStore, CERT_CONTEXT* pPrevCertContext);
	}

	internal static class Kernel32
	{
		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool CloseHandle(IntPtr handle);
	}

	internal static class SChannel
	{
		public struct SCHANNEL_ALERT_TOKEN
		{
			public uint dwTokenType;

			public uint dwAlertType;

			public uint dwAlertNumber;
		}
	}

	internal enum SECURITY_STATUS
	{
		OK = 0,
		ContinueNeeded = 590610,
		CompleteNeeded = 590611,
		CompAndContinue = 590612,
		ContextExpired = 590615,
		CredentialsNeeded = 590624,
		Renegotiate = 590625,
		OutOfMemory = -2146893056,
		InvalidHandle = -2146893055,
		Unsupported = -2146893054,
		TargetUnknown = -2146893053,
		InternalError = -2146893052,
		PackageNotFound = -2146893051,
		NotOwner = -2146893050,
		CannotInstall = -2146893049,
		InvalidToken = -2146893048,
		CannotPack = -2146893047,
		QopNotSupported = -2146893046,
		NoImpersonation = -2146893045,
		LogonDenied = -2146893044,
		UnknownCredentials = -2146893043,
		NoCredentials = -2146893042,
		MessageAltered = -2146893041,
		OutOfSequence = -2146893040,
		NoAuthenticatingAuthority = -2146893039,
		IncompleteMessage = -2146893032,
		IncompleteCredentials = -2146893024,
		BufferNotEnough = -2146893023,
		WrongPrincipal = -2146893022,
		TimeSkew = -2146893020,
		UntrustedRoot = -2146893019,
		IllegalMessage = -2146893018,
		CertUnknown = -2146893017,
		CertExpired = -2146893016,
		DecryptFailure = -2146893008,
		AlgorithmMismatch = -2146893007,
		SecurityQosFailed = -2146893006,
		SmartcardLogonRequired = -2146892994,
		UnsupportedPreauth = -2146892989,
		BadBinding = -2146892986,
		DowngradeDetected = -2146892976,
		ApplicationProtocolMismatch = -2146892953,
		NoRenegotiation = 590688
	}

	internal enum ApplicationProtocolNegotiationStatus
	{
		None,
		Success,
		SelectedClientOnly
	}

	internal enum ApplicationProtocolNegotiationExt
	{
		None,
		NPN,
		ALPN
	}

	internal struct SecPkgContext_ApplicationProtocol
	{
		public ApplicationProtocolNegotiationStatus ProtoNegoStatus;

		public ApplicationProtocolNegotiationExt ProtoNegoExt;

		public byte ProtocolIdSize;

		public unsafe fixed byte ProtocolId[255];

		public unsafe byte[] Protocol
		{
			get
			{
				fixed (byte* pointer = ProtocolId)
				{
					return new Span<byte>(pointer, ProtocolIdSize).ToArray();
				}
			}
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal struct Sec_Application_Protocols
	{
		public uint ProtocolListsSize;

		public ApplicationProtocolNegotiationExt ProtocolExtensionType;

		public short ProtocolListSize;

		public unsafe static byte[] ToByteArray(List<SslApplicationProtocol> applicationProtocols)
		{
			long num = 0L;
			ReadOnlyMemory<byte> protocol;
			for (int i = 0; i < applicationProtocols.Count; i++)
			{
				protocol = applicationProtocols[i].Protocol;
				int length = protocol.Length;
				if (length == 0 || length > 255)
				{
					throw new ArgumentException(System.SR.net_ssl_app_protocols_invalid, "applicationProtocols");
				}
				num += length + 1;
				if (num > 32767)
				{
					throw new ArgumentException(System.SR.net_ssl_app_protocols_invalid, "applicationProtocols");
				}
			}
			Sec_Application_Protocols value = default(Sec_Application_Protocols);
			int num2 = sizeof(Sec_Application_Protocols) - 4;
			value.ProtocolListsSize = (uint)(num2 + num);
			value.ProtocolExtensionType = ApplicationProtocolNegotiationExt.ALPN;
			value.ProtocolListSize = (short)num;
			byte[] array = new byte[sizeof(Sec_Application_Protocols) + num];
			int num3 = 0;
			MemoryMarshal.Write(array.AsSpan(num3), ref value);
			num3 += sizeof(Sec_Application_Protocols);
			for (int j = 0; j < applicationProtocols.Count; j++)
			{
				protocol = applicationProtocols[j].Protocol;
				ReadOnlySpan<byte> span = protocol.Span;
				array[num3++] = (byte)span.Length;
				span.CopyTo(array.AsSpan(num3));
				num3 += span.Length;
			}
			return array;
		}
	}

	internal static class SspiCli
	{
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		internal struct CredHandle
		{
			private IntPtr dwLower;

			private IntPtr dwUpper;

			public bool IsZero
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get
				{
					if (dwLower == IntPtr.Zero)
					{
						return dwUpper == IntPtr.Zero;
					}
					return false;
				}
			}

			internal void SetToInvalid()
			{
				dwLower = IntPtr.Zero;
				dwUpper = IntPtr.Zero;
			}

			public override string ToString()
			{
				return dwLower.ToString("x") + ":" + dwUpper.ToString("x");
			}
		}

		internal enum ContextAttribute
		{
			SECPKG_ATTR_SIZES = 0,
			SECPKG_ATTR_NAMES = 1,
			SECPKG_ATTR_LIFESPAN = 2,
			SECPKG_ATTR_DCE_INFO = 3,
			SECPKG_ATTR_STREAM_SIZES = 4,
			SECPKG_ATTR_AUTHORITY = 6,
			SECPKG_ATTR_PACKAGE_INFO = 10,
			SECPKG_ATTR_NEGOTIATION_INFO = 12,
			SECPKG_ATTR_UNIQUE_BINDINGS = 25,
			SECPKG_ATTR_ENDPOINT_BINDINGS = 26,
			SECPKG_ATTR_CLIENT_SPECIFIED_TARGET = 27,
			SECPKG_ATTR_APPLICATION_PROTOCOL = 35,
			SECPKG_ATTR_REMOTE_CERT_CONTEXT = 83,
			SECPKG_ATTR_LOCAL_CERT_CONTEXT = 84,
			SECPKG_ATTR_ROOT_STORE = 85,
			SECPKG_ATTR_ISSUER_LIST_EX = 89,
			SECPKG_ATTR_CLIENT_CERT_POLICY = 96,
			SECPKG_ATTR_CONNECTION_INFO = 90,
			SECPKG_ATTR_CIPHER_INFO = 100,
			SECPKG_ATTR_UI_INFO = 104
		}

		[Flags]
		internal enum ContextFlags
		{
			Zero = 0,
			Delegate = 1,
			MutualAuth = 2,
			ReplayDetect = 4,
			SequenceDetect = 8,
			Confidentiality = 0x10,
			UseSessionKey = 0x20,
			AllocateMemory = 0x100,
			Connection = 0x800,
			InitExtendedError = 0x4000,
			AcceptExtendedError = 0x8000,
			InitStream = 0x8000,
			AcceptStream = 0x10000,
			InitIntegrity = 0x10000,
			AcceptIntegrity = 0x20000,
			InitManualCredValidation = 0x80000,
			InitUseSuppliedCreds = 0x80,
			InitIdentify = 0x20000,
			AcceptIdentify = 0x80000,
			ProxyBindings = 0x4000000,
			AllowMissingBindings = 0x10000000,
			UnverifiedTargetName = 0x20000000
		}

		internal enum Endianness
		{
			SECURITY_NETWORK_DREP = 0,
			SECURITY_NATIVE_DREP = 0x10
		}

		internal enum CredentialUse
		{
			SECPKG_CRED_INBOUND = 1,
			SECPKG_CRED_OUTBOUND,
			SECPKG_CRED_BOTH
		}

		internal struct CERT_CHAIN_ELEMENT
		{
			public uint cbSize;

			public IntPtr pCertContext;
		}

		internal struct SecPkgContext_IssuerListInfoEx
		{
			public IntPtr aIssuers;

			public uint cIssuers;
		}

		internal struct SCHANNEL_CRED
		{
			[Flags]
			public enum Flags
			{
				Zero = 0,
				SCH_CRED_NO_SYSTEM_MAPPER = 2,
				SCH_CRED_NO_SERVERNAME_CHECK = 4,
				SCH_CRED_MANUAL_CRED_VALIDATION = 8,
				SCH_CRED_NO_DEFAULT_CREDS = 0x10,
				SCH_CRED_AUTO_CRED_VALIDATION = 0x20,
				SCH_SEND_AUX_RECORD = 0x200000,
				SCH_USE_STRONG_CRYPTO = 0x400000
			}

			public int dwVersion;

			public int cCreds;

			public unsafe Crypt32.CERT_CONTEXT** paCred;

			public IntPtr hRootStore;

			public int cMappers;

			public IntPtr aphMappers;

			public int cSupportedAlgs;

			public IntPtr palgSupportedAlgs;

			public int grbitEnabledProtocols;

			public int dwMinimumCipherStrength;

			public int dwMaximumCipherStrength;

			public int dwSessionLifespan;

			public Flags dwFlags;

			public int reserved;
		}

		internal struct SCH_CREDENTIALS
		{
			[Flags]
			public enum Flags
			{
				Zero = 0,
				SCH_CRED_NO_SYSTEM_MAPPER = 2,
				SCH_CRED_NO_SERVERNAME_CHECK = 4,
				SCH_CRED_MANUAL_CRED_VALIDATION = 8,
				SCH_CRED_NO_DEFAULT_CREDS = 0x10,
				SCH_CRED_AUTO_CRED_VALIDATION = 0x20,
				SCH_CRED_USE_DEFAULT_CREDS = 0x40,
				SCH_DISABLE_RECONNECTS = 0x80,
				SCH_CRED_REVOCATION_CHECK_END_CERT = 0x100,
				SCH_CRED_REVOCATION_CHECK_CHAIN = 0x200,
				SCH_CRED_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT = 0x400,
				SCH_CRED_IGNORE_NO_REVOCATION_CHECK = 0x800,
				SCH_CRED_IGNORE_REVOCATION_OFFLINE = 0x1000,
				SCH_CRED_CACHE_ONLY_URL_RETRIEVAL_ON_CREATE = 0x2000,
				SCH_SEND_ROOT_CERT = 0x40000,
				SCH_SEND_AUX_RECORD = 0x200000,
				SCH_USE_STRONG_CRYPTO = 0x400000,
				SCH_USE_PRESHAREDKEY_ONLY = 0x800000,
				SCH_ALLOW_NULL_ENCRYPTION = 0x2000000
			}

			public int dwVersion;

			public int dwCredformat;

			public int cCreds;

			public unsafe Crypt32.CERT_CONTEXT** paCred;

			public IntPtr hRootStore;

			public int cMappers;

			public IntPtr aphMappers;

			public int dwSessionLifespan;

			public Flags dwFlags;

			public int cTlsParameters;

			public unsafe TLS_PARAMETERS* pTlsParameters;
		}

		internal struct TLS_PARAMETERS
		{
			[Flags]
			public enum Flags
			{
				Zero = 0,
				TLS_PARAMS_OPTIONAL = 1
			}

			public int cAlpnIds;

			public IntPtr rgstrAlpnIds;

			public uint grbitDisabledProtocols;

			public int cDisabledCrypto;

			public unsafe CRYPTO_SETTINGS* pDisabledCrypto;

			public Flags dwFlags;
		}

		internal struct CRYPTO_SETTINGS
		{
			public enum TlsAlgorithmUsage
			{
				TlsParametersCngAlgUsageKeyExchange,
				TlsParametersCngAlgUsageSignature,
				TlsParametersCngAlgUsageCipher,
				TlsParametersCngAlgUsageDigest,
				TlsParametersCngAlgUsageCertSig
			}

			public TlsAlgorithmUsage eAlgorithmUsage;

			public unsafe UNICODE_STRING* strCngAlgId;

			public int cChainingModes;

			public unsafe UNICODE_STRING* rgstrChainingModes;

			public int dwMinBitLength;

			public int dwMaxBitLength;
		}

		internal struct SecBuffer
		{
			public int cbBuffer;

			public SecurityBufferType BufferType;

			public IntPtr pvBuffer;

			public unsafe static readonly int Size = sizeof(SecBuffer);
		}

		internal struct SecBufferDesc
		{
			public readonly int ulVersion;

			public readonly int cBuffers;

			public unsafe void* pBuffers;

			public unsafe SecBufferDesc(int count)
			{
				ulVersion = 0;
				cBuffers = count;
				pBuffers = null;
			}
		}

		internal struct SecPkgCred_ClientCertPolicy
		{
			public uint dwFlags;

			public Guid guidPolicyId;

			public uint dwCertFlags;

			public uint dwUrlRetrievalTimeout;

			public BOOL fCheckRevocationFreshnessTime;

			public uint dwRevocationFreshnessTime;

			public BOOL fOmitUsageCheck;

			public unsafe char* pwszSslCtlStoreName;

			public unsafe char* pwszSslCtlIdentifier;
		}

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern int EncryptMessage(ref CredHandle contextHandle, [In] uint qualityOfProtection, [In][Out] ref SecBufferDesc inputOutput, [In] uint sequenceNumber);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int DecryptMessage([In] ref CredHandle contextHandle, [In][Out] ref SecBufferDesc inputOutput, [In] uint sequenceNumber, uint* qualityOfProtection);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern int QuerySecurityContextToken(ref CredHandle phContext, out SecurityContextTokenHandle handle);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern int FreeContextBuffer([In] IntPtr contextBuffer);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern int FreeCredentialsHandle(ref CredHandle handlePtr);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern int DeleteSecurityContext(ref CredHandle handlePtr);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int AcceptSecurityContext(ref CredHandle credentialHandle, [In] void* inContextPtr, [In] SecBufferDesc* inputBuffer, [In] ContextFlags inFlags, [In] Endianness endianness, ref CredHandle outContextPtr, [In][Out] ref SecBufferDesc outputBuffer, [In][Out] ref ContextFlags attributes, out long timeStamp);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int QueryContextAttributesW(ref CredHandle contextHandle, [In] ContextAttribute attribute, [In] void* buffer);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern int EnumerateSecurityPackagesW(out int pkgnum, out SafeFreeContextBuffer_SECURITY handle);

		[DllImport("sspicli.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int AcquireCredentialsHandleW([In] string principal, [In] string moduleName, [In] int usage, [In] void* logonID, [In] IntPtr zero, [In] void* keyCallback, [In] void* keyArgument, ref CredHandle handlePtr, out long timeStamp);

		[DllImport("sspicli.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int AcquireCredentialsHandleW([In] string principal, [In] string moduleName, [In] int usage, [In] void* logonID, [In] SafeSspiAuthDataHandle authdata, [In] void* keyCallback, [In] void* keyArgument, ref CredHandle handlePtr, out long timeStamp);

		[DllImport("sspicli.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int AcquireCredentialsHandleW([In] string principal, [In] string moduleName, [In] int usage, [In] void* logonID, [In] SCHANNEL_CRED* authData, [In] void* keyCallback, [In] void* keyArgument, ref CredHandle handlePtr, out long timeStamp);

		[DllImport("sspicli.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int AcquireCredentialsHandleW([In] string principal, [In] string moduleName, [In] int usage, [In] void* logonID, [In] SCH_CREDENTIALS* authData, [In] void* keyCallback, [In] void* keyArgument, ref CredHandle handlePtr, out long timeStamp);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int InitializeSecurityContextW(ref CredHandle credentialHandle, [In] void* inContextPtr, [In] byte* targetName, [In] ContextFlags inFlags, [In] int reservedI, [In] Endianness endianness, [In] SecBufferDesc* inputBuffer, [In] int reservedII, ref CredHandle outContextPtr, [In][Out] ref SecBufferDesc outputBuffer, [In][Out] ref ContextFlags attributes, out long timeStamp);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int CompleteAuthToken([In] void* inContextPtr, [In][Out] ref SecBufferDesc inputBuffers);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int ApplyControlToken([In] void* inContextPtr, [In][Out] ref SecBufferDesc inputBuffers);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern SECURITY_STATUS SspiFreeAuthIdentity([In] IntPtr authData);

		[DllImport("sspicli.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal static extern SECURITY_STATUS SspiEncodeStringsAsAuthIdentity([In] string userName, [In] string domainName, [In] string password, out SafeSspiAuthDataHandle authData);

		[DllImport("sspicli.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal static extern SECURITY_STATUS SetCredentialsAttributesW([In] ref CredHandle handlePtr, [In] long ulAttribute, [In] ref SecPkgCred_ClientCertPolicy pBuffer, [In] long cbBuffer);
	}
}
