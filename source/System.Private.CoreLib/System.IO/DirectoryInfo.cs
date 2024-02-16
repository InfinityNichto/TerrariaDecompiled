using System.Collections.Generic;
using System.IO.Enumeration;

namespace System.IO;

public sealed class DirectoryInfo : FileSystemInfo
{
	private bool _isNormalized;

	public DirectoryInfo? Parent
	{
		get
		{
			string directoryName = Path.GetDirectoryName(PathInternal.IsRoot(FullPath.AsSpan()) ? FullPath : Path.TrimEndingDirectorySeparator(FullPath));
			if (directoryName == null)
			{
				return null;
			}
			return new DirectoryInfo(directoryName, null, null, isNormalized: true);
		}
	}

	public DirectoryInfo Root => new DirectoryInfo(Path.GetPathRoot(FullPath));

	public DirectoryInfo(string path)
	{
		Init(path, Path.GetFullPath(path), null, isNormalized: true);
	}

	internal DirectoryInfo(string originalPath, string fullPath = null, string fileName = null, bool isNormalized = false)
	{
		Init(originalPath, fullPath, fileName, isNormalized);
	}

	private void Init(string originalPath, string fullPath = null, string fileName = null, bool isNormalized = false)
	{
		OriginalPath = originalPath ?? throw new ArgumentNullException("originalPath");
		fullPath = fullPath ?? originalPath;
		fullPath = (isNormalized ? fullPath : Path.GetFullPath(fullPath));
		_name = fileName ?? (PathInternal.IsRoot(fullPath.AsSpan()) ? fullPath.AsSpan() : Path.GetFileName(Path.TrimEndingDirectorySeparator(fullPath.AsSpan()))).ToString();
		FullPath = fullPath;
		_isNormalized = isNormalized;
	}

	public DirectoryInfo CreateSubdirectory(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (PathInternal.IsEffectivelyEmpty(path.AsSpan()))
		{
			throw new ArgumentException(SR.Argument_PathEmpty, "path");
		}
		if (Path.IsPathRooted(path))
		{
			throw new ArgumentException(SR.Arg_Path2IsRooted, "path");
		}
		string fullPath = Path.GetFullPath(Path.Combine(FullPath, path));
		ReadOnlySpan<char> span = Path.TrimEndingDirectorySeparator(fullPath.AsSpan());
		ReadOnlySpan<char> value = Path.TrimEndingDirectorySeparator(FullPath.AsSpan());
		if (span.StartsWith(value, PathInternal.StringComparison) && (span.Length == value.Length || PathInternal.IsDirectorySeparator(fullPath[value.Length])))
		{
			FileSystem.CreateDirectory(fullPath);
			return new DirectoryInfo(fullPath);
		}
		throw new ArgumentException(SR.Format(SR.Argument_InvalidSubPath, path, FullPath), "path");
	}

	public void Create()
	{
		FileSystem.CreateDirectory(FullPath);
		Invalidate();
	}

	public FileInfo[] GetFiles()
	{
		return GetFiles("*", EnumerationOptions.Compatible);
	}

	public FileInfo[] GetFiles(string searchPattern)
	{
		return GetFiles(searchPattern, EnumerationOptions.Compatible);
	}

	public FileInfo[] GetFiles(string searchPattern, SearchOption searchOption)
	{
		return GetFiles(searchPattern, EnumerationOptions.FromSearchOption(searchOption));
	}

	public FileInfo[] GetFiles(string searchPattern, EnumerationOptions enumerationOptions)
	{
		return new List<FileInfo>((IEnumerable<FileInfo>)InternalEnumerateInfos(FullPath, searchPattern, SearchTarget.Files, enumerationOptions)).ToArray();
	}

	public FileSystemInfo[] GetFileSystemInfos()
	{
		return GetFileSystemInfos("*", EnumerationOptions.Compatible);
	}

	public FileSystemInfo[] GetFileSystemInfos(string searchPattern)
	{
		return GetFileSystemInfos(searchPattern, EnumerationOptions.Compatible);
	}

	public FileSystemInfo[] GetFileSystemInfos(string searchPattern, SearchOption searchOption)
	{
		return GetFileSystemInfos(searchPattern, EnumerationOptions.FromSearchOption(searchOption));
	}

	public FileSystemInfo[] GetFileSystemInfos(string searchPattern, EnumerationOptions enumerationOptions)
	{
		return new List<FileSystemInfo>(InternalEnumerateInfos(FullPath, searchPattern, SearchTarget.Both, enumerationOptions)).ToArray();
	}

	public DirectoryInfo[] GetDirectories()
	{
		return GetDirectories("*", EnumerationOptions.Compatible);
	}

	public DirectoryInfo[] GetDirectories(string searchPattern)
	{
		return GetDirectories(searchPattern, EnumerationOptions.Compatible);
	}

	public DirectoryInfo[] GetDirectories(string searchPattern, SearchOption searchOption)
	{
		return GetDirectories(searchPattern, EnumerationOptions.FromSearchOption(searchOption));
	}

	public DirectoryInfo[] GetDirectories(string searchPattern, EnumerationOptions enumerationOptions)
	{
		return new List<DirectoryInfo>((IEnumerable<DirectoryInfo>)InternalEnumerateInfos(FullPath, searchPattern, SearchTarget.Directories, enumerationOptions)).ToArray();
	}

	public IEnumerable<DirectoryInfo> EnumerateDirectories()
	{
		return EnumerateDirectories("*", EnumerationOptions.Compatible);
	}

	public IEnumerable<DirectoryInfo> EnumerateDirectories(string searchPattern)
	{
		return EnumerateDirectories(searchPattern, EnumerationOptions.Compatible);
	}

	public IEnumerable<DirectoryInfo> EnumerateDirectories(string searchPattern, SearchOption searchOption)
	{
		return EnumerateDirectories(searchPattern, EnumerationOptions.FromSearchOption(searchOption));
	}

	public IEnumerable<DirectoryInfo> EnumerateDirectories(string searchPattern, EnumerationOptions enumerationOptions)
	{
		return (IEnumerable<DirectoryInfo>)InternalEnumerateInfos(FullPath, searchPattern, SearchTarget.Directories, enumerationOptions);
	}

	public IEnumerable<FileInfo> EnumerateFiles()
	{
		return EnumerateFiles("*", EnumerationOptions.Compatible);
	}

	public IEnumerable<FileInfo> EnumerateFiles(string searchPattern)
	{
		return EnumerateFiles(searchPattern, EnumerationOptions.Compatible);
	}

	public IEnumerable<FileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption)
	{
		return EnumerateFiles(searchPattern, EnumerationOptions.FromSearchOption(searchOption));
	}

	public IEnumerable<FileInfo> EnumerateFiles(string searchPattern, EnumerationOptions enumerationOptions)
	{
		return (IEnumerable<FileInfo>)InternalEnumerateInfos(FullPath, searchPattern, SearchTarget.Files, enumerationOptions);
	}

	public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos()
	{
		return EnumerateFileSystemInfos("*", EnumerationOptions.Compatible);
	}

	public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(string searchPattern)
	{
		return EnumerateFileSystemInfos(searchPattern, EnumerationOptions.Compatible);
	}

	public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(string searchPattern, SearchOption searchOption)
	{
		return EnumerateFileSystemInfos(searchPattern, EnumerationOptions.FromSearchOption(searchOption));
	}

	public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(string searchPattern, EnumerationOptions enumerationOptions)
	{
		return InternalEnumerateInfos(FullPath, searchPattern, SearchTarget.Both, enumerationOptions);
	}

	private IEnumerable<FileSystemInfo> InternalEnumerateInfos(string path, string searchPattern, SearchTarget searchTarget, EnumerationOptions options)
	{
		if (searchPattern == null)
		{
			throw new ArgumentNullException("searchPattern");
		}
		_isNormalized &= FileSystemEnumerableFactory.NormalizeInputs(ref path, ref searchPattern, options.MatchType);
		return searchTarget switch
		{
			SearchTarget.Directories => FileSystemEnumerableFactory.DirectoryInfos(path, searchPattern, options, _isNormalized), 
			SearchTarget.Files => FileSystemEnumerableFactory.FileInfos(path, searchPattern, options, _isNormalized), 
			SearchTarget.Both => FileSystemEnumerableFactory.FileSystemInfos(path, searchPattern, options, _isNormalized), 
			_ => throw new ArgumentException(SR.ArgumentOutOfRange_Enum, "searchTarget"), 
		};
	}

	public void MoveTo(string destDirName)
	{
		if (destDirName == null)
		{
			throw new ArgumentNullException("destDirName");
		}
		if (destDirName.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyFileName, "destDirName");
		}
		string fullPath = Path.GetFullPath(destDirName);
		string text = PathInternal.EnsureTrailingSeparator(fullPath);
		string text2 = PathInternal.EnsureTrailingSeparator(FullPath);
		if (string.Equals(text2, text, PathInternal.StringComparison))
		{
			throw new IOException(SR.IO_SourceDestMustBeDifferent);
		}
		string pathRoot = Path.GetPathRoot(text2);
		string pathRoot2 = Path.GetPathRoot(text);
		if (!string.Equals(pathRoot, pathRoot2, PathInternal.StringComparison))
		{
			throw new IOException(SR.IO_SourceDestMustHaveSameRoot);
		}
		if (!Exists && !FileSystem.FileExists(FullPath))
		{
			throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, FullPath));
		}
		if (FileSystem.DirectoryExists(fullPath))
		{
			throw new IOException(SR.Format(SR.IO_AlreadyExists_Name, text));
		}
		FileSystem.MoveDirectory(FullPath, fullPath);
		Init(destDirName, text, null, isNormalized: true);
		Invalidate();
	}

	public override void Delete()
	{
		Delete(recursive: false);
	}

	public void Delete(bool recursive)
	{
		FileSystem.RemoveDirectory(FullPath, recursive);
		Invalidate();
	}
}
