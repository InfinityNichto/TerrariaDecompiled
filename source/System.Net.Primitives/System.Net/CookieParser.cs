using System.Globalization;
using System.Reflection;

namespace System.Net;

internal struct CookieParser
{
	private CookieTokenizer _tokenizer;

	private Cookie _savedCookie;

	private static FieldInfo s_isQuotedDomainField;

	private static FieldInfo s_isQuotedVersionField;

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

	private static FieldInfo IsQuotedVersionField
	{
		get
		{
			if (s_isQuotedVersionField == null)
			{
				FieldInfo field = typeof(Cookie).GetField("IsQuotedVersion", BindingFlags.Instance | BindingFlags.NonPublic);
				s_isQuotedVersionField = field;
			}
			return s_isQuotedVersionField;
		}
	}

	internal CookieParser(string cookieString)
	{
		_tokenizer = new CookieTokenizer(cookieString);
		_savedCookie = null;
	}

	private static bool InternalSetNameMethod(Cookie cookie, string value)
	{
		return cookie.InternalSetName(value);
	}

	internal Cookie Get()
	{
		Cookie cookie = null;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		bool flag6 = false;
		bool flag7 = false;
		bool flag8 = false;
		bool flag9 = false;
		do
		{
			CookieToken cookieToken = _tokenizer.Next(cookie == null, parseResponseCookies: true);
			if (cookie == null && (cookieToken == CookieToken.NameValuePair || cookieToken == CookieToken.Attribute))
			{
				cookie = new Cookie();
				InternalSetNameMethod(cookie, _tokenizer.Name);
				cookie.Value = _tokenizer.Value;
				continue;
			}
			switch (cookieToken)
			{
			case CookieToken.NameValuePair:
				switch (_tokenizer.Token)
				{
				case CookieToken.Comment:
					if (!flag)
					{
						flag = true;
						cookie.Comment = _tokenizer.Value;
					}
					break;
				case CookieToken.CommentUrl:
					if (!flag2)
					{
						flag2 = true;
						if (Uri.TryCreate(CheckQuoted(_tokenizer.Value), UriKind.Absolute, out Uri result3))
						{
							cookie.CommentUri = result3;
						}
					}
					break;
				case CookieToken.Domain:
					if (!flag3)
					{
						flag3 = true;
						cookie.Domain = CheckQuoted(_tokenizer.Value);
						IsQuotedDomainField.SetValue(cookie, _tokenizer.Quoted);
					}
					break;
				case CookieToken.Expires:
					if (!flag4)
					{
						flag4 = true;
						if (DateTime.TryParse(CheckQuoted(_tokenizer.Value), CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AdjustToUniversal, out var result4))
						{
							cookie.Expires = result4;
						}
						else
						{
							InternalSetNameMethod(cookie, string.Empty);
						}
					}
					break;
				case CookieToken.MaxAge:
					if (!flag4)
					{
						flag4 = true;
						if (int.TryParse(CheckQuoted(_tokenizer.Value), out var result2))
						{
							cookie.Expires = DateTime.Now.AddSeconds(result2);
						}
						else
						{
							InternalSetNameMethod(cookie, string.Empty);
						}
					}
					break;
				case CookieToken.Path:
					if (!flag5)
					{
						flag5 = true;
						cookie.Path = _tokenizer.Value;
					}
					break;
				case CookieToken.Port:
					if (!flag6)
					{
						flag6 = true;
						try
						{
							cookie.Port = _tokenizer.Value;
						}
						catch
						{
							InternalSetNameMethod(cookie, string.Empty);
						}
					}
					break;
				case CookieToken.Version:
					if (!flag7)
					{
						flag7 = true;
						if (int.TryParse(CheckQuoted(_tokenizer.Value), out var result))
						{
							cookie.Version = result;
							IsQuotedVersionField.SetValue(cookie, _tokenizer.Quoted);
						}
						else
						{
							InternalSetNameMethod(cookie, string.Empty);
						}
					}
					break;
				}
				break;
			case CookieToken.Attribute:
				switch (_tokenizer.Token)
				{
				case CookieToken.Discard:
					if (!flag9)
					{
						flag9 = true;
						cookie.Discard = true;
					}
					break;
				case CookieToken.Secure:
					if (!flag8)
					{
						flag8 = true;
						cookie.Secure = true;
					}
					break;
				case CookieToken.HttpOnly:
					cookie.HttpOnly = true;
					break;
				case CookieToken.Port:
					if (!flag6)
					{
						flag6 = true;
						cookie.Port = string.Empty;
					}
					break;
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

	internal bool EndofHeader()
	{
		return _tokenizer.Eof;
	}
}
