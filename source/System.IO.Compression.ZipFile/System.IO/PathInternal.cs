namespace System.IO;

internal static class PathInternal
{
	internal static StringComparison StringComparison
	{
		get
		{
			if (!IsCaseSensitive)
			{
				return StringComparison.OrdinalIgnoreCase;
			}
			return StringComparison.Ordinal;
		}
	}

	internal static bool IsCaseSensitive
	{
		get
		{
			if (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsIOS() && !OperatingSystem.IsTvOS())
			{
				return !OperatingSystem.IsWatchOS();
			}
			return false;
		}
	}
}
