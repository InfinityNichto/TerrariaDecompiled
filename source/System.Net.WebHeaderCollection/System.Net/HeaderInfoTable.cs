using System.Collections;
using System.Collections.Generic;

namespace System.Net;

internal sealed class HeaderInfoTable
{
	private static readonly Func<string, string[]> s_singleParser = (string value) => new string[1] { value };

	private static readonly Func<string, string[]> s_multiParser = (string value) => ParseValueHelper(value, isSetCookie: false);

	private static readonly Func<string, string[]> s_setCookieParser = (string value) => ParseValueHelper(value, isSetCookie: true);

	private static readonly HeaderInfo s_unknownHeaderInfo = new HeaderInfo(string.Empty, requestRestricted: false, responseRestricted: false, multi: false, s_singleParser);

	private static readonly Hashtable s_headerHashTable = CreateHeaderHashtable();

	internal HeaderInfo this[string name]
	{
		get
		{
			HeaderInfo headerInfo = (HeaderInfo)s_headerHashTable[name];
			if (headerInfo == null)
			{
				return s_unknownHeaderInfo;
			}
			return headerInfo;
		}
	}

	private static string[] ParseValueHelper(string value, bool isSetCookie)
	{
		if (isSetCookie && !value.Contains('='))
		{
			return Array.Empty<string>();
		}
		List<string> list = new List<string>();
		bool flag = false;
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < value.Length; i++)
		{
			if (value[i] == '"')
			{
				flag = !flag;
			}
			else if (value[i] == ',' && !flag)
			{
				string text = value.SubstringTrim(num, num2);
				if (!isSetCookie || !IsDuringExpiresAttributeParsing(text))
				{
					list.Add(text);
					num = i + 1;
					num2 = 0;
					continue;
				}
			}
			num2++;
		}
		if (num < value.Length && num2 > 0)
		{
			list.Add(value.SubstringTrim(num, num2));
		}
		return list.ToArray();
	}

	private static bool IsDuringExpiresAttributeParsing(string singleValue)
	{
		if (!singleValue.Contains(';'))
		{
			return false;
		}
		string text = singleValue.Split(';')[^1];
		bool flag = !text.Contains(',');
		string a = text.Split('=')[0].Trim();
		bool flag2 = string.Equals(a, "Expires", StringComparison.OrdinalIgnoreCase);
		return flag2 && flag;
	}

	private static Hashtable CreateHeaderHashtable()
	{
		return new Hashtable(104, CaseInsensitiveAscii.StaticInstance)
		{
			{
				"Age",
				new HeaderInfo("Age", requestRestricted: false, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"Allow",
				new HeaderInfo("Allow", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Accept",
				new HeaderInfo("Accept", requestRestricted: true, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Authorization",
				new HeaderInfo("Authorization", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Accept-Ranges",
				new HeaderInfo("Accept-Ranges", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Accept-Charset",
				new HeaderInfo("Accept-Charset", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Accept-Encoding",
				new HeaderInfo("Accept-Encoding", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Accept-Language",
				new HeaderInfo("Accept-Language", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Cookie",
				new HeaderInfo("Cookie", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Connection",
				new HeaderInfo("Connection", requestRestricted: true, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Content-MD5",
				new HeaderInfo("Content-MD5", requestRestricted: false, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"Content-Type",
				new HeaderInfo("Content-Type", requestRestricted: true, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"Cache-Control",
				new HeaderInfo("Cache-Control", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Content-Range",
				new HeaderInfo("Content-Range", requestRestricted: false, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"Content-Length",
				new HeaderInfo("Content-Length", requestRestricted: true, responseRestricted: true, multi: false, s_singleParser)
			},
			{
				"Content-Encoding",
				new HeaderInfo("Content-Encoding", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Content-Language",
				new HeaderInfo("Content-Language", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Content-Location",
				new HeaderInfo("Content-Location", requestRestricted: false, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"Date",
				new HeaderInfo("Date", requestRestricted: true, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"ETag",
				new HeaderInfo("ETag", requestRestricted: false, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"Expect",
				new HeaderInfo("Expect", requestRestricted: true, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Expires",
				new HeaderInfo("Expires", requestRestricted: false, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"From",
				new HeaderInfo("From", requestRestricted: false, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"Host",
				new HeaderInfo("Host", requestRestricted: true, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"If-Match",
				new HeaderInfo("If-Match", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"If-Range",
				new HeaderInfo("If-Range", requestRestricted: false, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"If-None-Match",
				new HeaderInfo("If-None-Match", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"If-Modified-Since",
				new HeaderInfo("If-Modified-Since", requestRestricted: true, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"If-Unmodified-Since",
				new HeaderInfo("If-Unmodified-Since", requestRestricted: false, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"Keep-Alive",
				new HeaderInfo("Keep-Alive", requestRestricted: false, responseRestricted: true, multi: false, s_singleParser)
			},
			{
				"Location",
				new HeaderInfo("Location", requestRestricted: false, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"Last-Modified",
				new HeaderInfo("Last-Modified", requestRestricted: false, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"Max-Forwards",
				new HeaderInfo("Max-Forwards", requestRestricted: false, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"Pragma",
				new HeaderInfo("Pragma", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Proxy-Authenticate",
				new HeaderInfo("Proxy-Authenticate", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Proxy-Authorization",
				new HeaderInfo("Proxy-Authorization", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Proxy-Connection",
				new HeaderInfo("Proxy-Connection", requestRestricted: true, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Range",
				new HeaderInfo("Range", requestRestricted: true, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Referer",
				new HeaderInfo("Referer", requestRestricted: true, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"Retry-After",
				new HeaderInfo("Retry-After", requestRestricted: false, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"Server",
				new HeaderInfo("Server", requestRestricted: false, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"Set-Cookie",
				new HeaderInfo("Set-Cookie", requestRestricted: false, responseRestricted: false, multi: true, s_setCookieParser)
			},
			{
				"Set-Cookie2",
				new HeaderInfo("Set-Cookie2", requestRestricted: false, responseRestricted: false, multi: true, s_setCookieParser)
			},
			{
				"TE",
				new HeaderInfo("TE", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Trailer",
				new HeaderInfo("Trailer", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Transfer-Encoding",
				new HeaderInfo("Transfer-Encoding", requestRestricted: true, responseRestricted: true, multi: true, s_multiParser)
			},
			{
				"Upgrade",
				new HeaderInfo("Upgrade", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"User-Agent",
				new HeaderInfo("User-Agent", requestRestricted: true, responseRestricted: false, multi: false, s_singleParser)
			},
			{
				"Via",
				new HeaderInfo("Via", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Vary",
				new HeaderInfo("Vary", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"Warning",
				new HeaderInfo("Warning", requestRestricted: false, responseRestricted: false, multi: true, s_multiParser)
			},
			{
				"WWW-Authenticate",
				new HeaderInfo("WWW-Authenticate", requestRestricted: false, responseRestricted: true, multi: true, s_singleParser)
			}
		};
	}
}
