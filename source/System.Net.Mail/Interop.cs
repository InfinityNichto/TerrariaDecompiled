using System;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static class Interop
{
	internal static class Crypt32
	{
		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool CertFreeCertificateContext(IntPtr pCertContext);
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

		internal struct SecBuffer
		{
			public int cbBuffer;

			public System.Net.Security.SecurityBufferType BufferType;

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

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern int EncryptMessage(ref CredHandle contextHandle, [In] uint qualityOfProtection, [In][Out] ref SecBufferDesc inputOutput, [In] uint sequenceNumber);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int DecryptMessage([In] ref CredHandle contextHandle, [In][Out] ref SecBufferDesc inputOutput, [In] uint sequenceNumber, uint* qualityOfProtection);

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
		internal static extern int EnumerateSecurityPackagesW(out int pkgnum, out System.Net.Security.SafeFreeContextBuffer_SECURITY handle);

		[DllImport("sspicli.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int AcquireCredentialsHandleW([In] string principal, [In] string moduleName, [In] int usage, [In] void* logonID, [In] IntPtr zero, [In] void* keyCallback, [In] void* keyArgument, ref CredHandle handlePtr, out long timeStamp);

		[DllImport("sspicli.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int AcquireCredentialsHandleW([In] string principal, [In] string moduleName, [In] int usage, [In] void* logonID, [In] System.Net.Security.SafeSspiAuthDataHandle authdata, [In] void* keyCallback, [In] void* keyArgument, ref CredHandle handlePtr, out long timeStamp);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int InitializeSecurityContextW(ref CredHandle credentialHandle, [In] void* inContextPtr, [In] byte* targetName, [In] ContextFlags inFlags, [In] int reservedI, [In] Endianness endianness, [In] SecBufferDesc* inputBuffer, [In] int reservedII, ref CredHandle outContextPtr, [In][Out] ref SecBufferDesc outputBuffer, [In][Out] ref ContextFlags attributes, out long timeStamp);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int CompleteAuthToken([In] void* inContextPtr, [In][Out] ref SecBufferDesc inputBuffers);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern SECURITY_STATUS SspiFreeAuthIdentity([In] IntPtr authData);

		[DllImport("sspicli.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal static extern SECURITY_STATUS SspiEncodeStringsAsAuthIdentity([In] string userName, [In] string domainName, [In] string password, out System.Net.Security.SafeSspiAuthDataHandle authData);
	}
}
