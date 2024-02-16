using System.Buffers;
using System.Text;

namespace System.IO.Compression;

public static class ZipFile
{
	public static ZipArchive OpenRead(string archiveFileName)
	{
		return Open(archiveFileName, ZipArchiveMode.Read);
	}

	public static ZipArchive Open(string archiveFileName, ZipArchiveMode mode)
	{
		return Open(archiveFileName, mode, null);
	}

	public static ZipArchive Open(string archiveFileName, ZipArchiveMode mode, Encoding? entryNameEncoding)
	{
		FileMode mode2;
		FileAccess access;
		FileShare share;
		switch (mode)
		{
		case ZipArchiveMode.Read:
			mode2 = FileMode.Open;
			access = FileAccess.Read;
			share = FileShare.Read;
			break;
		case ZipArchiveMode.Create:
			mode2 = FileMode.CreateNew;
			access = FileAccess.Write;
			share = FileShare.None;
			break;
		case ZipArchiveMode.Update:
			mode2 = FileMode.OpenOrCreate;
			access = FileAccess.ReadWrite;
			share = FileShare.None;
			break;
		default:
			throw new ArgumentOutOfRangeException("mode");
		}
		FileStream fileStream = new FileStream(archiveFileName, mode2, access, share, 4096, useAsync: false);
		try
		{
			return new ZipArchive(fileStream, mode, leaveOpen: false, entryNameEncoding);
		}
		catch
		{
			fileStream.Dispose();
			throw;
		}
	}

	public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
	{
		DoCreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, null, includeBaseDirectory: false, null);
	}

	public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, CompressionLevel compressionLevel, bool includeBaseDirectory)
	{
		DoCreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel, includeBaseDirectory, null);
	}

	public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, CompressionLevel compressionLevel, bool includeBaseDirectory, Encoding? entryNameEncoding)
	{
		DoCreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel, includeBaseDirectory, entryNameEncoding);
	}

	private static void DoCreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, CompressionLevel? compressionLevel, bool includeBaseDirectory, Encoding entryNameEncoding)
	{
		sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);
		destinationArchiveFileName = Path.GetFullPath(destinationArchiveFileName);
		using ZipArchive zipArchive = Open(destinationArchiveFileName, ZipArchiveMode.Create, entryNameEncoding);
		bool flag = true;
		DirectoryInfo directoryInfo = new DirectoryInfo(sourceDirectoryName);
		string fullName = directoryInfo.FullName;
		if (includeBaseDirectory && directoryInfo.Parent != null)
		{
			fullName = directoryInfo.Parent.FullName;
		}
		char[] buffer = ArrayPool<char>.Shared.Rent(260);
		try
		{
			foreach (FileSystemInfo item in directoryInfo.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
			{
				flag = false;
				int length = item.FullName.Length - fullName.Length;
				if (item is FileInfo)
				{
					string entryName = ZipFileUtils.EntryFromPath(item.FullName, fullName.Length, length, ref buffer);
					zipArchive.DoCreateEntryFromFile(item.FullName, entryName, compressionLevel);
				}
				else if (item is DirectoryInfo possiblyEmptyDir && ZipFileUtils.IsDirEmpty(possiblyEmptyDir))
				{
					string entryName2 = ZipFileUtils.EntryFromPath(item.FullName, fullName.Length, length, ref buffer, appendPathSeparator: true);
					zipArchive.CreateEntry(entryName2);
				}
			}
			if (includeBaseDirectory && flag)
			{
				zipArchive.CreateEntry(ZipFileUtils.EntryFromPath(directoryInfo.Name, 0, directoryInfo.Name.Length, ref buffer, appendPathSeparator: true));
			}
		}
		finally
		{
			ArrayPool<char>.Shared.Return(buffer);
		}
	}

	public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName)
	{
		ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, null, overwriteFiles: false);
	}

	public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, bool overwriteFiles)
	{
		ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, null, overwriteFiles);
	}

	public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, Encoding? entryNameEncoding)
	{
		ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, entryNameEncoding, overwriteFiles: false);
	}

	public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, Encoding? entryNameEncoding, bool overwriteFiles)
	{
		if (sourceArchiveFileName == null)
		{
			throw new ArgumentNullException("sourceArchiveFileName");
		}
		using ZipArchive source = Open(sourceArchiveFileName, ZipArchiveMode.Read, entryNameEncoding);
		source.ExtractToDirectory(destinationDirectoryName, overwriteFiles);
	}
}
