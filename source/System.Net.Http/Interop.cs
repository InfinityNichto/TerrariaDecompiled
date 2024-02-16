using System;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static class Interop
{
	internal static class WinHttp
	{
		internal class SafeWinHttpHandle : SafeHandleZeroOrMinusOneIsInvalid
		{
			private SafeWinHttpHandle _parentHandle;

			public SafeWinHttpHandle()
				: base(ownsHandle: true)
			{
			}

			public static void DisposeAndClearHandle(ref SafeWinHttpHandle safeHandle)
			{
				if (safeHandle != null)
				{
					safeHandle.Dispose();
					safeHandle = null;
				}
			}

			protected override bool ReleaseHandle()
			{
				if (_parentHandle != null)
				{
					_parentHandle.DangerousRelease();
					_parentHandle = null;
				}
				return WinHttpCloseHandle(handle);
			}
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct WINHTTP_AUTOPROXY_OPTIONS
		{
			public uint Flags;

			public uint AutoDetectFlags;

			[MarshalAs(UnmanagedType.LPWStr)]
			public string AutoConfigUrl;

			public IntPtr Reserved1;

			public uint Reserved2;

			[MarshalAs(UnmanagedType.Bool)]
			public bool AutoLoginIfChallenged;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct WINHTTP_CURRENT_USER_IE_PROXY_CONFIG
		{
			[MarshalAs(UnmanagedType.Bool)]
			public bool AutoDetect;

			public IntPtr AutoConfigUrl;

			public IntPtr Proxy;

			public IntPtr ProxyBypass;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct WINHTTP_PROXY_INFO
		{
			public uint AccessType;

			public IntPtr Proxy;

			public IntPtr ProxyBypass;
		}

		[DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern SafeWinHttpHandle WinHttpOpen(IntPtr userAgent, uint accessType, string proxyName, string proxyBypass, int flags);

		[DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool WinHttpCloseHandle(IntPtr handle);

		[DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool WinHttpGetIEProxyConfigForCurrentUser(out WINHTTP_CURRENT_USER_IE_PROXY_CONFIG proxyConfig);

		[DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool WinHttpGetProxyForUrl(SafeWinHttpHandle sessionHandle, string url, ref WINHTTP_AUTOPROXY_OPTIONS autoProxyOptions, out WINHTTP_PROXY_INFO proxyInfo);
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
		internal static extern int FreeContextBuffer([In] IntPtr contextBuffer);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern int FreeCredentialsHandle(ref CredHandle handlePtr);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern int DeleteSecurityContext(ref CredHandle handlePtr);

		[DllImport("sspicli.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int AcceptSecurityContext(ref CredHandle credentialHandle, [In] void* inContextPtr, [In] SecBufferDesc* inputBuffer, [In] ContextFlags inFlags, [In] Endianness endianness, ref CredHandle outContextPtr, [In][Out] ref SecBufferDesc outputBuffer, [In][Out] ref ContextFlags attributes, out long timeStamp);

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
