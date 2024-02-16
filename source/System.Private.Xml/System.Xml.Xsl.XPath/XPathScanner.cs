namespace System.Xml.Xsl.XPath;

internal sealed class XPathScanner
{
	private readonly string _xpathExpr;

	private int _curIndex;

	private char _curChar;

	private LexKind _kind;

	private string _name;

	private string _prefix;

	private string _stringValue;

	private bool _canBeFunction;

	private int _lexStart;

	private int _prevLexEnd;

	private LexKind _prevKind;

	private XPathAxis _axis;

	public string Source => _xpathExpr;

	public LexKind Kind => _kind;

	public int LexStart => _lexStart;

	public int LexSize => _curIndex - _lexStart;

	public int PrevLexEnd => _prevLexEnd;

	public string Name => _name;

	public string Prefix => _prefix;

	public string RawValue
	{
		get
		{
			if (_kind == LexKind.Eof)
			{
				return LexKindToString(_kind);
			}
			return _xpathExpr.Substring(_lexStart, _curIndex - _lexStart);
		}
	}

	public string StringValue => _stringValue;

	public bool CanBeFunction => _canBeFunction;

	public XPathAxis Axis => _axis;

	public XPathScanner(string xpathExpr)
		: this(xpathExpr, 0)
	{
	}

	public XPathScanner(string xpathExpr, int startFrom)
	{
		_xpathExpr = xpathExpr;
		_kind = LexKind.Unknown;
		SetSourceIndex(startFrom);
		NextLex();
	}

	private void SetSourceIndex(int index)
	{
		_curIndex = index - 1;
		NextChar();
	}

	private void NextChar()
	{
		_curIndex++;
		if (_curIndex < _xpathExpr.Length)
		{
			_curChar = _xpathExpr[_curIndex];
		}
		else
		{
			_curChar = '\0';
		}
	}

	private void SkipSpace()
	{
		while (XmlCharType.IsWhiteSpace(_curChar))
		{
			NextChar();
		}
	}

	private static bool IsAsciiDigit(char ch)
	{
		return (uint)(ch - 48) <= 9u;
	}

	public void NextLex()
	{
		_prevLexEnd = _curIndex;
		_prevKind = _kind;
		SkipSpace();
		_lexStart = _curIndex;
		switch (_curChar)
		{
		case '\0':
			_kind = LexKind.Eof;
			return;
		case '$':
		case '(':
		case ')':
		case ',':
		case '@':
		case '[':
		case ']':
		case '}':
			_kind = (LexKind)_curChar;
			NextChar();
			return;
		case '.':
			NextChar();
			if (_curChar == '.')
			{
				_kind = LexKind.DotDot;
				NextChar();
				return;
			}
			if (IsAsciiDigit(_curChar))
			{
				SetSourceIndex(_lexStart);
				goto case '0';
			}
			_kind = LexKind.Dot;
			return;
		case ':':
			NextChar();
			if (_curChar == ':')
			{
				_kind = LexKind.ColonColon;
				NextChar();
			}
			else
			{
				_kind = LexKind.Unknown;
			}
			return;
		case '*':
			_kind = LexKind.Star;
			NextChar();
			CheckOperator(star: true);
			return;
		case '/':
			NextChar();
			if (_curChar == '/')
			{
				_kind = LexKind.SlashSlash;
				NextChar();
			}
			else
			{
				_kind = LexKind.Slash;
			}
			return;
		case '|':
			_kind = LexKind.Union;
			NextChar();
			return;
		case '+':
			_kind = LexKind.Plus;
			NextChar();
			return;
		case '-':
			_kind = LexKind.Minus;
			NextChar();
			return;
		case '=':
			_kind = LexKind.Eq;
			NextChar();
			return;
		case '!':
			NextChar();
			if (_curChar == '=')
			{
				_kind = LexKind.Ne;
				NextChar();
			}
			else
			{
				_kind = LexKind.Unknown;
			}
			return;
		case '<':
			NextChar();
			if (_curChar == '=')
			{
				_kind = LexKind.Le;
				NextChar();
			}
			else
			{
				_kind = LexKind.Lt;
			}
			return;
		case '>':
			NextChar();
			if (_curChar == '=')
			{
				_kind = LexKind.Ge;
				NextChar();
			}
			else
			{
				_kind = LexKind.Gt;
			}
			return;
		case '"':
		case '\'':
			_kind = LexKind.String;
			ScanString();
			return;
		case '0':
		case '1':
		case '2':
		case '3':
		case '4':
		case '5':
		case '6':
		case '7':
		case '8':
		case '9':
			_kind = LexKind.Number;
			ScanNumber();
			return;
		}
		if (XmlCharType.IsStartNCNameSingleChar(_curChar))
		{
			_kind = LexKind.Name;
			_name = ScanNCName();
			_prefix = string.Empty;
			_canBeFunction = false;
			_axis = XPathAxis.Unknown;
			bool flag = false;
			int curIndex = _curIndex;
			if (_curChar == ':')
			{
				NextChar();
				if (_curChar == ':')
				{
					NextChar();
					flag = true;
					SetSourceIndex(curIndex);
				}
				else if (_curChar == '*')
				{
					NextChar();
					_prefix = _name;
					_name = "*";
				}
				else if (XmlCharType.IsStartNCNameSingleChar(_curChar))
				{
					_prefix = _name;
					_name = ScanNCName();
					curIndex = _curIndex;
					SkipSpace();
					_canBeFunction = _curChar == '(';
					SetSourceIndex(curIndex);
				}
				else
				{
					SetSourceIndex(curIndex);
				}
			}
			else
			{
				SkipSpace();
				if (_curChar == ':')
				{
					NextChar();
					if (_curChar == ':')
					{
						NextChar();
						flag = true;
					}
					SetSourceIndex(curIndex);
				}
				else
				{
					_canBeFunction = _curChar == '(';
				}
			}
			if (!CheckOperator(star: false) && flag)
			{
				_axis = CheckAxis();
			}
		}
		else
		{
			_kind = LexKind.Unknown;
			NextChar();
		}
	}

	private bool CheckOperator(bool star)
	{
		LexKind kind;
		if (star)
		{
			kind = LexKind.Multiply;
		}
		else
		{
			if (_prefix.Length != 0 || _name.Length > 3)
			{
				return false;
			}
			switch (_name)
			{
			case "or":
				kind = LexKind.Or;
				break;
			case "and":
				kind = LexKind.And;
				break;
			case "div":
				kind = LexKind.Divide;
				break;
			case "mod":
				kind = LexKind.Modulo;
				break;
			default:
				return false;
			}
		}
		if (_prevKind <= LexKind.Union)
		{
			return false;
		}
		switch (_prevKind)
		{
		case LexKind.ColonColon:
		case LexKind.SlashSlash:
		case LexKind.Dollar:
		case LexKind.LParens:
		case LexKind.Comma:
		case LexKind.Slash:
		case LexKind.At:
		case LexKind.LBracket:
			return false;
		default:
			_kind = kind;
			return true;
		}
	}

	private XPathAxis CheckAxis()
	{
		_kind = LexKind.Axis;
		switch (_name)
		{
		case "ancestor":
			return XPathAxis.Ancestor;
		case "ancestor-or-self":
			return XPathAxis.AncestorOrSelf;
		case "attribute":
			return XPathAxis.Attribute;
		case "child":
			return XPathAxis.Child;
		case "descendant":
			return XPathAxis.Descendant;
		case "descendant-or-self":
			return XPathAxis.DescendantOrSelf;
		case "following":
			return XPathAxis.Following;
		case "following-sibling":
			return XPathAxis.FollowingSibling;
		case "namespace":
			return XPathAxis.Namespace;
		case "parent":
			return XPathAxis.Parent;
		case "preceding":
			return XPathAxis.Preceding;
		case "preceding-sibling":
			return XPathAxis.PrecedingSibling;
		case "self":
			return XPathAxis.Self;
		default:
			_kind = LexKind.Name;
			return XPathAxis.Unknown;
		}
	}

	private void ScanNumber()
	{
		while (IsAsciiDigit(_curChar))
		{
			NextChar();
		}
		if (_curChar == '.')
		{
			NextChar();
			while (IsAsciiDigit(_curChar))
			{
				NextChar();
			}
		}
		if ((_curChar & -33) == 69)
		{
			NextChar();
			if (_curChar == '+' || _curChar == '-')
			{
				NextChar();
			}
			while (IsAsciiDigit(_curChar))
			{
				NextChar();
			}
			throw CreateException(System.SR.XPath_ScientificNotation);
		}
	}

	private void ScanString()
	{
		int num = _curIndex + 1;
		int num2 = _xpathExpr.IndexOf(_curChar, num);
		if (num2 < 0)
		{
			SetSourceIndex(_xpathExpr.Length);
			throw CreateException(System.SR.XPath_UnclosedString);
		}
		_stringValue = _xpathExpr.Substring(num, num2 - num);
		SetSourceIndex(num2 + 1);
	}

	private string ScanNCName()
	{
		int curIndex = _curIndex;
		while (XmlCharType.IsNCNameSingleChar(_curChar))
		{
			NextChar();
		}
		return _xpathExpr.Substring(curIndex, _curIndex - curIndex);
	}

	public void PassToken(LexKind t)
	{
		CheckToken(t);
		NextLex();
	}

	public void CheckToken(LexKind t)
	{
		if (_kind != t)
		{
			if (t == LexKind.Eof)
			{
				throw CreateException(System.SR.XPath_EofExpected, RawValue);
			}
			throw CreateException(System.SR.XPath_TokenExpected, LexKindToString(t), RawValue);
		}
	}

	private string LexKindToString(LexKind t)
	{
		if (LexKind.Eof < t)
		{
			return char.ToString((char)t);
		}
		return t switch
		{
			LexKind.Name => "<name>", 
			LexKind.String => "<string literal>", 
			LexKind.Eof => "<eof>", 
			_ => string.Empty, 
		};
	}

	public XPathCompileException CreateException(string resId, params string[] args)
	{
		return new XPathCompileException(_xpathExpr, _lexStart, _curIndex, resId, args);
	}
}
