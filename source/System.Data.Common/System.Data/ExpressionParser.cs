using System.Data.Common;
using System.Globalization;

namespace System.Data;

internal sealed class ExpressionParser
{
	private readonly struct ReservedWords
	{
		internal readonly string _word;

		internal readonly Tokens _token;

		internal readonly int _op;

		internal ReservedWords(string word, Tokens token, int op)
		{
			_word = word;
			_token = token;
			_op = op;
		}
	}

	private static readonly ReservedWords[] s_reservedwords = new ReservedWords[12]
	{
		new ReservedWords("And", Tokens.BinaryOp, 26),
		new ReservedWords("Between", Tokens.BinaryOp, 6),
		new ReservedWords("Child", Tokens.Child, 0),
		new ReservedWords("False", Tokens.ZeroOp, 34),
		new ReservedWords("In", Tokens.BinaryOp, 5),
		new ReservedWords("Is", Tokens.BinaryOp, 13),
		new ReservedWords("Like", Tokens.BinaryOp, 14),
		new ReservedWords("Not", Tokens.UnaryOp, 3),
		new ReservedWords("Null", Tokens.ZeroOp, 32),
		new ReservedWords("Or", Tokens.BinaryOp, 27),
		new ReservedWords("Parent", Tokens.Parent, 0),
		new ReservedWords("True", Tokens.ZeroOp, 33)
	};

	private readonly char _escape = '\\';

	private readonly char _decimalSeparator = '.';

	private readonly char _listSeparator = ',';

	private readonly char _exponentL = 'e';

	private readonly char _exponentU = 'E';

	internal char[] _text;

	internal int _pos;

	internal int _start;

	internal Tokens _token;

	internal int _op;

	internal OperatorInfo[] _ops = new OperatorInfo[100];

	internal int _topOperator;

	internal int _topNode;

	private readonly DataTable _table;

	internal ExpressionNode[] _nodeStack = new ExpressionNode[100];

	internal int _prevOperand;

	internal ExpressionNode _expression;

	internal ExpressionParser(DataTable table)
	{
		_table = table;
	}

	internal void LoadExpression(string data)
	{
		int num;
		if (data == null)
		{
			num = 0;
			_text = new char[num + 1];
		}
		else
		{
			num = data.Length;
			_text = new char[num + 1];
			data.CopyTo(0, _text, 0, num);
		}
		_text[num] = '\0';
		if (_expression != null)
		{
			_expression = null;
		}
	}

	internal void StartScan()
	{
		_op = 0;
		_pos = 0;
		_start = 0;
		_topOperator = 0;
		_ops[_topOperator++] = new OperatorInfo(Nodes.Noop, 0, 0);
	}

	internal ExpressionNode Parse()
	{
		_expression = null;
		StartScan();
		int num = 0;
		while (_token != Tokens.EOS)
		{
			while (true)
			{
				Scan();
				switch (_token)
				{
				case Tokens.EOS:
					break;
				case Tokens.Name:
				case Tokens.Numeric:
				case Tokens.Decimal:
				case Tokens.Float:
				case Tokens.StringConst:
				case Tokens.Date:
				case Tokens.Parent:
				{
					ExpressionNode node = null;
					string text = null;
					if (_prevOperand != 0)
					{
						throw ExprException.MissingOperator(new string(_text, _start, _pos - _start));
					}
					if (_topOperator > 0)
					{
						OperatorInfo operatorInfo = _ops[_topOperator - 1];
						if (operatorInfo._type == Nodes.Binop && operatorInfo._op == 5 && _token != Tokens.Parent)
						{
							throw ExprException.InWithoutParentheses();
						}
					}
					_prevOperand = 1;
					switch (_token)
					{
					case Tokens.Parent:
					{
						string relationName;
						try
						{
							Scan();
							if (_token == Tokens.LeftParen)
							{
								ScanToken(Tokens.Name);
								relationName = NameNode.ParseName(_text, _start, _pos);
								ScanToken(Tokens.RightParen);
								ScanToken(Tokens.Dot);
							}
							else
							{
								relationName = null;
								CheckToken(Tokens.Dot);
							}
						}
						catch (Exception e) when (ADP.IsCatchableExceptionType(e))
						{
							throw ExprException.LookupArgument();
						}
						ScanToken(Tokens.Name);
						string columnName = NameNode.ParseName(_text, _start, _pos);
						OperatorInfo operatorInfo = _ops[_topOperator - 1];
						node = new LookupNode(_table, columnName, relationName);
						break;
					}
					case Tokens.Name:
					{
						OperatorInfo operatorInfo = _ops[_topOperator - 1];
						node = new NameNode(_table, _text, _start, _pos);
						break;
					}
					case Tokens.Numeric:
						text = new string(_text, _start, _pos - _start);
						node = new ConstNode(_table, ValueType.Numeric, text);
						break;
					case Tokens.Decimal:
						text = new string(_text, _start, _pos - _start);
						node = new ConstNode(_table, ValueType.Decimal, text);
						break;
					case Tokens.Float:
						text = new string(_text, _start, _pos - _start);
						node = new ConstNode(_table, ValueType.Float, text);
						break;
					case Tokens.StringConst:
						text = new string(_text, _start + 1, _pos - _start - 2);
						node = new ConstNode(_table, ValueType.Str, text);
						break;
					case Tokens.Date:
						text = new string(_text, _start + 1, _pos - _start - 2);
						node = new ConstNode(_table, ValueType.Date, text);
						break;
					}
					NodePush(node);
					continue;
				}
				case Tokens.LeftParen:
				{
					num++;
					ExpressionNode node;
					if (_prevOperand == 0)
					{
						OperatorInfo operatorInfo = _ops[_topOperator - 1];
						if (operatorInfo._type == Nodes.Binop && operatorInfo._op == 5)
						{
							node = new FunctionNode(_table, "In");
							NodePush(node);
							_ops[_topOperator++] = new OperatorInfo(Nodes.Call, 0, 2);
						}
						else
						{
							_ops[_topOperator++] = new OperatorInfo(Nodes.Paren, 0, 2);
						}
						continue;
					}
					BuildExpression(22);
					_prevOperand = 0;
					ExpressionNode expressionNode2 = NodePeek();
					if (expressionNode2 == null || expressionNode2.GetType() != typeof(NameNode))
					{
						throw ExprException.SyntaxError();
					}
					NameNode nameNode2 = (NameNode)NodePop();
					node = new FunctionNode(_table, nameNode2._name);
					Aggregate aggregate = (Aggregate)((FunctionNode)node).Aggregate;
					if (aggregate != Aggregate.None)
					{
						node = ParseAggregateArgument((FunctionId)aggregate);
						NodePush(node);
						_prevOperand = 2;
					}
					else
					{
						NodePush(node);
						_ops[_topOperator++] = new OperatorInfo(Nodes.Call, 0, 2);
					}
					continue;
				}
				case Tokens.RightParen:
				{
					if (_prevOperand != 0)
					{
						BuildExpression(3);
					}
					if (_topOperator <= 1)
					{
						throw ExprException.TooManyRightParentheses();
					}
					_topOperator--;
					OperatorInfo operatorInfo = _ops[_topOperator];
					if (_prevOperand == 0 && operatorInfo._type != Nodes.Call)
					{
						throw ExprException.MissingOperand(operatorInfo);
					}
					if (operatorInfo._type == Nodes.Call)
					{
						if (_prevOperand != 0)
						{
							ExpressionNode argument2 = NodePop();
							FunctionNode functionNode2 = (FunctionNode)NodePop();
							functionNode2.AddArgument(argument2);
							functionNode2.Check();
							NodePush(functionNode2);
						}
					}
					else
					{
						ExpressionNode node = NodePop();
						node = new UnaryNode(_table, 0, node);
						NodePush(node);
					}
					_prevOperand = 2;
					num--;
					continue;
				}
				case Tokens.ListSeparator:
				{
					if (_prevOperand == 0)
					{
						throw ExprException.MissingOperandBefore(",");
					}
					BuildExpression(3);
					OperatorInfo operatorInfo = _ops[_topOperator - 1];
					if (operatorInfo._type != Nodes.Call)
					{
						throw ExprException.SyntaxError();
					}
					ExpressionNode argument = NodePop();
					FunctionNode functionNode = (FunctionNode)NodePop();
					functionNode.AddArgument(argument);
					NodePush(functionNode);
					_prevOperand = 0;
					continue;
				}
				case Tokens.BinaryOp:
					if (_prevOperand == 0)
					{
						if (_op == 15)
						{
							_op = 2;
						}
						else
						{
							if (_op != 16)
							{
								throw ExprException.MissingOperandBefore(Operators.ToString(_op));
							}
							_op = 1;
						}
						goto case Tokens.UnaryOp;
					}
					_prevOperand = 0;
					BuildExpression(Operators.Priority(_op));
					_ops[_topOperator++] = new OperatorInfo(Nodes.Binop, _op, Operators.Priority(_op));
					continue;
				case Tokens.UnaryOp:
					_ops[_topOperator++] = new OperatorInfo(Nodes.Unop, _op, Operators.Priority(_op));
					continue;
				case Tokens.ZeroOp:
					if (_prevOperand != 0)
					{
						throw ExprException.MissingOperator(new string(_text, _start, _pos - _start));
					}
					_ops[_topOperator++] = new OperatorInfo(Nodes.Zop, _op, 24);
					_prevOperand = 2;
					continue;
				case Tokens.Dot:
				{
					ExpressionNode expressionNode = NodePeek();
					if (expressionNode != null && expressionNode.GetType() == typeof(NameNode))
					{
						Scan();
						if (_token == Tokens.Name)
						{
							NameNode nameNode = (NameNode)NodePop();
							string name = nameNode._name + "." + NameNode.ParseName(_text, _start, _pos);
							NodePush(new NameNode(_table, name));
							continue;
						}
					}
					goto default;
				}
				default:
					throw ExprException.UnknownToken(new string(_text, _start, _pos - _start), _start + 1);
				}
				break;
			}
			if (_prevOperand == 0)
			{
				if (_topNode != 0)
				{
					OperatorInfo operatorInfo = _ops[_topOperator - 1];
					throw ExprException.MissingOperand(operatorInfo);
				}
			}
			else
			{
				BuildExpression(3);
				if (_topOperator != 1)
				{
					throw ExprException.MissingRightParen();
				}
			}
		}
		_expression = _nodeStack[0];
		return _expression;
	}

	private ExpressionNode ParseAggregateArgument(FunctionId aggregate)
	{
		Scan();
		bool flag;
		string relationName;
		string columnName;
		try
		{
			if (_token != Tokens.Child)
			{
				if (_token != Tokens.Name)
				{
					throw ExprException.AggregateArgument();
				}
				columnName = NameNode.ParseName(_text, _start, _pos);
				ScanToken(Tokens.RightParen);
				return new AggregateNode(_table, aggregate, columnName);
			}
			flag = _token == Tokens.Child;
			_prevOperand = 1;
			Scan();
			if (_token == Tokens.LeftParen)
			{
				ScanToken(Tokens.Name);
				relationName = NameNode.ParseName(_text, _start, _pos);
				ScanToken(Tokens.RightParen);
				ScanToken(Tokens.Dot);
			}
			else
			{
				relationName = null;
				CheckToken(Tokens.Dot);
			}
			ScanToken(Tokens.Name);
			columnName = NameNode.ParseName(_text, _start, _pos);
			ScanToken(Tokens.RightParen);
		}
		catch (Exception e) when (ADP.IsCatchableExceptionType(e))
		{
			throw ExprException.AggregateArgument();
		}
		return new AggregateNode(_table, aggregate, columnName, !flag, relationName);
	}

	private ExpressionNode NodePop()
	{
		return _nodeStack[--_topNode];
	}

	private ExpressionNode NodePeek()
	{
		if (_topNode <= 0)
		{
			return null;
		}
		return _nodeStack[_topNode - 1];
	}

	private void NodePush(ExpressionNode node)
	{
		if (_topNode >= 98)
		{
			throw ExprException.ExpressionTooComplex();
		}
		_nodeStack[_topNode++] = node;
	}

	private void BuildExpression(int pri)
	{
		ExpressionNode expressionNode = null;
		while (true)
		{
			OperatorInfo operatorInfo = _ops[_topOperator - 1];
			if (operatorInfo._priority < pri)
			{
				break;
			}
			_topOperator--;
			switch (operatorInfo._type)
			{
			default:
				return;
			case Nodes.Binop:
			{
				ExpressionNode right = NodePop();
				ExpressionNode expressionNode2 = NodePop();
				switch (operatorInfo._op)
				{
				case 4:
				case 6:
				case 22:
				case 23:
				case 24:
				case 25:
					throw ExprException.UnsupportedOperator(operatorInfo._op);
				default:
					expressionNode = ((operatorInfo._op != 14) ? new BinaryNode(_table, operatorInfo._op, expressionNode2, right) : new LikeNode(_table, operatorInfo._op, expressionNode2, right));
					break;
				}
				break;
			}
			case Nodes.Unop:
			{
				ExpressionNode expressionNode2 = null;
				ExpressionNode right = NodePop();
				int op = operatorInfo._op;
				if (op != 1 && op != 3 && op == 25)
				{
					throw ExprException.UnsupportedOperator(operatorInfo._op);
				}
				expressionNode = new UnaryNode(_table, operatorInfo._op, right);
				break;
			}
			case Nodes.Zop:
				expressionNode = new ZeroOpNode(operatorInfo._op);
				break;
			case Nodes.UnopSpec:
			case Nodes.BinopSpec:
				return;
			}
			NodePush(expressionNode);
		}
	}

	internal void CheckToken(Tokens token)
	{
		if (_token != token)
		{
			throw ExprException.UnknownToken(token, _token, _pos);
		}
	}

	internal Tokens Scan()
	{
		char[] text = _text;
		_token = Tokens.None;
		while (true)
		{
			_start = _pos;
			_op = 0;
			char c = text[_pos++];
			switch (c)
			{
			case '\0':
				_token = Tokens.EOS;
				break;
			case '\t':
			case '\n':
			case '\r':
			case ' ':
				goto IL_0111;
			case '(':
				_token = Tokens.LeftParen;
				break;
			case ')':
				_token = Tokens.RightParen;
				break;
			case '#':
				ScanDate();
				CheckToken(Tokens.Date);
				break;
			case '\'':
				ScanString('\'');
				CheckToken(Tokens.StringConst);
				break;
			case '=':
				_token = Tokens.BinaryOp;
				_op = 7;
				break;
			case '>':
				_token = Tokens.BinaryOp;
				ScanWhite();
				if (text[_pos] == '=')
				{
					_pos++;
					_op = 10;
				}
				else
				{
					_op = 8;
				}
				break;
			case '<':
				_token = Tokens.BinaryOp;
				ScanWhite();
				if (text[_pos] == '=')
				{
					_pos++;
					_op = 11;
				}
				else if (text[_pos] == '>')
				{
					_pos++;
					_op = 12;
				}
				else
				{
					_op = 9;
				}
				break;
			case '+':
				_token = Tokens.BinaryOp;
				_op = 15;
				break;
			case '-':
				_token = Tokens.BinaryOp;
				_op = 16;
				break;
			case '*':
				_token = Tokens.BinaryOp;
				_op = 17;
				break;
			case '/':
				_token = Tokens.BinaryOp;
				_op = 18;
				break;
			case '%':
				_token = Tokens.BinaryOp;
				_op = 20;
				break;
			case '&':
				_token = Tokens.BinaryOp;
				_op = 22;
				break;
			case '|':
				_token = Tokens.BinaryOp;
				_op = 23;
				break;
			case '^':
				_token = Tokens.BinaryOp;
				_op = 24;
				break;
			case '~':
				_token = Tokens.BinaryOp;
				_op = 25;
				break;
			case '[':
				ScanName(']', _escape, "]\\");
				CheckToken(Tokens.Name);
				break;
			case '`':
				ScanName('`', '`', "`");
				CheckToken(Tokens.Name);
				break;
			default:
				if (c == _listSeparator)
				{
					_token = Tokens.ListSeparator;
					break;
				}
				if (c == '.')
				{
					if (_prevOperand == 0)
					{
						ScanNumeric();
					}
					else
					{
						_token = Tokens.Dot;
					}
					break;
				}
				if (c == '0' && (text[_pos] == 'x' || text[_pos] == 'X'))
				{
					_token = Tokens.BinaryConst;
					break;
				}
				if (IsDigit(c))
				{
					ScanNumeric();
					break;
				}
				ScanReserved();
				if (_token != 0)
				{
					break;
				}
				if (IsAlphaNumeric(c))
				{
					ScanName();
					if (_token != 0)
					{
						CheckToken(Tokens.Name);
						break;
					}
				}
				_token = Tokens.Unknown;
				throw ExprException.UnknownToken(new string(text, _start, _pos - _start), _start + 1);
			}
			break;
			IL_0111:
			ScanWhite();
		}
		return _token;
	}

	private void ScanNumeric()
	{
		char[] text = _text;
		bool flag = false;
		bool flag2 = false;
		while (IsDigit(text[_pos]))
		{
			_pos++;
		}
		if (text[_pos] == _decimalSeparator)
		{
			flag = true;
			_pos++;
		}
		while (IsDigit(text[_pos]))
		{
			_pos++;
		}
		if (text[_pos] == _exponentL || text[_pos] == _exponentU)
		{
			flag2 = true;
			_pos++;
			if (text[_pos] == '-' || text[_pos] == '+')
			{
				_pos++;
			}
			while (IsDigit(text[_pos]))
			{
				_pos++;
			}
		}
		if (flag2)
		{
			_token = Tokens.Float;
		}
		else if (flag)
		{
			_token = Tokens.Decimal;
		}
		else
		{
			_token = Tokens.Numeric;
		}
	}

	private void ScanName()
	{
		char[] text = _text;
		while (IsAlphaNumeric(text[_pos]))
		{
			_pos++;
		}
		_token = Tokens.Name;
	}

	private void ScanName(char chEnd, char esc, string charsToEscape)
	{
		char[] text = _text;
		do
		{
			if (text[_pos] == esc && _pos + 1 < text.Length && charsToEscape.Contains(text[_pos + 1]))
			{
				_pos++;
			}
			_pos++;
		}
		while (_pos < text.Length && text[_pos] != chEnd);
		if (_pos >= text.Length)
		{
			throw ExprException.InvalidNameBracketing(new string(text, _start, _pos - 1 - _start));
		}
		_pos++;
		_token = Tokens.Name;
	}

	private void ScanDate()
	{
		char[] text = _text;
		do
		{
			_pos++;
		}
		while (_pos < text.Length && text[_pos] != '#');
		if (_pos >= text.Length || text[_pos] != '#')
		{
			if (_pos >= text.Length)
			{
				throw ExprException.InvalidDate(new string(text, _start, _pos - 1 - _start));
			}
			throw ExprException.InvalidDate(new string(text, _start, _pos - _start));
		}
		_token = Tokens.Date;
		_pos++;
	}

	private void ScanReserved()
	{
		char[] text = _text;
		if (!IsAlpha(text[_pos]))
		{
			return;
		}
		ScanName();
		string @string = new string(text, _start, _pos - _start);
		CompareInfo compareInfo = CultureInfo.InvariantCulture.CompareInfo;
		int num = 0;
		int num2 = s_reservedwords.Length - 1;
		do
		{
			int num3 = (num + num2) / 2;
			int num4 = compareInfo.Compare(s_reservedwords[num3]._word, @string, CompareOptions.IgnoreCase);
			if (num4 == 0)
			{
				_token = s_reservedwords[num3]._token;
				_op = s_reservedwords[num3]._op;
				break;
			}
			if (num4 < 0)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		while (num <= num2);
	}

	private void ScanString(char escape)
	{
		char[] text = _text;
		while (_pos < text.Length)
		{
			char c = text[_pos++];
			if (c == escape && _pos < text.Length && text[_pos] == escape)
			{
				_pos++;
			}
			else if (c == escape)
			{
				break;
			}
		}
		if (_pos >= text.Length)
		{
			throw ExprException.InvalidString(new string(text, _start, _pos - 1 - _start));
		}
		_token = Tokens.StringConst;
	}

	internal void ScanToken(Tokens token)
	{
		Scan();
		CheckToken(token);
	}

	private void ScanWhite()
	{
		char[] text = _text;
		while (_pos < text.Length && IsWhiteSpace(text[_pos]))
		{
			_pos++;
		}
	}

	private bool IsWhiteSpace(char ch)
	{
		if (ch <= ' ')
		{
			return ch != '\0';
		}
		return false;
	}

	private bool IsAlphaNumeric(char ch)
	{
		switch (ch)
		{
		case '$':
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
		case 'A':
		case 'B':
		case 'C':
		case 'D':
		case 'E':
		case 'F':
		case 'G':
		case 'H':
		case 'I':
		case 'J':
		case 'K':
		case 'L':
		case 'M':
		case 'N':
		case 'O':
		case 'P':
		case 'Q':
		case 'R':
		case 'S':
		case 'T':
		case 'U':
		case 'V':
		case 'W':
		case 'X':
		case 'Y':
		case 'Z':
		case '_':
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
			return true;
		default:
			if (ch > '\u007f')
			{
				return true;
			}
			return false;
		}
	}

	private bool IsDigit(char ch)
	{
		switch (ch)
		{
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
			return true;
		default:
			return false;
		}
	}

	private bool IsAlpha(char ch)
	{
		switch (ch)
		{
		case 'A':
		case 'B':
		case 'C':
		case 'D':
		case 'E':
		case 'F':
		case 'G':
		case 'H':
		case 'I':
		case 'J':
		case 'K':
		case 'L':
		case 'M':
		case 'N':
		case 'O':
		case 'P':
		case 'Q':
		case 'R':
		case 'S':
		case 'T':
		case 'U':
		case 'V':
		case 'W':
		case 'X':
		case 'Y':
		case 'Z':
		case '_':
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
			return true;
		default:
			return false;
		}
	}
}
