namespace System.Text.RegularExpressions;

public abstract class RegexRunner
{
	protected internal int runtextbeg;

	protected internal int runtextend;

	protected internal int runtextstart;

	protected internal string? runtext;

	protected internal int runtextpos;

	protected internal int[]? runtrack;

	protected internal int runtrackpos;

	protected internal int[]? runstack;

	protected internal int runstackpos;

	protected internal int[]? runcrawl;

	protected internal int runcrawlpos;

	protected internal int runtrackcount;

	protected internal Match? runmatch;

	protected internal Regex? runregex;

	private int _timeout;

	private bool _ignoreTimeout;

	private int _timeoutOccursAt;

	private const int TimeoutCheckFrequency = 1000;

	private int _timeoutChecksToSkip;

	protected internal RegexRunner()
	{
	}

	protected internal Match? Scan(Regex regex, string text, int textbeg, int textend, int textstart, int prevlen, bool quick)
	{
		return Scan(regex, text, textbeg, textend, textstart, prevlen, quick, regex.MatchTimeout);
	}

	protected internal Match? Scan(Regex regex, string text, int textbeg, int textend, int textstart, int prevlen, bool quick, TimeSpan timeout)
	{
		_timeout = -1;
		bool flag = (_ignoreTimeout = Regex.InfiniteMatchTimeout == timeout);
		if (!flag)
		{
			Regex.ValidateMatchTimeout(timeout);
			_timeout = (int)(timeout.TotalMilliseconds + 0.5);
			_timeoutOccursAt = Environment.TickCount + _timeout;
			_timeoutChecksToSkip = 1000;
		}
		int num = 1;
		int num2 = textend;
		if (regex.RightToLeft)
		{
			num = -1;
			num2 = textbeg;
		}
		runtextpos = textstart;
		if (prevlen == 0)
		{
			if (textstart == num2)
			{
				return Match.Empty;
			}
			runtextpos += num;
		}
		runregex = regex;
		runtext = text;
		runtextstart = textstart;
		runtextbeg = textbeg;
		runtextend = textend;
		bool flag2 = false;
		while (true)
		{
			if (FindFirstChar())
			{
				if (!flag)
				{
					DoCheckTimeout();
				}
				if (!flag2)
				{
					InitializeForGo();
					flag2 = true;
				}
				Go();
				Match match = runmatch;
				if (match._matchcount[0] > 0)
				{
					runtext = null;
					if (quick)
					{
						runmatch.Text = null;
						return null;
					}
					runmatch = null;
					match.Tidy(runtextpos);
					return match;
				}
				runtrackpos = runtrack.Length;
				runstackpos = runstack.Length;
				runcrawlpos = runcrawl.Length;
			}
			if (runtextpos == num2)
			{
				break;
			}
			runtextpos += num;
		}
		runtext = null;
		if (runmatch != null)
		{
			runmatch.Text = null;
		}
		return Match.Empty;
	}

	internal void Scan<TState>(Regex regex, string text, int textstart, ref TState state, MatchCallback<TState> callback, bool reuseMatchObject, TimeSpan timeout)
	{
		_timeout = -1;
		bool flag = (_ignoreTimeout = Regex.InfiniteMatchTimeout == timeout);
		if (!flag)
		{
			_timeout = (int)(timeout.TotalMilliseconds + 0.5);
			_timeoutOccursAt = Environment.TickCount + _timeout;
			_timeoutChecksToSkip = 1000;
		}
		int num = 1;
		int num2 = text.Length;
		if (regex.RightToLeft)
		{
			num = -1;
			num2 = 0;
		}
		runregex = regex;
		runtextstart = (runtextpos = textstart);
		runtext = text;
		runtextend = text.Length;
		runtextbeg = 0;
		bool flag2 = false;
		while (true)
		{
			if (FindFirstChar())
			{
				if (!flag)
				{
					DoCheckTimeout();
				}
				if (!flag2)
				{
					InitializeForGo();
					flag2 = true;
				}
				Go();
				Match match = runmatch;
				if (match._matchcount[0] > 0)
				{
					if (!reuseMatchObject)
					{
						runmatch = null;
					}
					match.Tidy(runtextpos);
					flag2 = false;
					if (!callback(ref state, match))
					{
						runtext = null;
						if (reuseMatchObject)
						{
							match.Text = null;
						}
						return;
					}
					runtextstart = runtextpos;
					runtrackpos = runtrack.Length;
					runstackpos = runstack.Length;
					runcrawlpos = runcrawl.Length;
					if (match.Length != 0)
					{
						continue;
					}
					if (runtextpos == num2)
					{
						runtext = null;
						if (reuseMatchObject)
						{
							match.Text = null;
						}
						return;
					}
					runtextpos += num;
					continue;
				}
				runtrackpos = runtrack.Length;
				runstackpos = runstack.Length;
				runcrawlpos = runcrawl.Length;
			}
			if (runtextpos == num2)
			{
				break;
			}
			runtextpos += num;
		}
		runtext = null;
		if (runmatch != null)
		{
			runmatch.Text = null;
		}
	}

	protected void CheckTimeout()
	{
		if (!_ignoreTimeout)
		{
			DoCheckTimeout();
		}
	}

	private void DoCheckTimeout()
	{
		if (--_timeoutChecksToSkip == 0)
		{
			_timeoutChecksToSkip = 1000;
			int tickCount = Environment.TickCount;
			if (tickCount >= _timeoutOccursAt && (0 <= _timeoutOccursAt || 0 >= tickCount))
			{
				throw new RegexMatchTimeoutException(runtext, runregex.pattern, TimeSpan.FromMilliseconds(_timeout));
			}
		}
	}

	protected abstract void Go();

	protected abstract bool FindFirstChar();

	protected abstract void InitTrackCount();

	private void InitializeForGo()
	{
		if (runmatch == null)
		{
			runmatch = ((runregex.caps == null) ? new Match(runregex, runregex.capsize, runtext, runtextbeg, runtextend - runtextbeg, runtextstart) : new MatchSparse(runregex, runregex.caps, runregex.capsize, runtext, runtextbeg, runtextend - runtextbeg, runtextstart));
		}
		else
		{
			runmatch.Reset(runregex, runtext, runtextbeg, runtextend, runtextstart);
		}
		if (runcrawl != null)
		{
			runtrackpos = runtrack.Length;
			runstackpos = runstack.Length;
			runcrawlpos = runcrawl.Length;
			return;
		}
		InitTrackCount();
		int num;
		int num2 = (num = runtrackcount * 8);
		if (num2 < 32)
		{
			num2 = 32;
		}
		if (num < 16)
		{
			num = 16;
		}
		runtrack = new int[num2];
		runtrackpos = num2;
		runstack = new int[num];
		runstackpos = num;
		runcrawl = new int[32];
		runcrawlpos = 32;
	}

	protected void EnsureStorage()
	{
		int num = runtrackcount * 4;
		if (runstackpos < num)
		{
			DoubleStack();
		}
		if (runtrackpos < num)
		{
			DoubleTrack();
		}
	}

	protected bool IsBoundary(int index, int startpos, int endpos)
	{
		return (index > startpos && RegexCharClass.IsWordChar(runtext[index - 1])) != (index < endpos && RegexCharClass.IsWordChar(runtext[index]));
	}

	protected bool IsECMABoundary(int index, int startpos, int endpos)
	{
		return (index > startpos && RegexCharClass.IsECMAWordChar(runtext[index - 1])) != (index < endpos && RegexCharClass.IsECMAWordChar(runtext[index]));
	}

	protected static bool CharInSet(char ch, string set, string category)
	{
		string set2 = RegexCharClass.ConvertOldStringsToClass(set, category);
		return RegexCharClass.CharInClass(ch, set2);
	}

	protected static bool CharInClass(char ch, string charClass)
	{
		return RegexCharClass.CharInClass(ch, charClass);
	}

	protected void DoubleTrack()
	{
		int[] destinationArray = new int[runtrack.Length * 2];
		Array.Copy(runtrack, 0, destinationArray, runtrack.Length, runtrack.Length);
		runtrackpos += runtrack.Length;
		runtrack = destinationArray;
	}

	protected void DoubleStack()
	{
		int[] destinationArray = new int[runstack.Length * 2];
		Array.Copy(runstack, 0, destinationArray, runstack.Length, runstack.Length);
		runstackpos += runstack.Length;
		runstack = destinationArray;
	}

	protected void DoubleCrawl()
	{
		int[] destinationArray = new int[runcrawl.Length * 2];
		Array.Copy(runcrawl, 0, destinationArray, runcrawl.Length, runcrawl.Length);
		runcrawlpos += runcrawl.Length;
		runcrawl = destinationArray;
	}

	protected void Crawl(int i)
	{
		if (runcrawlpos == 0)
		{
			DoubleCrawl();
		}
		runcrawl[--runcrawlpos] = i;
	}

	protected int Popcrawl()
	{
		return runcrawl[runcrawlpos++];
	}

	protected int Crawlpos()
	{
		return runcrawl.Length - runcrawlpos;
	}

	protected void Capture(int capnum, int start, int end)
	{
		if (end < start)
		{
			int num = end;
			end = start;
			start = num;
		}
		Crawl(capnum);
		runmatch.AddMatch(capnum, start, end - start);
	}

	protected void TransferCapture(int capnum, int uncapnum, int start, int end)
	{
		if (end < start)
		{
			int num = end;
			end = start;
			start = num;
		}
		int num2 = MatchIndex(uncapnum);
		int num3 = num2 + MatchLength(uncapnum);
		if (start >= num3)
		{
			end = start;
			start = num3;
		}
		else if (end <= num2)
		{
			start = num2;
		}
		else
		{
			if (end > num3)
			{
				end = num3;
			}
			if (num2 > start)
			{
				start = num2;
			}
		}
		Crawl(uncapnum);
		runmatch.BalanceMatch(uncapnum);
		if (capnum != -1)
		{
			Crawl(capnum);
			runmatch.AddMatch(capnum, start, end - start);
		}
	}

	protected void Uncapture()
	{
		int cap = Popcrawl();
		runmatch.RemoveMatch(cap);
	}

	protected bool IsMatched(int cap)
	{
		return runmatch.IsMatched(cap);
	}

	protected int MatchIndex(int cap)
	{
		return runmatch.MatchIndex(cap);
	}

	protected int MatchLength(int cap)
	{
		return runmatch.MatchLength(cap);
	}
}
