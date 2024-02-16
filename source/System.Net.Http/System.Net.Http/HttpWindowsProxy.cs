using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Net.Http;

internal sealed class HttpWindowsProxy : IMultiWebProxy, IWebProxy, IDisposable
{
	private readonly MultiProxy _insecureProxy;

	private readonly MultiProxy _secureProxy;

	private readonly FailedProxyCache _failedProxies = new FailedProxyCache();

	private readonly List<string> _bypass;

	private readonly bool _bypassLocal;

	private readonly List<IPAddress> _localIp;

	private ICredentials _credentials;

	private readonly WinInetProxyHelper _proxyHelper;

	private global::Interop.WinHttp.SafeWinHttpHandle _sessionHandle;

	private bool _disposed;

	public ICredentials Credentials
	{
		get
		{
			return _credentials;
		}
		set
		{
			_credentials = value;
		}
	}

	public static bool TryCreate([NotNullWhen(true)] out IWebProxy proxy)
	{
		global::Interop.WinHttp.SafeWinHttpHandle safeWinHttpHandle = null;
		proxy = null;
		WinInetProxyHelper winInetProxyHelper = new WinInetProxyHelper();
		if (!winInetProxyHelper.ManualSettingsOnly && !winInetProxyHelper.AutoSettingsUsed)
		{
			return false;
		}
		if (winInetProxyHelper.AutoSettingsUsed)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(winInetProxyHelper, FormattableStringFactory.Create("AutoSettingsUsed, calling {0}", "WinHttpOpen"), "TryCreate");
			}
			safeWinHttpHandle = global::Interop.WinHttp.WinHttpOpen(IntPtr.Zero, 1u, null, null, 268435456);
			if (safeWinHttpHandle.IsInvalid)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(winInetProxyHelper, FormattableStringFactory.Create("{0} returned invalid handle", "WinHttpOpen"), "TryCreate");
				}
				return false;
			}
		}
		proxy = new HttpWindowsProxy(winInetProxyHelper, safeWinHttpHandle);
		return true;
	}

	private HttpWindowsProxy(WinInetProxyHelper proxyHelper, global::Interop.WinHttp.SafeWinHttpHandle sessionHandle)
	{
		_proxyHelper = proxyHelper;
		_sessionHandle = sessionHandle;
		if (!proxyHelper.ManualSettingsUsed)
		{
			return;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(proxyHelper, $"ManualSettingsUsed, {proxyHelper.Proxy}", ".ctor");
		}
		_secureProxy = MultiProxy.Parse(_failedProxies, proxyHelper.Proxy, secure: true);
		_insecureProxy = MultiProxy.Parse(_failedProxies, proxyHelper.Proxy, secure: false);
		if (!string.IsNullOrWhiteSpace(proxyHelper.ProxyBypass))
		{
			int i = 0;
			int num = 0;
			_bypass = new List<string>(proxyHelper.ProxyBypass.Length / 5);
			while (i < proxyHelper.ProxyBypass.Length)
			{
				for (; i < proxyHelper.ProxyBypass.Length && proxyHelper.ProxyBypass[i] == ' '; i++)
				{
				}
				if (string.Compare(proxyHelper.ProxyBypass, i, "http://", 0, 7, StringComparison.OrdinalIgnoreCase) == 0)
				{
					i += 7;
				}
				else if (string.Compare(proxyHelper.ProxyBypass, i, "https://", 0, 8, StringComparison.OrdinalIgnoreCase) == 0)
				{
					i += 8;
				}
				if (i < proxyHelper.ProxyBypass.Length && proxyHelper.ProxyBypass[i] == '[')
				{
					i++;
				}
				num = i;
				for (; i < proxyHelper.ProxyBypass.Length && proxyHelper.ProxyBypass[i] != ' ' && proxyHelper.ProxyBypass[i] != ';' && proxyHelper.ProxyBypass[i] != ']'; i++)
				{
				}
				string text;
				if (i == num)
				{
					text = null;
				}
				else if (string.Compare(proxyHelper.ProxyBypass, num, "<local>", 0, 7, StringComparison.OrdinalIgnoreCase) == 0)
				{
					_bypassLocal = true;
					text = null;
				}
				else
				{
					text = proxyHelper.ProxyBypass.Substring(num, i - num);
				}
				if (i < proxyHelper.ProxyBypass.Length && proxyHelper.ProxyBypass[i] != ';')
				{
					for (; i < proxyHelper.ProxyBypass.Length && proxyHelper.ProxyBypass[i] != ';'; i++)
					{
					}
				}
				if (i < proxyHelper.ProxyBypass.Length && proxyHelper.ProxyBypass[i] == ';')
				{
					i++;
				}
				if (text != null)
				{
					_bypass.Add(text);
				}
			}
			if (_bypass.Count == 0)
			{
				_bypass = null;
			}
		}
		if (!_bypassLocal)
		{
			return;
		}
		_localIp = new List<IPAddress>();
		NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
		foreach (NetworkInterface networkInterface in allNetworkInterfaces)
		{
			IPInterfaceProperties iPProperties = networkInterface.GetIPProperties();
			foreach (UnicastIPAddressInformation unicastAddress in iPProperties.UnicastAddresses)
			{
				_localIp.Add(unicastAddress.Address);
			}
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			if (_sessionHandle != null && !_sessionHandle.IsInvalid)
			{
				global::Interop.WinHttp.SafeWinHttpHandle.DisposeAndClearHandle(ref _sessionHandle);
			}
		}
	}

	public Uri GetProxy(Uri uri)
	{
		GetMultiProxy(uri).ReadNext(out var uri2, out var _);
		return uri2;
	}

	public MultiProxy GetMultiProxy(Uri uri)
	{
		if (_proxyHelper.AutoSettingsUsed && !_proxyHelper.RecentAutoDetectionFailure)
		{
			global::Interop.WinHttp.WINHTTP_PROXY_INFO proxyInfo = default(global::Interop.WinHttp.WINHTTP_PROXY_INFO);
			try
			{
				if (!_proxyHelper.GetProxyForUrl(_sessionHandle, uri, out proxyInfo))
				{
					return MultiProxy.Empty;
				}
				if (proxyInfo.ProxyBypass == IntPtr.Zero)
				{
					if (proxyInfo.Proxy != IntPtr.Zero)
					{
						string proxyConfig = Marshal.PtrToStringUni(proxyInfo.Proxy);
						return MultiProxy.CreateLazy(_failedProxies, proxyConfig, IsSecureUri(uri));
					}
					return MultiProxy.Empty;
				}
			}
			finally
			{
				Marshal.FreeHGlobal(proxyInfo.Proxy);
				Marshal.FreeHGlobal(proxyInfo.ProxyBypass);
			}
		}
		if (_proxyHelper.ManualSettingsUsed)
		{
			if (_bypassLocal)
			{
				if (uri.IsLoopback)
				{
					return MultiProxy.Empty;
				}
				if ((uri.HostNameType == UriHostNameType.IPv6 || uri.HostNameType == UriHostNameType.IPv4) && IPAddress.TryParse(uri.IdnHost, out IPAddress address))
				{
					foreach (IPAddress item in _localIp)
					{
						if (item.Equals(address))
						{
							return MultiProxy.Empty;
						}
					}
				}
				if (uri.HostNameType != UriHostNameType.IPv6 && !uri.IdnHost.Contains('.'))
				{
					return MultiProxy.Empty;
				}
			}
			if (_bypass != null)
			{
				foreach (string item2 in _bypass)
				{
					if (SimpleRegex.IsMatchWithStarWildcard(uri.IdnHost, item2))
					{
						return MultiProxy.Empty;
					}
				}
			}
			if (!IsSecureUri(uri))
			{
				return _insecureProxy;
			}
			return _secureProxy;
		}
		return MultiProxy.Empty;
	}

	private static bool IsSecureUri(Uri uri)
	{
		if (!(uri.Scheme == "https"))
		{
			return uri.Scheme == "wss";
		}
		return true;
	}

	public bool IsBypassed(Uri uri)
	{
		return false;
	}
}
