using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Net;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class CookieContainer
{
	public const int DefaultCookieLimit = 300;

	public const int DefaultPerDomainCookieLimit = 20;

	public const int DefaultCookieLengthLimit = 4096;

	private static readonly string s_fqdnMyDomain = CreateFqdnMyDomain();

	private static readonly HeaderVariantInfo[] s_headerInfo = new HeaderVariantInfo[2]
	{
		new HeaderVariantInfo("Set-Cookie", CookieVariant.Rfc2109),
		new HeaderVariantInfo("Set-Cookie2", CookieVariant.Rfc2965)
	};

	private readonly Hashtable m_domainTable = new Hashtable();

	private int m_maxCookieSize = 4096;

	private int m_maxCookies = 300;

	private int m_maxCookiesPerDomain = 20;

	private int m_count;

	private readonly string m_fqdnMyDomain = s_fqdnMyDomain;

	public int Capacity
	{
		get
		{
			return m_maxCookies;
		}
		set
		{
			if (value <= 0 || (value < m_maxCookiesPerDomain && m_maxCookiesPerDomain != int.MaxValue))
			{
				throw new ArgumentOutOfRangeException("value", System.SR.Format(System.SR.net_cookie_capacity_range, "Capacity", 0, m_maxCookiesPerDomain));
			}
			if (value < m_maxCookies)
			{
				m_maxCookies = value;
				AgeCookies(null);
			}
			m_maxCookies = value;
		}
	}

	public int Count => m_count;

	public int MaxCookieSize
	{
		get
		{
			return m_maxCookieSize;
		}
		set
		{
			if (value <= 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			m_maxCookieSize = value;
		}
	}

	public int PerDomainCapacity
	{
		get
		{
			return m_maxCookiesPerDomain;
		}
		set
		{
			if (value <= 0 || (value > m_maxCookies && value != int.MaxValue))
			{
				throw new ArgumentOutOfRangeException("value");
			}
			if (value < m_maxCookiesPerDomain)
			{
				m_maxCookiesPerDomain = value;
				AgeCookies(null);
			}
			m_maxCookiesPerDomain = value;
		}
	}

	public CookieContainer()
	{
	}

	public CookieContainer(int capacity)
	{
		if (capacity <= 0)
		{
			throw new ArgumentException(System.SR.net_toosmall, "capacity");
		}
		m_maxCookies = capacity;
	}

	public CookieContainer(int capacity, int perDomainCapacity, int maxCookieSize)
		: this(capacity)
	{
		if (perDomainCapacity != int.MaxValue && (perDomainCapacity <= 0 || perDomainCapacity > capacity))
		{
			throw new ArgumentOutOfRangeException("perDomainCapacity", System.SR.Format(System.SR.net_cookie_capacity_range, "PerDomainCapacity", 0, capacity));
		}
		m_maxCookiesPerDomain = perDomainCapacity;
		if (maxCookieSize <= 0)
		{
			throw new ArgumentException(System.SR.net_toosmall, "maxCookieSize");
		}
		m_maxCookieSize = maxCookieSize;
	}

	private static string CreateFqdnMyDomain()
	{
		string domainName = HostInformation.DomainName;
		if (domainName == null || domainName.Length <= 1)
		{
			return string.Empty;
		}
		return "." + domainName;
	}

	public void Add(Cookie cookie)
	{
		if (cookie == null)
		{
			throw new ArgumentNullException("cookie");
		}
		if (cookie.Domain.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_emptystringcall, "cookie.Domain"), "cookie");
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(cookie.Secure ? "https" : "http").Append("://");
		if (!cookie.DomainImplicit && cookie.Domain[0] == '.')
		{
			stringBuilder.Append('0');
		}
		stringBuilder.Append(cookie.Domain);
		if (cookie.PortList != null)
		{
			stringBuilder.Append(':').Append(cookie.PortList[0]);
		}
		stringBuilder.Append(cookie.Path);
		if (!Uri.TryCreate(stringBuilder.ToString(), UriKind.Absolute, out Uri result))
		{
			throw new CookieException(System.SR.Format(System.SR.net_cookie_attribute, "Domain", cookie.Domain));
		}
		Cookie cookie2 = cookie.Clone();
		cookie2.VerifySetDefaults(cookie2.Variant, result, IsLocalDomain(result.Host), m_fqdnMyDomain, setDefault: true, shouldThrow: true);
		Add(cookie2, throwOnError: true);
	}

	internal void Add(Cookie cookie, bool throwOnError)
	{
		if (cookie.Value.Length > m_maxCookieSize)
		{
			if (throwOnError)
			{
				throw new CookieException(System.SR.Format(System.SR.net_cookie_size, cookie, m_maxCookieSize));
			}
			return;
		}
		try
		{
			PathList pathList;
			lock (m_domainTable.SyncRoot)
			{
				pathList = (PathList)m_domainTable[cookie.DomainKey];
				if (pathList == null)
				{
					pathList = (PathList)(m_domainTable[cookie.DomainKey] = new PathList());
				}
			}
			int cookiesCount = pathList.GetCookiesCount();
			CookieCollection cookieCollection;
			lock (pathList.SyncRoot)
			{
				cookieCollection = (CookieCollection)pathList[cookie.Path];
				if (cookieCollection == null)
				{
					cookieCollection = new CookieCollection();
					pathList[cookie.Path] = cookieCollection;
				}
			}
			if (cookie.Expired)
			{
				lock (cookieCollection)
				{
					int num = cookieCollection.IndexOf(cookie);
					if (num != -1)
					{
						cookieCollection.RemoveAt(num);
						m_count--;
					}
				}
			}
			else
			{
				if ((cookiesCount >= m_maxCookiesPerDomain && !AgeCookies(cookie.DomainKey)) || (m_count >= m_maxCookies && !AgeCookies(null)))
				{
					return;
				}
				lock (cookieCollection)
				{
					m_count += cookieCollection.InternalAdd(cookie, isStrict: true);
				}
			}
			if (m_domainTable.Count > m_count || pathList.Count > m_maxCookiesPerDomain)
			{
				DomainTableCleanup();
			}
		}
		catch (OutOfMemoryException)
		{
			throw;
		}
		catch (Exception inner)
		{
			if (throwOnError)
			{
				throw new CookieException(System.SR.net_container_add_cookie, inner);
			}
		}
	}

	private bool AgeCookies(string domain)
	{
		int num = 0;
		DateTime dateTime = DateTime.MaxValue;
		CookieCollection cookieCollection = null;
		string text = null;
		int num2 = 0;
		int num3 = 0;
		float num4 = 1f;
		if (m_count > m_maxCookies)
		{
			num4 = (float)m_maxCookies / (float)m_count;
		}
		lock (m_domainTable.SyncRoot)
		{
			foreach (object item in m_domainTable)
			{
				DictionaryEntry dictionaryEntry = (DictionaryEntry)item;
				string text2;
				PathList pathList;
				if (domain == null)
				{
					text2 = (string)dictionaryEntry.Key;
					pathList = (PathList)dictionaryEntry.Value;
				}
				else
				{
					text2 = domain;
					pathList = (PathList)m_domainTable[domain];
				}
				num2 = 0;
				lock (pathList.SyncRoot)
				{
					foreach (CookieCollection value in pathList.Values)
					{
						num3 = ExpireCollection(value);
						num += num3;
						m_count -= num3;
						num2 += value.Count;
						DateTime dateTime2;
						if (value.Count > 0 && (dateTime2 = value.TimeStamp(CookieCollection.Stamp.Check)) < dateTime)
						{
							text = text2;
							cookieCollection = value;
							dateTime = dateTime2;
						}
					}
				}
				int num5 = Math.Min((int)((float)num2 * num4), Math.Min(m_maxCookiesPerDomain, m_maxCookies) - 1);
				if (num2 <= num5)
				{
					continue;
				}
				CookieCollection[] array;
				DateTime[] array2;
				lock (pathList.SyncRoot)
				{
					array = new CookieCollection[pathList.Count];
					array2 = new DateTime[pathList.Count];
					foreach (CookieCollection value2 in pathList.Values)
					{
						array2[num3] = value2.TimeStamp(CookieCollection.Stamp.Check);
						array[num3] = value2;
						num3++;
					}
				}
				Array.Sort(array2, array);
				num3 = 0;
				foreach (CookieCollection cookieCollection4 in array)
				{
					lock (cookieCollection4)
					{
						while (num2 > num5 && cookieCollection4.Count > 0)
						{
							cookieCollection4.RemoveAt(0);
							num2--;
							m_count--;
							num++;
						}
					}
					if (num2 <= num5)
					{
						break;
					}
				}
				if (num2 > num5 && domain != null)
				{
					return false;
				}
			}
		}
		if (domain != null)
		{
			return true;
		}
		if (num != 0)
		{
			return true;
		}
		if (dateTime == DateTime.MaxValue)
		{
			return false;
		}
		lock (cookieCollection)
		{
			while (m_count >= m_maxCookies && cookieCollection.Count > 0)
			{
				cookieCollection.RemoveAt(0);
				m_count--;
			}
		}
		return true;
	}

	private void DomainTableCleanup()
	{
		List<object> list = new List<object>();
		List<string> list2 = new List<string>();
		lock (m_domainTable.SyncRoot)
		{
			IDictionaryEnumerator enumerator = m_domainTable.GetEnumerator();
			while (enumerator.MoveNext())
			{
				string item = (string)enumerator.Key;
				PathList pathList = (PathList)enumerator.Value;
				lock (pathList.SyncRoot)
				{
					IDictionaryEnumerator enumerator2 = pathList.GetEnumerator();
					while (enumerator2.MoveNext())
					{
						CookieCollection cookieCollection = (CookieCollection)enumerator2.Value;
						if (cookieCollection.Count == 0)
						{
							list.Add(enumerator2.Key);
						}
					}
					foreach (object item2 in list)
					{
						pathList.Remove(item2);
					}
					list.Clear();
					if (pathList.Count == 0)
					{
						list2.Add(item);
					}
				}
			}
			foreach (string item3 in list2)
			{
				m_domainTable.Remove(item3);
			}
		}
	}

	private int ExpireCollection(CookieCollection cc)
	{
		lock (cc)
		{
			int count = cc.Count;
			for (int num = count - 1; num >= 0; num--)
			{
				Cookie cookie = cc[num];
				if (cookie.Expired)
				{
					cc.RemoveAt(num);
				}
			}
			return count - cc.Count;
		}
	}

	public void Add(CookieCollection cookies)
	{
		if (cookies == null)
		{
			throw new ArgumentNullException("cookies");
		}
		foreach (Cookie item in (IEnumerable<Cookie>)cookies)
		{
			Add(item);
		}
	}

	internal bool IsLocalDomain(string host)
	{
		int num = host.IndexOf('.');
		if (num == -1)
		{
			return true;
		}
		switch (host)
		{
		case "127.0.0.1":
		case "::1":
		case "0:0:0:0:0:0:0:1":
			return true;
		default:
		{
			if (string.Compare(m_fqdnMyDomain, 0, host, num, m_fqdnMyDomain.Length, StringComparison.OrdinalIgnoreCase) == 0)
			{
				return true;
			}
			string[] array = host.Split('.');
			if (array != null && array.Length == 4 && array[0] == "127")
			{
				int i;
				for (i = 1; i < array.Length; i++)
				{
					string text = array[i];
					switch (text.Length)
					{
					case 3:
						if (text[2] < '0' || text[2] > '9')
						{
							break;
						}
						goto case 2;
					case 2:
						if (text[1] < '0' || text[1] > '9')
						{
							break;
						}
						goto case 1;
					case 1:
						if (text[0] >= '0' && text[0] <= '9')
						{
							continue;
						}
						break;
					}
					break;
				}
				if (i == 4)
				{
					return true;
				}
			}
			return false;
		}
		}
	}

	public void Add(Uri uri, Cookie cookie)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		if (cookie == null)
		{
			throw new ArgumentNullException("cookie");
		}
		Cookie cookie2 = cookie.Clone();
		cookie2.VerifySetDefaults(cookie2.Variant, uri, IsLocalDomain(uri.Host), m_fqdnMyDomain, setDefault: true, shouldThrow: true);
		Add(cookie2, throwOnError: true);
	}

	public void Add(Uri uri, CookieCollection cookies)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		if (cookies == null)
		{
			throw new ArgumentNullException("cookies");
		}
		bool isLocalDomain = IsLocalDomain(uri.Host);
		foreach (Cookie cookie3 in cookies)
		{
			Cookie cookie2 = cookie3.Clone();
			cookie2.VerifySetDefaults(cookie2.Variant, uri, isLocalDomain, m_fqdnMyDomain, setDefault: true, shouldThrow: true);
			Add(cookie2, throwOnError: true);
		}
	}

	internal CookieCollection CookieCutter(Uri uri, string headerName, string setCookieHeader, bool isThrow)
	{
		if (NetEventSource.Log.IsEnabled())
		{
			NetEventSource.Info(this, $"uri:{uri} headerName:{headerName} setCookieHeader:{setCookieHeader} isThrow:{isThrow}", "CookieCutter");
		}
		CookieCollection cookieCollection = new CookieCollection();
		CookieVariant variant = CookieVariant.Unknown;
		if (headerName == null)
		{
			variant = CookieVariant.Rfc2109;
		}
		else
		{
			for (int i = 0; i < s_headerInfo.Length; i++)
			{
				if (string.Equals(headerName, s_headerInfo[i].Name, StringComparison.OrdinalIgnoreCase))
				{
					variant = s_headerInfo[i].Variant;
				}
			}
		}
		bool isLocalDomain = IsLocalDomain(uri.Host);
		try
		{
			CookieParser cookieParser = new CookieParser(setCookieHeader);
			while (true)
			{
				Cookie cookie = cookieParser.Get();
				if (NetEventSource.Log.IsEnabled())
				{
					NetEventSource.Info(this, $"CookieParser returned cookie:{cookie}", "CookieCutter");
				}
				if (cookie == null)
				{
					if (cookieParser.EndofHeader())
					{
						break;
					}
				}
				else if (string.IsNullOrEmpty(cookie.Name))
				{
					if (isThrow)
					{
						throw new CookieException(System.SR.net_cookie_format);
					}
				}
				else if (cookie.VerifySetDefaults(variant, uri, isLocalDomain, m_fqdnMyDomain, setDefault: true, isThrow))
				{
					cookieCollection.InternalAdd(cookie, isStrict: true);
				}
			}
		}
		catch (OutOfMemoryException)
		{
			throw;
		}
		catch (Exception inner)
		{
			if (isThrow)
			{
				throw new CookieException(System.SR.Format(System.SR.net_cookie_parse_header, uri.AbsoluteUri), inner);
			}
		}
		int count = cookieCollection.Count;
		for (int j = 0; j < count; j++)
		{
			Add(cookieCollection[j], isThrow);
		}
		return cookieCollection;
	}

	public CookieCollection GetCookies(Uri uri)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		return InternalGetCookies(uri) ?? new CookieCollection();
	}

	public CookieCollection GetAllCookies()
	{
		CookieCollection cookieCollection = new CookieCollection();
		lock (m_domainTable.SyncRoot)
		{
			IDictionaryEnumerator enumerator = m_domainTable.GetEnumerator();
			while (enumerator.MoveNext())
			{
				PathList pathList = (PathList)enumerator.Value;
				lock (pathList.SyncRoot)
				{
					IDictionaryEnumerator enumerator2 = pathList.List.GetEnumerator();
					while (enumerator2.MoveNext())
					{
						cookieCollection.Add((CookieCollection)enumerator2.Value);
					}
				}
			}
			return cookieCollection;
		}
	}

	internal CookieCollection InternalGetCookies(Uri uri)
	{
		if (m_count == 0)
		{
			return null;
		}
		bool isSecure = uri.Scheme == "https" || uri.Scheme == "wss";
		int port = uri.Port;
		CookieCollection cookies = null;
		List<string> list = new List<string>();
		List<string> list2 = null;
		string host = uri.Host;
		list.Add(host);
		list.Add("." + host);
		int num = host.IndexOf('.');
		if (num == -1)
		{
			if (m_fqdnMyDomain != null && m_fqdnMyDomain.Length != 0)
			{
				list.Add(host + m_fqdnMyDomain);
				list.Add(m_fqdnMyDomain);
			}
		}
		else
		{
			list.Add(host.Substring(num));
			if (host.Length > 2)
			{
				int num2 = host.LastIndexOf('.', host.Length - 2);
				if (num2 > 0)
				{
					num2 = host.LastIndexOf('.', num2 - 1);
				}
				if (num2 != -1)
				{
					while (num < num2 && (num = host.IndexOf('.', num + 1)) != -1)
					{
						if (list2 == null)
						{
							list2 = new List<string>();
						}
						list2.Add(host.Substring(num));
					}
				}
			}
		}
		BuildCookieCollectionFromDomainMatches(uri, isSecure, port, ref cookies, list, matchOnlyPlainCookie: false);
		if (list2 != null)
		{
			BuildCookieCollectionFromDomainMatches(uri, isSecure, port, ref cookies, list2, matchOnlyPlainCookie: true);
		}
		return cookies;
	}

	private void BuildCookieCollectionFromDomainMatches(Uri uri, bool isSecure, int port, ref CookieCollection cookies, List<string> domainAttribute, bool matchOnlyPlainCookie)
	{
		for (int i = 0; i < domainAttribute.Count; i++)
		{
			PathList pathList;
			lock (m_domainTable.SyncRoot)
			{
				pathList = (PathList)m_domainTable[domainAttribute[i]];
				if (pathList == null)
				{
					continue;
				}
			}
			lock (pathList.SyncRoot)
			{
				SortedList list = pathList.List;
				int count = list.Count;
				for (int j = 0; j < count; j++)
				{
					string cookiePath = (string)list.GetKey(j);
					if (PathMatch(uri.AbsolutePath, cookiePath))
					{
						CookieCollection cookieCollection = (CookieCollection)list.GetByIndex(j);
						cookieCollection.TimeStamp(CookieCollection.Stamp.Set);
						MergeUpdateCollections(ref cookies, cookieCollection, port, isSecure, matchOnlyPlainCookie);
					}
				}
			}
			if (pathList.Count == 0)
			{
				lock (m_domainTable.SyncRoot)
				{
					m_domainTable.Remove(domainAttribute[i]);
				}
			}
		}
	}

	private static bool PathMatch(string requestPath, string cookiePath)
	{
		cookiePath = CookieParser.CheckQuoted(cookiePath);
		if (!requestPath.StartsWith(cookiePath, StringComparison.Ordinal))
		{
			return false;
		}
		if (requestPath.Length != cookiePath.Length && (cookiePath.Length <= 0 || cookiePath[^1] != '/'))
		{
			return requestPath[cookiePath.Length] == '/';
		}
		return true;
	}

	private void MergeUpdateCollections(ref CookieCollection destination, CookieCollection source, int port, bool isSecure, bool isPlainOnly)
	{
		lock (source)
		{
			for (int i = 0; i < source.Count; i++)
			{
				bool flag = false;
				Cookie cookie = source[i];
				if (cookie.Expired)
				{
					source.RemoveAt(i);
					m_count--;
					i--;
					continue;
				}
				if (!isPlainOnly || cookie.Variant == CookieVariant.Plain)
				{
					if (cookie.PortList != null)
					{
						int[] portList = cookie.PortList;
						foreach (int num in portList)
						{
							if (num == port)
							{
								flag = true;
								break;
							}
						}
					}
					else
					{
						flag = true;
					}
				}
				if (cookie.Secure && !isSecure)
				{
					flag = false;
				}
				if (flag)
				{
					if (destination == null)
					{
						destination = new CookieCollection();
					}
					destination.InternalAdd(cookie, isStrict: false);
				}
			}
		}
	}

	public string GetCookieHeader(Uri uri)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		string optCookie;
		return GetCookieHeader(uri, out optCookie);
	}

	internal string GetCookieHeader(Uri uri, out string optCookie2)
	{
		CookieCollection cookieCollection = InternalGetCookies(uri);
		if (cookieCollection == null)
		{
			optCookie2 = string.Empty;
			return string.Empty;
		}
		string value = string.Empty;
		StringBuilder stringBuilder = System.Text.StringBuilderCache.Acquire();
		for (int i = 0; i < cookieCollection.Count; i++)
		{
			stringBuilder.Append(value);
			cookieCollection[i].ToString(stringBuilder);
			value = "; ";
		}
		optCookie2 = (cookieCollection.IsOtherVersionSeen ? "$Version=1" : string.Empty);
		return System.Text.StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	public void SetCookies(Uri uri, string cookieHeader)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		if (cookieHeader == null)
		{
			throw new ArgumentNullException("cookieHeader");
		}
		CookieCutter(uri, null, cookieHeader, isThrow: true);
	}
}
