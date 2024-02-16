using System.Collections.Generic;

namespace System.IO.Enumeration;

internal static class FileSystemEnumerableFactory
{
	private static readonly char[] s_unixEscapeChars = new char[4] { '\\', '"', '<', '>' };

	internal static bool NormalizeInputs(ref string directory, ref string expression, MatchType matchType)
	{
		if (Path.IsPathRooted(expression))
		{
			throw new ArgumentException(SR.Arg_Path2IsRooted, "expression");
		}
		if (expression.Contains('\0'))
		{
			throw new ArgumentException(SR.Argument_InvalidPathChars, expression);
		}
		if (directory.Contains('\0'))
		{
			throw new ArgumentException(SR.Argument_InvalidPathChars, directory);
		}
		ReadOnlySpan<char> directoryName = Path.GetDirectoryName(expression.AsSpan());
		bool result = true;
		if (directoryName.Length != 0)
		{
			directory = Path.Join(directory.AsSpan(), directoryName);
			expression = expression.Substring(directoryName.Length + 1);
			result = false;
		}
		switch (matchType)
		{
		case MatchType.Win32:
			if (expression == "*")
			{
				break;
			}
			if (string.IsNullOrEmpty(expression) || expression == "." || expression == "*.*")
			{
				expression = "*";
				break;
			}
			if (Path.DirectorySeparatorChar != '\\' && expression.IndexOfAny(s_unixEscapeChars) != -1)
			{
				expression = expression.Replace("\\", "\\\\");
				expression = expression.Replace("\"", "\\\"");
				expression = expression.Replace(">", "\\>");
				expression = expression.Replace("<", "\\<");
			}
			expression = FileSystemName.TranslateWin32Expression(expression);
			break;
		default:
			throw new ArgumentOutOfRangeException("matchType");
		case MatchType.Simple:
			break;
		}
		return result;
	}

	private static bool MatchesPattern(string expression, ReadOnlySpan<char> name, EnumerationOptions options)
	{
		bool ignoreCase = (options.MatchCasing == MatchCasing.PlatformDefault && !PathInternal.IsCaseSensitive) || options.MatchCasing == MatchCasing.CaseInsensitive;
		return options.MatchType switch
		{
			MatchType.Simple => FileSystemName.MatchesSimpleExpression(expression.AsSpan(), name, ignoreCase), 
			MatchType.Win32 => FileSystemName.MatchesWin32Expression(expression.AsSpan(), name, ignoreCase), 
			_ => throw new ArgumentOutOfRangeException("options"), 
		};
	}

	internal static IEnumerable<string> UserFiles(string directory, string expression, EnumerationOptions options)
	{
		return new FileSystemEnumerable<string>(directory, delegate(ref FileSystemEntry entry)
		{
			return entry.ToSpecifiedFullPath();
		}, options)
		{
			ShouldIncludePredicate = delegate(ref FileSystemEntry entry)
			{
				return !entry.IsDirectory && MatchesPattern(expression, entry.FileName, options);
			}
		};
	}

	internal static IEnumerable<string> UserDirectories(string directory, string expression, EnumerationOptions options)
	{
		return new FileSystemEnumerable<string>(directory, delegate(ref FileSystemEntry entry)
		{
			return entry.ToSpecifiedFullPath();
		}, options)
		{
			ShouldIncludePredicate = delegate(ref FileSystemEntry entry)
			{
				return entry.IsDirectory && MatchesPattern(expression, entry.FileName, options);
			}
		};
	}

	internal static IEnumerable<string> UserEntries(string directory, string expression, EnumerationOptions options)
	{
		return new FileSystemEnumerable<string>(directory, delegate(ref FileSystemEntry entry)
		{
			return entry.ToSpecifiedFullPath();
		}, options)
		{
			ShouldIncludePredicate = delegate(ref FileSystemEntry entry)
			{
				return MatchesPattern(expression, entry.FileName, options);
			}
		};
	}

	internal static IEnumerable<FileInfo> FileInfos(string directory, string expression, EnumerationOptions options, bool isNormalized)
	{
		return new FileSystemEnumerable<FileInfo>(directory, delegate(ref FileSystemEntry entry)
		{
			return (FileInfo)entry.ToFileSystemInfo();
		}, options, isNormalized)
		{
			ShouldIncludePredicate = delegate(ref FileSystemEntry entry)
			{
				return !entry.IsDirectory && MatchesPattern(expression, entry.FileName, options);
			}
		};
	}

	internal static IEnumerable<DirectoryInfo> DirectoryInfos(string directory, string expression, EnumerationOptions options, bool isNormalized)
	{
		return new FileSystemEnumerable<DirectoryInfo>(directory, delegate(ref FileSystemEntry entry)
		{
			return (DirectoryInfo)entry.ToFileSystemInfo();
		}, options, isNormalized)
		{
			ShouldIncludePredicate = delegate(ref FileSystemEntry entry)
			{
				return entry.IsDirectory && MatchesPattern(expression, entry.FileName, options);
			}
		};
	}

	internal static IEnumerable<FileSystemInfo> FileSystemInfos(string directory, string expression, EnumerationOptions options, bool isNormalized)
	{
		return new FileSystemEnumerable<FileSystemInfo>(directory, delegate(ref FileSystemEntry entry)
		{
			return entry.ToFileSystemInfo();
		}, options, isNormalized)
		{
			ShouldIncludePredicate = delegate(ref FileSystemEntry entry)
			{
				return MatchesPattern(expression, entry.FileName, options);
			}
		};
	}
}
