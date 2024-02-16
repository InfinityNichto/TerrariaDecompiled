using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

internal static class Interop
{
	internal enum BOOLEAN : byte
	{
		FALSE,
		TRUE
	}

	internal struct SECURITY_QUALITY_OF_SERVICE
	{
		public uint Length;

		public ImpersonationLevel ImpersonationLevel;

		public ContextTrackingMode ContextTrackingMode;

		public BOOLEAN EffectiveOnly;
	}

	public enum ImpersonationLevel : uint
	{
		Anonymous,
		Identification,
		Impersonation,
		Delegation
	}

	public enum ContextTrackingMode : byte
	{
		Static,
		Dynamic
	}

	internal struct UNICODE_STRING
	{
		internal ushort Length;

		internal ushort MaximumLength;

		internal IntPtr Buffer;
	}

	internal struct OBJECT_ATTRIBUTES
	{
		public uint Length;

		public IntPtr RootDirectory;

		public unsafe UNICODE_STRING* ObjectName;

		public ObjectAttributes Attributes;

		public unsafe void* SecurityDescriptor;

		public unsafe SECURITY_QUALITY_OF_SERVICE* SecurityQualityOfService;
	}

	[Flags]
	public enum ObjectAttributes : uint
	{
		OBJ_INHERIT = 2u,
		OBJ_PERMANENT = 0x10u,
		OBJ_EXCLUSIVE = 0x20u,
		OBJ_CASE_INSENSITIVE = 0x40u,
		OBJ_OPENIF = 0x80u,
		OBJ_OPENLINK = 0x100u
	}

	internal struct LUID
	{
		internal uint LowPart;

		internal int HighPart;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	internal struct TOKEN_GROUPS
	{
		internal uint GroupCount;

		internal SID_AND_ATTRIBUTES Groups;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	internal struct SID_AND_ATTRIBUTES
	{
		internal IntPtr Sid;

		internal uint Attributes;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	internal struct TOKEN_PRIMARY_GROUP
	{
		internal IntPtr PrimaryGroup;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	internal struct TOKEN_STATISTICS
	{
		internal LUID TokenId;

		internal LUID AuthenticationId;

		internal long ExpirationTime;

		internal uint TokenType;

		internal uint ImpersonationLevel;

		internal uint DynamicCharged;

		internal uint DynamicAvailable;

		internal uint GroupCount;

		internal uint PrivilegeCount;

		internal LUID ModifiedId;
	}

	internal struct LSA_TRANSLATED_NAME
	{
		internal int Use;

		internal UNICODE_INTPTR_STRING Name;

		internal int DomainIndex;
	}

	internal struct LSA_TRANSLATED_SID2
	{
		internal int Use;

		internal IntPtr Sid;

		internal int DomainIndex;

		private uint Flags;
	}

	internal struct LSA_TRUST_INFORMATION
	{
		internal UNICODE_INTPTR_STRING Name;

		internal IntPtr Sid;
	}

	internal struct LSA_REFERENCED_DOMAIN_LIST
	{
		internal int Entries;

		internal IntPtr Domains;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	internal struct UNICODE_INTPTR_STRING
	{
		internal ushort Length;

		internal ushort MaxLength;

		internal IntPtr Buffer;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	internal struct SECURITY_LOGON_SESSION_DATA
	{
		internal uint Size;

		internal LUID LogonId;

		internal UNICODE_INTPTR_STRING UserName;

		internal UNICODE_INTPTR_STRING LogonDomain;

		internal UNICODE_INTPTR_STRING AuthenticationPackage;

		internal uint LogonType;

		internal uint Session;

		internal IntPtr Sid;

		internal long LogonTime;
	}

	internal static class Kernel32
	{
		[DllImport("kernel32.dll")]
		internal static extern IntPtr GetCurrentProcess();

		[DllImport("kernel32.dll")]
		internal static extern IntPtr GetCurrentThread();

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle, IntPtr hTargetProcessHandle, ref SafeAccessTokenHandle lpTargetHandle, uint dwDesiredAccess, bool bInheritHandle, uint dwOptions);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool CloseHandle(IntPtr handle);
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct CLAIM_SECURITY_ATTRIBUTE_INFORMATION_V1
	{
		[FieldOffset(0)]
		public IntPtr pAttributeV1;
	}

	internal struct CLAIM_SECURITY_ATTRIBUTES_INFORMATION
	{
		public ushort Version;

		public ushort Reserved;

		public uint AttributeCount;

		public CLAIM_SECURITY_ATTRIBUTE_INFORMATION_V1 Attribute;
	}

	[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
	internal struct CLAIM_VALUES_ATTRIBUTE_V1
	{
		[FieldOffset(0)]
		public IntPtr pInt64;

		[FieldOffset(0)]
		public IntPtr pUint64;

		[FieldOffset(0)]
		public IntPtr ppString;

		[FieldOffset(0)]
		public IntPtr pFqbn;

		[FieldOffset(0)]
		public IntPtr pOctetString;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	internal struct CLAIM_SECURITY_ATTRIBUTE_V1
	{
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Name;

		public ClaimSecurityAttributeType ValueType;

		public ushort Reserved;

		public uint Flags;

		public uint ValueCount;

		public CLAIM_VALUES_ATTRIBUTE_V1 Values;
	}

	internal enum ClaimSecurityAttributeType : ushort
	{
		CLAIM_SECURITY_ATTRIBUTE_TYPE_INVALID = 0,
		CLAIM_SECURITY_ATTRIBUTE_TYPE_INT64 = 1,
		CLAIM_SECURITY_ATTRIBUTE_TYPE_UINT64 = 2,
		CLAIM_SECURITY_ATTRIBUTE_TYPE_STRING = 3,
		CLAIM_SECURITY_ATTRIBUTE_TYPE_FQBN = 4,
		CLAIM_SECURITY_ATTRIBUTE_TYPE_SID = 5,
		CLAIM_SECURITY_ATTRIBUTE_TYPE_BOOLEAN = 6,
		CLAIM_SECURITY_ATTRIBUTE_TYPE_OCTET_STRING = 16
	}

	internal static class Advapi32
	{
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct MARSHALLED_UNICODE_STRING
		{
			internal ushort Length;

			internal ushort MaximumLength;

			[MarshalAs(UnmanagedType.LPWStr)]
			internal string Buffer;
		}

		internal struct LSA_STRING
		{
			internal ushort Length;

			internal ushort MaximumLength;

			internal IntPtr Buffer;

			internal LSA_STRING(IntPtr pBuffer, ushort length)
			{
				Length = length;
				MaximumLength = length;
				Buffer = pBuffer;
			}
		}

		[DllImport("advapi32.dll", SetLastError = true)]
		internal static extern bool OpenProcessToken(IntPtr ProcessToken, TokenAccessLevels DesiredAccess, out SafeAccessTokenHandle TokenHandle);

		[DllImport("advapi32.dll", SetLastError = true)]
		internal static extern bool GetTokenInformation(SafeAccessTokenHandle TokenHandle, uint TokenInformationClass, SafeLocalAllocHandle TokenInformation, uint TokenInformationLength, out uint ReturnLength);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool GetTokenInformation(IntPtr TokenHandle, uint TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);

		[DllImport("advapi32.dll", SetLastError = true)]
		internal static extern bool DuplicateTokenEx(SafeAccessTokenHandle hExistingToken, uint dwDesiredAccess, IntPtr lpTokenAttributes, uint ImpersonationLevel, uint TokenType, ref SafeAccessTokenHandle phNewToken);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern uint LsaLookupNames2(SafeLsaPolicyHandle handle, int flags, int count, MARSHALLED_UNICODE_STRING[] names, out SafeLsaMemoryHandle referencedDomains, out SafeLsaMemoryHandle sids);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern uint LsaLookupSids(SafeLsaPolicyHandle handle, int count, IntPtr[] sids, out SafeLsaMemoryHandle referencedDomains, out SafeLsaMemoryHandle names);

		[DllImport("advapi32.dll", SetLastError = true)]
		internal static extern int LsaClose(IntPtr handle);

		[DllImport("advapi32.dll", SetLastError = true)]
		internal static extern int LsaFreeMemory(IntPtr handle);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern uint LsaOpenPolicy(ref UNICODE_STRING SystemName, ref OBJECT_ATTRIBUTES ObjectAttributes, int AccessMask, out SafeLsaPolicyHandle PolicyHandle);

		internal unsafe static uint LsaOpenPolicy(string SystemName, ref OBJECT_ATTRIBUTES Attributes, int AccessMask, out SafeLsaPolicyHandle PolicyHandle)
		{
			UNICODE_STRING SystemName2 = default(UNICODE_STRING);
			checked
			{
				if (SystemName != null)
				{
					fixed (char* ptr = SystemName)
					{
						SystemName2.Length = (ushort)(SystemName.Length * 2);
						SystemName2.MaximumLength = (ushort)(SystemName.Length * 2);
						SystemName2.Buffer = (IntPtr)ptr;
						return LsaOpenPolicy(ref SystemName2, ref Attributes, AccessMask, out PolicyHandle);
					}
				}
				return LsaOpenPolicy(ref SystemName2, ref Attributes, AccessMask, out PolicyHandle);
			}
		}

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "ConvertStringSidToSidW", SetLastError = true)]
		internal static extern int ConvertStringSidToSid(string stringSid, out IntPtr ByteArray);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern int CreateWellKnownSid(int sidType, byte[] domainSid, byte[] resultSid, ref uint resultSidLength);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern int GetWindowsAccountDomainSid(byte[] sid, byte[] resultSid, ref uint resultSidLength);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern int IsWellKnownSid(byte[] sid, int type);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "EqualDomainSid", SetLastError = true)]
		internal static extern int IsEqualDomainSid(byte[] sid1, byte[] sid2, out bool result);

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern bool OpenThreadToken(IntPtr ThreadHandle, TokenAccessLevels dwDesiredAccess, bool bOpenAsSelf, out SafeAccessTokenHandle phThreadToken);

		internal static bool OpenThreadToken(TokenAccessLevels desiredAccess, WinSecurityContext openAs, out SafeAccessTokenHandle tokenHandle)
		{
			bool bOpenAsSelf = true;
			if (openAs == WinSecurityContext.Thread)
			{
				bOpenAsSelf = false;
			}
			if (OpenThreadToken(Kernel32.GetCurrentThread(), desiredAccess, bOpenAsSelf, out tokenHandle))
			{
				return true;
			}
			if (openAs == WinSecurityContext.Both)
			{
				bOpenAsSelf = false;
				if (OpenThreadToken(Kernel32.GetCurrentThread(), desiredAccess, bOpenAsSelf, out tokenHandle))
				{
					return true;
				}
			}
			return false;
		}

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal static extern bool RevertToSelf();

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool ImpersonateLoggedOnUser(SafeAccessTokenHandle userToken);

		[DllImport("advapi32.dll")]
		internal static extern uint LsaNtStatusToWinError(uint status);

		[DllImport("advapi32.dll")]
		internal static extern bool AllocateLocallyUniqueId(out LUID Luid);

		[DllImport("advapi32.dll", SetLastError = true)]
		internal static extern bool CheckTokenMembership(SafeAccessTokenHandle TokenHandle, byte[] SidToCheck, ref bool IsMember);
	}

	internal static class SspiCli
	{
		internal enum KERB_LOGON_SUBMIT_TYPE
		{
			KerbS4ULogon = 12
		}

		internal struct KERB_S4U_LOGON
		{
			internal KERB_LOGON_SUBMIT_TYPE MessageType;

			internal KerbS4uLogonFlags Flags;

			internal UNICODE_STRING ClientUpn;

			internal UNICODE_STRING ClientRealm;
		}

		[Flags]
		internal enum KerbS4uLogonFlags
		{
			None = 0,
			KERB_S4U_LOGON_FLAG_CHECK_LOGONHOURS = 2,
			KERB_S4U_LOGON_FLAG_IDENTITY = 8
		}

		internal struct QUOTA_LIMITS
		{
			internal IntPtr PagedPoolLimit;

			internal IntPtr NonPagedPoolLimit;

			internal IntPtr MinimumWorkingSetSize;

			internal IntPtr MaximumWorkingSetSize;

			internal IntPtr PagefileLimit;

			internal long TimeLimit;
		}

		internal enum SECURITY_LOGON_TYPE
		{
			Network = 3
		}

		internal struct TOKEN_SOURCE
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			internal byte[] SourceName;

			internal LUID SourceIdentifier;
		}

		[DllImport("sspicli.dll", SetLastError = true)]
		internal static extern int LsaGetLogonSessionData(ref LUID LogonId, out SafeLsaReturnBufferHandle ppLogonSessionData);

		[DllImport("sspicli.dll", SetLastError = true)]
		internal static extern int LsaFreeReturnBuffer(IntPtr handle);

		[DllImport("sspicli.dll")]
		internal static extern int LsaConnectUntrusted(out SafeLsaHandle LsaHandle);

		[DllImport("sspicli.dll")]
		internal static extern int LsaDeregisterLogonProcess(IntPtr LsaHandle);

		[DllImport("sspicli.dll")]
		internal static extern int LsaLogonUser([In] SafeLsaHandle LsaHandle, [In] ref Advapi32.LSA_STRING OriginName, [In] SECURITY_LOGON_TYPE LogonType, [In] int AuthenticationPackage, [In] IntPtr AuthenticationInformation, [In] int AuthenticationInformationLength, [In] IntPtr LocalGroups, [In] ref TOKEN_SOURCE SourceContext, out SafeLsaReturnBufferHandle ProfileBuffer, out int ProfileBufferLength, out LUID LogonId, out SafeAccessTokenHandle Token, out QUOTA_LIMITS Quotas, out int SubStatus);

		[DllImport("sspicli.dll")]
		internal static extern int LsaLookupAuthenticationPackage(SafeLsaHandle LsaHandle, [In] ref Advapi32.LSA_STRING PackageName, out int AuthenticationPackage);
	}
}
