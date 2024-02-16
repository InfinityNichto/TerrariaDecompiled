namespace System.Text.RegularExpressions;

public class Match : Group
{
	internal GroupCollection _groupcoll;

	internal Regex _regex;

	internal int _textbeg;

	internal int _textpos;

	internal int _textend;

	internal int _textstart;

	internal int[][] _matches;

	internal int[] _matchcount;

	internal bool _balancing;

	public static Match Empty { get; } = new Match(null, 1, string.Empty, 0, 0, 0);


	public virtual GroupCollection Groups => _groupcoll ?? (_groupcoll = new GroupCollection(this, null));

	internal Match(Regex regex, int capcount, string text, int begpos, int len, int startpos)
		: base(text, new int[2], 0, "0")
	{
		_regex = regex;
		_matchcount = new int[capcount];
		_matches = new int[capcount][];
		_matches[0] = _caps;
		_textbeg = begpos;
		_textend = begpos + len;
		_textstart = startpos;
		_balancing = false;
	}

	internal void Reset(Regex regex, string text, int textbeg, int textend, int textstart)
	{
		_regex = regex;
		base.Text = text;
		_textbeg = textbeg;
		_textend = textend;
		_textstart = textstart;
		int[] matchcount = _matchcount;
		for (int i = 0; i < matchcount.Length; i++)
		{
			matchcount[i] = 0;
		}
		_balancing = false;
		_groupcoll?.Reset();
	}

	public Match NextMatch()
	{
		Regex regex = _regex;
		if (regex == null)
		{
			return this;
		}
		return regex.Run(quick: false, base.Length, base.Text, _textbeg, _textend - _textbeg, _textpos);
	}

	public virtual string Result(string replacement)
	{
		if (replacement == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.replacement);
		}
		Regex regex = _regex;
		if (regex == null)
		{
			throw new NotSupportedException(System.SR.NoResultOnFailed);
		}
		RegexReplacement orCreate = RegexReplacement.GetOrCreate(regex._replref, replacement, regex.caps, regex.capsize, regex.capnames, regex.roptions);
		SegmentStringBuilder segments = SegmentStringBuilder.Create();
		orCreate.ReplacementImpl(ref segments, this);
		return segments.ToString();
	}

	internal ReadOnlyMemory<char> GroupToStringImpl(int groupnum)
	{
		int num = _matchcount[groupnum];
		if (num == 0)
		{
			return default(ReadOnlyMemory<char>);
		}
		int[] array = _matches[groupnum];
		return base.Text.AsMemory(array[(num - 1) * 2], array[num * 2 - 1]);
	}

	internal ReadOnlyMemory<char> LastGroupToStringImpl()
	{
		return GroupToStringImpl(_matchcount.Length - 1);
	}

	public static Match Synchronized(Match inner)
	{
		if (inner == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.inner);
		}
		int num = inner._matchcount.Length;
		for (int i = 0; i < num; i++)
		{
			Group.Synchronized(inner.Groups[i]);
		}
		return inner;
	}

	internal void AddMatch(int cap, int start, int len)
	{
		int[][] matches = _matches;
		if (matches[cap] == null)
		{
			matches[cap] = new int[2];
		}
		int[][] matches2 = _matches;
		int[] matchcount = _matchcount;
		int num = matchcount[cap];
		if (num * 2 + 2 > matches2[cap].Length)
		{
			int[] array = matches2[cap];
			int[] array2 = new int[num * 8];
			for (int i = 0; i < num * 2; i++)
			{
				array2[i] = array[i];
			}
			matches2[cap] = array2;
		}
		matches2[cap][num * 2] = start;
		matches2[cap][num * 2 + 1] = len;
		matchcount[cap] = num + 1;
	}

	internal void BalanceMatch(int cap)
	{
		_balancing = true;
		int num = _matchcount[cap];
		int num2 = num * 2 - 2;
		int[][] matches = _matches;
		if (matches[cap][num2] < 0)
		{
			num2 = -3 - matches[cap][num2];
		}
		num2 -= 2;
		if (num2 >= 0 && matches[cap][num2] < 0)
		{
			AddMatch(cap, matches[cap][num2], matches[cap][num2 + 1]);
		}
		else
		{
			AddMatch(cap, -3 - num2, -4 - num2);
		}
	}

	internal void RemoveMatch(int cap)
	{
		_matchcount[cap]--;
	}

	internal bool IsMatched(int cap)
	{
		int[] matchcount = _matchcount;
		if ((uint)cap < (uint)matchcount.Length && matchcount[cap] > 0)
		{
			return _matches[cap][matchcount[cap] * 2 - 1] != -2;
		}
		return false;
	}

	internal int MatchIndex(int cap)
	{
		int[][] matches = _matches;
		int num = matches[cap][_matchcount[cap] * 2 - 2];
		if (num < 0)
		{
			return matches[cap][-3 - num];
		}
		return num;
	}

	internal int MatchLength(int cap)
	{
		int[][] matches = _matches;
		int num = matches[cap][_matchcount[cap] * 2 - 1];
		if (num < 0)
		{
			return matches[cap][-3 - num];
		}
		return num;
	}

	internal void Tidy(int textpos)
	{
		_textpos = textpos;
		_capcount = _matchcount[0];
		int[] array = _matches[0];
		base.Index = array[0];
		base.Length = array[1];
		if (_balancing)
		{
			TidyBalancing();
		}
	}

	private void TidyBalancing()
	{
		int[] matchcount = _matchcount;
		int[][] matches = _matches;
		for (int i = 0; i < matchcount.Length; i++)
		{
			int num = matchcount[i] * 2;
			int[] array = matches[i];
			int j;
			for (j = 0; j < num && array[j] >= 0; j++)
			{
			}
			int num2 = j;
			for (; j < num; j++)
			{
				if (array[j] < 0)
				{
					num2--;
					continue;
				}
				if (j != num2)
				{
					array[num2] = array[j];
				}
				num2++;
			}
			matchcount[i] = num2 / 2;
		}
		_balancing = false;
	}
}
