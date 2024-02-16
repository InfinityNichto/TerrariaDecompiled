using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http;

internal struct MultiProxy
{
	private static readonly char[] s_proxyDelimiters = new char[5] { ';', ' ', '\n', '\r', '\t' };

	private readonly FailedProxyCache _failedProxyCache;

	private readonly Uri[] _uris;

	private readonly string _proxyConfig;

	private readonly bool _secure;

	private int _currentIndex;

	private Uri _currentUri;

	public static MultiProxy Empty => new MultiProxy(null, Array.Empty<Uri>());

	private MultiProxy(FailedProxyCache failedProxyCache, Uri[] uris)
	{
		_failedProxyCache = failedProxyCache;
		_uris = uris;
		_proxyConfig = null;
		_secure = false;
		_currentIndex = 0;
		_currentUri = null;
	}

	private MultiProxy(FailedProxyCache failedProxyCache, string proxyConfig, bool secure)
	{
		_failedProxyCache = failedProxyCache;
		_uris = null;
		_proxyConfig = proxyConfig;
		_secure = secure;
		_currentIndex = 0;
		_currentUri = null;
	}

	public static MultiProxy Parse(FailedProxyCache failedProxyCache, string proxyConfig, bool secure)
	{
		Uri[] array = Array.Empty<Uri>();
		ReadOnlySpan<char> proxyString = proxyConfig;
		Uri uri;
		int charactersConsumed;
		while (TryParseProxyConfigPart(proxyString, secure, out uri, out charactersConsumed))
		{
			int num = array.Length;
			Array.Resize(ref array, num + 1);
			array[num] = uri;
			proxyString = proxyString.Slice(charactersConsumed);
		}
		return new MultiProxy(failedProxyCache, array);
	}

	public static MultiProxy CreateLazy(FailedProxyCache failedProxyCache, string proxyConfig, bool secure)
	{
		if (string.IsNullOrEmpty(proxyConfig))
		{
			return Empty;
		}
		return new MultiProxy(failedProxyCache, proxyConfig, secure);
	}

	public bool ReadNext([NotNullWhen(true)] out Uri uri, out bool isFinalProxy)
	{
		if (_currentUri != null)
		{
			_failedProxyCache.SetProxyFailed(_currentUri);
		}
		if (!ReadNextHelper(out uri, out isFinalProxy))
		{
			_currentUri = null;
			return false;
		}
		Uri uri2 = null;
		long num = long.MaxValue;
		do
		{
			long proxyRenewTicks = _failedProxyCache.GetProxyRenewTicks(uri);
			if (proxyRenewTicks == 0L)
			{
				_currentUri = uri;
				return true;
			}
			if (proxyRenewTicks < num)
			{
				uri2 = uri;
				num = proxyRenewTicks;
			}
		}
		while (ReadNextHelper(out uri, out isFinalProxy));
		if (_currentUri == null)
		{
			uri = uri2;
			_currentUri = uri2;
			if (uri2 != null)
			{
				_failedProxyCache.TryRenewProxy(uri, num);
				return true;
			}
		}
		return false;
	}

	private bool ReadNextHelper([NotNullWhen(true)] out Uri uri, out bool isFinalProxy)
	{
		if (_uris != null)
		{
			if (_currentIndex == _uris.Length)
			{
				uri = null;
				isFinalProxy = false;
				return false;
			}
			uri = _uris[_currentIndex++];
			isFinalProxy = _currentIndex == _uris.Length;
			return true;
		}
		if (_currentIndex < _proxyConfig.Length)
		{
			int charactersConsumed;
			bool result = TryParseProxyConfigPart(_proxyConfig.AsSpan(_currentIndex), _secure, out uri, out charactersConsumed);
			_currentIndex += charactersConsumed;
			isFinalProxy = _currentIndex == _proxyConfig.Length;
			return result;
		}
		uri = null;
		isFinalProxy = false;
		return false;
	}

	private static bool TryParseProxyConfigPart(ReadOnlySpan<char> proxyString, bool secure, [NotNullWhen(true)] out Uri uri, out int charactersConsumed)
	{
		int num = (secure ? 1 : 2);
		int length = proxyString.Length;
		while (true)
		{
			int i;
			for (i = 0; i < proxyString.Length && Array.IndexOf(s_proxyDelimiters, proxyString[i]) >= 0; i++)
			{
			}
			if (i == proxyString.Length)
			{
				break;
			}
			proxyString = proxyString.Slice(i);
			int num2 = 3;
			if (proxyString.StartsWith("http="))
			{
				num2 = 2;
				proxyString = proxyString.Slice("http=".Length);
			}
			else if (proxyString.StartsWith("https="))
			{
				num2 = 1;
				proxyString = proxyString.Slice("https=".Length);
			}
			if (proxyString.StartsWith("http://"))
			{
				num2 = 2;
				proxyString = proxyString.Slice("http://".Length);
			}
			else if (proxyString.StartsWith("https://"))
			{
				num2 = 1;
				proxyString = proxyString.Slice("https://".Length);
			}
			i = proxyString.IndexOfAny(s_proxyDelimiters);
			if (i < 0)
			{
				i = proxyString.Length;
			}
			if ((num2 & num) != 0 && Uri.TryCreate("http://" + proxyString.Slice(0, i), UriKind.Absolute, out uri))
			{
				charactersConsumed = length - proxyString.Length + i;
				return true;
			}
			proxyString = proxyString.Slice(i);
		}
		uri = null;
		charactersConsumed = length;
		return false;
	}
}
