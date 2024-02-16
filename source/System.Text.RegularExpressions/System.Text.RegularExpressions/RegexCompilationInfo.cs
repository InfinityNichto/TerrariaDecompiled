using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions;

public class RegexCompilationInfo
{
	private string _pattern;

	private string _name;

	private string _nspace;

	private TimeSpan _matchTimeout;

	public bool IsPublic { get; set; }

	public TimeSpan MatchTimeout
	{
		get
		{
			return _matchTimeout;
		}
		set
		{
			Regex.ValidateMatchTimeout(value);
			_matchTimeout = value;
		}
	}

	public string Name
	{
		get
		{
			return _name;
		}
		[MemberNotNull("_name")]
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("Name");
			}
			if (value.Length == 0)
			{
				throw new ArgumentException(System.SR.Format(System.SR.InvalidEmptyArgument, "Name"), "Name");
			}
			_name = value;
		}
	}

	public string Namespace
	{
		get
		{
			return _nspace;
		}
		[MemberNotNull("_nspace")]
		set
		{
			_nspace = value ?? throw new ArgumentNullException("Namespace");
		}
	}

	public RegexOptions Options { get; set; }

	public string Pattern
	{
		get
		{
			return _pattern;
		}
		[MemberNotNull("_pattern")]
		set
		{
			_pattern = value ?? throw new ArgumentNullException("Pattern");
		}
	}

	public RegexCompilationInfo(string pattern, RegexOptions options, string name, string fullnamespace, bool ispublic)
		: this(pattern, options, name, fullnamespace, ispublic, Regex.s_defaultMatchTimeout)
	{
	}

	public RegexCompilationInfo(string pattern, RegexOptions options, string name, string fullnamespace, bool ispublic, TimeSpan matchTimeout)
	{
		Pattern = pattern;
		Name = name;
		Namespace = fullnamespace;
		Options = options;
		IsPublic = ispublic;
		MatchTimeout = matchTimeout;
	}
}
