namespace System.Text.RegularExpressions;

internal sealed class CompiledRegexRunner : RegexRunner
{
	private readonly Action<RegexRunner> _goMethod;

	private readonly Func<RegexRunner, bool> _findFirstCharMethod;

	public CompiledRegexRunner(Action<RegexRunner> go, Func<RegexRunner, bool> findFirstChar, int trackCount)
	{
		_goMethod = go;
		_findFirstCharMethod = findFirstChar;
		runtrackcount = trackCount;
	}

	protected override void Go()
	{
		_goMethod(this);
	}

	protected override bool FindFirstChar()
	{
		return _findFirstCharMethod(this);
	}

	protected override void InitTrackCount()
	{
	}
}
