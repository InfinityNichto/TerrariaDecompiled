using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Principal;

internal static class Win32
{
	internal static int OpenThreadToken(TokenAccessLevels dwDesiredAccess, System.Security.Principal.WinSecurityContext dwOpenAs, out SafeTokenHandle phThreadToken)
	{
		int num = 0;
		bool bOpenAsSelf = true;
		if (dwOpenAs == System.Security.Principal.WinSecurityContext.Thread)
		{
			bOpenAsSelf = false;
		}
		if (!global::Interop.Advapi32.OpenThreadToken((IntPtr)(-2), dwDesiredAccess, bOpenAsSelf, out phThreadToken))
		{
			if (dwOpenAs == System.Security.Principal.WinSecurityContext.Both)
			{
				bOpenAsSelf = false;
				num = 0;
				if (!global::Interop.Advapi32.OpenThreadToken((IntPtr)(-2), dwDesiredAccess, bOpenAsSelf, out phThreadToken))
				{
					num = Marshal.GetHRForLastWin32Error();
				}
			}
			else
			{
				num = Marshal.GetHRForLastWin32Error();
			}
		}
		if (num != 0)
		{
			phThreadToken = null;
		}
		return num;
	}

	internal static int SetThreadToken(SafeTokenHandle hToken)
	{
		int result = 0;
		if (!global::Interop.Advapi32.SetThreadToken(IntPtr.Zero, hToken))
		{
			result = Marshal.GetHRForLastWin32Error();
		}
		return result;
	}
}
