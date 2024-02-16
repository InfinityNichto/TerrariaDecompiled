namespace System.Text.RegularExpressions;

public class Group : Capture
{
	internal static readonly Group s_emptyGroup = new Group(string.Empty, Array.Empty<int>(), 0, string.Empty);

	internal readonly int[] _caps;

	internal int _capcount;

	internal CaptureCollection _capcoll;

	public bool Success => _capcount != 0;

	public string Name { get; }

	public CaptureCollection Captures => _capcoll ?? (_capcoll = new CaptureCollection(this));

	internal Group(string text, int[] caps, int capcount, string name)
		: base(text, (capcount != 0) ? caps[(capcount - 1) * 2] : 0, (capcount != 0) ? caps[capcount * 2 - 1] : 0)
	{
		_caps = caps;
		_capcount = capcount;
		Name = name;
	}

	public static Group Synchronized(Group inner)
	{
		if (inner == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.inner);
		}
		CaptureCollection captures = inner.Captures;
		if (inner.Success)
		{
			captures.ForceInitialized();
		}
		return inner;
	}
}
