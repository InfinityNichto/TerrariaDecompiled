using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal struct XPathScanner
{
	public enum LexKind
	{
		Comma = 44,
		Slash = 47,
		At = 64,
		Dot = 46,
		LParens = 40,
		RParens = 41,
		LBracket = 91,
		RBracket = 93,
		Star = 42,
		Plus = 43,
		Minus = 45,
		Eq = 61,
		Lt = 60,
		Gt = 62,
		Bang = 33,
		Dollar = 36,
		Apos = 39,
		Quote = 34,
		Union = 124,
		Ne = 78,
		Le = 76,
		Ge = 71,
		And = 65,
		Or = 79,
		DotDot = 68,
		SlashSlash = 83,
		Name = 110,
		String = 115,
		Number = 100,
		Axe = 97,
		Eof = 69
	}

	private readonly string _xpathExpr;

	private int _xpathExprIndex;

	private LexKind _kind;

	private char _currentChar;

	private string _name;

	private string _prefix;

	private string _stringValue;

	private double _numberValue;

	private bool _canBeFunction;

	public string SourceText => _xpathExpr;

	private char CurrentChar => _currentChar;

	public LexKind Kind => _kind;

	public string Name => _name;

	public string Prefix => _prefix;

	public string StringValue => _stringValue;

	public double NumberValue => _numberValue;

	public bool CanBeFunction => _canBeFunction;

	public XPathScanner(string xpathExpr)
	{
		this = default(XPathScanner);
		if (xpathExpr == null)
		{
			throw XPathException.Create(System.SR.Xp_ExprExpected, string.Empty);
		}
		_xpathExpr = xpathExpr;
		_numberValue = double.NaN;
		NextChar();
		NextLex();
	}

	private bool NextChar()
	{
		string xpathExpr = _xpathExpr;
		int xpathExprIndex = _xpathExprIndex;
		if ((uint)xpathExprIndex < (uint)xpathExpr.Length)
		{
			_currentChar = xpathExpr[xpathExprIndex];
			_xpathExprIndex = xpathExprIndex + 1;
			return true;
		}
		_currentChar = '\0';
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SkipSpace()
	{
		if (XmlCharType.IsWhiteSpace(CurrentChar))
		{
			SkipKnownSpace();
		}
	}

	private void SkipKnownSpace()
	{
		while (NextChar() && XmlCharType.IsWhiteSpace(CurrentChar))
		{
		}
	}

	public bool NextLex()
	{
		SkipSpace();
		switch (CurrentChar)
		{
		case '\0':
			_kind = LexKind.Eof;
			return false;
		case '#':
		case '$':
		case '(':
		case ')':
		case '*':
		case '+':
		case ',':
		case '-':
		case '=':
		case '@':
		case '[':
		case ']':
		case '|':
			_kind = (LexKind)Convert.ToInt32(CurrentChar, CultureInfo.InvariantCulture);
			NextChar();
			break;
		case '<':
			_kind = LexKind.Lt;
			NextChar();
			if (CurrentChar == '=')
			{
				_kind = LexKind.Le;
				NextChar();
			}
			break;
		case '>':
			_kind = LexKind.Gt;
			NextChar();
			if (CurrentChar == '=')
			{
				_kind = LexKind.Ge;
				NextChar();
			}
			break;
		case '!':
			_kind = LexKind.Bang;
			NextChar();
			if (CurrentChar == '=')
			{
				_kind = LexKind.Ne;
				NextChar();
			}
			break;
		case '.':
			_kind = LexKind.Dot;
			NextChar();
			if (CurrentChar == '.')
			{
				_kind = LexKind.DotDot;
				NextChar();
			}
			else if (XmlCharType.IsDigit(CurrentChar))
			{
				_kind = LexKind.Number;
				_numberValue = ScanFraction();
			}
			break;
		case '/':
			_kind = LexKind.Slash;
			NextChar();
			if (CurrentChar == '/')
			{
				_kind = LexKind.SlashSlash;
				NextChar();
			}
			break;
		case '"':
		case '\'':
			_kind = LexKind.String;
			_stringValue = ScanString();
			break;
		default:
			if (XmlCharType.IsDigit(CurrentChar))
			{
				_kind = LexKind.Number;
				_numberValue = ScanNumber();
				break;
			}
			if (XmlCharType.IsStartNCNameSingleChar(CurrentChar))
			{
				_kind = LexKind.Name;
				_name = ScanName();
				_prefix = string.Empty;
				if (CurrentChar == ':')
				{
					NextChar();
					if (CurrentChar == ':')
					{
						NextChar();
						_kind = LexKind.Axe;
					}
					else
					{
						_prefix = _name;
						if (CurrentChar == '*')
						{
							NextChar();
							_name = "*";
						}
						else
						{
							if (!XmlCharType.IsStartNCNameSingleChar(CurrentChar))
							{
								throw XPathException.Create(System.SR.Xp_InvalidName, SourceText);
							}
							_name = ScanName();
						}
					}
				}
				else
				{
					SkipSpace();
					if (CurrentChar == ':')
					{
						NextChar();
						if (CurrentChar != ':')
						{
							throw XPathException.Create(System.SR.Xp_InvalidName, SourceText);
						}
						NextChar();
						_kind = LexKind.Axe;
					}
				}
				SkipSpace();
				_canBeFunction = CurrentChar == '(';
				break;
			}
			throw XPathException.Create(System.SR.Xp_InvalidToken, SourceText);
		}
		return true;
	}

	private double ScanNumber()
	{
		int startIndex = _xpathExprIndex - 1;
		int num = 0;
		while (XmlCharType.IsDigit(CurrentChar))
		{
			NextChar();
			num++;
		}
		if (CurrentChar == '.')
		{
			NextChar();
			num++;
			while (XmlCharType.IsDigit(CurrentChar))
			{
				NextChar();
				num++;
			}
		}
		return XmlConvert.ToXPathDouble(_xpathExpr.Substring(startIndex, num));
	}

	private double ScanFraction()
	{
		int startIndex = _xpathExprIndex - 2;
		int num = 1;
		while (XmlCharType.IsDigit(CurrentChar))
		{
			NextChar();
			num++;
		}
		return XmlConvert.ToXPathDouble(_xpathExpr.Substring(startIndex, num));
	}

	private string ScanString()
	{
		char currentChar = CurrentChar;
		NextChar();
		int startIndex = _xpathExprIndex - 1;
		int num = 0;
		while (CurrentChar != currentChar)
		{
			if (!NextChar())
			{
				throw XPathException.Create(System.SR.Xp_UnclosedString);
			}
			num++;
		}
		NextChar();
		return _xpathExpr.Substring(startIndex, num);
	}

	private string ScanName()
	{
		ReadOnlySpan<char> readOnlySpan = _xpathExpr.AsSpan(_xpathExprIndex - 1);
		int i;
		for (i = 1; i < readOnlySpan.Length && XmlCharType.IsNCNameSingleChar(readOnlySpan[i]); i++)
		{
		}
		if ((uint)i < (uint)readOnlySpan.Length)
		{
			_currentChar = readOnlySpan[i];
			_xpathExprIndex += i;
			return readOnlySpan.Slice(0, i).ToString();
		}
		_currentChar = '\0';
		_xpathExprIndex += i - 1;
		return readOnlySpan.ToString();
	}
}
