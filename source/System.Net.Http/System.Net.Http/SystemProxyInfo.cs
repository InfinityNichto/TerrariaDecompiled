namespace System.Net.Http;

internal static class SystemProxyInfo
{
	private static readonly Lazy<IWebProxy> s_proxy = new Lazy<IWebProxy>(ConstructSystemProxy);

	public static IWebProxy Proxy => s_proxy.Value;

	public static IWebProxy ConstructSystemProxy()
	{
		if (!HttpEnvironmentProxy.TryCreate(out var proxy))
		{
			HttpWindowsProxy.TryCreate(out proxy);
		}
		return proxy ?? new HttpNoProxy();
	}
}
