using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies;

internal sealed class SyncWindowsFileStreamStrategy : OSFileStreamStrategy
{
	internal override bool IsAsync => false;

	internal SyncWindowsFileStreamStrategy(SafeFileHandle handle, FileAccess access)
		: base(handle, access)
	{
	}

	internal SyncWindowsFileStreamStrategy(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
		: base(path, mode, access, share, options, preallocationSize)
	{
	}
}
