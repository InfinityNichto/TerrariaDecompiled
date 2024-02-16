using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.IO;

internal static class PathInternal
{
	internal static StringComparison StringComparison
	{
		get
		{
			_ = IsCaseSensitive;
			return StringComparison.OrdinalIgnoreCase;
		}
	}

	internal static bool IsCaseSensitive
	{
		get
		{
			if (!OperatingSystem.IsWindows())
			{
			}
			return false;
		}
	}

	internal static bool StartsWithDirectorySeparator(ReadOnlySpan<char> path)
	{
		if (path.Length > 0)
		{
			return IsDirectorySeparator(path[0]);
		}
		return false;
	}

	internal static string EnsureTrailingSeparator(string path)
	{
		if (!EndsInDirectorySeparator(path.AsSpan()))
		{
			return path + "\\";
		}
		return path;
	}

	internal static bool IsRoot(ReadOnlySpan<char> path)
	{
		return path.Length == GetRootLength(path);
	}

	internal static int GetCommonPathLength(string first, string second, bool ignoreCase)
	{
		int num = EqualStartingCharacterCount(first, second, ignoreCase);
		if (num == 0)
		{
			return num;
		}
		if (num == first.Length && (num == second.Length || IsDirectorySeparator(second[num])))
		{
			return num;
		}
		if (num == second.Length && IsDirectorySeparator(first[num]))
		{
			return num;
		}
		while (num > 0 && !IsDirectorySeparator(first[num - 1]))
		{
			num--;
		}
		return num;
	}

	internal unsafe static int EqualStartingCharacterCount(string first, string second, bool ignoreCase)
	{
		//The blocks IL_0037, IL_005c, IL_006c, IL_0072, IL_0078, IL_0080, IL_0083 are reachable both inside and outside the pinned region starting at IL_0032. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second))
		{
			return 0;
		}
		int num = 0;
		fixed (char* ptr2 = first)
		{
			char* intPtr;
			if (second == null)
			{
				char* ptr;
				intPtr = (ptr = null);
				char* ptr3 = ptr2;
				char* ptr4 = ptr;
				char* ptr5 = ptr3 + first.Length;
				char* ptr6 = ptr4 + second.Length;
				while (ptr3 != ptr5 && ptr4 != ptr6 && (*ptr3 == *ptr4 || (ignoreCase && char.ToUpperInvariant(*ptr3) == char.ToUpperInvariant(*ptr4))))
				{
					num++;
					ptr3++;
					ptr4++;
				}
			}
			else
			{
				fixed (char* ptr7 = &second.GetPinnableReference())
				{
					char* ptr;
					intPtr = (ptr = ptr7);
					char* ptr3 = ptr2;
					char* ptr4 = ptr;
					char* ptr5 = ptr3 + first.Length;
					char* ptr6 = ptr4 + second.Length;
					while (ptr3 != ptr5 && ptr4 != ptr6 && (*ptr3 == *ptr4 || (ignoreCase && char.ToUpperInvariant(*ptr3) == char.ToUpperInvariant(*ptr4))))
					{
						num++;
						ptr3++;
						ptr4++;
					}
				}
			}
		}
		return num;
	}

	internal static bool AreRootsEqual(string first, string second, StringComparison comparisonType)
	{
		int rootLength = GetRootLength(first.AsSpan());
		int rootLength2 = GetRootLength(second.AsSpan());
		if (rootLength == rootLength2)
		{
			return string.Compare(first, 0, second, 0, rootLength, comparisonType) == 0;
		}
		return false;
	}

	internal static string RemoveRelativeSegments(string path, int rootLength)
	{
		Span<char> initialBuffer = stackalloc char[260];
		ValueStringBuilder sb = new ValueStringBuilder(initialBuffer);
		if (RemoveRelativeSegments(path.AsSpan(), rootLength, ref sb))
		{
			path = sb.ToString();
		}
		sb.Dispose();
		return path;
	}

	internal static bool RemoveRelativeSegments(ReadOnlySpan<char> path, int rootLength, ref ValueStringBuilder sb)
	{
		bool flag = false;
		int num = rootLength;
		if (IsDirectorySeparator(path[num - 1]))
		{
			num--;
		}
		if (num > 0)
		{
			sb.Append(path.Slice(0, num));
		}
		for (int i = num; i < path.Length; i++)
		{
			char c = path[i];
			if (IsDirectorySeparator(c) && i + 1 < path.Length)
			{
				if (IsDirectorySeparator(path[i + 1]))
				{
					continue;
				}
				if ((i + 2 == path.Length || IsDirectorySeparator(path[i + 2])) && path[i + 1] == '.')
				{
					i++;
					continue;
				}
				if (i + 2 < path.Length && (i + 3 == path.Length || IsDirectorySeparator(path[i + 3])) && path[i + 1] == '.' && path[i + 2] == '.')
				{
					int num2;
					for (num2 = sb.Length - 1; num2 >= num; num2--)
					{
						if (IsDirectorySeparator(sb[num2]))
						{
							sb.Length = ((i + 3 >= path.Length && num2 == num) ? (num2 + 1) : num2);
							break;
						}
					}
					if (num2 < num)
					{
						sb.Length = num;
					}
					i += 2;
					continue;
				}
			}
			if (c != '\\' && c == '/')
			{
				c = '\\';
				flag = true;
			}
			sb.Append(c);
		}
		if (!flag && sb.Length == path.Length)
		{
			return false;
		}
		if (num != rootLength && sb.Length < rootLength)
		{
			sb.Append(path[rootLength - 1]);
		}
		return true;
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

	internal static ReadOnlySpan<char> TrimEndingDirectorySeparator(ReadOnlySpan<char> path)
	{
		if (!EndsInDirectorySeparator(path) || IsRoot(path))
		{
			return path;
		}
		return path.Slice(0, path.Length - 1);
	}

	internal static bool EndsInDirectorySeparator(ReadOnlySpan<char> path)
	{
		if (path.Length > 0)
		{
			return IsDirectorySeparator(path[path.Length - 1]);
		}
		return false;
	}

	internal static string GetLinkTargetFullPath(string path, string pathToTarget)
	{
		if (!IsPartiallyQualified(pathToTarget.AsSpan()))
		{
			return pathToTarget;
		}
		return Path.Join(Path.GetDirectoryName(path.AsSpan()), pathToTarget.AsSpan());
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

	[return: NotNullIfNotNull("path")]
	internal static string NormalizeDirectorySeparators(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return path;
		}
		bool flag = true;
		for (int i = 0; i < path.Length; i++)
		{
			char c = path[i];
			if (IsDirectorySeparator(c) && (c != '\\' || (i > 0 && i + 1 < path.Length && IsDirectorySeparator(path[i + 1]))))
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			return path;
		}
		Span<char> initialBuffer = stackalloc char[260];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		int num = 0;
		if (IsDirectorySeparator(path[num]))
		{
			num++;
			valueStringBuilder.Append('\\');
		}
		for (int j = num; j < path.Length; j++)
		{
			char c = path[j];
			if (IsDirectorySeparator(c))
			{
				if (j + 1 < path.Length && IsDirectorySeparator(path[j + 1]))
				{
					continue;
				}
				c = '\\';
			}
			valueStringBuilder.Append(c);
		}
		return valueStringBuilder.ToString();
	}

	internal static bool IsEffectivelyEmpty(ReadOnlySpan<char> path)
	{
		if (path.IsEmpty)
		{
			return true;
		}
		ReadOnlySpan<char> readOnlySpan = path;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			char c = readOnlySpan[i];
			if (c != ' ')
			{
				return false;
			}
		}
		return true;
	}
}
