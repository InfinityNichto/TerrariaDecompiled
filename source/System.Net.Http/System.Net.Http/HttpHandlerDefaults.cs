using System.Threading;

namespace System.Net.Http;

internal static class HttpHandlerDefaults
{
	public static readonly TimeSpan DefaultKeepAlivePingTimeout = TimeSpan.FromSeconds(20.0);

	public static readonly TimeSpan DefaultKeepAlivePingDelay = Timeout.InfiniteTimeSpan;

	public static readonly TimeSpan DefaultResponseDrainTimeout = TimeSpan.FromSeconds(2.0);

	public static readonly TimeSpan DefaultPooledConnectionLifetime = Timeout.InfiniteTimeSpan;

	public static readonly TimeSpan DefaultPooledConnectionIdleTimeout = TimeSpan.FromMinutes(1.0);

	public static readonly TimeSpan DefaultExpect100ContinueTimeout = TimeSpan.FromSeconds(1.0);

	public static readonly TimeSpan DefaultConnectTimeout = Timeout.InfiniteTimeSpan;
}
