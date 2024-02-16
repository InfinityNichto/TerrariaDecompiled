using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net.Http;

internal sealed class WinInetProxyHelper
{
	private readonly string _autoConfigUrl;

	private readonly string _proxy;

	private readonly string _proxyBypass;

	private readonly bool _autoDetect;

	private readonly bool _useProxy;

	private bool _autoDetectionFailed;

	private int _lastTimeAutoDetectionFailed;

	public string AutoConfigUrl => _autoConfigUrl;

	public bool AutoDetect => _autoDetect;

	public bool AutoSettingsUsed
	{
		get
		{
			if (!AutoDetect)
			{
				return !string.IsNullOrEmpty(AutoConfigUrl);
			}
			return true;
		}
	}

	public bool ManualSettingsUsed => !string.IsNullOrEmpty(Proxy);

	public bool ManualSettingsOnly
	{
		get
		{
			if (!AutoSettingsUsed)
			{
				return ManualSettingsUsed;
			}
			return false;
		}
	}

	public string Proxy => _proxy;

	public string ProxyBypass => _proxyBypass;

	public bool RecentAutoDetectionFailure
	{
		get
		{
			if (_autoDetectionFailed)
			{
				return Environment.TickCount - _lastTimeAutoDetectionFailed <= 120000;
			}
			return false;
		}
	}

	public WinInetProxyHelper()
	{
		global::Interop.WinHttp.WINHTTP_CURRENT_USER_IE_PROXY_CONFIG proxyConfig = default(global::Interop.WinHttp.WINHTTP_CURRENT_USER_IE_PROXY_CONFIG);
		try
		{
			if (global::Interop.WinHttp.WinHttpGetIEProxyConfigForCurrentUser(out proxyConfig))
			{
				_autoConfigUrl = Marshal.PtrToStringUni(proxyConfig.AutoConfigUrl);
				_autoDetect = proxyConfig.AutoDetect;
				_proxy = Marshal.PtrToStringUni(proxyConfig.Proxy);
				_proxyBypass = Marshal.PtrToStringUni(proxyConfig.ProxyBypass);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, $"AutoConfigUrl={AutoConfigUrl}, AutoDetect={AutoDetect}, Proxy={Proxy}, ProxyBypass={ProxyBypass}", ".ctor");
				}
				_useProxy = true;
			}
			else
			{
				int lastWin32Error = Marshal.GetLastWin32Error();
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(this, $"error={lastWin32Error}", ".ctor");
				}
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"_useProxy={_useProxy}", ".ctor");
			}
		}
		finally
		{
			Marshal.FreeHGlobal(proxyConfig.AutoConfigUrl);
			Marshal.FreeHGlobal(proxyConfig.Proxy);
			Marshal.FreeHGlobal(proxyConfig.ProxyBypass);
		}
	}

	public bool GetProxyForUrl(global::Interop.WinHttp.SafeWinHttpHandle sessionHandle, Uri uri, out global::Interop.WinHttp.WINHTTP_PROXY_INFO proxyInfo)
	{
		proxyInfo.AccessType = 1u;
		proxyInfo.Proxy = IntPtr.Zero;
		proxyInfo.ProxyBypass = IntPtr.Zero;
		if (!_useProxy)
		{
			return false;
		}
		bool flag = false;
		Unsafe.SkipInit(out global::Interop.WinHttp.WINHTTP_AUTOPROXY_OPTIONS autoProxyOptions);
		autoProxyOptions.AutoConfigUrl = AutoConfigUrl;
		autoProxyOptions.AutoDetectFlags = (AutoDetect ? 3u : 0u);
		autoProxyOptions.AutoLoginIfChallenged = false;
		autoProxyOptions.Flags = (AutoDetect ? 1u : 0u) | ((!string.IsNullOrEmpty(AutoConfigUrl)) ? 2u : 0u);
		autoProxyOptions.Reserved1 = IntPtr.Zero;
		autoProxyOptions.Reserved2 = 0u;
		string text = uri.AbsoluteUri;
		if (uri.Scheme == "wss")
		{
			text = "https" + text.Substring("wss".Length);
		}
		else if (uri.Scheme == "ws")
		{
			text = "http" + text.Substring("ws".Length);
		}
		bool flag2 = false;
		do
		{
			_autoDetectionFailed = false;
			if (global::Interop.WinHttp.WinHttpGetProxyForUrl(sessionHandle, text, ref autoProxyOptions, out proxyInfo))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "Using autoconfig proxy settings", "GetProxyForUrl");
				}
				flag = true;
				break;
			}
			int lastWin32Error = Marshal.GetLastWin32Error();
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, $"error={lastWin32Error}", "GetProxyForUrl");
			}
			if ((long)lastWin32Error == 12015)
			{
				if (flag2)
				{
					break;
				}
				flag2 = true;
				autoProxyOptions.AutoLoginIfChallenged = true;
				continue;
			}
			if ((long)lastWin32Error == 12180)
			{
				_autoDetectionFailed = true;
				_lastTimeAutoDetectionFailed = Environment.TickCount;
			}
			break;
		}
		while (flag2);
		if (!flag && !string.IsNullOrEmpty(Proxy))
		{
			proxyInfo.AccessType = 3u;
			proxyInfo.Proxy = Marshal.StringToHGlobalUni(Proxy);
			proxyInfo.ProxyBypass = (string.IsNullOrEmpty(ProxyBypass) ? IntPtr.Zero : Marshal.StringToHGlobalUni(ProxyBypass));
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"Fallback to Proxy={Proxy}, ProxyBypass={ProxyBypass}", "GetProxyForUrl");
			}
			flag = true;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"useProxy={flag}", "GetProxyForUrl");
		}
		return flag;
	}
}
