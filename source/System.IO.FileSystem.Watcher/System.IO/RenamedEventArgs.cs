namespace System.IO;

public class RenamedEventArgs : FileSystemEventArgs
{
	private readonly string _oldName;

	private readonly string _oldFullPath;

	public string OldFullPath => _oldFullPath;

	public string? OldName => _oldName;

	public RenamedEventArgs(WatcherChangeTypes changeType, string directory, string? name, string? oldName)
		: base(changeType, directory, name)
	{
		_oldName = oldName;
		_oldFullPath = FileSystemEventArgs.Combine(directory, oldName);
	}
}
