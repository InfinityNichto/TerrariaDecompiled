namespace System.Net;

internal struct CookieTokenizer
{
	private struct RecognizedAttribute
	{
		private readonly string _name;

		private readonly CookieToken _token;

		internal CookieToken Token => _token;

		internal RecognizedAttribute(string name, CookieToken token)
		{
			_name = name;
			_token = token;
		}

		internal bool IsEqualTo(string value)
		{
			return string.Equals(_name, value, StringComparison.OrdinalIgnoreCase);
		}
	}

	private bool _eofCookie;

	private int _index;

	private readonly int _length;

	private string _name;

	private bool _quoted;

	private int _start;

	private CookieToken _token;

	private int _tokenLength;

	private readonly string _tokenStream;

	private string _value;

	private int _cookieStartIndex;

	private int _cookieLength;

	private static readonly RecognizedAttribute[] s_recognizedAttributes = new RecognizedAttribute[11]
	{
		new RecognizedAttribute("Path", CookieToken.Path),
		new RecognizedAttribute("Max-Age", CookieToken.MaxAge),
		new RecognizedAttribute("Expires", CookieToken.Expires),
		new RecognizedAttribute("Version", CookieToken.Version),
		new RecognizedAttribute("Domain", CookieToken.Domain),
		new RecognizedAttribute("Secure", CookieToken.Secure),
		new RecognizedAttribute("Discard", CookieToken.Discard),
		new RecognizedAttribute("Port", CookieToken.Port),
		new RecognizedAttribute("Comment", CookieToken.Comment),
		new RecognizedAttribute("CommentURL", CookieToken.CommentUrl),
		new RecognizedAttribute("HttpOnly", CookieToken.HttpOnly)
	};

	private static readonly RecognizedAttribute[] s_recognizedServerAttributes = new RecognizedAttribute[5]
	{
		new RecognizedAttribute("$Path", CookieToken.Path),
		new RecognizedAttribute("$Version", CookieToken.Version),
		new RecognizedAttribute("$Domain", CookieToken.Domain),
		new RecognizedAttribute("$Port", CookieToken.Port),
		new RecognizedAttribute("$HttpOnly", CookieToken.HttpOnly)
	};

	internal bool EndOfCookie
	{
		get
		{
			return _eofCookie;
		}
		set
		{
			_eofCookie = value;
		}
	}

	internal bool Eof => _index >= _length;

	internal string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
		}
	}

	internal bool Quoted
	{
		get
		{
			return _quoted;
		}
		set
		{
			_quoted = value;
		}
	}

	internal CookieToken Token
	{
		get
		{
			return _token;
		}
		set
		{
			_token = value;
		}
	}

	internal string Value
	{
		get
		{
			return _value;
		}
		set
		{
			_value = value;
		}
	}

	internal CookieTokenizer(string tokenStream)
	{
		this = default(CookieTokenizer);
		_length = tokenStream.Length;
		_tokenStream = tokenStream;
		_value = string.Empty;
	}

	internal string Extract()
	{
		string result = string.Empty;
		if (_tokenLength != 0)
		{
			result = (Quoted ? _tokenStream.Substring(_start, _tokenLength) : _tokenStream.SubstringTrim(_start, _tokenLength));
		}
		return result;
	}

	internal CookieToken FindNext(bool ignoreComma, bool ignoreEquals)
	{
		_tokenLength = 0;
		_start = _index;
		while (_index < _length && char.IsWhiteSpace(_tokenStream[_index]))
		{
			_index++;
			_start++;
		}
		CookieToken result = CookieToken.End;
		int num = 1;
		if (!Eof)
		{
			if (_tokenStream[_index] == '"')
			{
				Quoted = true;
				_index++;
				bool flag = false;
				while (_index < _length)
				{
					char c = _tokenStream[_index];
					if (!flag && c == '"')
					{
						break;
					}
					if (flag)
					{
						flag = false;
					}
					else if (c == '\\')
					{
						flag = true;
					}
					_index++;
				}
				if (_index < _length)
				{
					_index++;
				}
				_tokenLength = _index - _start;
				num = 0;
				ignoreComma = false;
			}
			while (_index < _length && _tokenStream[_index] != ';' && (ignoreEquals || _tokenStream[_index] != '=') && (ignoreComma || _tokenStream[_index] != ','))
			{
				if (_tokenStream[_index] == ',')
				{
					_start = _index + 1;
					_tokenLength = -1;
					ignoreComma = false;
				}
				_index++;
				_tokenLength += num;
			}
			if (!Eof)
			{
				switch (_tokenStream[_index])
				{
				case ';':
					result = CookieToken.EndToken;
					break;
				case '=':
					result = CookieToken.Equals;
					break;
				default:
					_cookieLength = _index - _cookieStartIndex;
					result = CookieToken.EndCookie;
					break;
				}
				_index++;
			}
			if (Eof)
			{
				_cookieLength = _index - _cookieStartIndex;
			}
		}
		return result;
	}

	internal CookieToken Next(bool first, bool parseResponseCookies)
	{
		Reset();
		if (first)
		{
			_cookieStartIndex = _index;
			_cookieLength = 0;
		}
		CookieToken cookieToken = FindNext(ignoreComma: false, ignoreEquals: false);
		if (cookieToken == CookieToken.EndCookie)
		{
			EndOfCookie = true;
		}
		if (cookieToken == CookieToken.End || cookieToken == CookieToken.EndCookie)
		{
			string text2 = (Name = Extract());
			if (text2.Length != 0)
			{
				Token = TokenFromName(parseResponseCookies);
				return CookieToken.Attribute;
			}
			return cookieToken;
		}
		Name = Extract();
		if (first)
		{
			Token = CookieToken.CookieName;
		}
		else
		{
			Token = TokenFromName(parseResponseCookies);
		}
		if (cookieToken == CookieToken.Equals)
		{
			cookieToken = FindNext(!first && Token == CookieToken.Expires, ignoreEquals: true);
			if (cookieToken == CookieToken.EndCookie)
			{
				EndOfCookie = true;
			}
			Value = Extract();
			return CookieToken.NameValuePair;
		}
		return CookieToken.Attribute;
	}

	internal void Reset()
	{
		_eofCookie = false;
		_name = string.Empty;
		_quoted = false;
		_start = _index;
		_token = CookieToken.Nothing;
		_tokenLength = 0;
		_value = string.Empty;
	}

	internal CookieToken TokenFromName(bool parseResponseCookies)
	{
		if (!parseResponseCookies)
		{
			for (int i = 0; i < s_recognizedServerAttributes.Length; i++)
			{
				if (s_recognizedServerAttributes[i].IsEqualTo(Name))
				{
					return s_recognizedServerAttributes[i].Token;
				}
			}
		}
		else
		{
			for (int j = 0; j < s_recognizedAttributes.Length; j++)
			{
				if (s_recognizedAttributes[j].IsEqualTo(Name))
				{
					return s_recognizedAttributes[j].Token;
				}
			}
		}
		return CookieToken.Unknown;
	}
}
