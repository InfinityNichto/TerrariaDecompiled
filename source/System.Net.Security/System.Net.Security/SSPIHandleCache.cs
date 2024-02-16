using System.Threading;

namespace System.Net.Security;

internal static class SSPIHandleCache
{
	private static readonly SafeCredentialReference[] s_cacheSlots = new SafeCredentialReference[32];

	private static int s_current = -1;

	internal static void CacheCredential(SafeFreeCredentials newHandle)
	{
		try
		{
			SafeCredentialReference safeCredentialReference = SafeCredentialReference.CreateReference(newHandle);
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
