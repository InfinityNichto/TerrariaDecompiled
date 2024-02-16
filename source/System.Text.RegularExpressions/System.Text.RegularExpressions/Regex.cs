using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

namespace System.Text.RegularExpressions;

public class Regex : ISerializable
{
	protected internal string? pattern;

	protected internal RegexOptions roptions;

	protected internal RegexRunnerFactory? factory;

	protected internal Hashtable? caps;

	protected internal Hashtable? capnames;

	protected internal string[]? capslist;

	protected internal int capsize;

	internal WeakReference<RegexReplacement> _replref;

	private volatile RegexRunner _runner;

	private RegexCode _code;

	private bool _refsInitialized;

	public static readonly TimeSpan InfiniteMatchTimeout = Timeout.InfiniteTimeSpan;

	internal static readonly TimeSpan s_defaultMatchTimeout = InitDefaultMatchTimeout();

	protected internal TimeSpan internalMatchTimeout;

	[CLSCompliant(false)]
	protected IDictionary? Caps
	{
		get
		{
			return caps;
		}
		[param: DisallowNull]
		set
		{
			if (value == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
			}
			caps = (value as Hashtable) ?? new Hashtable(value);
		}
	}

	[CLSCompliant(false)]
	protected IDictionary? CapNames
	{
		get
		{
			return capnames;
		}
		[param: DisallowNull]
		set
		{
			if (value == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
			}
			capnames = (value as Hashtable) ?? new Hashtable(value);
		}
	}

	public RegexOptions Options => roptions;

	public bool RightToLeft => UseOptionR();

	public static int CacheSize
	{
		get
		{
			return RegexCache.MaxCacheSize;
		}
		set
		{
			if (value < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
			}
			RegexCache.MaxCacheSize = value;
		}
	}

	public TimeSpan MatchTimeout => internalMatchTimeout;

	protected Regex()
	{
		internalMatchTimeout = s_defaultMatchTimeout;
	}

	public Regex(string pattern)
		: this(pattern, null)
	{
	}

	public Regex(string pattern, RegexOptions options)
		: this(pattern, options, s_defaultMatchTimeout, null)
	{
	}

	public Regex(string pattern, RegexOptions options, TimeSpan matchTimeout)
		: this(pattern, options, matchTimeout, null)
	{
	}

	internal Regex(string pattern, CultureInfo culture)
	{
		Init(pattern, RegexOptions.None, s_defaultMatchTimeout, culture);
	}

	internal Regex(string pattern, RegexOptions options, TimeSpan matchTimeout, CultureInfo culture)
	{
		Init(pattern, options, matchTimeout, culture);
		if (RuntimeFeature.IsDynamicCodeCompiled && UseOptionC())
		{
			factory = Compile(pattern, _code, options, matchTimeout != InfiniteMatchTimeout);
			_code = null;
		}
	}

	private void Init(string pattern, RegexOptions options, TimeSpan matchTimeout, CultureInfo culture)
	{
		ValidatePattern(pattern);
		ValidateOptions(options);
		ValidateMatchTimeout(matchTimeout);
		this.pattern = pattern;
		roptions = options;
		internalMatchTimeout = matchTimeout;
		RegexTree regexTree = RegexParser.Parse(pattern, roptions, culture ?? (((options & RegexOptions.CultureInvariant) != 0) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture));
		capnames = regexTree.CapNames;
		capslist = regexTree.CapsList;
		_code = RegexWriter.Write(regexTree);
		caps = _code.Caps;
		capsize = _code.CapSize;
		InitializeReferences();
	}

	internal static void ValidatePattern(string pattern)
	{
		if (pattern == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.pattern);
		}
	}

	internal static void ValidateOptions(RegexOptions options)
	{
		if ((uint)options >> 10 != 0 || ((options & RegexOptions.ECMAScript) != 0 && ((uint)options & 0xFFFFFCF4u) != 0))
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.options);
		}
	}

	protected internal static void ValidateMatchTimeout(TimeSpan matchTimeout)
	{
		long ticks = matchTimeout.Ticks;
		if (ticks != -10000 && (ulong)(ticks - 1) >= 21474836460000uL)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.matchTimeout);
		}
	}

	protected Regex(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static RegexRunnerFactory Compile(string pattern, RegexCode code, RegexOptions options, bool hasTimeout)
	{
		return RegexCompiler.Compile(pattern, code, options, hasTimeout);
	}

	public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname)
	{
		CompileToAssembly(regexinfos, assemblyname, null, null);
	}

	public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname, CustomAttributeBuilder[]? attributes)
	{
		CompileToAssembly(regexinfos, assemblyname, attributes, null);
	}

	public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname, CustomAttributeBuilder[]? attributes, string? resourceFile)
	{
		if (assemblyname == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.assemblyname);
		}
		if (regexinfos == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.regexinfos);
		}
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CompileToAssembly);
	}

	public static string Escape(string str)
	{
		if (str == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.str);
		}
		return RegexParser.Escape(str);
	}

	public static string Unescape(string str)
	{
		if (str == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.str);
		}
		return RegexParser.Unescape(str);
	}

	public override string ToString()
	{
		return pattern;
	}

	public string[] GetGroupNames()
	{
		string[] array;
		if (capslist == null)
		{
			array = new string[capsize];
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array;
				int num = i;
				uint num2 = (uint)i;
				array2[num] = num2.ToString();
			}
		}
		else
		{
			array = capslist.AsSpan().ToArray();
		}
		return array;
	}

	public int[] GetGroupNumbers()
	{
		int[] array;
		if (caps == null)
		{
			array = new int[capsize];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = i;
			}
		}
		else
		{
			array = new int[caps.Count];
			IDictionaryEnumerator enumerator = caps.GetEnumerator();
			while (enumerator.MoveNext())
			{
				array[(int)enumerator.Value] = (int)enumerator.Key;
			}
		}
		return array;
	}

	public string GroupNameFromNumber(int i)
	{
		if (capslist == null)
		{
			if ((uint)i >= (uint)capsize)
			{
				return string.Empty;
			}
			uint num = (uint)i;
			return num.ToString();
		}
		if (caps == null || caps.TryGetValue<int>(i, out i))
		{
			if ((uint)i >= (uint)capslist.Length)
			{
				return string.Empty;
			}
			return capslist[i];
		}
		return string.Empty;
	}

	public int GroupNumberFromName(string name)
	{
		if (name == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.name);
		}
		if (capnames != null)
		{
			if (!capnames.TryGetValue<int>(name, out var value))
			{
				return -1;
			}
			return value;
		}
		if (!uint.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out var result) || result >= capsize)
		{
			return -1;
		}
		return (int)result;
	}

	protected void InitializeReferences()
	{
		if (_refsInitialized)
		{
			ThrowHelper.ThrowNotSupportedException(ExceptionResource.OnlyAllowedOnce);
		}
		_replref = new WeakReference<RegexReplacement>(null);
		_refsInitialized = true;
	}

	internal Match Run(bool quick, int prevlen, string input, int beginning, int length, int startat)
	{
		if ((uint)startat > (uint)input.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startat, ExceptionResource.BeginIndexNotNegative);
		}
		if ((uint)length > (uint)input.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length, ExceptionResource.LengthNotNegative);
		}
		RegexRunner regexRunner = RentRunner();
		try
		{
			return regexRunner.Scan(this, input, beginning, beginning + length, startat, prevlen, quick, internalMatchTimeout);
		}
		finally
		{
			ReturnRunner(regexRunner);
		}
	}

	internal void Run<TState>(string input, int startat, ref TState state, MatchCallback<TState> callback, bool reuseMatchObject)
	{
		RegexRunner regexRunner = RentRunner();
		try
		{
			regexRunner.Scan(this, input, startat, ref state, callback, reuseMatchObject, internalMatchTimeout);
		}
		finally
		{
			ReturnRunner(regexRunner);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private RegexRunner RentRunner()
	{
		RegexRunner regexRunner = Interlocked.Exchange(ref _runner, null);
		if (regexRunner == null)
		{
			if (factory == null)
			{
				return new RegexInterpreter(_code, UseOptionInvariant() ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
			}
			regexRunner = factory.CreateInstance();
		}
		return regexRunner;
	}

	internal void ReturnRunner(RegexRunner runner)
	{
		_runner = runner;
	}

	protected bool UseOptionC()
	{
		return (roptions & RegexOptions.Compiled) != 0;
	}

	protected internal bool UseOptionR()
	{
		return (roptions & RegexOptions.RightToLeft) != 0;
	}

	internal bool UseOptionInvariant()
	{
		return (roptions & RegexOptions.CultureInvariant) != 0;
	}

	public static bool IsMatch(string input, string pattern)
	{
		return RegexCache.GetOrAdd(pattern).IsMatch(input);
	}

	public static bool IsMatch(string input, string pattern, RegexOptions options)
	{
		return RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).IsMatch(input);
	}

	public static bool IsMatch(string input, string pattern, RegexOptions options, TimeSpan matchTimeout)
	{
		return RegexCache.GetOrAdd(pattern, options, matchTimeout).IsMatch(input);
	}

	public bool IsMatch(string input)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Run(quick: true, -1, input, 0, input.Length, UseOptionR() ? input.Length : 0) == null;
	}

	public bool IsMatch(string input, int startat)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Run(quick: true, -1, input, 0, input.Length, startat) == null;
	}

	public static Match Match(string input, string pattern)
	{
		return RegexCache.GetOrAdd(pattern).Match(input);
	}

	public static Match Match(string input, string pattern, RegexOptions options)
	{
		return RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Match(input);
	}

	public static Match Match(string input, string pattern, RegexOptions options, TimeSpan matchTimeout)
	{
		return RegexCache.GetOrAdd(pattern, options, matchTimeout).Match(input);
	}

	public Match Match(string input)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Run(quick: false, -1, input, 0, input.Length, UseOptionR() ? input.Length : 0);
	}

	public Match Match(string input, int startat)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Run(quick: false, -1, input, 0, input.Length, startat);
	}

	public Match Match(string input, int beginning, int length)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Run(quick: false, -1, input, beginning, length, UseOptionR() ? (beginning + length) : beginning);
	}

	public static MatchCollection Matches(string input, string pattern)
	{
		return RegexCache.GetOrAdd(pattern).Matches(input);
	}

	public static MatchCollection Matches(string input, string pattern, RegexOptions options)
	{
		return RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Matches(input);
	}

	public static MatchCollection Matches(string input, string pattern, RegexOptions options, TimeSpan matchTimeout)
	{
		return RegexCache.GetOrAdd(pattern, options, matchTimeout).Matches(input);
	}

	public MatchCollection Matches(string input)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return new MatchCollection(this, input, UseOptionR() ? input.Length : 0);
	}

	public MatchCollection Matches(string input, int startat)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return new MatchCollection(this, input, startat);
	}

	public static string Replace(string input, string pattern, string replacement)
	{
		return RegexCache.GetOrAdd(pattern).Replace(input, replacement);
	}

	public static string Replace(string input, string pattern, string replacement, RegexOptions options)
	{
		return RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Replace(input, replacement);
	}

	public static string Replace(string input, string pattern, string replacement, RegexOptions options, TimeSpan matchTimeout)
	{
		return RegexCache.GetOrAdd(pattern, options, matchTimeout).Replace(input, replacement);
	}

	public string Replace(string input, string replacement)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Replace(input, replacement, -1, UseOptionR() ? input.Length : 0);
	}

	public string Replace(string input, string replacement, int count)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Replace(input, replacement, count, UseOptionR() ? input.Length : 0);
	}

	public string Replace(string input, string replacement, int count, int startat)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		if (replacement == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.replacement);
		}
		return RegexReplacement.GetOrCreate(_replref, replacement, caps, capsize, capnames, roptions).Replace(this, input, count, startat);
	}

	public static string Replace(string input, string pattern, MatchEvaluator evaluator)
	{
		return RegexCache.GetOrAdd(pattern).Replace(input, evaluator);
	}

	public static string Replace(string input, string pattern, MatchEvaluator evaluator, RegexOptions options)
	{
		return RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Replace(input, evaluator);
	}

	public static string Replace(string input, string pattern, MatchEvaluator evaluator, RegexOptions options, TimeSpan matchTimeout)
	{
		return RegexCache.GetOrAdd(pattern, options, matchTimeout).Replace(input, evaluator);
	}

	public string Replace(string input, MatchEvaluator evaluator)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Replace(evaluator, this, input, -1, UseOptionR() ? input.Length : 0);
	}

	public string Replace(string input, MatchEvaluator evaluator, int count)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Replace(evaluator, this, input, count, UseOptionR() ? input.Length : 0);
	}

	public string Replace(string input, MatchEvaluator evaluator, int count, int startat)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Replace(evaluator, this, input, count, startat);
	}

	private static string Replace(MatchEvaluator evaluator, Regex regex, string input, int count, int startat)
	{
		if (evaluator == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.evaluator);
		}
		if (count < -1)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.CountTooSmall);
		}
		if ((uint)startat > (uint)input.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startat, ExceptionResource.BeginIndexNotNegative);
		}
		if (count == 0)
		{
			return input;
		}
		(SegmentStringBuilder, MatchEvaluator, int, string, int) state2 = (SegmentStringBuilder.Create(), evaluator, 0, input, count);
		if (!regex.RightToLeft)
		{
			regex.Run<(SegmentStringBuilder, MatchEvaluator, int, string, int)>(input, startat, ref state2, delegate(ref (SegmentStringBuilder segments, MatchEvaluator evaluator, int prevat, string input, int count) state, Match match)
			{
				state.segments.Add(state.input.AsMemory(state.prevat, match.Index - state.prevat));
				state.prevat = match.Index + match.Length;
				state.segments.Add(state.evaluator(match).AsMemory());
				return --state.count != 0;
			}, reuseMatchObject: false);
			if (state2.Item1.Count == 0)
			{
				return input;
			}
			state2.Item1.Add(input.AsMemory(state2.Item3, input.Length - state2.Item3));
		}
		else
		{
			state2.Item3 = input.Length;
			regex.Run<(SegmentStringBuilder, MatchEvaluator, int, string, int)>(input, startat, ref state2, delegate(ref (SegmentStringBuilder segments, MatchEvaluator evaluator, int prevat, string input, int count) state, Match match)
			{
				state.segments.Add(state.input.AsMemory(match.Index + match.Length, state.prevat - match.Index - match.Length));
				state.prevat = match.Index;
				state.segments.Add(state.evaluator(match).AsMemory());
				return --state.count != 0;
			}, reuseMatchObject: false);
			if (state2.Item1.Count == 0)
			{
				return input;
			}
			state2.Item1.Add(input.AsMemory(0, state2.Item3));
			state2.Item1.AsSpan().Reverse();
		}
		return state2.Item1.ToString();
	}

	public static string[] Split(string input, string pattern)
	{
		return RegexCache.GetOrAdd(pattern).Split(input);
	}

	public static string[] Split(string input, string pattern, RegexOptions options)
	{
		return RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Split(input);
	}

	public static string[] Split(string input, string pattern, RegexOptions options, TimeSpan matchTimeout)
	{
		return RegexCache.GetOrAdd(pattern, options, matchTimeout).Split(input);
	}

	public string[] Split(string input)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Split(this, input, 0, UseOptionR() ? input.Length : 0);
	}

	public string[] Split(string input, int count)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Split(this, input, count, UseOptionR() ? input.Length : 0);
	}

	public string[] Split(string input, int count, int startat)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		return Split(this, input, count, startat);
	}

	private static string[] Split(Regex regex, string input, int count, int startat)
	{
		if (count < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.CountTooSmall);
		}
		if ((uint)startat > (uint)input.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startat, ExceptionResource.BeginIndexNotNegative);
		}
		if (count == 1)
		{
			return new string[1] { input };
		}
		count--;
		(List<string>, int, string, int) state2 = (new List<string>(), 0, input, count);
		if (!regex.RightToLeft)
		{
			regex.Run<(List<string>, int, string, int)>(input, startat, ref state2, delegate(ref (List<string> results, int prevat, string input, int count) state, Match match)
			{
				state.results.Add(state.input.Substring(state.prevat, match.Index - state.prevat));
				state.prevat = match.Index + match.Length;
				for (int j = 1; j < match.Groups.Count; j++)
				{
					if (match.IsMatched(j))
					{
						state.results.Add(match.Groups[j].ToString());
					}
				}
				return --state.count != 0;
			}, reuseMatchObject: true);
			if (state2.Item1.Count == 0)
			{
				return new string[1] { input };
			}
			state2.Item1.Add(input.Substring(state2.Item2, input.Length - state2.Item2));
		}
		else
		{
			state2.Item2 = input.Length;
			regex.Run<(List<string>, int, string, int)>(input, startat, ref state2, delegate(ref (List<string> results, int prevat, string input, int count) state, Match match)
			{
				state.results.Add(state.input.Substring(match.Index + match.Length, state.prevat - match.Index - match.Length));
				state.prevat = match.Index;
				for (int i = 1; i < match.Groups.Count; i++)
				{
					if (match.IsMatched(i))
					{
						state.results.Add(match.Groups[i].ToString());
					}
				}
				return --state.count != 0;
			}, reuseMatchObject: true);
			if (state2.Item1.Count == 0)
			{
				return new string[1] { input };
			}
			state2.Item1.Add(input.Substring(0, state2.Item2));
			state2.Item1.Reverse(0, state2.Item1.Count);
		}
		return state2.Item1.ToArray();
	}

	private static TimeSpan InitDefaultMatchTimeout()
	{
		AppDomain currentDomain = AppDomain.CurrentDomain;
		object data = currentDomain.GetData("REGEX_DEFAULT_MATCH_TIMEOUT");
		if (data == null)
		{
			return InfiniteMatchTimeout;
		}
		if (data is TimeSpan timeSpan)
		{
			try
			{
				ValidateMatchTimeout(timeSpan);
				return timeSpan;
			}
			catch (ArgumentOutOfRangeException)
			{
				throw new ArgumentOutOfRangeException(System.SR.Format(System.SR.IllegalDefaultRegexMatchTimeoutInAppDomain, "REGEX_DEFAULT_MATCH_TIMEOUT", timeSpan));
			}
		}
		throw new InvalidCastException(System.SR.Format(System.SR.IllegalDefaultRegexMatchTimeoutInAppDomain, "REGEX_DEFAULT_MATCH_TIMEOUT", data));
	}
}
