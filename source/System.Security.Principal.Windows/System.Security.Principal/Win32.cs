using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Principal;

internal static class Win32
{
	internal static SafeLsaPolicyHandle LsaOpenPolicy(string systemName, PolicyRights rights)
	{
		global::Interop.OBJECT_ATTRIBUTES Attributes = default(global::Interop.OBJECT_ATTRIBUTES);
		SafeLsaPolicyHandle PolicyHandle;
		uint num = global::Interop.Advapi32.LsaOpenPolicy(systemName, ref Attributes, (int)rights, out PolicyHandle);
		switch (num)
		{
		case 0u:
			return PolicyHandle;
		case 3221225506u:
			throw new UnauthorizedAccessException();
		case 3221225495u:
		case 3221225626u:
			throw new OutOfMemoryException();
		default:
		{
			uint error = global::Interop.Advapi32.LsaNtStatusToWinError(num);
			throw new Win32Exception((int)error);
		}
		}
	}

	internal static byte[] ConvertIntPtrSidToByteArraySid(IntPtr binaryForm)
	{
		byte b = Marshal.ReadByte(binaryForm, 0);
		if (b != SecurityIdentifier.Revision)
		{
			throw new ArgumentException(System.SR.IdentityReference_InvalidSidRevision, "binaryForm");
		}
		byte b2 = Marshal.ReadByte(binaryForm, 1);
		if (b2 < 0 || b2 > 15)
		{
			throw new ArgumentException(System.SR.Format(System.SR.IdentityReference_InvalidNumberOfSubauthorities, 15), "binaryForm");
		}
		int num = 8 + b2 * 4;
		byte[] array = new byte[num];
		Marshal.Copy(binaryForm, array, 0, num);
		return array;
	}

	internal static int CreateSidFromString(string stringSid, out byte[] resultSid)
	{
		IntPtr ByteArray = IntPtr.Zero;
		int lastWin32Error;
		try
		{
			if (global::Interop.Advapi32.ConvertStringSidToSid(stringSid, out ByteArray) == 0)
			{
				lastWin32Error = Marshal.GetLastWin32Error();
				goto IL_002b;
			}
			resultSid = ConvertIntPtrSidToByteArraySid(ByteArray);
		}
		finally
		{
			Marshal.FreeHGlobal(ByteArray);
		}
		return 0;
		IL_002b:
		resultSid = null;
		return lastWin32Error;
	}

	internal static int CreateWellKnownSid(WellKnownSidType sidType, SecurityIdentifier domainSid, out byte[] resultSid)
	{
		uint resultSidLength = (uint)SecurityIdentifier.MaxBinaryLength;
		resultSid = new byte[resultSidLength];
		if (global::Interop.Advapi32.CreateWellKnownSid((int)sidType, domainSid?.BinaryForm, resultSid, ref resultSidLength) != 0)
		{
			return 0;
		}
		resultSid = null;
		return Marshal.GetLastWin32Error();
	}

	internal static bool IsEqualDomainSid(SecurityIdentifier sid1, SecurityIdentifier sid2)
	{
		if (sid1 == null || sid2 == null)
		{
			return false;
		}
		byte[] array = new byte[sid1.BinaryLength];
		sid1.GetBinaryForm(array, 0);
		byte[] array2 = new byte[sid2.BinaryLength];
		sid2.GetBinaryForm(array2, 0);
		if (global::Interop.Advapi32.IsEqualDomainSid(array, array2, out var result) != 0)
		{
			return result;
		}
		return false;
	}

	internal unsafe static void InitializeReferencedDomainsPointer(SafeLsaMemoryHandle referencedDomains)
	{
		referencedDomains.Initialize((uint)Marshal.SizeOf<global::Interop.LSA_REFERENCED_DOMAIN_LIST>());
		global::Interop.LSA_REFERENCED_DOMAIN_LIST lSA_REFERENCED_DOMAIN_LIST = referencedDomains.Read<global::Interop.LSA_REFERENCED_DOMAIN_LIST>(0uL);
		byte* pointer = null;
		try
		{
			referencedDomains.AcquirePointer(ref pointer);
			if (lSA_REFERENCED_DOMAIN_LIST.Domains != IntPtr.Zero)
			{
				global::Interop.LSA_TRUST_INFORMATION* ptr = (global::Interop.LSA_TRUST_INFORMATION*)(void*)lSA_REFERENCED_DOMAIN_LIST.Domains;
				ptr += lSA_REFERENCED_DOMAIN_LIST.Entries;
				long numBytes = (byte*)ptr - pointer;
				referencedDomains.Initialize((ulong)numBytes);
			}
		}
		finally
		{
			if (pointer != null)
			{
				referencedDomains.ReleasePointer();
			}
		}
	}

	internal static int GetWindowsAccountDomainSid(SecurityIdentifier sid, out SecurityIdentifier resultSid)
	{
		byte[] array = new byte[sid.BinaryLength];
		sid.GetBinaryForm(array, 0);
		uint resultSidLength = (uint)SecurityIdentifier.MaxBinaryLength;
		byte[] array2 = new byte[resultSidLength];
		if (global::Interop.Advapi32.GetWindowsAccountDomainSid(array, array2, ref resultSidLength) != 0)
		{
			resultSid = new SecurityIdentifier(array2, 0);
			return 0;
		}
		resultSid = null;
		return Marshal.GetLastWin32Error();
	}

	internal static bool IsWellKnownSid(SecurityIdentifier sid, WellKnownSidType type)
	{
		byte[] array = new byte[sid.BinaryLength];
		sid.GetBinaryForm(array, 0);
		if (global::Interop.Advapi32.IsWellKnownSid(array, (int)type) == 0)
		{
			return false;
		}
		return true;
	}
}
