using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.IO;

internal static class PathInternal
{
	internal static bool IsRoot(ReadOnlySpan<char> path)
	{
		return path.Length == GetRootLength(path);
	}

	[return: NotNullIfNotNull("path")]
	internal static string TrimEndingDirectorySeparator(string path)
	{
		if (!EndsInDirectorySeparator(path) || IsRoot(path.AsSpan()))
		{
			return path;
		}
		return path.Substring(0, path.Length - 1);
	}

	internal static bool EndsInDirectorySeparator(string path)
	{
		if (!string.IsNullOrEmpty(path))
		{
			return IsDirectorySeparator(path[path.Length - 1]);
		}
		return false;
	}

	internal static bool EndsInDirectorySeparator(ReadOnlySpan<char> path)
	{
		if (path.Length > 0)
		{
			return IsDirectorySeparator(path[path.Length - 1]);
		}
		return false;
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

	internal static bool EndsWithPeriodOrSpace(string path)
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
		if (IsPartiallyQualified(path.AsSpan()) || IsDevice(path.AsSpan()))
		{
			return path;
		}
		if (path.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase))
		{
			return path.Insert(2, "?\\UNC\\");
		}
		return "\\\\?\\" + path;
	}

	internal static bool IsDevice(ReadOnlySpan<char> path)
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

	internal static bool IsDeviceUNC(ReadOnlySpan<char> path)
	{
		if (path.Length >= 8 && IsDevice(path) && IsDirectorySeparator(path[7]) && path[4] == 'U' && path[5] == 'N')
		{
			return path[6] == 'C';
		}
		return false;
	}

	internal static bool IsExtended(ReadOnlySpan<char> path)
	{
		if (path.Length >= 4 && path[0] == '\\' && (path[1] == '\\' || path[1] == '?') && path[2] == '?')
		{
			return path[3] == '\\';
		}
		return false;
	}

	internal static int GetRootLength(ReadOnlySpan<char> path)
	{
		int length = path.Length;
		int i = 0;
		bool flag = IsDevice(path);
		bool flag2 = flag && IsDeviceUNC(path);
		if ((!flag || flag2) && length > 0 && IsDirectorySeparator(path[0]))
		{
			if (flag2 || (length > 1 && IsDirectorySeparator(path[1])))
			{
				i = (flag2 ? 8 : 2);
				int num = 2;
				for (; i < length; i++)
				{
					if (IsDirectorySeparator(path[i]) && --num <= 0)
					{
						break;
					}
				}
			}
			else
			{
				i = 1;
			}
		}
		else if (flag)
		{
			for (i = 4; i < length && !IsDirectorySeparator(path[i]); i++)
			{
			}
			if (i < length && i > 4 && IsDirectorySeparator(path[i]))
			{
				i++;
			}
		}
		else if (length >= 2 && path[1] == ':' && IsValidDriveChar(path[0]))
		{
			i = 2;
			if (length > 2 && IsDirectorySeparator(path[2]))
			{
				i++;
			}
		}
		return i;
	}

	internal static bool IsPartiallyQualified(ReadOnlySpan<char> path)
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
		if (path.Length >= 3 && path[1] == ':' && IsDirectorySeparator(path[2]))
		{
			return !IsValidDriveChar(path[0]);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsDirectorySeparator(char c)
	{
		if (c != '\\')
		{
			return c == '/';
		}
		return true;
	}
}
