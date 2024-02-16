using System.Reflection;

namespace System.Net;

internal struct CookieParser
{
	private System.Net.CookieTokenizer _tokenizer;

	private Cookie _savedCookie;

	private static Func<Cookie, string, bool> s_internalSetNameMethod;

	private static FieldInfo s_isQuotedDomainField;

	private static Func<Cookie, string, bool> InternalSetNameMethod
	{
		get
		{
			if (s_internalSetNameMethod == null)
			{
				MethodInfo method = typeof(Cookie).GetMethod("InternalSetName", BindingFlags.Instance | BindingFlags.NonPublic);
				s_internalSetNameMethod = (Func<Cookie, string, bool>)Delegate.CreateDelegate(typeof(Func<Cookie, string, bool>), method);
			}
			return s_internalSetNameMethod;
		}
	}

	private static FieldInfo IsQuotedDomainField
	{
		get
		{
			if (s_isQuotedDomainField == null)
			{
				FieldInfo field = typeof(Cookie).GetField("IsQuotedDomain", BindingFlags.Instance | BindingFlags.NonPublic);
				s_isQuotedDomainField = field;
			}
			return s_isQuotedDomainField;
		}
	}

	internal CookieParser(string cookieString)
	{
		_tokenizer = new System.Net.CookieTokenizer(cookieString);
		_savedCookie = null;
	}

	internal Cookie GetServer()
	{
		Cookie cookie = _savedCookie;
		_savedCookie = null;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		do
		{
			bool flag4 = cookie == null || string.IsNullOrEmpty(cookie.Name);
			System.Net.CookieToken cookieToken = _tokenizer.Next(flag4, parseResponseCookies: false);
			if (flag4 && (cookieToken == System.Net.CookieToken.NameValuePair || cookieToken == System.Net.CookieToken.Attribute))
			{
				if (cookie == null)
				{
					cookie = new Cookie();
				}
				InternalSetNameMethod(cookie, _tokenizer.Name);
				cookie.Value = _tokenizer.Value;
				continue;
			}
			switch (cookieToken)
			{
			case System.Net.CookieToken.NameValuePair:
				switch (_tokenizer.Token)
				{
				case System.Net.CookieToken.Domain:
					if (!flag)
					{
						flag = true;
						cookie.Domain = CheckQuoted(_tokenizer.Value);
						IsQuotedDomainField.SetValue(cookie, _tokenizer.Quoted);
					}
					break;
				case System.Net.CookieToken.Path:
					if (!flag2)
					{
						flag2 = true;
						cookie.Path = _tokenizer.Value;
					}
					break;
				case System.Net.CookieToken.Port:
					if (!flag3)
					{
						flag3 = true;
						try
						{
							cookie.Port = _tokenizer.Value;
						}
						catch (CookieException)
						{
							InternalSetNameMethod(cookie, string.Empty);
						}
					}
					break;
				case System.Net.CookieToken.Version:
				{
					_savedCookie = new Cookie();
					if (int.TryParse(_tokenizer.Value, out var result))
					{
						_savedCookie.Version = result;
					}
					return cookie;
				}
				case System.Net.CookieToken.Unknown:
					_savedCookie = new Cookie();
					InternalSetNameMethod(_savedCookie, _tokenizer.Name);
					_savedCookie.Value = _tokenizer.Value;
					return cookie;
				}
				break;
			case System.Net.CookieToken.Attribute:
				if (_tokenizer.Token == System.Net.CookieToken.Port && !flag3)
				{
					flag3 = true;
					cookie.Port = string.Empty;
				}
				break;
			}
		}
		while (!_tokenizer.Eof && !_tokenizer.EndOfCookie);
		return cookie;
	}

	internal static string CheckQuoted(string value)
	{
		if (value.Length < 2 || value[0] != '"' || value[value.Length - 1] != '"')
		{
			return value;
		}
		if (value.Length != 2)
		{
			return value.Substring(1, value.Length - 2);
		}
		return string.Empty;
	}
}
