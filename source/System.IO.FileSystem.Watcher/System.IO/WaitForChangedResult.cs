namespace System.IO;

public struct WaitForChangedResult
{
	internal static readonly WaitForChangedResult TimedOutResult = new WaitForChangedResult((WatcherChangeTypes)0, null, null, timedOut: true);

	public WatcherChangeTypes ChangeType { get; set; }

	public string? Name { get; set; }

	public string? OldName { get; set; }

	public bool TimedOut { get; set; }

	internal WaitForChangedResult(WatcherChangeTypes changeType, string name, string oldName, bool timedOut)
	{
		ChangeType = changeType;
		Name = name;
		OldName = oldName;
		TimedOut = timedOut;
	}
}
