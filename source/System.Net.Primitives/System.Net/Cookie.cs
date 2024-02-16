using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace System.Net;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class Cookie
{
	internal static readonly char[] PortSplitDelimiters = new char[3] { ' ', ',', '"' };

	internal static readonly char[] ReservedToName = new char[6] { '\t', '\r', '\n', '=', ';', ',' };

	internal static readonly char[] ReservedToValue = new char[2] { ';', ',' };

	private string m_comment = string.Empty;

	private Uri m_commentUri;

	private CookieVariant m_cookieVariant = CookieVariant.Plain;

	private bool m_discard;

	private string m_domain = string.Empty;

	private bool m_domain_implicit = true;

	private DateTime m_expires = DateTime.MinValue;

	private string m_name = string.Empty;

	private string m_path = string.Empty;

	private bool m_path_implicit = true;

	private string m_port = string.Empty;

	private bool m_port_implicit = true;

	private int[] m_port_list;

	private bool m_secure;

	[OptionalField]
	private bool m_httpOnly;

	private DateTime m_timeStamp = DateTime.Now;

	private string m_value = string.Empty;

	private int m_version;

	private string m_domainKey = string.Empty;

	internal bool IsQuotedVersion;

	internal bool IsQuotedDomain;

	public string Comment
	{
		get
		{
			return m_comment;
		}
		[param: AllowNull]
		set
		{
			m_comment = value ?? string.Empty;
		}
	}

	public Uri? CommentUri
	{
		get
		{
			return m_commentUri;
		}
		set
		{
			m_commentUri = value;
		}
	}

	public bool HttpOnly
	{
		get
		{
			return m_httpOnly;
		}
		set
		{
			m_httpOnly = value;
		}
	}

	public bool Discard
	{
		get
		{
			return m_discard;
		}
		set
		{
			m_discard = value;
		}
	}

	public string Domain
	{
		get
		{
			return m_domain;
		}
		[param: AllowNull]
		set
		{
			m_domain = value ?? string.Empty;
			m_domain_implicit = false;
			m_domainKey = string.Empty;
		}
	}

	internal bool DomainImplicit
	{
		get
		{
			return m_domain_implicit;
		}
		set
		{
			m_domain_implicit = value;
		}
	}

	public bool Expired
	{
		get
		{
			if (m_expires != DateTime.MinValue)
			{
				return m_expires.ToLocalTime() <= DateTime.Now;
			}
			return false;
		}
		set
		{
			if (value)
			{
				m_expires = DateTime.Now;
			}
		}
	}

	public DateTime Expires
	{
		get
		{
			return m_expires;
		}
		set
		{
			m_expires = value;
		}
	}

	public string Name
	{
		get
		{
			return m_name;
		}
		set
		{
			if (string.IsNullOrEmpty(value) || !InternalSetName(value))
			{
				throw new CookieException(System.SR.Format(System.SR.net_cookie_attribute, "Name", (value == null) ? "<null>" : value));
			}
		}
	}

	public string Path
	{
		get
		{
			return m_path;
		}
		[param: AllowNull]
		set
		{
			m_path = value ?? string.Empty;
			m_path_implicit = false;
		}
	}

	internal bool Plain => Variant == CookieVariant.Plain;

	public string Port
	{
		get
		{
			return m_port;
		}
		[param: AllowNull]
		set
		{
			m_port_implicit = false;
			if (string.IsNullOrEmpty(value))
			{
				m_port = string.Empty;
				return;
			}
			if (value[0] != '"' || value[value.Length - 1] != '"')
			{
				throw new CookieException(System.SR.Format(System.SR.net_cookie_attribute, "Port", value));
			}
			string[] array = value.Split(PortSplitDelimiters);
			List<int> list = new List<int>();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] != string.Empty)
				{
					if (!int.TryParse(array[i], out var result))
					{
						throw new CookieException(System.SR.Format(System.SR.net_cookie_attribute, "Port", value));
					}
					if (result < 0 || result > 65535)
					{
						throw new CookieException(System.SR.Format(System.SR.net_cookie_attribute, "Port", value));
					}
					list.Add(result);
				}
			}
			m_port_list = list.ToArray();
			m_port = value;
			m_version = 1;
			m_cookieVariant = CookieVariant.Rfc2965;
		}
	}

	internal int[]? PortList => m_port_list;

	public bool Secure
	{
		get
		{
			return m_secure;
		}
		set
		{
			m_secure = value;
		}
	}

	public DateTime TimeStamp => m_timeStamp;

	public string Value
	{
		get
		{
			return m_value;
		}
		[param: AllowNull]
		set
		{
			m_value = value ?? string.Empty;
		}
	}

	internal CookieVariant Variant => m_cookieVariant;

	internal string DomainKey
	{
		get
		{
			if (!m_domain_implicit)
			{
				return m_domainKey;
			}
			return Domain;
		}
	}

	public int Version
	{
		get
		{
			return m_version;
		}
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			m_version = value;
			if (value > 0 && m_cookieVariant < CookieVariant.Rfc2109)
			{
				m_cookieVariant = CookieVariant.Rfc2109;
			}
		}
	}

	[DynamicDependency("ToServerString")]
	public Cookie()
	{
	}

	[DynamicDependency("ToServerString")]
	public Cookie(string name, string? value)
	{
		Name = name;
		Value = value;
	}

	public Cookie(string name, string? value, string? path)
		: this(name, value)
	{
		Path = path;
	}

	public Cookie(string name, string? value, string? path, string? domain)
		: this(name, value, path)
	{
		Domain = domain;
	}

	internal bool InternalSetName(string value)
	{
		if (string.IsNullOrEmpty(value) || value[0] == '$' || value.IndexOfAny(ReservedToName) != -1 || value[0] == ' ' || value[value.Length - 1] == ' ')
		{
			m_name = string.Empty;
			return false;
		}
		m_name = value;
		return true;
	}

	internal Cookie Clone()
	{
		Cookie cookie = new Cookie(m_name, m_value);
		if (!m_port_implicit)
		{
			cookie.Port = m_port;
		}
		if (!m_path_implicit)
		{
			cookie.Path = m_path;
		}
		cookie.Domain = m_domain;
		cookie.DomainImplicit = m_domain_implicit;
		cookie.m_timeStamp = m_timeStamp;
		cookie.Comment = m_comment;
		cookie.CommentUri = m_commentUri;
		cookie.HttpOnly = m_httpOnly;
		cookie.Discard = m_discard;
		cookie.Expires = m_expires;
		cookie.Version = m_version;
		cookie.Secure = m_secure;
		cookie.m_cookieVariant = m_cookieVariant;
		return cookie;
	}

	private static bool IsDomainEqualToHost(string domain, string host)
	{
		if (host.Length + 1 == domain.Length)
		{
			return string.Compare(host, 0, domain, 1, host.Length, StringComparison.OrdinalIgnoreCase) == 0;
		}
		return false;
	}

	internal bool VerifySetDefaults(CookieVariant variant, Uri uri, bool isLocalDomain, string localDomain, bool setDefault, bool shouldThrow)
	{
		string host = uri.Host;
		int port = uri.Port;
		string absolutePath = uri.AbsolutePath;
		bool flag = true;
		if (setDefault)
		{
			if (Version == 0)
			{
				variant = CookieVariant.Plain;
			}
			else if (Version == 1 && variant == CookieVariant.Unknown)
			{
				variant = CookieVariant.Rfc2109;
			}
			m_cookieVariant = variant;
		}
		if (string.IsNullOrEmpty(m_name) || m_name[0] == '$' || m_name.IndexOfAny(ReservedToName) != -1 || m_name[0] == ' ' || m_name[m_name.Length - 1] == ' ')
		{
			if (shouldThrow)
			{
				throw new CookieException(System.SR.Format(System.SR.net_cookie_attribute, "Name", (m_name == null) ? "<null>" : m_name));
			}
			return false;
		}
		if (m_value == null || ((m_value.Length <= 2 || m_value[0] != '"' || m_value[m_value.Length - 1] != '"') && m_value.IndexOfAny(ReservedToValue) != -1))
		{
			if (shouldThrow)
			{
				throw new CookieException(System.SR.Format(System.SR.net_cookie_attribute, "Value", (m_value == null) ? "<null>" : m_value));
			}
			return false;
		}
		if (Comment != null && (Comment.Length <= 2 || Comment[0] != '"' || Comment[Comment.Length - 1] != '"') && Comment.IndexOfAny(ReservedToValue) != -1)
		{
			if (shouldThrow)
			{
				throw new CookieException(System.SR.Format(System.SR.net_cookie_attribute, "Comment", Comment));
			}
			return false;
		}
		if (Path != null && (Path.Length <= 2 || Path[0] != '"' || Path[Path.Length - 1] != '"') && Path.IndexOfAny(ReservedToValue) != -1)
		{
			if (shouldThrow)
			{
				throw new CookieException(System.SR.Format(System.SR.net_cookie_attribute, "Path", Path));
			}
			return false;
		}
		if (setDefault && m_domain_implicit)
		{
			m_domain = host;
		}
		else
		{
			if (!m_domain_implicit)
			{
				string text = m_domain;
				if (!DomainCharsTest(text))
				{
					if (shouldThrow)
					{
						throw new CookieException(System.SR.Format(System.SR.net_cookie_attribute, "Domain", (text == null) ? "<null>" : text));
					}
					return false;
				}
				if (text[0] != '.')
				{
					text = "." + text;
				}
				int num = host.IndexOf('.');
				if (isLocalDomain && string.Equals(localDomain, text, StringComparison.OrdinalIgnoreCase))
				{
					flag = true;
				}
				else if (text.IndexOf('.', 1, text.Length - 2) == -1)
				{
					if (!IsDomainEqualToHost(text, host))
					{
						flag = false;
					}
				}
				else if (variant == CookieVariant.Plain)
				{
					if (!IsDomainEqualToHost(text, host) && (host.Length <= text.Length || string.Compare(host, host.Length - text.Length, text, 0, text.Length, StringComparison.OrdinalIgnoreCase) != 0))
					{
						flag = false;
					}
				}
				else if ((num == -1 || text.Length != host.Length - num || string.Compare(host, num, text, 0, text.Length, StringComparison.OrdinalIgnoreCase) != 0) && !IsDomainEqualToHost(text, host))
				{
					flag = false;
				}
				if (flag)
				{
					m_domainKey = text.ToLowerInvariant();
				}
			}
			else if (!string.Equals(host, m_domain, StringComparison.OrdinalIgnoreCase))
			{
				flag = false;
			}
			if (!flag)
			{
				if (shouldThrow)
				{
					throw new CookieException(System.SR.Format(System.SR.net_cookie_attribute, "Domain", m_domain));
				}
				return false;
			}
		}
		if (setDefault && m_path_implicit)
		{
			switch (m_cookieVariant)
			{
			case CookieVariant.Plain:
			{
				int length;
				if (absolutePath.Length == 0 || absolutePath[0] != '/' || (length = absolutePath.LastIndexOf('/')) == 0)
				{
					m_path = "/";
				}
				else
				{
					m_path = absolutePath.Substring(0, length);
				}
				break;
			}
			case CookieVariant.Rfc2109:
				m_path = absolutePath.Substring(0, absolutePath.LastIndexOf('/'));
				break;
			default:
				m_path = absolutePath.Substring(0, absolutePath.LastIndexOf('/') + 1);
				break;
			}
		}
		if (setDefault && !m_port_implicit && m_port.Length == 0)
		{
			m_port_list = new int[1] { port };
		}
		if (!m_port_implicit)
		{
			flag = false;
			int[] port_list = m_port_list;
			foreach (int num2 in port_list)
			{
				if (num2 == port)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				if (shouldThrow)
				{
					throw new CookieException(System.SR.Format(System.SR.net_cookie_attribute, "Port", m_port));
				}
				return false;
			}
		}
		return true;
	}

	private static bool DomainCharsTest(string name)
	{
		if (name == null || name.Length == 0)
		{
			return false;
		}
		foreach (char c in name)
		{
			if (c >= '0' && c <= '9')
			{
				continue;
			}
			switch (c)
			{
			case '-':
			case '.':
			case 'a':
			case 'b':
			case 'c':
			case 'd':
			case 'e':
			case 'f':
			case 'g':
			case 'h':
			case 'i':
			case 'j':
			case 'k':
			case 'l':
			case 'm':
			case 'n':
			case 'o':
			case 'p':
			case 'q':
			case 'r':
			case 's':
			case 't':
			case 'u':
			case 'v':
			case 'w':
			case 'x':
			case 'y':
			case 'z':
				continue;
			}
			if ((c < 'A' || c > 'Z') && c != '_')
			{
				return false;
			}
		}
		return true;
	}

	public override bool Equals([NotNullWhen(true)] object? comparand)
	{
		if (comparand is Cookie cookie && string.Equals(Name, cookie.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(Value, cookie.Value, StringComparison.Ordinal) && string.Equals(Path, cookie.Path, StringComparison.Ordinal) && string.Equals(Domain, cookie.Domain, StringComparison.OrdinalIgnoreCase))
		{
			return Version == cookie.Version;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (Name + "=" + Value + ";" + Path + "; " + Domain + "; " + Version).GetHashCode();
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = System.Text.StringBuilderCache.Acquire();
		ToString(stringBuilder);
		return System.Text.StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	internal void ToString(StringBuilder sb)
	{
		int length = sb.Length;
		if (Version != 0)
		{
			sb.Append("$Version=");
			if (IsQuotedVersion)
			{
				sb.Append('"');
			}
			IFormatProvider invariantInfo = NumberFormatInfo.InvariantInfo;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, sb, invariantInfo);
			handler.AppendFormatted(m_version);
			sb.Append(invariantInfo, ref handler);
			if (IsQuotedVersion)
			{
				sb.Append('"');
			}
			sb.Append("; ");
		}
		sb.Append(Name).Append('=').Append(Value);
		if (!Plain)
		{
			if (!m_path_implicit && m_path.Length > 0)
			{
				sb.Append("; $Path=");
				sb.Append(m_path);
			}
			if (!m_domain_implicit && m_domain.Length > 0)
			{
				sb.Append("; $Domain=");
				if (IsQuotedDomain)
				{
					sb.Append('"');
				}
				sb.Append(m_domain);
				if (IsQuotedDomain)
				{
					sb.Append('"');
				}
			}
		}
		if (!m_port_implicit)
		{
			sb.Append("; $Port");
			if (m_port.Length > 0)
			{
				sb.Append('=');
				sb.Append(m_port);
			}
		}
		int length2 = sb.Length;
		if (length2 == 1 + length && sb[length] == '=')
		{
			sb.Length = length;
		}
	}

	internal string ToServerString()
	{
		string text = Name + "=" + Value;
		if (m_comment != null && m_comment.Length > 0)
		{
			text = text + "; Comment=" + m_comment;
		}
		if (m_commentUri != null)
		{
			text = text + "; CommentURL=\"" + m_commentUri.ToString() + "\"";
		}
		if (m_discard)
		{
			text += "; Discard";
		}
		if (!m_domain_implicit && m_domain != null && m_domain.Length > 0)
		{
			text = text + "; Domain=" + m_domain;
		}
		if (Expires != DateTime.MinValue)
		{
			int num = (int)(Expires.ToLocalTime() - DateTime.Now).TotalSeconds;
			if (num < 0)
			{
				num = 0;
			}
			text = text + "; Max-Age=" + num.ToString(NumberFormatInfo.InvariantInfo);
		}
		if (!m_path_implicit && m_path != null && m_path.Length > 0)
		{
			text = text + "; Path=" + m_path;
		}
		if (!Plain && !m_port_implicit && m_port != null && m_port.Length > 0)
		{
			text = text + "; Port=" + m_port;
		}
		if (m_version > 0)
		{
			text = text + "; Version=" + m_version.ToString(NumberFormatInfo.InvariantInfo);
		}
		if (!(text == "="))
		{
			return text;
		}
		return null;
	}
}
