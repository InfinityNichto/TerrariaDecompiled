using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

internal static class Interop
{
	internal static class Advapi32
	{
		internal struct LUID
		{
			internal int LowPart;

			internal int HighPart;
		}

		internal struct LUID_AND_ATTRIBUTES
		{
			public LUID Luid;

			public uint Attributes;
		}

		internal struct TOKEN_PRIVILEGE
		{
			public uint PrivilegeCount;

			public LUID_AND_ATTRIBUTES Privileges;
		}

		internal enum SECURITY_IMPERSONATION_LEVEL : uint
		{
			SecurityAnonymous,
			SecurityIdentification,
			SecurityImpersonation,
			SecurityDelegation
		}

		[DllImport("advapi32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "LookupPrivilegeValueW", SetLastError = true)]
		internal static extern bool LookupPrivilegeValue([MarshalAs(UnmanagedType.LPTStr)] string lpSystemName, [MarshalAs(UnmanagedType.LPTStr)] string lpName, out LUID lpLuid);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal static extern bool RevertToSelf();

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "ConvertSecurityDescriptorToStringSecurityDescriptorW", ExactSpelling = true, SetLastError = true)]
		internal static extern bool ConvertSdToStringSd(byte[] securityDescriptor, uint requestedRevision, uint securityInformation, out IntPtr resultString, ref uint resultStringLength);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "ConvertStringSecurityDescriptorToSecurityDescriptorW", ExactSpelling = true, SetLastError = true)]
		internal static extern bool ConvertStringSdToSd(string stringSd, uint stringSdRevision, out IntPtr resultSd, ref uint resultSdLength);

		[DllImport("advapi32.dll", EntryPoint = "GetSecurityInfo", ExactSpelling = true, SetLastError = true)]
		internal static extern uint GetSecurityInfoByHandle(SafeHandle handle, uint objectType, uint securityInformation, out IntPtr sidOwner, out IntPtr sidGroup, out IntPtr dacl, out IntPtr sacl, out IntPtr securityDescriptor);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "SetSecurityInfo", ExactSpelling = true, SetLastError = true)]
		internal static extern uint SetSecurityInfoByHandle(SafeHandle handle, uint objectType, uint securityInformation, byte[] owner, byte[] group, byte[] dacl, byte[] sacl);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetNamedSecurityInfoW", ExactSpelling = true, SetLastError = true)]
		internal static extern uint GetSecurityInfoByName(string name, uint objectType, uint securityInformation, out IntPtr sidOwner, out IntPtr sidGroup, out IntPtr dacl, out IntPtr sacl, out IntPtr securityDescriptor);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "SetNamedSecurityInfoW", ExactSpelling = true, SetLastError = true)]
		internal static extern uint SetSecurityInfoByName(string name, uint objectType, uint securityInformation, byte[] owner, byte[] group, byte[] dacl, byte[] sacl);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal static extern uint GetSecurityDescriptorLength(IntPtr byteArray);

		[DllImport("advapi32.dll", SetLastError = true)]
		internal static extern bool OpenThreadToken(IntPtr ThreadHandle, TokenAccessLevels dwDesiredAccess, bool bOpenAsSelf, out SafeTokenHandle phThreadToken);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool OpenProcessToken(IntPtr ProcessToken, TokenAccessLevels DesiredAccess, out SafeTokenHandle TokenHandle);

		[DllImport("advapi32.dll", SetLastError = true)]
		internal static extern bool SetThreadToken(IntPtr ThreadHandle, SafeTokenHandle hToken);

		[DllImport("advapi32.dll", SetLastError = true)]
		internal unsafe static extern bool AdjustTokenPrivileges(SafeTokenHandle TokenHandle, bool DisableAllPrivileges, TOKEN_PRIVILEGE* NewState, uint BufferLength, TOKEN_PRIVILEGE* PreviousState, uint* ReturnLength);

		[DllImport("advapi32.dll", SetLastError = true)]
		internal static extern bool DuplicateTokenEx(SafeTokenHandle ExistingTokenHandle, TokenAccessLevels DesiredAccess, IntPtr TokenAttributes, SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, System.Security.Principal.TokenType TokenType, ref SafeTokenHandle DuplicateTokenHandle);
	}

	internal static class Kernel32
	{
		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool CloseHandle(IntPtr handle);

		[DllImport("kernel32.dll")]
		internal static extern IntPtr GetCurrentProcess();
	}
}
