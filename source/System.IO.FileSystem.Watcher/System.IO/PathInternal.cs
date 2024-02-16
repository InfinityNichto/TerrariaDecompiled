using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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

	internal static bool IsValidDriveChar(char value)
	{
		if (value < 'A' || value > 'Z')
		{
			if (value >= 'a')
			{
				return value <= 'z';
			}
			return false;
		}
		return true;
	}

	private static bool EndsWithPeriodOrSpace(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return false;
		}
		char c = path[path.Length - 1];
		if (c != ' ')
		{
			return c == '.';
		}
		return true;
	}

	[return: NotNullIfNotNull("path")]
	internal static string EnsureExtendedPrefixIfNeeded(string path)
	{
		if (path != null && (path.Length >= 260 || EndsWithPeriodOrSpace(path)))
		{
			return EnsureExtendedPrefix(path);
		}
		return path;
	}

	internal static string EnsureExtendedPrefix(string path)
	{
		if (IsPartiallyQualified(path) || IsDevice(path))
		{
			return path;
		}
		if (path.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase))
		{
			return path.Insert(2, "?\\UNC\\");
		}
		return "\\\\?\\" + path;
	}

	internal static bool IsDevice(string path)
	{
		if (!IsExtended(path))
		{
			if (path.Length >= 4 && IsDirectorySeparator(path[0]) && IsDirectorySeparator(path[1]) && (path[2] == '.' || path[2] == '?'))
			{
				return IsDirectorySeparator(path[3]);
			}
			return false;
		}
		return true;
	}

	internal static bool IsExtended(string path)
	{
		if (path.Length >= 4 && path[0] == '\\' && (path[1] == '\\' || path[1] == '?') && path[2] == '?')
		{
			return path[3] == '\\';
		}
		return false;
	}

	internal static bool IsPartiallyQualified(string path)
	{
		if (path.Length < 2)
		{
			return true;
		}
		if (IsDirectorySeparator(path[0]))
		{
			if (path[1] != '?')
			{
				return !IsDirectorySeparator(path[1]);
			}
			return false;
		}
		if (path.Length >= 3 && path[1] == Path.VolumeSeparatorChar && IsDirectorySeparator(path[2]))
		{
			return !IsValidDriveChar(path[0]);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsDirectorySeparator(char c)
	{
		if (c != Path.DirectorySeparatorChar)
		{
			return c == Path.AltDirectorySeparatorChar;
		}
		return true;
	}
}
