using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions;

internal sealed class RegexInterpreter : RegexRunner
{
	private readonly RegexCode _code;

	private readonly TextInfo _textInfo;

	private int _operator;

	private int _codepos;

	private bool _rightToLeft;

	private bool _caseInsensitive;

	public RegexInterpreter(RegexCode code, CultureInfo culture)
	{
		_code = code;
		_textInfo = culture.TextInfo;
	}

	protected override void InitTrackCount()
	{
		runtrackcount = _code.TrackCount;
	}

	private void Advance(int i)
	{
		_codepos += i + 1;
		SetOperator(_code.Codes[_codepos]);
	}

	private void Goto(int newpos)
	{
		if (newpos < _codepos)
		{
			EnsureStorage();
		}
		_codepos = newpos;
		SetOperator(_code.Codes[newpos]);
	}

	private void Trackto(int newpos)
	{
		runtrackpos = runtrack.Length - newpos;
	}

	private int Trackpos()
	{
		return runtrack.Length - runtrackpos;
	}

	private void TrackPush()
	{
		runtrack[--runtrackpos] = _codepos;
	}

	private void TrackPush(int i1)
	{
		int[] array = runtrack;
		int num = runtrackpos;
		array[--num] = i1;
		array[--num] = _codepos;
		runtrackpos = num;
	}

	private void TrackPush(int i1, int i2)
	{
		int[] array = runtrack;
		int num = runtrackpos;
		array[--num] = i1;
		array[--num] = i2;
		array[--num] = _codepos;
		runtrackpos = num;
	}

	private void TrackPush(int i1, int i2, int i3)
	{
		int[] array = runtrack;
		int num = runtrackpos;
		array[--num] = i1;
		array[--num] = i2;
		array[--num] = i3;
		array[--num] = _codepos;
		runtrackpos = num;
	}

	private void TrackPush2(int i1)
	{
		int[] array = runtrack;
		int num = runtrackpos;
		array[--num] = i1;
		array[--num] = -_codepos;
		runtrackpos = num;
	}

	private void TrackPush2(int i1, int i2)
	{
		int[] array = runtrack;
		int num = runtrackpos;
		array[--num] = i1;
		array[--num] = i2;
		array[--num] = -_codepos;
		runtrackpos = num;
	}

	private void Backtrack()
	{
		int num = runtrack[runtrackpos];
		runtrackpos++;
		int num2 = 128;
		if (num < 0)
		{
			num = -num;
			num2 = 256;
		}
		SetOperator(_code.Codes[num] | num2);
		if (num < _codepos)
		{
			EnsureStorage();
		}
		_codepos = num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetOperator(int op)
	{
		_operator = op & -577;
		_caseInsensitive = (op & 0x200) != 0;
		_rightToLeft = (op & 0x40) != 0;
	}

	private void TrackPop()
	{
		runtrackpos++;
	}

	private void TrackPop(int framesize)
	{
		runtrackpos += framesize;
	}

	private int TrackPeek()
	{
		return runtrack[runtrackpos - 1];
	}

	private int TrackPeek(int i)
	{
		return runtrack[runtrackpos - i - 1];
	}

	private void StackPush(int i1)
	{
		runstack[--runstackpos] = i1;
	}

	private void StackPush(int i1, int i2)
	{
		int[] array = runstack;
		int num = runstackpos;
		array[--num] = i1;
		array[--num] = i2;
		runstackpos = num;
	}

	private void StackPop()
	{
		runstackpos++;
	}

	private void StackPop(int framesize)
	{
		runstackpos += framesize;
	}

	private int StackPeek()
	{
		return runstack[runstackpos - 1];
	}

	private int StackPeek(int i)
	{
		return runstack[runstackpos - i - 1];
	}

	private int Operand(int i)
	{
		return _code.Codes[_codepos + i + 1];
	}

	private int Leftchars()
	{
		return runtextpos - runtextbeg;
	}

	private int Rightchars()
	{
		return runtextend - runtextpos;
	}

	private int Bump()
	{
		if (!_rightToLeft)
		{
			return 1;
		}
		return -1;
	}

	private int Forwardchars()
	{
		if (!_rightToLeft)
		{
			return runtextend - runtextpos;
		}
		return runtextpos - runtextbeg;
	}

	private char Forwardcharnext()
	{
		char c = (_rightToLeft ? runtext[--runtextpos] : runtext[runtextpos++]);
		if (!_caseInsensitive)
		{
			return c;
		}
		return _textInfo.ToLower(c);
	}

	private bool MatchString(string str)
	{
		int num = str.Length;
		int num2;
		if (!_rightToLeft)
		{
			if (runtextend - runtextpos < num)
			{
				return false;
			}
			num2 = runtextpos + num;
		}
		else
		{
			if (runtextpos - runtextbeg < num)
			{
				return false;
			}
			num2 = runtextpos;
		}
		if (!_caseInsensitive)
		{
			while (num != 0)
			{
				if (str[--num] != runtext[--num2])
				{
					return false;
				}
			}
		}
		else
		{
			TextInfo textInfo = _textInfo;
			while (num != 0)
			{
				if (str[--num] != textInfo.ToLower(runtext[--num2]))
				{
					return false;
				}
			}
		}
		if (!_rightToLeft)
		{
			num2 += str.Length;
		}
		runtextpos = num2;
		return true;
	}

	private bool MatchRef(int index, int length)
	{
		int num;
		if (!_rightToLeft)
		{
			if (runtextend - runtextpos < length)
			{
				return false;
			}
			num = runtextpos + length;
		}
		else
		{
			if (runtextpos - runtextbeg < length)
			{
				return false;
			}
			num = runtextpos;
		}
		int num2 = index + length;
		int num3 = length;
		if (!_caseInsensitive)
		{
			while (num3-- != 0)
			{
				if (runtext[--num2] != runtext[--num])
				{
					return false;
				}
			}
		}
		else
		{
			TextInfo textInfo = _textInfo;
			while (num3-- != 0)
			{
				if (textInfo.ToLower(runtext[--num2]) != textInfo.ToLower(runtext[--num]))
				{
					return false;
				}
			}
		}
		if (!_rightToLeft)
		{
			num += length;
		}
		runtextpos = num;
		return true;
	}

	private void Backwardnext()
	{
		runtextpos += (_rightToLeft ? 1 : (-1));
	}

	protected override bool FindFirstChar()
	{
		if (!_code.RightToLeft)
		{
			if (runtextpos > runtextend - _code.Tree.MinRequiredLength)
			{
				runtextpos = runtextend;
				return false;
			}
		}
		else if (runtextpos - _code.Tree.MinRequiredLength < runtextbeg)
		{
			runtextpos = runtextbeg;
			return false;
		}
		if (((uint)_code.LeadingAnchor & 0x35u) != 0)
		{
			if (!_code.RightToLeft)
			{
				int leadingAnchor = _code.LeadingAnchor;
				if (leadingAnchor <= 4)
				{
					if (leadingAnchor != 1)
					{
						if (leadingAnchor == 4 && runtextpos > runtextstart)
						{
							goto IL_00da;
						}
					}
					else if (runtextpos > runtextbeg)
					{
						goto IL_00da;
					}
				}
				else
				{
					switch (leadingAnchor)
					{
					case 16:
						if (runtextpos < runtextend - 1)
						{
							runtextpos = runtextend - 1;
						}
						break;
					case 32:
						if (runtextpos < runtextend)
						{
							runtextpos = runtextend;
						}
						break;
					}
				}
			}
			else
			{
				int leadingAnchor2 = _code.LeadingAnchor;
				if (leadingAnchor2 <= 4)
				{
					if (leadingAnchor2 != 1)
					{
						if (leadingAnchor2 == 4 && runtextpos < runtextstart)
						{
							goto IL_01ac;
						}
					}
					else if (runtextpos > runtextbeg)
					{
						runtextpos = runtextbeg;
					}
				}
				else if (leadingAnchor2 != 16)
				{
					if (leadingAnchor2 == 32 && runtextpos < runtextend)
					{
						goto IL_01ac;
					}
				}
				else if (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && runtext[runtextpos] != '\n'))
				{
					goto IL_01ac;
				}
			}
			if (_code.BoyerMoorePrefix != null)
			{
				return _code.BoyerMoorePrefix.IsMatch(runtext, runtextpos, runtextbeg, runtextend);
			}
			return true;
		}
		if (_code.LeadingAnchor == 2 && !_code.RightToLeft && runtextpos > runtextbeg && runtext[runtextpos - 1] != '\n')
		{
			int num = runtext.IndexOf('\n', runtextpos);
			if (num == -1 || num + 1 > runtextend)
			{
				runtextpos = runtextend;
				return false;
			}
			runtextpos = num + 1;
		}
		if (_code.BoyerMoorePrefix != null)
		{
			runtextpos = _code.BoyerMoorePrefix.Scan(runtext, runtextpos, runtextbeg, runtextend);
			if (runtextpos == -1)
			{
				runtextpos = (_code.RightToLeft ? runtextbeg : runtextend);
				return false;
			}
			return true;
		}
		if (_code.LeadingCharClasses == null)
		{
			return true;
		}
		string item = _code.LeadingCharClasses[0].CharClass;
		if (RegexCharClass.IsSingleton(item))
		{
			char c = RegexCharClass.SingletonChar(item);
			if (!_code.RightToLeft)
			{
				ReadOnlySpan<char> span = runtext.AsSpan(runtextpos, runtextend - runtextpos);
				if (!_code.LeadingCharClasses[0].CaseInsensitive)
				{
					int num2 = span.IndexOf(c);
					if (num2 >= 0)
					{
						runtextpos += num2;
						return true;
					}
				}
				else
				{
					TextInfo textInfo = _textInfo;
					for (int i = 0; i < span.Length; i++)
					{
						if (c == textInfo.ToLower(span[i]))
						{
							runtextpos += i;
							return true;
						}
					}
				}
				runtextpos = runtextend;
			}
			else
			{
				if (!_code.LeadingCharClasses[0].CaseInsensitive)
				{
					for (int num3 = runtextpos - 1; num3 >= runtextbeg; num3--)
					{
						if (c == runtext[num3])
						{
							runtextpos = num3 + 1;
							return true;
						}
					}
				}
				else
				{
					TextInfo textInfo2 = _textInfo;
					for (int num4 = runtextpos - 1; num4 >= runtextbeg; num4--)
					{
						if (c == textInfo2.ToLower(runtext[num4]))
						{
							runtextpos = num4 + 1;
							return true;
						}
					}
				}
				runtextpos = runtextbeg;
			}
		}
		else if (!_code.RightToLeft)
		{
			ReadOnlySpan<char> readOnlySpan = runtext.AsSpan(runtextpos, runtextend - runtextpos);
			if (!_code.LeadingCharClasses[0].CaseInsensitive)
			{
				for (int j = 0; j < readOnlySpan.Length; j++)
				{
					if (RegexCharClass.CharInClass(readOnlySpan[j], item, ref _code.LeadingCharClassAsciiLookup))
					{
						runtextpos += j;
						return true;
					}
				}
			}
			else
			{
				TextInfo textInfo3 = _textInfo;
				for (int k = 0; k < readOnlySpan.Length; k++)
				{
					if (RegexCharClass.CharInClass(textInfo3.ToLower(readOnlySpan[k]), item, ref _code.LeadingCharClassAsciiLookup))
					{
						runtextpos += k;
						return true;
					}
				}
			}
			runtextpos = runtextend;
		}
		else
		{
			if (!_code.LeadingCharClasses[0].CaseInsensitive)
			{
				for (int num5 = runtextpos - 1; num5 >= runtextbeg; num5--)
				{
					if (RegexCharClass.CharInClass(runtext[num5], item, ref _code.LeadingCharClassAsciiLookup))
					{
						runtextpos = num5 + 1;
						return true;
					}
				}
			}
			else
			{
				TextInfo textInfo4 = _textInfo;
				for (int num6 = runtextpos - 1; num6 >= runtextbeg; num6--)
				{
					if (RegexCharClass.CharInClass(textInfo4.ToLower(runtext[num6]), item, ref _code.LeadingCharClassAsciiLookup))
					{
						runtextpos = num6 + 1;
						return true;
					}
				}
			}
			runtextpos = runtextbeg;
		}
		return false;
		IL_01ac:
		runtextpos = runtextbeg;
		return false;
		IL_00da:
		runtextpos = runtextend;
		return false;
	}

	protected override void Go()
	{
		SetOperator(_code.Codes[0]);
		_codepos = 0;
		int num = -1;
		while (true)
		{
			if (num >= 0)
			{
				Advance(num);
				num = -1;
			}
			CheckTimeout();
			switch (_operator)
			{
			case 40:
				return;
			case 38:
				Goto(Operand(0));
				continue;
			case 37:
				if (IsMatched(Operand(0)))
				{
					num = 1;
					continue;
				}
				break;
			case 23:
				TrackPush(runtextpos);
				num = 1;
				continue;
			case 151:
				TrackPop();
				runtextpos = TrackPeek();
				Goto(Operand(0));
				continue;
			case 31:
				StackPush(runtextpos);
				TrackPush();
				num = 0;
				continue;
			case 30:
				StackPush(-1);
				TrackPush();
				num = 0;
				continue;
			case 158:
			case 159:
				StackPop();
				break;
			case 33:
				StackPop();
				TrackPush(StackPeek());
				runtextpos = StackPeek();
				num = 0;
				continue;
			case 161:
				TrackPop();
				StackPush(TrackPeek());
				break;
			case 32:
				if (Operand(1) == -1 || IsMatched(Operand(1)))
				{
					StackPop();
					if (Operand(1) != -1)
					{
						TransferCapture(Operand(0), Operand(1), StackPeek(), runtextpos);
					}
					else
					{
						Capture(Operand(0), StackPeek(), runtextpos);
					}
					TrackPush(StackPeek());
					num = 2;
					continue;
				}
				break;
			case 160:
				TrackPop();
				StackPush(TrackPeek());
				Uncapture();
				if (Operand(0) != -1 && Operand(1) != -1)
				{
					Uncapture();
				}
				break;
			case 24:
				StackPop();
				if (runtextpos != StackPeek())
				{
					TrackPush(StackPeek(), runtextpos);
					StackPush(runtextpos);
					Goto(Operand(0));
				}
				else
				{
					TrackPush2(StackPeek());
					num = 1;
				}
				continue;
			case 152:
				TrackPop(2);
				StackPop();
				runtextpos = TrackPeek(1);
				TrackPush2(TrackPeek());
				num = 1;
				continue;
			case 280:
				TrackPop();
				StackPush(TrackPeek());
				break;
			case 25:
			{
				StackPop();
				int num26 = StackPeek();
				if (runtextpos != num26)
				{
					if (num26 != -1)
					{
						TrackPush(num26, runtextpos);
					}
					else
					{
						TrackPush(runtextpos, runtextpos);
					}
				}
				else
				{
					StackPush(num26);
					TrackPush2(StackPeek());
				}
				num = 1;
				continue;
			}
			case 153:
			{
				TrackPop(2);
				int i2 = TrackPeek(1);
				TrackPush2(TrackPeek());
				StackPush(i2);
				runtextpos = i2;
				Goto(Operand(0));
				continue;
			}
			case 281:
				StackPop();
				TrackPop();
				StackPush(TrackPeek());
				break;
			case 27:
				StackPush(runtextpos, Operand(0));
				TrackPush();
				num = 1;
				continue;
			case 26:
				StackPush(-1, Operand(0));
				TrackPush();
				num = 1;
				continue;
			case 154:
			case 155:
			case 162:
				StackPop(2);
				break;
			case 28:
			{
				StackPop(2);
				int num17 = StackPeek();
				int num18 = StackPeek(1);
				int num19 = runtextpos - num17;
				if (num18 >= Operand(1) || (num19 == 0 && num18 >= 0))
				{
					TrackPush2(num17, num18);
					num = 2;
				}
				else
				{
					TrackPush(num17);
					StackPush(runtextpos, num18 + 1);
					Goto(Operand(0));
				}
				continue;
			}
			case 156:
				TrackPop();
				StackPop(2);
				if (StackPeek(1) > 0)
				{
					runtextpos = StackPeek();
					TrackPush2(TrackPeek(), StackPeek(1) - 1);
					num = 2;
					continue;
				}
				StackPush(TrackPeek(), StackPeek(1) - 1);
				break;
			case 284:
				TrackPop(2);
				StackPush(TrackPeek(), TrackPeek(1));
				break;
			case 29:
			{
				StackPop(2);
				int i = StackPeek();
				int num8 = StackPeek(1);
				if (num8 < 0)
				{
					TrackPush2(i);
					StackPush(runtextpos, num8 + 1);
					Goto(Operand(0));
				}
				else
				{
					TrackPush(i, num8, runtextpos);
					num = 2;
				}
				continue;
			}
			case 157:
			{
				TrackPop(3);
				int num29 = TrackPeek();
				int num30 = TrackPeek(2);
				if (TrackPeek(1) < Operand(1) && num30 != num29)
				{
					runtextpos = num30;
					StackPush(num30, TrackPeek(1) + 1);
					TrackPush2(num29);
					Goto(Operand(0));
					continue;
				}
				StackPush(TrackPeek(), TrackPeek(1));
				break;
			}
			case 285:
				TrackPop();
				StackPop(2);
				StackPush(TrackPeek(), StackPeek(1) - 1);
				break;
			case 34:
				StackPush(Trackpos(), Crawlpos());
				TrackPush();
				num = 0;
				continue;
			case 35:
				StackPop(2);
				Trackto(StackPeek());
				while (Crawlpos() != StackPeek(1))
				{
					Uncapture();
				}
				break;
			case 36:
				StackPop(2);
				Trackto(StackPeek());
				TrackPush(StackPeek(1));
				num = 0;
				continue;
			case 164:
				TrackPop();
				while (Crawlpos() != TrackPeek())
				{
					Uncapture();
				}
				break;
			case 14:
				if (Leftchars() <= 0 || runtext[runtextpos - 1] == '\n')
				{
					num = 0;
					continue;
				}
				break;
			case 15:
				if (Rightchars() <= 0 || runtext[runtextpos] == '\n')
				{
					num = 0;
					continue;
				}
				break;
			case 16:
				if (IsBoundary(runtextpos, runtextbeg, runtextend))
				{
					num = 0;
					continue;
				}
				break;
			case 17:
				if (!IsBoundary(runtextpos, runtextbeg, runtextend))
				{
					num = 0;
					continue;
				}
				break;
			case 41:
				if (IsECMABoundary(runtextpos, runtextbeg, runtextend))
				{
					num = 0;
					continue;
				}
				break;
			case 42:
				if (!IsECMABoundary(runtextpos, runtextbeg, runtextend))
				{
					num = 0;
					continue;
				}
				break;
			case 18:
				if (Leftchars() <= 0)
				{
					num = 0;
					continue;
				}
				break;
			case 19:
				if (runtextpos == runtextstart)
				{
					num = 0;
					continue;
				}
				break;
			case 20:
				if (Rightchars() <= 1 && (Rightchars() != 1 || runtext[runtextpos] == '\n'))
				{
					num = 0;
					continue;
				}
				break;
			case 21:
				if (Rightchars() <= 0)
				{
					num = 0;
					continue;
				}
				break;
			case 9:
				if (Forwardchars() >= 1 && Forwardcharnext() == (ushort)Operand(0))
				{
					num = 1;
					continue;
				}
				break;
			case 10:
				if (Forwardchars() >= 1 && Forwardcharnext() != (ushort)Operand(0))
				{
					num = 1;
					continue;
				}
				break;
			case 11:
				if (Forwardchars() >= 1)
				{
					int num7 = Operand(0);
					if (RegexCharClass.CharInClass(Forwardcharnext(), _code.Strings[num7], ref _code.StringsAsciiLookup[num7]))
					{
						num = 1;
						continue;
					}
				}
				break;
			case 12:
				if (MatchString(_code.Strings[Operand(0)]))
				{
					num = 1;
					continue;
				}
				break;
			case 13:
			{
				int cap = Operand(0);
				if (IsMatched(cap))
				{
					if (!MatchRef(MatchIndex(cap), MatchLength(cap)))
					{
						break;
					}
				}
				else if ((runregex.roptions & RegexOptions.ECMAScript) == 0)
				{
					break;
				}
				num = 1;
				continue;
			}
			case 0:
			{
				int num28 = Operand(1);
				if (Forwardchars() < num28)
				{
					break;
				}
				char c4 = (char)Operand(0);
				while (num28-- > 0)
				{
					if (Forwardcharnext() != c4)
					{
						goto end_IL_0037;
					}
				}
				num = 2;
				continue;
			}
			case 1:
			{
				int num27 = Operand(1);
				if (Forwardchars() < num27)
				{
					break;
				}
				char c3 = (char)Operand(0);
				while (num27-- > 0)
				{
					if (Forwardcharnext() == c3)
					{
						goto end_IL_0037;
					}
				}
				num = 2;
				continue;
			}
			case 2:
			{
				int num24 = Operand(1);
				if (Forwardchars() < num24)
				{
					break;
				}
				int num25 = Operand(0);
				string set2 = _code.Strings[num25];
				ref int[] asciiResultCache2 = ref _code.StringsAsciiLookup[num25];
				while (num24-- > 0)
				{
					if ((uint)num24 % 2048u == 0)
					{
						CheckTimeout();
					}
					if (!RegexCharClass.CharInClass(Forwardcharnext(), set2, ref asciiResultCache2))
					{
						goto end_IL_0037;
					}
				}
				num = 2;
				continue;
			}
			case 3:
			case 43:
			{
				int num22 = Math.Min(Operand(1), Forwardchars());
				char c2 = (char)Operand(0);
				int num23;
				for (num23 = num22; num23 > 0; num23--)
				{
					if (Forwardcharnext() != c2)
					{
						Backwardnext();
						break;
					}
				}
				if (num22 > num23 && _operator == 3)
				{
					TrackPush(num22 - num23 - 1, runtextpos - Bump());
				}
				num = 2;
				continue;
			}
			case 4:
			case 44:
			{
				int num20 = Math.Min(Operand(1), Forwardchars());
				char c = (char)Operand(0);
				int num21;
				if (!_rightToLeft && !_caseInsensitive)
				{
					num21 = runtext.AsSpan(runtextpos, num20).IndexOf(c);
					if (num21 == -1)
					{
						runtextpos += num20;
						num21 = 0;
					}
					else
					{
						runtextpos += num21;
						num21 = num20 - num21;
					}
				}
				else
				{
					for (num21 = num20; num21 > 0; num21--)
					{
						if (Forwardcharnext() == c)
						{
							Backwardnext();
							break;
						}
					}
				}
				if (num20 > num21 && _operator == 4)
				{
					TrackPush(num20 - num21 - 1, runtextpos - Bump());
				}
				num = 2;
				continue;
			}
			case 5:
			case 45:
			{
				int num14 = Math.Min(Operand(1), Forwardchars());
				int num15 = Operand(0);
				string set = _code.Strings[num15];
				ref int[] asciiResultCache = ref _code.StringsAsciiLookup[num15];
				int num16;
				for (num16 = num14; num16 > 0; num16--)
				{
					if ((uint)num16 % 2048u == 0)
					{
						CheckTimeout();
					}
					if (!RegexCharClass.CharInClass(Forwardcharnext(), set, ref asciiResultCache))
					{
						Backwardnext();
						break;
					}
				}
				if (num14 > num16 && _operator == 5)
				{
					TrackPush(num14 - num16 - 1, runtextpos - Bump());
				}
				num = 2;
				continue;
			}
			case 131:
			case 132:
			case 133:
			{
				TrackPop(2);
				int num12 = TrackPeek();
				int num13 = (runtextpos = TrackPeek(1));
				if (num12 > 0)
				{
					TrackPush(num12 - 1, num13 - Bump());
				}
				num = 2;
				continue;
			}
			case 6:
			case 7:
			case 8:
			{
				int num11 = Math.Min(Operand(1), Forwardchars());
				if (num11 > 0)
				{
					TrackPush(num11 - 1, runtextpos);
				}
				num = 2;
				continue;
			}
			case 134:
			{
				TrackPop(2);
				int num9 = (runtextpos = TrackPeek(1));
				if (Forwardcharnext() == (ushort)Operand(0))
				{
					int num10 = TrackPeek();
					if (num10 > 0)
					{
						TrackPush(num10 - 1, num9 + Bump());
					}
					num = 2;
					continue;
				}
				break;
			}
			case 135:
			{
				TrackPop(2);
				int num5 = (runtextpos = TrackPeek(1));
				if (Forwardcharnext() != (ushort)Operand(0))
				{
					int num6 = TrackPeek();
					if (num6 > 0)
					{
						TrackPush(num6 - 1, num5 + Bump());
					}
					num = 2;
					continue;
				}
				break;
			}
			case 136:
			{
				TrackPop(2);
				int num2 = (runtextpos = TrackPeek(1));
				int num3 = Operand(0);
				if (RegexCharClass.CharInClass(Forwardcharnext(), _code.Strings[num3], ref _code.StringsAsciiLookup[num3]))
				{
					int num4 = TrackPeek();
					if (num4 > 0)
					{
						TrackPush(num4 - 1, num2 + Bump());
					}
					num = 2;
					continue;
				}
				break;
			}
			case 46:
				{
					runtrack[runtrack.Length - 1] = runtextpos;
					num = 0;
					continue;
				}
				end_IL_0037:
				break;
			}
			Backtrack();
		}
	}
}
