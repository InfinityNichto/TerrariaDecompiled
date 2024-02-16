using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;

namespace System.Net;

public class WebProxy : IWebProxy, ISerializable
{
	private ArrayList _bypassList;

	private Regex[] _regexBypassList;

	private static volatile string s_domainName;

	private static volatile IPAddress[] s_localAddresses;

	private static int s_networkChangeRegistered;

	public Uri? Address { get; set; }

	public bool BypassProxyOnLocal { get; set; }

	public string[] BypassList
	{
		get
		{
			if (_bypassList == null)
			{
				return Array.Empty<string>();
			}
			string[] array = new string[_bypassList.Count];
			_bypassList.CopyTo(array);
			return array;
		}
		[param: AllowNull]
		set
		{
			_bypassList = ((value != null) ? new ArrayList(value) : null);
			UpdateRegexList(canThrow: true);
		}
	}

	public ArrayList BypassArrayList => _bypassList ?? (_bypassList = new ArrayList());

	public ICredentials? Credentials { get; set; }

	public bool UseDefaultCredentials
	{
		get
		{
			return Credentials == CredentialCache.DefaultCredentials;
		}
		set
		{
			Credentials = (value ? CredentialCache.DefaultCredentials : null);
		}
	}

	public WebProxy()
		: this((Uri?)null, BypassOnLocal: false, (string[]?)null, (ICredentials?)null)
	{
	}

	public WebProxy(Uri? Address)
		: this(Address, BypassOnLocal: false, null, null)
	{
	}

	public WebProxy(Uri? Address, bool BypassOnLocal)
		: this(Address, BypassOnLocal, null, null)
	{
	}

	public WebProxy(Uri? Address, bool BypassOnLocal, string[]? BypassList)
		: this(Address, BypassOnLocal, BypassList, null)
	{
	}

	public WebProxy(Uri? Address, bool BypassOnLocal, string[]? BypassList, ICredentials? Credentials)
	{
		this.Address = Address;
		this.Credentials = Credentials;
		BypassProxyOnLocal = BypassOnLocal;
		if (BypassList != null)
		{
			_bypassList = new ArrayList(BypassList);
			UpdateRegexList(canThrow: true);
		}
	}

	public WebProxy(string Host, int Port)
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(8, 2, invariantCulture);
		handler.AppendLiteral("http://");
		handler.AppendFormatted(Host);
		handler.AppendLiteral(":");
		handler.AppendFormatted(Port);
		this._002Ector(new Uri(string.Create(invariantCulture, ref handler)), BypassOnLocal: false, null, null);
	}

	public WebProxy(string? Address)
		: this(CreateProxyUri(Address), BypassOnLocal: false, null, null)
	{
	}

	public WebProxy(string? Address, bool BypassOnLocal)
		: this(CreateProxyUri(Address), BypassOnLocal, null, null)
	{
	}

	public WebProxy(string? Address, bool BypassOnLocal, string[]? BypassList)
		: this(CreateProxyUri(Address), BypassOnLocal, BypassList, null)
	{
	}

	public WebProxy(string? Address, bool BypassOnLocal, string[]? BypassList, ICredentials? Credentials)
		: this(CreateProxyUri(Address), BypassOnLocal, BypassList, Credentials)
	{
	}

	public Uri? GetProxy(Uri destination)
	{
		if (destination == null)
		{
			throw new ArgumentNullException("destination");
		}
		if (!IsBypassed(destination))
		{
			return Address;
		}
		return destination;
	}

	private static Uri CreateProxyUri(string address)
	{
		if (address != null)
		{
			if (address.Contains("://"))
			{
				return new Uri(address);
			}
			return new Uri("http://" + address);
		}
		return null;
	}

	private void UpdateRegexList(bool canThrow)
	{
		Regex[] array = null;
		ArrayList bypassList = _bypassList;
		try
		{
			if (bypassList != null && bypassList.Count > 0)
			{
				array = new Regex[bypassList.Count];
				for (int i = 0; i < bypassList.Count; i++)
				{
					array[i] = new Regex((string)bypassList[i], RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
				}
			}
		}
		catch
		{
			if (!canThrow)
			{
				_regexBypassList = null;
				return;
			}
			throw;
		}
		_regexBypassList = array;
	}

	private bool IsMatchInBypassList(Uri input)
	{
		UpdateRegexList(canThrow: false);
		if (_regexBypassList != null)
		{
			Span<char> span = stackalloc char[128];
			string text;
			if (!input.IsDefaultPort)
			{
				IFormatProvider formatProvider = null;
				IFormatProvider provider = formatProvider;
				Span<char> span2 = span;
				Span<char> initialBuffer = span2;
				DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(4, 3, formatProvider, span2);
				handler.AppendFormatted(input.Scheme);
				handler.AppendLiteral("://");
				handler.AppendFormatted(input.Host);
				handler.AppendLiteral(":");
				handler.AppendFormatted((uint)input.Port);
				text = string.Create(provider, initialBuffer, ref handler);
			}
			else
			{
				IFormatProvider formatProvider = null;
				IFormatProvider provider2 = formatProvider;
				Span<char> span2 = span;
				Span<char> initialBuffer2 = span2;
				DefaultInterpolatedStringHandler handler2 = new DefaultInterpolatedStringHandler(3, 2, formatProvider, span2);
				handler2.AppendFormatted(input.Scheme);
				handler2.AppendLiteral("://");
				handler2.AppendFormatted(input.Host);
				text = string.Create(provider2, initialBuffer2, ref handler2);
			}
			string input2 = text;
			Regex[] regexBypassList = _regexBypassList;
			foreach (Regex regex in regexBypassList)
			{
				if (regex.IsMatch(input2))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsBypassed(Uri host)
	{
		if (host == null)
		{
			throw new ArgumentNullException("host");
		}
		if (!(Address == null) && (!BypassProxyOnLocal || !IsLocal(host)))
		{
			return IsMatchInBypassList(host);
		}
		return true;
	}

	protected WebProxy(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	protected virtual void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	[Obsolete("WebProxy.GetDefaultProxy has been deprecated. Use the proxy selected for you by default.")]
	public static WebProxy GetDefaultProxy()
	{
		throw new PlatformNotSupportedException();
	}

	private bool IsLocal(Uri host)
	{
		if (host.IsLoopback)
		{
			return true;
		}
		string host2 = host.Host;
		if (IPAddress.TryParse(host2, out IPAddress address))
		{
			EnsureNetworkChangeRegistration();
			IPAddress[] array = s_localAddresses ?? (s_localAddresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList);
			return Array.IndexOf(array, address) != -1;
		}
		int num = host2.IndexOf('.');
		if (num == -1)
		{
			return true;
		}
		EnsureNetworkChangeRegistration();
		string text = s_domainName ?? (s_domainName = "." + IPGlobalProperties.GetIPGlobalProperties().DomainName);
		if (text.Length == host2.Length - num)
		{
			return string.Compare(text, 0, host2, num, text.Length, StringComparison.OrdinalIgnoreCase) == 0;
		}
		return false;
	}

	private static void EnsureNetworkChangeRegistration()
	{
		if (s_networkChangeRegistered == 0)
		{
			Register();
		}
		static void Register()
		{
			if (Interlocked.Exchange(ref s_networkChangeRegistered, 1) == 0)
			{
				NetworkChange.NetworkAddressChanged += delegate
				{
					s_domainName = null;
					s_localAddresses = null;
				};
				NetworkChange.NetworkAvailabilityChanged += delegate
				{
					s_domainName = null;
					s_localAddresses = null;
				};
			}
		}
	}
}
