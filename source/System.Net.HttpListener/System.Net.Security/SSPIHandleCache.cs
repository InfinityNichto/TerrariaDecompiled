using System.Threading;

namespace System.Net.Security;

internal static class SSPIHandleCache
{
	private static readonly System.Net.Security.SafeCredentialReference[] s_cacheSlots = new System.Net.Security.SafeCredentialReference[32];

	private static int s_current = -1;

	internal static void CacheCredential(System.Net.Security.SafeFreeCredentials newHandle)
	{
		try
		{
			System.Net.Security.SafeCredentialReference safeCredentialReference = System.Net.Security.SafeCredentialReference.CreateReference(newHandle);
			if (safeCredentialReference != null)
			{
				int num = Interlocked.Increment(ref s_current) & 0x1F;
				Interlocked.Exchange(ref s_cacheSlots[num], safeCredentialReference)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled() && !System.Net.ExceptionCheck.IsFatal(ex))
			{
				System.Net.NetEventSource.Error(null, $"Attempted to throw: {ex}", "CacheCredential");
			}
		}
	}
}
