using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Security;
using System.Runtime.Versioning;

namespace System.Net;

public class ServicePointManager
{
	public const int DefaultNonPersistentConnectionLimit = 4;

	public const int DefaultPersistentConnectionLimit = 2;

	private static readonly ConcurrentDictionary<string, WeakReference<ServicePoint>> s_servicePointTable = new ConcurrentDictionary<string, WeakReference<ServicePoint>>();

	private static SecurityProtocolType s_securityProtocolType = SecurityProtocolType.SystemDefault;

	private static int s_connectionLimit = 2;

	private static int s_maxServicePoints;

	private static int s_maxServicePointIdleTime = 100000;

	private static int s_dnsRefreshTimeout = 120000;

	public static SecurityProtocolType SecurityProtocol
	{
		get
		{
			return s_securityProtocolType;
		}
		set
		{
			ValidateSecurityProtocol(value);
			s_securityProtocolType = value;
		}
	}

	public static int MaxServicePoints
	{
		get
		{
			return s_maxServicePoints;
		}
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			s_maxServicePoints = value;
		}
	}

	public static int DefaultConnectionLimit
	{
		get
		{
			return s_connectionLimit;
		}
		set
		{
			if (value <= 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			s_connectionLimit = value;
		}
	}

	public static int MaxServicePointIdleTime
	{
		get
		{
			return s_maxServicePointIdleTime;
		}
		set
		{
			if (value < -1)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			s_maxServicePointIdleTime = value;
		}
	}

	public static bool UseNagleAlgorithm { get; set; } = true;


	public static bool Expect100Continue { get; set; } = true;


	public static bool EnableDnsRoundRobin { get; set; }

	public static int DnsRefreshTimeout
	{
		get
		{
			return s_dnsRefreshTimeout;
		}
		set
		{
			s_dnsRefreshTimeout = Math.Max(-1, value);
		}
	}

	public static RemoteCertificateValidationCallback? ServerCertificateValidationCallback { get; set; }

	public static bool ReusePort { get; set; }

	public static bool CheckCertificateRevocationList { get; set; }

	[UnsupportedOSPlatform("browser")]
	public static EncryptionPolicy EncryptionPolicy { get; } = EncryptionPolicy.RequireEncryption;


	private static void ValidateSecurityProtocol(SecurityProtocolType value)
	{
		if (((uint)value & 0xFFFFC03Fu) != 0)
		{
			throw new NotSupportedException(System.SR.net_securityprotocolnotsupported);
		}
	}

	[Obsolete("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static ServicePoint FindServicePoint(Uri address)
	{
		return FindServicePoint(address, null);
	}

	[Obsolete("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static ServicePoint FindServicePoint(string uriString, IWebProxy? proxy)
	{
		return FindServicePoint(new Uri(uriString), proxy);
	}

	[Obsolete("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static ServicePoint FindServicePoint(Uri address, IWebProxy? proxy)
	{
		if (address == null)
		{
			throw new ArgumentNullException("address");
		}
		bool isProxy = ProxyAddressIfNecessary(ref address, proxy);
		string key = MakeQueryString(address, isProxy);
		WeakReference<ServicePoint> value;
		ServicePoint target;
		while (!s_servicePointTable.TryGetValue(key, out value) || !value.TryGetTarget(out target))
		{
			foreach (KeyValuePair<string, WeakReference<ServicePoint>> item in s_servicePointTable)
			{
				if (!item.Value.TryGetTarget(out var _))
				{
					s_servicePointTable.TryRemove(item);
				}
			}
			target = new ServicePoint(address)
			{
				ConnectionLimit = DefaultConnectionLimit,
				IdleSince = DateTime.Now,
				Expect100Continue = Expect100Continue,
				UseNagleAlgorithm = UseNagleAlgorithm
			};
			s_servicePointTable[key] = new WeakReference<ServicePoint>(target);
		}
		target.IdleSince = DateTime.Now;
		return target;
	}

	private static bool ProxyAddressIfNecessary(ref Uri address, IWebProxy proxy)
	{
		if (proxy != null && !address.IsLoopback)
		{
			try
			{
				Uri proxy2 = proxy.GetProxy(address);
				if (proxy2 != null)
				{
					if (proxy2.Scheme != Uri.UriSchemeHttp)
					{
						throw new NotSupportedException(System.SR.Format(System.SR.net_proxyschemenotsupported, address.Scheme));
					}
					address = proxy2;
					return true;
				}
			}
			catch (PlatformNotSupportedException)
			{
			}
		}
		return false;
	}

	private static string MakeQueryString(Uri address)
	{
		if (!address.IsDefaultPort)
		{
			return address.Scheme + "://" + address.DnsSafeHost + ":" + address.Port;
		}
		return address.Scheme + "://" + address.DnsSafeHost;
	}

	private static string MakeQueryString(Uri address, bool isProxy)
	{
		string text = MakeQueryString(address);
		if (!isProxy)
		{
			return text;
		}
		return text + "://proxy";
	}

	public static void SetTcpKeepAlive(bool enabled, int keepAliveTime, int keepAliveInterval)
	{
		if (enabled)
		{
			if (keepAliveTime <= 0)
			{
				throw new ArgumentOutOfRangeException("keepAliveTime");
			}
			if (keepAliveInterval <= 0)
			{
				throw new ArgumentOutOfRangeException("keepAliveInterval");
			}
		}
	}
}
