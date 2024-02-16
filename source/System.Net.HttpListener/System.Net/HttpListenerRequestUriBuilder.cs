using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace System.Net;

internal sealed class HttpListenerRequestUriBuilder
{
	private enum ParsingResult
	{
		Success,
		InvalidString,
		EncodingError
	}

	private enum EncodingType
	{
		Primary,
		Secondary
	}

	private static readonly Encoding s_utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	private static readonly Encoding s_ansiEncoding = Encoding.GetEncoding(0, new EncoderExceptionFallback(), new DecoderExceptionFallback());

	private readonly string _rawUri;

	private readonly string _cookedUriScheme;

	private readonly string _cookedUriHost;

	private readonly string _cookedUriPath;

	private readonly string _cookedUriQuery;

	private StringBuilder _requestUriString;

	private List<byte> _rawOctets;

	private string _rawPath;

	private Uri _requestUri;

	private HttpListenerRequestUriBuilder(string rawUri, string cookedUriScheme, string cookedUriHost, string cookedUriPath, string cookedUriQuery)
	{
		_rawUri = rawUri;
		_cookedUriScheme = cookedUriScheme;
		_cookedUriHost = cookedUriHost;
		_cookedUriPath = AddSlashToAsteriskOnlyPath(cookedUriPath);
		_cookedUriQuery = cookedUriQuery ?? string.Empty;
	}

	public static Uri GetRequestUri(string rawUri, string cookedUriScheme, string cookedUriHost, string cookedUriPath, string cookedUriQuery)
	{
		HttpListenerRequestUriBuilder httpListenerRequestUriBuilder = new HttpListenerRequestUriBuilder(rawUri, cookedUriScheme, cookedUriHost, cookedUriPath, cookedUriQuery);
		return httpListenerRequestUriBuilder.Build();
	}

	private Uri Build()
	{
		BuildRequestUriUsingRawPath();
		if (_requestUri == null)
		{
			BuildRequestUriUsingCookedPath();
		}
		return _requestUri;
	}

	private void BuildRequestUriUsingCookedPath()
	{
		if (!Uri.TryCreate(_cookedUriScheme + Uri.SchemeDelimiter + _cookedUriHost + _cookedUriPath + _cookedUriQuery, UriKind.Absolute, out _requestUri) && System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Error(this, System.SR.Format(System.SR.net_log_listener_cant_create_uri, _cookedUriScheme, _cookedUriHost, _cookedUriPath, _cookedUriQuery), "BuildRequestUriUsingCookedPath");
		}
	}

	private void BuildRequestUriUsingRawPath()
	{
		bool flag = false;
		_rawPath = GetPath(_rawUri);
		ParsingResult parsingResult = BuildRequestUriUsingRawPath(GetEncoding(EncodingType.Primary));
		if (parsingResult == ParsingResult.EncodingError)
		{
			Encoding encoding = GetEncoding(EncodingType.Secondary);
			parsingResult = BuildRequestUriUsingRawPath(encoding);
		}
		if (parsingResult != 0 && 0 == 0 && System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Error(this, System.SR.Format(System.SR.net_log_listener_cant_create_uri, _cookedUriScheme, _cookedUriHost, _rawPath, _cookedUriQuery), "BuildRequestUriUsingRawPath");
		}
	}

	private static Encoding GetEncoding(EncodingType type)
	{
		if (type == EncodingType.Secondary)
		{
			return s_ansiEncoding;
		}
		return s_utf8Encoding;
	}

	private ParsingResult BuildRequestUriUsingRawPath(Encoding encoding)
	{
		_rawOctets = new List<byte>();
		_requestUriString = new StringBuilder();
		_requestUriString.Append(_cookedUriScheme);
		_requestUriString.Append(Uri.SchemeDelimiter);
		_requestUriString.Append(_cookedUriHost);
		ParsingResult parsingResult = ParseRawPath(encoding);
		if (parsingResult == ParsingResult.Success)
		{
			_requestUriString.Append(_cookedUriQuery);
			if (!Uri.TryCreate(_requestUriString.ToString(), UriKind.Absolute, out _requestUri))
			{
				parsingResult = ParsingResult.InvalidString;
			}
		}
		if (parsingResult != 0 && System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Error(this, System.SR.Format(System.SR.net_log_listener_cant_convert_raw_path, _rawPath, encoding.EncodingName), "BuildRequestUriUsingRawPath");
		}
		return parsingResult;
	}

	private ParsingResult ParseRawPath(Encoding encoding)
	{
		int num = 0;
		char c = '\0';
		while (num < _rawPath.Length)
		{
			c = _rawPath[num];
			if (c == '%')
			{
				num++;
				c = _rawPath[num];
				if (c == 'u' || c == 'U')
				{
					if (!EmptyDecodeAndAppendRawOctetsList(encoding))
					{
						return ParsingResult.EncodingError;
					}
					if (!AppendUnicodeCodePointValuePercentEncoded(_rawPath.Substring(num + 1, 4)))
					{
						return ParsingResult.InvalidString;
					}
					num += 5;
				}
				else
				{
					if (!AddPercentEncodedOctetToRawOctetsList(encoding, _rawPath.Substring(num, 2)))
					{
						return ParsingResult.InvalidString;
					}
					num += 2;
				}
			}
			else
			{
				if (!EmptyDecodeAndAppendRawOctetsList(encoding))
				{
					return ParsingResult.EncodingError;
				}
				_requestUriString.Append(c);
				num++;
			}
		}
		if (!EmptyDecodeAndAppendRawOctetsList(encoding))
		{
			return ParsingResult.EncodingError;
		}
		return ParsingResult.Success;
	}

	private bool AppendUnicodeCodePointValuePercentEncoded(string codePoint)
	{
		if (!int.TryParse(codePoint, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, System.SR.Format(System.SR.net_log_listener_cant_convert_percent_value, codePoint), "AppendUnicodeCodePointValuePercentEncoded");
			}
			return false;
		}
		string text = null;
		try
		{
			text = char.ConvertFromUtf32(result);
			AppendOctetsPercentEncoded(_requestUriString, s_utf8Encoding.GetBytes(text));
			return true;
		}
		catch (ArgumentOutOfRangeException)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, System.SR.Format(System.SR.net_log_listener_cant_convert_percent_value, codePoint), "AppendUnicodeCodePointValuePercentEncoded");
			}
		}
		catch (EncoderFallbackException ex2)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, System.SR.Format(System.SR.net_log_listener_cant_convert_to_utf8, text, ex2.Message), "AppendUnicodeCodePointValuePercentEncoded");
			}
		}
		return false;
	}

	private bool AddPercentEncodedOctetToRawOctetsList(Encoding encoding, string escapedCharacter)
	{
		if (!byte.TryParse(escapedCharacter, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, System.SR.Format(System.SR.net_log_listener_cant_convert_percent_value, escapedCharacter), "AddPercentEncodedOctetToRawOctetsList");
			}
			return false;
		}
		_rawOctets.Add(result);
		return true;
	}

	private bool EmptyDecodeAndAppendRawOctetsList(Encoding encoding)
	{
		if (_rawOctets.Count == 0)
		{
			return true;
		}
		string text = null;
		try
		{
			text = encoding.GetString(_rawOctets.ToArray());
			if (encoding == s_utf8Encoding)
			{
				AppendOctetsPercentEncoded(_requestUriString, _rawOctets.ToArray());
			}
			else
			{
				AppendOctetsPercentEncoded(_requestUriString, s_utf8Encoding.GetBytes(text));
			}
			_rawOctets.Clear();
			return true;
		}
		catch (DecoderFallbackException ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, System.SR.Format(System.SR.net_log_listener_cant_convert_bytes, GetOctetsAsString(_rawOctets), ex.Message), "EmptyDecodeAndAppendRawOctetsList");
			}
		}
		catch (EncoderFallbackException ex2)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, System.SR.Format(System.SR.net_log_listener_cant_convert_to_utf8, text, ex2.Message), "EmptyDecodeAndAppendRawOctetsList");
			}
		}
		return false;
	}

	private static void AppendOctetsPercentEncoded(StringBuilder target, IEnumerable<byte> octets)
	{
		foreach (byte octet in octets)
		{
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(1, 1, target);
			handler.AppendLiteral("%");
			handler.AppendFormatted(octet, "X2");
			target.Append(ref handler);
		}
	}

	private static string GetOctetsAsString(IEnumerable<byte> octets)
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = true;
		foreach (byte octet in octets)
		{
			if (flag)
			{
				flag = false;
			}
			else
			{
				stringBuilder.Append(' ');
			}
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2);
			handler.AppendFormatted(octet, "X2");
			stringBuilder2.Append(ref handler);
		}
		return stringBuilder.ToString();
	}

	private static string GetPath(string uriString)
	{
		int num = 0;
		if (uriString[0] != '/')
		{
			int num2 = 0;
			if (uriString.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
			{
				num2 = 7;
			}
			else if (uriString.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				num2 = 8;
			}
			if (num2 > 0)
			{
				num = uriString.IndexOf('/', num2);
				if (num == -1)
				{
					num = uriString.Length;
				}
			}
			else
			{
				uriString = "/" + uriString;
			}
		}
		int num3 = uriString.IndexOf('?');
		if (num3 == -1)
		{
			num3 = uriString.Length;
		}
		return AddSlashToAsteriskOnlyPath(uriString.Substring(num, num3 - num));
	}

	private static string AddSlashToAsteriskOnlyPath(string path)
	{
		if (path.Length == 1 && path[0] == '*')
		{
			return "/*";
		}
		return path;
	}
}
