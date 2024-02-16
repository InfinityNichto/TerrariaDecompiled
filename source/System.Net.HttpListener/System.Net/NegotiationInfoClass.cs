using System.Runtime.InteropServices;

namespace System.Net;

internal static class NegotiationInfoClass
{
	internal unsafe static string GetAuthenticationPackageName(SafeHandle safeHandle, int negotiationState)
	{
		if (safeHandle.IsInvalid)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, $"Invalid handle:{safeHandle}", "GetAuthenticationPackageName");
			}
			return null;
		}
		bool success = false;
		try
		{
			safeHandle.DangerousAddRef(ref success);
			IntPtr intPtr = safeHandle.DangerousGetHandle();
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, $"packageInfo:{intPtr} negotiationState:{negotiationState:x}", "GetAuthenticationPackageName");
			}
			if (negotiationState == 0 || negotiationState == 1)
			{
				string text = Marshal.PtrToStringUni(((System.Net.SecurityPackageInfo*)(void*)intPtr)->Name);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(null, $"packageInfo:{intPtr} negotiationState:{negotiationState:x} name:{text}", "GetAuthenticationPackageName");
				}
				return string.Equals(text, "Kerberos", StringComparison.OrdinalIgnoreCase) ? "Kerberos" : (string.Equals(text, "NTLM", StringComparison.OrdinalIgnoreCase) ? "NTLM" : text);
			}
		}
		finally
		{
			if (success)
			{
				safeHandle.DangerousRelease();
			}
		}
		return null;
	}
}
