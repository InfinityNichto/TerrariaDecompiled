using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace System.Text.RegularExpressions;

internal ref struct RegexParser
{
	private RegexNode _stack;

	private RegexNode _group;

	private RegexNode _alternation;

	private RegexNode _concatenation;

	private RegexNode _unit;

	private readonly string _pattern;

	private int _currentPos;

	private readonly CultureInfo _culture;

	private int _autocap;

	private int _capcount;

	private int _captop;

	private readonly int _capsize;

	private readonly Hashtable _caps;

	private Hashtable _capnames;

	private int[] _capnumlist;

	private List<string> _capnamelist;

	private RegexOptions _options;

	private System.Collections.Generic.ValueListBuilder<int> _optionsStack;

	private bool _ignoreNextParen;

	private static ReadOnlySpan<byte> Category => new byte[128]
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 2,
		2, 0, 2, 2, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 2, 0, 0, 3, 4, 0, 0, 0,
		4, 4, 5, 5, 0, 0, 4, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 5, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 4, 4, 0, 4, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 5, 4, 0, 0, 0
	};

	private RegexParser(string pattern, RegexOptions options, CultureInfo culture, Hashtable caps, int capsize, Hashtable capnames, Span<int> optionSpan)
	{
		_pattern = pattern;
		_options = options;
		_culture = culture;
		_caps = caps;
		_capsize = capsize;
		_capnames = capnames;
		_optionsStack = new System.Collections.Generic.ValueListBuilder<int>(optionSpan);
		_stack = null;
		_group = null;
		_alternation = null;
		_concatenation = null;
		_unit = null;
		_currentPos = 0;
		_autocap = 0;
		_capcount = 0;
		_captop = 0;
		_capnumlist = null;
		_capnamelist = null;
		_ignoreNextParen = false;
	}

	private RegexParser(string pattern, RegexOptions options, CultureInfo culture, Span<int> optionSpan)
		: this(pattern, options, culture, new Hashtable(), 0, null, optionSpan)
	{
	}

	public static RegexTree Parse(string pattern, RegexOptions options, CultureInfo culture)
	{
		Span<int> optionSpan = stackalloc int[32];
		RegexParser regexParser = new RegexParser(pattern, options, culture, optionSpan);
		regexParser.CountCaptures();
		regexParser.Reset(options);
		RegexNode regexNode = regexParser.ScanRegex();
		int minRequiredLength = regexNode.ComputeMinLength();
		string[] capsList = regexParser._capnamelist?.ToArray();
		RegexTree result = new RegexTree(regexNode, regexParser._caps, regexParser._capnumlist, regexParser._captop, regexParser._capnames, capsList, options, minRequiredLength);
		regexParser.Dispose();
		return result;
	}

	public static RegexReplacement ParseReplacement(string pattern, RegexOptions options, Hashtable caps, int capsize, Hashtable capnames)
	{
		CultureInfo cultureInfo = (((options & RegexOptions.CultureInvariant) != 0) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
		CultureInfo culture = cultureInfo;
		Span<int> optionSpan = stackalloc int[32];
		RegexParser regexParser = new RegexParser(pattern, options, culture, caps, capsize, capnames, optionSpan);
		RegexNode concat = regexParser.ScanReplacement();
		RegexReplacement result = new RegexReplacement(pattern, concat, caps);
		regexParser.Dispose();
		return result;
	}

	public static string Escape(string input)
	{
		for (int i = 0; i < input.Length; i++)
		{
			if (IsMetachar(input[i]))
			{
				return EscapeImpl(input, i);
			}
		}
		return input;
	}

	private static string EscapeImpl(string input, int i)
	{
		System.Text.ValueStringBuilder valueStringBuilder;
		if (input.Length <= 85)
		{
			Span<char> initialBuffer = stackalloc char[256];
			valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		}
		else
		{
			valueStringBuilder = new System.Text.ValueStringBuilder(input.Length + 200);
		}
		System.Text.ValueStringBuilder valueStringBuilder2 = valueStringBuilder;
		char c = input[i];
		valueStringBuilder2.Append(input.AsSpan(0, i));
		do
		{
			valueStringBuilder2.Append('\\');
			switch (c)
			{
			case '\n':
				c = 'n';
				break;
			case '\r':
				c = 'r';
				break;
			case '\t':
				c = 't';
				break;
			case '\f':
				c = 'f';
				break;
			}
			valueStringBuilder2.Append(c);
			i++;
			int num = i;
			while (i < input.Length)
			{
				c = input[i];
				if (IsMetachar(c))
				{
					break;
				}
				i++;
			}
			valueStringBuilder2.Append(input.AsSpan(num, i - num));
		}
		while (i < input.Length);
		return valueStringBuilder2.ToString();
	}

	public static string Unescape(string input)
	{
		int num = input.IndexOf('\\');
		if (num < 0)
		{
			return input;
		}
		return UnescapeImpl(input, num);
	}

	private static string UnescapeImpl(string input, int i)
	{
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		Span<int> optionSpan = stackalloc int[32];
		RegexParser regexParser = new RegexParser(input, RegexOptions.None, invariantCulture, optionSpan);
		System.Text.ValueStringBuilder valueStringBuilder;
		if (input.Length <= 256)
		{
			Span<char> initialBuffer = stackalloc char[256];
			valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		}
		else
		{
			valueStringBuilder = new System.Text.ValueStringBuilder(input.Length);
		}
		System.Text.ValueStringBuilder valueStringBuilder2 = valueStringBuilder;
		valueStringBuilder2.Append(input.AsSpan(0, i));
		do
		{
			i++;
			regexParser.Textto(i);
			if (i < input.Length)
			{
				valueStringBuilder2.Append(regexParser.ScanCharEscape());
			}
			i = regexParser.Textpos();
			int num = i;
			while (i < input.Length && input[i] != '\\')
			{
				i++;
			}
			valueStringBuilder2.Append(input.AsSpan(num, i - num));
		}
		while (i < input.Length);
		regexParser.Dispose();
		return valueStringBuilder2.ToString();
	}

	private void Reset(RegexOptions options)
	{
		_currentPos = 0;
		_autocap = 1;
		_ignoreNextParen = false;
		_optionsStack.Length = 0;
		_options = options;
		_stack = null;
	}

	public void Dispose()
	{
		_optionsStack.Dispose();
	}

	private RegexNode ScanRegex()
	{
		char c = '@';
		bool flag = false;
		StartGroup(new RegexNode(28, _options, 0, -1));
		while (CharsRight() > 0)
		{
			bool flag2 = flag;
			flag = false;
			ScanBlank();
			int num = Textpos();
			if (UseOptionX())
			{
				while (CharsRight() > 0 && (!IsStopperX(c = RightChar()) || (c == '{' && !IsTrueQuantifier())))
				{
					MoveRight();
				}
			}
			else
			{
				while (CharsRight() > 0 && (!IsSpecial(c = RightChar()) || (c == '{' && !IsTrueQuantifier())))
				{
					MoveRight();
				}
			}
			int num2 = Textpos();
			ScanBlank();
			if (CharsRight() == 0)
			{
				c = '!';
			}
			else if (IsSpecial(c = RightChar()))
			{
				flag = IsQuantifier(c);
				MoveRight();
			}
			else
			{
				c = ' ';
			}
			if (num < num2)
			{
				int num3 = num2 - num - (flag ? 1 : 0);
				flag2 = false;
				if (num3 > 0)
				{
					AddConcatenate(num, num3, isReplacement: false);
				}
				if (flag)
				{
					AddUnitOne(CharAt(num2 - 1));
				}
			}
			switch (c)
			{
			case '[':
				AddUnitSet(ScanCharClass(UseOptionI(), scanOnly: false).ToStringClass());
				goto IL_02cf;
			case '(':
			{
				PushOptions();
				RegexNode openGroup;
				if ((openGroup = ScanGroupOpen()) == null)
				{
					PopKeepOptions();
					continue;
				}
				PushGroup();
				StartGroup(openGroup);
				continue;
			}
			case '|':
				AddAlternate();
				continue;
			case ')':
				if (EmptyStack())
				{
					throw MakeException(RegexParseError.InsufficientOpeningParentheses, System.SR.InsufficientOpeningParentheses);
				}
				AddGroup();
				PopGroup();
				PopOptions();
				if (Unit() == null)
				{
					continue;
				}
				goto IL_02cf;
			case '\\':
				if (CharsRight() == 0)
				{
					throw MakeException(RegexParseError.UnescapedEndingBackslash, System.SR.UnescapedEndingBackslash);
				}
				AddUnitNode(ScanBackslash(scanOnly: false));
				goto IL_02cf;
			case '^':
				AddUnitType(UseOptionM() ? 14 : 18);
				goto IL_02cf;
			case '$':
				AddUnitType(UseOptionM() ? 15 : 20);
				goto IL_02cf;
			case '.':
				if (UseOptionS())
				{
					AddUnitSet("\0\u0001\0\0");
				}
				else
				{
					AddUnitNotone('\n');
				}
				goto IL_02cf;
			case '*':
			case '+':
			case '?':
			case '{':
				if (Unit() == null)
				{
					throw flag2 ? MakeException(RegexParseError.NestedQuantifiersNotParenthesized, System.SR.Format(System.SR.NestedQuantifiersNotParenthesized, c)) : MakeException(RegexParseError.QuantifierAfterNothing, System.SR.QuantifierAfterNothing);
				}
				MoveLeft();
				goto IL_02cf;
			default:
				throw new InvalidOperationException(System.SR.InternalError_ScanRegex);
			case ' ':
				continue;
			case '!':
				break;
				IL_02cf:
				ScanBlank();
				if (CharsRight() == 0 || !(flag = IsTrueQuantifier()))
				{
					AddConcatenate();
					continue;
				}
				c = RightCharMoveRight();
				while (Unit() != null)
				{
					int num4;
					int num5;
					if ((uint)c <= 43u)
					{
						if (c != '*')
						{
							if (c != '+')
							{
								goto IL_03cd;
							}
							num4 = 1;
							num5 = int.MaxValue;
						}
						else
						{
							num4 = 0;
							num5 = int.MaxValue;
						}
					}
					else if (c != '?')
					{
						if (c != '{')
						{
							goto IL_03cd;
						}
						num = Textpos();
						num5 = (num4 = ScanDecimal());
						if (num < Textpos() && CharsRight() > 0 && RightChar() == ',')
						{
							MoveRight();
							num5 = ((CharsRight() != 0 && RightChar() != '}') ? ScanDecimal() : int.MaxValue);
						}
						if (num == Textpos() || CharsRight() == 0 || RightCharMoveRight() != '}')
						{
							AddConcatenate();
							Textto(num - 1);
							break;
						}
					}
					else
					{
						num4 = 0;
						num5 = 1;
					}
					ScanBlank();
					bool lazy = false;
					if (CharsRight() != 0 && RightChar() == '?')
					{
						MoveRight();
						lazy = true;
					}
					if (num4 > num5)
					{
						throw MakeException(RegexParseError.ReversedQuantifierRange, System.SR.ReversedQuantifierRange);
					}
					AddConcatenate(lazy, num4, num5);
					continue;
					IL_03cd:
					throw new InvalidOperationException(System.SR.InternalError_ScanRegex);
				}
				continue;
			}
			break;
		}
		if (!EmptyStack())
		{
			throw MakeException(RegexParseError.InsufficientClosingParentheses, System.SR.InsufficientClosingParentheses);
		}
		AddGroup();
		return Unit().FinalOptimize();
	}

	private RegexNode ScanReplacement()
	{
		_concatenation = new RegexNode(25, _options);
		while (true)
		{
			int num = CharsRight();
			if (num == 0)
			{
				break;
			}
			int num2 = Textpos();
			while (num > 0 && RightChar() != '$')
			{
				MoveRight();
				num--;
			}
			AddConcatenate(num2, Textpos() - num2, isReplacement: true);
			if (num > 0)
			{
				if (RightCharMoveRight() == '$')
				{
					AddUnitNode(ScanDollar());
				}
				AddConcatenate();
			}
		}
		return _concatenation;
	}

	private RegexCharClass ScanCharClass(bool caseInsensitive, bool scanOnly)
	{
		char c = '\0';
		char c2 = '\0';
		bool flag = false;
		bool flag2 = true;
		bool flag3 = false;
		RegexCharClass regexCharClass = (scanOnly ? null : new RegexCharClass());
		if (CharsRight() > 0 && RightChar() == '^')
		{
			MoveRight();
			if (!scanOnly)
			{
				regexCharClass.Negate = true;
			}
			if ((_options & RegexOptions.ECMAScript) != 0 && CharAt(_currentPos) == ']')
			{
				flag2 = false;
			}
		}
		for (; CharsRight() > 0; flag2 = false)
		{
			bool flag4 = false;
			c = RightCharMoveRight();
			if (c == ']')
			{
				if (!flag2)
				{
					flag3 = true;
					break;
				}
			}
			else if (c == '\\' && CharsRight() > 0)
			{
				switch (c = RightCharMoveRight())
				{
				case 'D':
				case 'd':
					if (!scanOnly)
					{
						if (flag)
						{
							throw MakeException(RegexParseError.ShorthandClassInCharacterRange, System.SR.Format(System.SR.ShorthandClassInCharacterRange, c));
						}
						regexCharClass.AddDigit(UseOptionE(), c == 'D', _pattern, _currentPos);
					}
					continue;
				case 'S':
				case 's':
					if (!scanOnly)
					{
						if (flag)
						{
							throw MakeException(RegexParseError.ShorthandClassInCharacterRange, System.SR.Format(System.SR.ShorthandClassInCharacterRange, c));
						}
						regexCharClass.AddSpace(UseOptionE(), c == 'S');
					}
					continue;
				case 'W':
				case 'w':
					if (!scanOnly)
					{
						if (flag)
						{
							throw MakeException(RegexParseError.ShorthandClassInCharacterRange, System.SR.Format(System.SR.ShorthandClassInCharacterRange, c));
						}
						regexCharClass.AddWord(UseOptionE(), c == 'W');
					}
					continue;
				case 'P':
				case 'p':
					if (!scanOnly)
					{
						if (flag)
						{
							throw MakeException(RegexParseError.ShorthandClassInCharacterRange, System.SR.Format(System.SR.ShorthandClassInCharacterRange, c));
						}
						regexCharClass.AddCategoryFromName(ParseProperty(), c != 'p', caseInsensitive, _pattern, _currentPos);
					}
					else
					{
						ParseProperty();
					}
					continue;
				case '-':
					if (scanOnly)
					{
						continue;
					}
					if (flag)
					{
						if (c2 > c)
						{
							throw MakeException(RegexParseError.ReversedCharacterRange, System.SR.ReversedCharacterRange);
						}
						regexCharClass.AddRange(c2, c);
						flag = false;
						c2 = '\0';
					}
					else
					{
						regexCharClass.AddRange(c, c);
					}
					continue;
				}
				MoveLeft();
				c = ScanCharEscape();
				flag4 = true;
			}
			else if (c == '[' && CharsRight() > 0 && RightChar() == ':' && !flag)
			{
				int pos = Textpos();
				MoveRight();
				if (CharsRight() < 2 || RightCharMoveRight() != ':' || RightCharMoveRight() != ']')
				{
					Textto(pos);
				}
			}
			if (flag)
			{
				flag = false;
				if (scanOnly)
				{
					continue;
				}
				if (c == '[' && !flag4 && !flag2)
				{
					regexCharClass.AddChar(c2);
					regexCharClass.AddSubtraction(ScanCharClass(caseInsensitive, scanOnly));
					if (CharsRight() > 0 && RightChar() != ']')
					{
						throw MakeException(RegexParseError.ExclusionGroupNotLast, System.SR.ExclusionGroupNotLast);
					}
				}
				else
				{
					if (c2 > c)
					{
						throw MakeException(RegexParseError.ReversedCharacterRange, System.SR.ReversedCharacterRange);
					}
					regexCharClass.AddRange(c2, c);
				}
			}
			else if (CharsRight() >= 2 && RightChar() == '-' && RightChar(1) != ']')
			{
				c2 = c;
				flag = true;
				MoveRight();
			}
			else if (CharsRight() >= 1 && c == '-' && !flag4 && RightChar() == '[' && !flag2)
			{
				if (!scanOnly)
				{
					MoveRight(1);
					regexCharClass.AddSubtraction(ScanCharClass(caseInsensitive, scanOnly));
					if (CharsRight() > 0 && RightChar() != ']')
					{
						throw MakeException(RegexParseError.ExclusionGroupNotLast, System.SR.ExclusionGroupNotLast);
					}
				}
				else
				{
					MoveRight(1);
					ScanCharClass(caseInsensitive, scanOnly);
				}
			}
			else if (!scanOnly)
			{
				regexCharClass.AddRange(c, c);
			}
		}
		if (!flag3)
		{
			throw MakeException(RegexParseError.UnterminatedBracket, System.SR.UnterminatedBracket);
		}
		if (!scanOnly && caseInsensitive)
		{
			regexCharClass.AddLowercase(_culture);
		}
		return regexCharClass;
	}

	private RegexNode ScanGroupOpen()
	{
		if (CharsRight() == 0 || RightChar() != '?' || (RightChar() == '?' && CharsRight() > 1 && RightChar(1) == ')'))
		{
			if (UseOptionN() || _ignoreNextParen)
			{
				_ignoreNextParen = false;
				return new RegexNode(29, _options);
			}
			return new RegexNode(28, _options, _autocap++, -1);
		}
		MoveRight();
		if (CharsRight() != 0)
		{
			char c = '>';
			int type;
			switch (RightCharMoveRight())
			{
			case ':':
				type = 29;
				goto IL_0501;
			case '=':
				_options &= ~RegexOptions.RightToLeft;
				type = 30;
				goto IL_0501;
			case '!':
				_options &= ~RegexOptions.RightToLeft;
				type = 31;
				goto IL_0501;
			case '>':
				type = 32;
				goto IL_0501;
			case '\'':
				c = '\'';
				goto case '<';
			case '<':
			{
				if (CharsRight() == 0)
				{
					break;
				}
				char c2;
				char c3 = (c2 = RightCharMoveRight());
				if (c3 != '!')
				{
					if (c3 != '=')
					{
						MoveLeft();
						int num = -1;
						int num2 = -1;
						bool flag = false;
						if ((uint)(c2 - 48) <= 9u)
						{
							num = ScanDecimal();
							if (!IsCaptureSlot(num))
							{
								num = -1;
							}
							if (CharsRight() > 0 && RightChar() != c && RightChar() != '-')
							{
								throw MakeException(RegexParseError.CaptureGroupNameInvalid, System.SR.CaptureGroupNameInvalid);
							}
							if (num == 0)
							{
								throw MakeException(RegexParseError.CaptureGroupOfZero, System.SR.CaptureGroupOfZero);
							}
						}
						else if (RegexCharClass.IsWordChar(c2))
						{
							string capname = ScanCapname();
							if (IsCaptureName(capname))
							{
								num = CaptureSlotFromName(capname);
							}
							if (CharsRight() > 0 && RightChar() != c && RightChar() != '-')
							{
								throw MakeException(RegexParseError.CaptureGroupNameInvalid, System.SR.CaptureGroupNameInvalid);
							}
						}
						else
						{
							if (c2 != '-')
							{
								throw MakeException(RegexParseError.CaptureGroupNameInvalid, System.SR.CaptureGroupNameInvalid);
							}
							flag = true;
						}
						if ((num != -1 || flag) && CharsRight() > 1 && RightChar() == '-')
						{
							MoveRight();
							c2 = RightChar();
							if ((uint)(c2 - 48) <= 9u)
							{
								num2 = ScanDecimal();
								if (!IsCaptureSlot(num2))
								{
									throw MakeException(RegexParseError.UndefinedNumberedReference, System.SR.Format(System.SR.UndefinedNumberedReference, num2));
								}
								if (CharsRight() > 0 && RightChar() != c)
								{
									throw MakeException(RegexParseError.CaptureGroupNameInvalid, System.SR.CaptureGroupNameInvalid);
								}
							}
							else
							{
								if (!RegexCharClass.IsWordChar(c2))
								{
									throw MakeException(RegexParseError.CaptureGroupNameInvalid, System.SR.CaptureGroupNameInvalid);
								}
								string text = ScanCapname();
								if (!IsCaptureName(text))
								{
									throw MakeException(RegexParseError.UndefinedNamedReference, System.SR.Format(System.SR.UndefinedNamedReference, text));
								}
								num2 = CaptureSlotFromName(text);
								if (CharsRight() > 0 && RightChar() != c)
								{
									throw MakeException(RegexParseError.CaptureGroupNameInvalid, System.SR.CaptureGroupNameInvalid);
								}
							}
						}
						if ((num != -1 || num2 != -1) && CharsRight() > 0 && RightCharMoveRight() == c)
						{
							return new RegexNode(28, _options, num, num2);
						}
						break;
					}
					if (c == '\'')
					{
						break;
					}
					_options |= RegexOptions.RightToLeft;
					type = 30;
				}
				else
				{
					if (c == '\'')
					{
						break;
					}
					_options |= RegexOptions.RightToLeft;
					type = 31;
				}
				goto IL_0501;
			}
			case '(':
			{
				int num3 = Textpos();
				if (CharsRight() > 0)
				{
					char c2 = RightChar();
					if (c2 >= '0' && c2 <= '9')
					{
						int num4 = ScanDecimal();
						if (CharsRight() > 0 && RightCharMoveRight() == ')')
						{
							if (IsCaptureSlot(num4))
							{
								return new RegexNode(33, _options, num4);
							}
							throw MakeException(RegexParseError.AlternationHasUndefinedReference, System.SR.Format(System.SR.AlternationHasUndefinedReference, num4.ToString()));
						}
						throw MakeException(RegexParseError.AlternationHasMalformedReference, System.SR.Format(System.SR.AlternationHasMalformedReference, num4.ToString()));
					}
					if (RegexCharClass.IsWordChar(c2))
					{
						string capname2 = ScanCapname();
						if (IsCaptureName(capname2) && CharsRight() > 0 && RightCharMoveRight() == ')')
						{
							return new RegexNode(33, _options, CaptureSlotFromName(capname2));
						}
					}
				}
				type = 34;
				Textto(num3 - 1);
				_ignoreNextParen = true;
				int num5 = CharsRight();
				if (num5 >= 3 && RightChar(1) == '?')
				{
					char c4 = RightChar(2);
					switch (c4)
					{
					case '#':
						throw MakeException(RegexParseError.AlternationHasComment, System.SR.AlternationHasComment);
					case '\'':
						throw MakeException(RegexParseError.AlternationHasNamedCapture, System.SR.AlternationHasNamedCapture);
					}
					if (num5 >= 4 && c4 == '<' && RightChar(3) != '!' && RightChar(3) != '=')
					{
						throw MakeException(RegexParseError.AlternationHasNamedCapture, System.SR.AlternationHasNamedCapture);
					}
				}
				goto IL_0501;
			}
			default:
				{
					MoveLeft();
					type = 29;
					if (_group.Type != 34)
					{
						ScanOptions();
					}
					if (CharsRight() == 0)
					{
						break;
					}
					char c2;
					if ((c2 = RightCharMoveRight()) == ')')
					{
						return null;
					}
					if (c2 != ':')
					{
						break;
					}
					goto IL_0501;
				}
				IL_0501:
				return new RegexNode(type, _options);
			}
		}
		throw MakeException(RegexParseError.InvalidGroupingConstruct, System.SR.InvalidGroupingConstruct);
	}

	private void ScanBlank()
	{
		if (UseOptionX())
		{
			while (true)
			{
				if (CharsRight() > 0 && IsSpace(RightChar()))
				{
					MoveRight();
					continue;
				}
				if (CharsRight() == 0)
				{
					break;
				}
				if (RightChar() == '#')
				{
					while (CharsRight() > 0 && RightChar() != '\n')
					{
						MoveRight();
					}
					continue;
				}
				if (CharsRight() >= 3 && RightChar(2) == '#' && RightChar(1) == '?' && RightChar() == '(')
				{
					while (CharsRight() > 0 && RightChar() != ')')
					{
						MoveRight();
					}
					if (CharsRight() == 0)
					{
						throw MakeException(RegexParseError.UnterminatedComment, System.SR.UnterminatedComment);
					}
					MoveRight();
					continue;
				}
				break;
			}
			return;
		}
		while (true)
		{
			if (CharsRight() < 3 || RightChar(2) != '#' || RightChar(1) != '?' || RightChar() != '(')
			{
				return;
			}
			while (CharsRight() > 0 && RightChar() != ')')
			{
				MoveRight();
			}
			if (CharsRight() == 0)
			{
				break;
			}
			MoveRight();
		}
		throw MakeException(RegexParseError.UnterminatedComment, System.SR.UnterminatedComment);
	}

	private RegexNode ScanBackslash(bool scanOnly)
	{
		char c;
		switch (c = RightChar())
		{
		case 'A':
		case 'B':
		case 'G':
		case 'Z':
		case 'b':
		case 'z':
			MoveRight();
			if (!scanOnly)
			{
				return new RegexNode(TypeFromCode(c), _options);
			}
			return null;
		case 'w':
			MoveRight();
			if (!scanOnly)
			{
				return new RegexNode(11, _options, UseOptionE() ? "\0\n\00:A[_`a{İı" : "\0\0\n\0\u0002\u0004\u0005\u0003\u0001\u0006\t\u0013\0");
			}
			return null;
		case 'W':
			MoveRight();
			if (!scanOnly)
			{
				return new RegexNode(11, _options, UseOptionE() ? "\u0001\n\00:A[_`a{İı" : "\u0001\0\n\0\u0002\u0004\u0005\u0003\u0001\u0006\t\u0013\0");
			}
			return null;
		case 's':
			MoveRight();
			if (!scanOnly)
			{
				return new RegexNode(11, _options, UseOptionE() ? "\0\u0004\0\t\u000e !" : "\0\0\u0001d");
			}
			return null;
		case 'S':
			MoveRight();
			if (!scanOnly)
			{
				return new RegexNode(11, _options, UseOptionE() ? "\u0001\u0004\0\t\u000e !" : "\u0001\0\u0001d");
			}
			return null;
		case 'd':
			MoveRight();
			if (!scanOnly)
			{
				return new RegexNode(11, _options, UseOptionE() ? "\0\u0002\00:" : "\0\0\u0001\t");
			}
			return null;
		case 'D':
			MoveRight();
			if (!scanOnly)
			{
				return new RegexNode(11, _options, UseOptionE() ? "\u0001\u0002\00:" : "\0\0\u0001\ufff7");
			}
			return null;
		case 'P':
		case 'p':
		{
			MoveRight();
			if (scanOnly)
			{
				return null;
			}
			RegexCharClass regexCharClass = new RegexCharClass();
			regexCharClass.AddCategoryFromName(ParseProperty(), c != 'p', UseOptionI(), _pattern, _currentPos);
			if (UseOptionI())
			{
				regexCharClass.AddLowercase(_culture);
			}
			return new RegexNode(11, _options, regexCharClass.ToStringClass());
		}
		default:
			return ScanBasicBackslash(scanOnly);
		}
	}

	private RegexNode ScanBasicBackslash(bool scanOnly)
	{
		if (CharsRight() == 0)
		{
			throw MakeException(RegexParseError.UnescapedEndingBackslash, System.SR.UnescapedEndingBackslash);
		}
		int pos = Textpos();
		char c = '\0';
		bool flag = false;
		char c2 = RightChar();
		switch (c2)
		{
		case 'k':
			if (CharsRight() >= 2)
			{
				MoveRight();
				c2 = RightCharMoveRight();
				if (c2 == '<' || c2 == '\'')
				{
					flag = true;
					c = ((c2 == '\'') ? '\'' : '>');
				}
			}
			if (!flag || CharsRight() <= 0)
			{
				throw MakeException(RegexParseError.MalformedNamedReference, System.SR.MalformedNamedReference);
			}
			c2 = RightChar();
			break;
		case '\'':
		case '<':
			if (CharsRight() > 1)
			{
				flag = true;
				c = ((c2 == '\'') ? '\'' : '>');
				MoveRight();
				c2 = RightChar();
			}
			break;
		}
		if (flag && c2 >= '0' && c2 <= '9')
		{
			int num = ScanDecimal();
			if (CharsRight() > 0 && RightCharMoveRight() == c)
			{
				if (!scanOnly)
				{
					if (!IsCaptureSlot(num))
					{
						throw MakeException(RegexParseError.UndefinedNumberedReference, System.SR.Format(System.SR.UndefinedNumberedReference, num.ToString()));
					}
					return new RegexNode(13, _options, num);
				}
				return null;
			}
		}
		else if (!flag && c2 >= '1' && c2 <= '9')
		{
			if (UseOptionE())
			{
				int num2 = -1;
				int num3 = c2 - 48;
				int num4 = Textpos() - 1;
				while (num3 <= _captop)
				{
					if (IsCaptureSlot(num3) && (_caps == null || (int)_caps[num3] < num4))
					{
						num2 = num3;
					}
					MoveRight();
					if (CharsRight() == 0 || (c2 = RightChar()) < '0' || c2 > '9')
					{
						break;
					}
					num3 = num3 * 10 + (c2 - 48);
				}
				if (num2 >= 0)
				{
					if (!scanOnly)
					{
						return new RegexNode(13, _options, num2);
					}
					return null;
				}
			}
			else
			{
				int num5 = ScanDecimal();
				if (scanOnly)
				{
					return null;
				}
				if (IsCaptureSlot(num5))
				{
					return new RegexNode(13, _options, num5);
				}
				if (num5 <= 9)
				{
					throw MakeException(RegexParseError.UndefinedNumberedReference, System.SR.Format(System.SR.UndefinedNumberedReference, num5.ToString()));
				}
			}
		}
		else if (flag && RegexCharClass.IsWordChar(c2))
		{
			string text = ScanCapname();
			if (CharsRight() > 0 && RightCharMoveRight() == c)
			{
				if (!scanOnly)
				{
					if (!IsCaptureName(text))
					{
						throw MakeException(RegexParseError.UndefinedNamedReference, System.SR.Format(System.SR.UndefinedNamedReference, text));
					}
					return new RegexNode(13, _options, CaptureSlotFromName(text));
				}
				return null;
			}
		}
		Textto(pos);
		c2 = ScanCharEscape();
		if (UseOptionI())
		{
			c2 = _culture.TextInfo.ToLower(c2);
		}
		if (!scanOnly)
		{
			return new RegexNode(9, _options, c2);
		}
		return null;
	}

	private RegexNode ScanDollar()
	{
		if (CharsRight() == 0)
		{
			return new RegexNode(9, _options, '$');
		}
		char c = RightChar();
		int num = Textpos();
		int pos = num;
		bool flag;
		if (c == '{' && CharsRight() > 1)
		{
			flag = true;
			MoveRight();
			c = RightChar();
		}
		else
		{
			flag = false;
		}
		if (c >= '0' && c <= '9')
		{
			if (!flag && UseOptionE())
			{
				int num2 = -1;
				int num3 = c - 48;
				MoveRight();
				if (IsCaptureSlot(num3))
				{
					num2 = num3;
					pos = Textpos();
				}
				while (CharsRight() > 0 && (c = RightChar()) >= '0' && c <= '9')
				{
					int num4 = c - 48;
					if (num3 > 214748364 || (num3 == 214748364 && num4 > 7))
					{
						throw MakeException(RegexParseError.QuantifierOrCaptureGroupOutOfRange, System.SR.QuantifierOrCaptureGroupOutOfRange);
					}
					num3 = num3 * 10 + num4;
					MoveRight();
					if (IsCaptureSlot(num3))
					{
						num2 = num3;
						pos = Textpos();
					}
				}
				Textto(pos);
				if (num2 >= 0)
				{
					return new RegexNode(13, _options, num2);
				}
			}
			else
			{
				int num5 = ScanDecimal();
				if ((!flag || (CharsRight() > 0 && RightCharMoveRight() == '}')) && IsCaptureSlot(num5))
				{
					return new RegexNode(13, _options, num5);
				}
			}
		}
		else if (flag && RegexCharClass.IsWordChar(c))
		{
			string capname = ScanCapname();
			if (CharsRight() > 0 && RightCharMoveRight() == '}' && IsCaptureName(capname))
			{
				return new RegexNode(13, _options, CaptureSlotFromName(capname));
			}
		}
		else if (!flag)
		{
			int num6 = 1;
			switch (c)
			{
			case '$':
				MoveRight();
				return new RegexNode(9, _options, '$');
			case '&':
				num6 = 0;
				break;
			case '`':
				num6 = -1;
				break;
			case '\'':
				num6 = -2;
				break;
			case '+':
				num6 = -3;
				break;
			case '_':
				num6 = -4;
				break;
			}
			if (num6 != 1)
			{
				MoveRight();
				return new RegexNode(13, _options, num6);
			}
		}
		Textto(num);
		return new RegexNode(9, _options, '$');
	}

	private string ScanCapname()
	{
		int num = Textpos();
		while (CharsRight() > 0)
		{
			if (!RegexCharClass.IsWordChar(RightCharMoveRight()))
			{
				MoveLeft();
				break;
			}
		}
		return _pattern.Substring(num, Textpos() - num);
	}

	private char ScanOctal()
	{
		int num = 3;
		if (num > CharsRight())
		{
			num = CharsRight();
		}
		int num2 = 0;
		int num3;
		while (num > 0 && (uint)(num3 = RightChar() - 48) <= 7u)
		{
			MoveRight();
			num2 = num2 * 8 + num3;
			if (UseOptionE() && num2 >= 32)
			{
				break;
			}
			num--;
		}
		num2 &= 0xFF;
		return (char)num2;
	}

	private int ScanDecimal()
	{
		int num = 0;
		int num2;
		while (CharsRight() > 0 && (uint)(num2 = (ushort)(RightChar() - 48)) <= 9u)
		{
			MoveRight();
			if (num > 214748364 || (num == 214748364 && num2 > 7))
			{
				throw MakeException(RegexParseError.QuantifierOrCaptureGroupOutOfRange, System.SR.QuantifierOrCaptureGroupOutOfRange);
			}
			num = num * 10 + num2;
		}
		return num;
	}

	private char ScanHex(int c)
	{
		int num = 0;
		if (CharsRight() >= c)
		{
			int num2;
			while (c > 0 && (num2 = HexDigit(RightCharMoveRight())) >= 0)
			{
				num = num * 16 + num2;
				c--;
			}
		}
		if (c > 0)
		{
			throw MakeException(RegexParseError.InsufficientOrInvalidHexDigits, System.SR.InsufficientOrInvalidHexDigits);
		}
		return (char)num;
	}

	private static int HexDigit(char ch)
	{
		int result;
		if ((uint)(result = ch - 48) <= 9u)
		{
			return result;
		}
		if ((uint)(result = ch - 97) <= 5u)
		{
			return result + 10;
		}
		if ((uint)(result = ch - 65) <= 5u)
		{
			return result + 10;
		}
		return -1;
	}

	private char ScanControl()
	{
		if (CharsRight() == 0)
		{
			throw MakeException(RegexParseError.MissingControlCharacter, System.SR.MissingControlCharacter);
		}
		char c = RightCharMoveRight();
		if ((uint)(c - 97) <= 25u)
		{
			c = (char)(c - 32);
		}
		if ((c = (char)(c - 64)) < ' ')
		{
			return c;
		}
		throw MakeException(RegexParseError.UnrecognizedControlCharacter, System.SR.UnrecognizedControlCharacter);
	}

	private bool IsOnlyTopOption(RegexOptions options)
	{
		if (options != RegexOptions.RightToLeft && options != RegexOptions.CultureInvariant)
		{
			return options == RegexOptions.ECMAScript;
		}
		return true;
	}

	private void ScanOptions()
	{
		bool flag = false;
		while (CharsRight() > 0)
		{
			char c = RightChar();
			switch (c)
			{
			case '-':
				flag = true;
				break;
			case '+':
				flag = false;
				break;
			default:
			{
				RegexOptions regexOptions = OptionFromCode(c);
				if (regexOptions == RegexOptions.None || IsOnlyTopOption(regexOptions))
				{
					return;
				}
				if (flag)
				{
					_options &= ~regexOptions;
				}
				else
				{
					_options |= regexOptions;
				}
				break;
			}
			}
			MoveRight();
		}
	}

	private char ScanCharEscape()
	{
		char c = RightCharMoveRight();
		if (c >= '0' && c <= '7')
		{
			MoveLeft();
			return ScanOctal();
		}
		switch (c)
		{
		case 'x':
			return ScanHex(2);
		case 'u':
			return ScanHex(4);
		case 'a':
			return '\a';
		case 'b':
			return '\b';
		case 'e':
			return '\u001b';
		case 'f':
			return '\f';
		case 'n':
			return '\n';
		case 'r':
			return '\r';
		case 't':
			return '\t';
		case 'v':
			return '\v';
		case 'c':
			return ScanControl();
		default:
			if (!UseOptionE() && RegexCharClass.IsWordChar(c))
			{
				throw MakeException(RegexParseError.UnrecognizedEscape, System.SR.Format(System.SR.UnrecognizedEscape, c));
			}
			return c;
		}
	}

	private string ParseProperty()
	{
		if (CharsRight() < 3)
		{
			throw MakeException(RegexParseError.InvalidUnicodePropertyEscape, System.SR.InvalidUnicodePropertyEscape);
		}
		char c = RightCharMoveRight();
		if (c != '{')
		{
			throw MakeException(RegexParseError.MalformedUnicodePropertyEscape, System.SR.MalformedUnicodePropertyEscape);
		}
		int num = Textpos();
		while (CharsRight() > 0)
		{
			c = RightCharMoveRight();
			if (!RegexCharClass.IsWordChar(c) && c != '-')
			{
				MoveLeft();
				break;
			}
		}
		string result = _pattern.Substring(num, Textpos() - num);
		if (CharsRight() == 0 || RightCharMoveRight() != '}')
		{
			throw MakeException(RegexParseError.InvalidUnicodePropertyEscape, System.SR.InvalidUnicodePropertyEscape);
		}
		return result;
	}

	private int TypeFromCode(char ch)
	{
		return ch switch
		{
			'b' => UseOptionE() ? 41 : 16, 
			'B' => UseOptionE() ? 42 : 17, 
			'A' => 18, 
			'G' => 19, 
			'Z' => 20, 
			'z' => 21, 
			_ => 22, 
		};
	}

	private static RegexOptions OptionFromCode(char ch)
	{
		if ((uint)(ch - 65) <= 25u)
		{
			ch = (char)(ch + 32);
		}
		return ch switch
		{
			'i' => RegexOptions.IgnoreCase, 
			'r' => RegexOptions.RightToLeft, 
			'm' => RegexOptions.Multiline, 
			'n' => RegexOptions.ExplicitCapture, 
			's' => RegexOptions.Singleline, 
			'x' => RegexOptions.IgnorePatternWhitespace, 
			'e' => RegexOptions.ECMAScript, 
			_ => RegexOptions.None, 
		};
	}

	private void CountCaptures()
	{
		NoteCaptureSlot(0, 0);
		_autocap = 1;
		while (CharsRight() > 0)
		{
			int pos = Textpos();
			switch (RightCharMoveRight())
			{
			case '\\':
				if (CharsRight() > 0)
				{
					ScanBackslash(scanOnly: true);
				}
				break;
			case '#':
				if (UseOptionX())
				{
					MoveLeft();
					ScanBlank();
				}
				break;
			case '[':
				ScanCharClass(caseInsensitive: false, scanOnly: true);
				break;
			case ')':
				if (!EmptyOptionsStack())
				{
					PopOptions();
				}
				break;
			case '(':
				if (CharsRight() >= 2 && RightChar(1) == '#' && RightChar() == '?')
				{
					MoveLeft();
					ScanBlank();
				}
				else
				{
					PushOptions();
					if (CharsRight() > 0 && RightChar() == '?')
					{
						MoveRight();
						if (CharsRight() > 1 && (RightChar() == '<' || RightChar() == '\''))
						{
							MoveRight();
							char c = RightChar();
							if (c != '0' && RegexCharClass.IsWordChar(c))
							{
								if ((uint)(c - 49) <= 8u)
								{
									NoteCaptureSlot(ScanDecimal(), pos);
								}
								else
								{
									NoteCaptureName(ScanCapname(), pos);
								}
							}
						}
						else
						{
							ScanOptions();
							if (CharsRight() > 0)
							{
								if (RightChar() == ')')
								{
									MoveRight();
									PopKeepOptions();
								}
								else if (RightChar() == '(')
								{
									_ignoreNextParen = true;
									break;
								}
							}
						}
					}
					else if (!UseOptionN() && !_ignoreNextParen)
					{
						NoteCaptureSlot(_autocap++, pos);
					}
				}
				_ignoreNextParen = false;
				break;
			}
		}
		AssignNameSlots();
	}

	private void NoteCaptureSlot(int i, int pos)
	{
		object key = i;
		if (!_caps.ContainsKey(key))
		{
			_caps.Add(key, pos);
			_capcount++;
			if (_captop <= i)
			{
				_captop = ((i == int.MaxValue) ? i : (i + 1));
			}
		}
	}

	private void NoteCaptureName(string name, int pos)
	{
		if (_capnames == null)
		{
			_capnames = new Hashtable();
			_capnamelist = new List<string>();
		}
		if (!_capnames.ContainsKey(name))
		{
			_capnames.Add(name, pos);
			_capnamelist.Add(name);
		}
	}

	private void AssignNameSlots()
	{
		if (_capnames != null)
		{
			for (int i = 0; i < _capnamelist.Count; i++)
			{
				while (IsCaptureSlot(_autocap))
				{
					_autocap++;
				}
				string key = _capnamelist[i];
				int pos = (int)_capnames[key];
				_capnames[key] = _autocap;
				NoteCaptureSlot(_autocap, pos);
				_autocap++;
			}
		}
		if (_capcount < _captop)
		{
			_capnumlist = new int[_capcount];
			int num = 0;
			IDictionaryEnumerator enumerator = _caps.GetEnumerator();
			while (enumerator.MoveNext())
			{
				_capnumlist[num++] = (int)enumerator.Key;
			}
			Array.Sort(_capnumlist);
		}
		if (_capnames == null && _capnumlist == null)
		{
			return;
		}
		int num2 = 0;
		List<string> list;
		int num3;
		if (_capnames == null)
		{
			list = null;
			_capnames = new Hashtable();
			_capnamelist = new List<string>();
			num3 = -1;
		}
		else
		{
			list = _capnamelist;
			_capnamelist = new List<string>();
			num3 = (int)_capnames[list[0]];
		}
		for (int j = 0; j < _capcount; j++)
		{
			int num4 = ((_capnumlist == null) ? j : _capnumlist[j]);
			if (num3 == num4)
			{
				_capnamelist.Add(list[num2++]);
				num3 = ((num2 == list.Count) ? (-1) : ((int)_capnames[list[num2]]));
			}
			else
			{
				string text = num4.ToString(_culture);
				_capnamelist.Add(text);
				_capnames[text] = num4;
			}
		}
	}

	private int CaptureSlotFromName(string capname)
	{
		return (int)_capnames[capname];
	}

	private bool IsCaptureSlot(int i)
	{
		if (_caps != null)
		{
			return _caps.ContainsKey(i);
		}
		if (i >= 0)
		{
			return i < _capsize;
		}
		return false;
	}

	private bool IsCaptureName(string capname)
	{
		if (_capnames != null)
		{
			return _capnames.ContainsKey(capname);
		}
		return false;
	}

	private bool UseOptionN()
	{
		return (_options & RegexOptions.ExplicitCapture) != 0;
	}

	private bool UseOptionI()
	{
		return (_options & RegexOptions.IgnoreCase) != 0;
	}

	private bool UseOptionM()
	{
		return (_options & RegexOptions.Multiline) != 0;
	}

	private bool UseOptionS()
	{
		return (_options & RegexOptions.Singleline) != 0;
	}

	private bool UseOptionX()
	{
		return (_options & RegexOptions.IgnorePatternWhitespace) != 0;
	}

	private bool UseOptionE()
	{
		return (_options & RegexOptions.ECMAScript) != 0;
	}

	private static bool IsSpecial(char ch)
	{
		if (ch <= '|')
		{
			return Category[ch] >= 4;
		}
		return false;
	}

	private static bool IsStopperX(char ch)
	{
		if (ch <= '|')
		{
			return Category[ch] >= 2;
		}
		return false;
	}

	private static bool IsQuantifier(char ch)
	{
		if (ch <= '{')
		{
			return Category[ch] >= 5;
		}
		return false;
	}

	private bool IsTrueQuantifier()
	{
		int num = Textpos();
		char c = CharAt(num);
		if (c != '{')
		{
			if (c <= '{')
			{
				return Category[c] >= 5;
			}
			return false;
		}
		int num2 = num;
		int num3 = CharsRight();
		while (--num3 > 0 && (uint)((c = CharAt(++num2)) - 48) <= 9u)
		{
		}
		if (num3 == 0 || num2 - num == 1)
		{
			return false;
		}
		switch (c)
		{
		case '}':
			return true;
		default:
			return false;
		case ',':
			break;
		}
		while (--num3 > 0 && (uint)((c = CharAt(++num2)) - 48) <= 9u)
		{
		}
		if (num3 > 0)
		{
			return c == '}';
		}
		return false;
	}

	private static bool IsSpace(char ch)
	{
		if (ch <= ' ')
		{
			return Category[ch] == 2;
		}
		return false;
	}

	private static bool IsMetachar(char ch)
	{
		if (ch <= '|')
		{
			return Category[ch] >= 1;
		}
		return false;
	}

	private void AddConcatenate(int pos, int cch, bool isReplacement)
	{
		if (cch == 0)
		{
			return;
		}
		RegexNode newChild;
		if (cch > 1)
		{
			string str = ((UseOptionI() && !isReplacement) ? string.Create(cch, (_pattern, _culture, pos, cch), delegate(Span<char> span, (string _pattern, CultureInfo _culture, int pos, int cch) state)
			{
				state._pattern.AsSpan(state.pos, state.cch).ToLower(span, state._culture);
			}) : _pattern.Substring(pos, cch));
			newChild = new RegexNode(12, _options, str);
		}
		else
		{
			char c = _pattern[pos];
			if (UseOptionI() && !isReplacement)
			{
				c = _culture.TextInfo.ToLower(c);
			}
			newChild = new RegexNode(9, _options, c);
		}
		_concatenation.AddChild(newChild);
	}

	private void PushGroup()
	{
		_group.Next = _stack;
		_alternation.Next = _group;
		_concatenation.Next = _alternation;
		_stack = _concatenation;
	}

	private void PopGroup()
	{
		_concatenation = _stack;
		_alternation = _concatenation.Next;
		_group = _alternation.Next;
		_stack = _group.Next;
		if (_group.Type == 34 && _group.ChildCount() == 0)
		{
			if (_unit == null)
			{
				throw MakeException(RegexParseError.AlternationHasMalformedCondition, System.SR.AlternationHasMalformedCondition);
			}
			_group.AddChild(_unit);
			_unit = null;
		}
	}

	private bool EmptyStack()
	{
		return _stack == null;
	}

	private void StartGroup(RegexNode openGroup)
	{
		_group = openGroup;
		_alternation = new RegexNode(24, _options);
		_concatenation = new RegexNode(25, _options);
	}

	private void AddAlternate()
	{
		if (_group.Type == 34 || _group.Type == 33)
		{
			_group.AddChild(_concatenation.ReverseLeft());
		}
		else
		{
			_alternation.AddChild(_concatenation.ReverseLeft());
		}
		_concatenation = new RegexNode(25, _options);
	}

	private void AddConcatenate()
	{
		_concatenation.AddChild(_unit);
		_unit = null;
	}

	private void AddConcatenate(bool lazy, int min, int max)
	{
		_concatenation.AddChild(_unit.MakeQuantifier(lazy, min, max));
		_unit = null;
	}

	private RegexNode Unit()
	{
		return _unit;
	}

	private void AddUnitOne(char ch)
	{
		if (UseOptionI())
		{
			ch = _culture.TextInfo.ToLower(ch);
		}
		_unit = new RegexNode(9, _options, ch);
	}

	private void AddUnitNotone(char ch)
	{
		if (UseOptionI())
		{
			ch = _culture.TextInfo.ToLower(ch);
		}
		_unit = new RegexNode(10, _options, ch);
	}

	private void AddUnitSet(string cc)
	{
		_unit = new RegexNode(11, _options, cc);
	}

	private void AddUnitNode(RegexNode node)
	{
		_unit = node;
	}

	private void AddUnitType(int type)
	{
		_unit = new RegexNode(type, _options);
	}

	private void AddGroup()
	{
		if (_group.Type == 34 || _group.Type == 33)
		{
			_group.AddChild(_concatenation.ReverseLeft());
			if ((_group.Type == 33 && _group.ChildCount() > 2) || _group.ChildCount() > 3)
			{
				throw MakeException(RegexParseError.AlternationHasTooManyConditions, System.SR.AlternationHasTooManyConditions);
			}
		}
		else
		{
			_alternation.AddChild(_concatenation.ReverseLeft());
			_group.AddChild(_alternation);
		}
		_unit = _group;
	}

	private void PushOptions()
	{
		_optionsStack.Append((int)_options);
	}

	private void PopOptions()
	{
		_options = (RegexOptions)_optionsStack.Pop();
	}

	private bool EmptyOptionsStack()
	{
		return _optionsStack.Length == 0;
	}

	private void PopKeepOptions()
	{
		_optionsStack.Length--;
	}

	private RegexParseException MakeException(RegexParseError error, string message)
	{
		return new RegexParseException(error, _currentPos, System.SR.Format(System.SR.MakeException, _pattern, _currentPos, message));
	}

	private int Textpos()
	{
		return _currentPos;
	}

	private void Textto(int pos)
	{
		_currentPos = pos;
	}

	private char RightCharMoveRight()
	{
		return _pattern[_currentPos++];
	}

	private void MoveRight()
	{
		_currentPos++;
	}

	private void MoveRight(int i)
	{
		_currentPos += i;
	}

	private void MoveLeft()
	{
		_currentPos--;
	}

	private char CharAt(int i)
	{
		return _pattern[i];
	}

	private char RightChar()
	{
		return _pattern[_currentPos];
	}

	private char RightChar(int i)
	{
		return _pattern[_currentPos + i];
	}

	private int CharsRight()
	{
		return _pattern.Length - _currentPos;
	}
}
