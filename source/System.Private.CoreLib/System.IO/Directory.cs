using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Enumeration;

namespace System.IO;

public static class Directory
{
	public static DirectoryInfo? GetParent(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_PathEmpty, "path");
		}
		string fullPath = Path.GetFullPath(path);
		string directoryName = Path.GetDirectoryName(fullPath);
		if (directoryName == null)
		{
			return null;
		}
		return new DirectoryInfo(directoryName);
	}

	public static DirectoryInfo CreateDirectory(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_PathEmpty, "path");
		}
		string fullPath = Path.GetFullPath(path);
		FileSystem.CreateDirectory(fullPath);
		return new DirectoryInfo(path, fullPath, null, isNormalized: true);
	}

	public static bool Exists([NotNullWhen(true)] string? path)
	{
		try
		{
			if (path == null)
			{
				return false;
			}
			if (path.Length == 0)
			{
				return false;
			}
			string fullPath = Path.GetFullPath(path);
			return FileSystem.DirectoryExists(fullPath);
		}
		catch (ArgumentException)
		{
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
		return false;
	}

	public static void SetCreationTime(string path, DateTime creationTime)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetCreationTime(fullPath, creationTime, asDirectory: true);
	}

	public static void SetCreationTimeUtc(string path, DateTime creationTimeUtc)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetCreationTime(fullPath, File.GetUtcDateTimeOffset(creationTimeUtc), asDirectory: true);
	}

	public static DateTime GetCreationTime(string path)
	{
		return File.GetCreationTime(path);
	}

	public static DateTime GetCreationTimeUtc(string path)
	{
		return File.GetCreationTimeUtc(path);
	}

	public static void SetLastWriteTime(string path, DateTime lastWriteTime)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetLastWriteTime(fullPath, lastWriteTime, asDirectory: true);
	}

	public static void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetLastWriteTime(fullPath, File.GetUtcDateTimeOffset(lastWriteTimeUtc), asDirectory: true);
	}

	public static DateTime GetLastWriteTime(string path)
	{
		return File.GetLastWriteTime(path);
	}

	public static DateTime GetLastWriteTimeUtc(string path)
	{
		return File.GetLastWriteTimeUtc(path);
	}

	public static void SetLastAccessTime(string path, DateTime lastAccessTime)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetLastAccessTime(fullPath, lastAccessTime, asDirectory: true);
	}

	public static void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetLastAccessTime(fullPath, File.GetUtcDateTimeOffset(lastAccessTimeUtc), asDirectory: true);
	}

	public static DateTime GetLastAccessTime(string path)
	{
		return File.GetLastAccessTime(path);
	}

	public static DateTime GetLastAccessTimeUtc(string path)
	{
		return File.GetLastAccessTimeUtc(path);
	}

	public static string[] GetFiles(string path)
	{
		return GetFiles(path, "*", EnumerationOptions.Compatible);
	}

	public static string[] GetFiles(string path, string searchPattern)
	{
		return GetFiles(path, searchPattern, EnumerationOptions.Compatible);
	}

	public static string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
	{
		return GetFiles(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));
	}

	public static string[] GetFiles(string path, string searchPattern, EnumerationOptions enumerationOptions)
	{
		return new List<string>(InternalEnumeratePaths(path, searchPattern, SearchTarget.Files, enumerationOptions)).ToArray();
	}

	public static string[] GetDirectories(string path)
	{
		return GetDirectories(path, "*", EnumerationOptions.Compatible);
	}

	public static string[] GetDirectories(string path, string searchPattern)
	{
		return GetDirectories(path, searchPattern, EnumerationOptions.Compatible);
	}

	public static string[] GetDirectories(string path, string searchPattern, SearchOption searchOption)
	{
		return GetDirectories(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));
	}

	public static string[] GetDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions)
	{
		return new List<string>(InternalEnumeratePaths(path, searchPattern, SearchTarget.Directories, enumerationOptions)).ToArray();
	}

	public static string[] GetFileSystemEntries(string path)
	{
		return GetFileSystemEntries(path, "*", EnumerationOptions.Compatible);
	}

	public static string[] GetFileSystemEntries(string path, string searchPattern)
	{
		return GetFileSystemEntries(path, searchPattern, EnumerationOptions.Compatible);
	}

	public static string[] GetFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
	{
		return GetFileSystemEntries(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));
	}

	public static string[] GetFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions)
	{
		return new List<string>(InternalEnumeratePaths(path, searchPattern, SearchTarget.Both, enumerationOptions)).ToArray();
	}

	internal static IEnumerable<string> InternalEnumeratePaths(string path, string searchPattern, SearchTarget searchTarget, EnumerationOptions options)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (searchPattern == null)
		{
			throw new ArgumentNullException("searchPattern");
		}
		FileSystemEnumerableFactory.NormalizeInputs(ref path, ref searchPattern, options.MatchType);
		return searchTarget switch
		{
			SearchTarget.Files => FileSystemEnumerableFactory.UserFiles(path, searchPattern, options), 
			SearchTarget.Directories => FileSystemEnumerableFactory.UserDirectories(path, searchPattern, options), 
			SearchTarget.Both => FileSystemEnumerableFactory.UserEntries(path, searchPattern, options), 
			_ => throw new ArgumentOutOfRangeException("searchTarget"), 
		};
	}

	public static IEnumerable<string> EnumerateDirectories(string path)
	{
		return EnumerateDirectories(path, "*", EnumerationOptions.Compatible);
	}

	public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern)
	{
		return EnumerateDirectories(path, searchPattern, EnumerationOptions.Compatible);
	}

	public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
	{
		return EnumerateDirectories(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));
	}

	public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions)
	{
		return InternalEnumeratePaths(path, searchPattern, SearchTarget.Directories, enumerationOptions);
	}

	public static IEnumerable<string> EnumerateFiles(string path)
	{
		return EnumerateFiles(path, "*", EnumerationOptions.Compatible);
	}

	public static IEnumerable<string> EnumerateFiles(string path, string searchPattern)
	{
		return EnumerateFiles(path, searchPattern, EnumerationOptions.Compatible);
	}

	public static IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
	{
		return EnumerateFiles(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));
	}

	public static IEnumerable<string> EnumerateFiles(string path, string searchPattern, EnumerationOptions enumerationOptions)
	{
		return InternalEnumeratePaths(path, searchPattern, SearchTarget.Files, enumerationOptions);
	}

	public static IEnumerable<string> EnumerateFileSystemEntries(string path)
	{
		return EnumerateFileSystemEntries(path, "*", EnumerationOptions.Compatible);
	}

	public static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern)
	{
		return EnumerateFileSystemEntries(path, searchPattern, EnumerationOptions.Compatible);
	}

	public static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
	{
		return EnumerateFileSystemEntries(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));
	}

	public static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions)
	{
		return InternalEnumeratePaths(path, searchPattern, SearchTarget.Both, enumerationOptions);
	}

	public static string GetDirectoryRoot(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		string fullPath = Path.GetFullPath(path);
		return Path.GetPathRoot(fullPath);
	}

	public static string GetCurrentDirectory()
	{
		return Environment.CurrentDirectory;
	}

	public static void SetCurrentDirectory(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_PathEmpty, "path");
		}
		Environment.CurrentDirectory = Path.GetFullPath(path);
	}

	public static void Move(string sourceDirName, string destDirName)
	{
		if (sourceDirName == null)
		{
			throw new ArgumentNullException("sourceDirName");
		}
		if (sourceDirName.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyFileName, "sourceDirName");
		}
		if (destDirName == null)
		{
			throw new ArgumentNullException("destDirName");
		}
		if (destDirName.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyFileName, "destDirName");
		}
		string fullPath = Path.GetFullPath(sourceDirName);
		string text = PathInternal.EnsureTrailingSeparator(fullPath);
		string fullPath2 = Path.GetFullPath(destDirName);
		string text2 = PathInternal.EnsureTrailingSeparator(fullPath2);
		ReadOnlySpan<char> fileName = Path.GetFileName(fullPath.AsSpan());
		ReadOnlySpan<char> fileName2 = Path.GetFileName(fullPath2.AsSpan());
		StringComparison stringComparison = PathInternal.StringComparison;
		bool flag = !fileName.SequenceEqual(fileName2) && MemoryExtensions.Equals(fileName, fileName2, StringComparison.OrdinalIgnoreCase) && MemoryExtensions.Equals(fileName2, fileName, stringComparison);
		if (!flag && string.Equals(text, text2, stringComparison))
		{
			throw new IOException(SR.IO_SourceDestMustBeDifferent);
		}
		ReadOnlySpan<char> pathRoot = Path.GetPathRoot(text.AsSpan());
		ReadOnlySpan<char> pathRoot2 = Path.GetPathRoot(text2.AsSpan());
		if (!MemoryExtensions.Equals(pathRoot, pathRoot2, StringComparison.OrdinalIgnoreCase))
		{
			throw new IOException(SR.IO_SourceDestMustHaveSameRoot);
		}
		if (!FileSystem.DirectoryExists(fullPath) && !FileSystem.FileExists(fullPath))
		{
			throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, fullPath));
		}
		if (!flag && FileSystem.DirectoryExists(fullPath2))
		{
			throw new IOException(SR.Format(SR.IO_AlreadyExists_Name, fullPath2));
		}
		if (!flag && Exists(fullPath2))
		{
			throw new IOException(SR.Format(SR.IO_AlreadyExists_Name, fullPath2));
		}
		FileSystem.MoveDirectory(fullPath, fullPath2);
	}

	public static void Delete(string path)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.RemoveDirectory(fullPath, recursive: false);
	}

	public static void Delete(string path, bool recursive)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.RemoveDirectory(fullPath, recursive);
	}

	public static string[] GetLogicalDrives()
	{
		return FileSystem.GetLogicalDrives();
	}

	public static FileSystemInfo CreateSymbolicLink(string path, string pathToTarget)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.VerifyValidPath(pathToTarget, "pathToTarget");
		FileSystem.CreateSymbolicLink(path, pathToTarget, isDirectory: true);
		return new DirectoryInfo(path, fullPath, null, isNormalized: true);
	}

	public static FileSystemInfo? ResolveLinkTarget(string linkPath, bool returnFinalTarget)
	{
		FileSystem.VerifyValidPath(linkPath, "linkPath");
		return FileSystem.ResolveLinkTarget(linkPath, returnFinalTarget, isDirectory: true);
	}
}
