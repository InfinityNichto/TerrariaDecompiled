using System.ComponentModel;

namespace System.IO.Compression;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ZipFileExtensions
{
	public static ZipArchiveEntry CreateEntryFromFile(this ZipArchive destination, string sourceFileName, string entryName)
	{
		return destination.DoCreateEntryFromFile(sourceFileName, entryName, null);
	}

	public static ZipArchiveEntry CreateEntryFromFile(this ZipArchive destination, string sourceFileName, string entryName, CompressionLevel compressionLevel)
	{
		return destination.DoCreateEntryFromFile(sourceFileName, entryName, compressionLevel);
	}

	internal static ZipArchiveEntry DoCreateEntryFromFile(this ZipArchive destination, string sourceFileName, string entryName, CompressionLevel? compressionLevel)
	{
		if (destination == null)
		{
			throw new ArgumentNullException("destination");
		}
		if (sourceFileName == null)
		{
			throw new ArgumentNullException("sourceFileName");
		}
		if (entryName == null)
		{
			throw new ArgumentNullException("entryName");
		}
		using FileStream fileStream = new FileStream(sourceFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: false);
		ZipArchiveEntry zipArchiveEntry = (compressionLevel.HasValue ? destination.CreateEntry(entryName, compressionLevel.Value) : destination.CreateEntry(entryName));
		DateTime dateTime = File.GetLastWriteTime(sourceFileName);
		if (dateTime.Year < 1980 || dateTime.Year > 2107)
		{
			dateTime = new DateTime(1980, 1, 1, 0, 0, 0);
		}
		zipArchiveEntry.LastWriteTime = dateTime;
		using (Stream destination2 = zipArchiveEntry.Open())
		{
			fileStream.CopyTo(destination2);
		}
		return zipArchiveEntry;
	}

	public static void ExtractToDirectory(this ZipArchive source, string destinationDirectoryName)
	{
		source.ExtractToDirectory(destinationDirectoryName, overwriteFiles: false);
	}

	public static void ExtractToDirectory(this ZipArchive source, string destinationDirectoryName, bool overwriteFiles)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (destinationDirectoryName == null)
		{
			throw new ArgumentNullException("destinationDirectoryName");
		}
		foreach (ZipArchiveEntry entry in source.Entries)
		{
			entry.ExtractRelativeToDirectory(destinationDirectoryName, overwriteFiles);
		}
	}

	public static void ExtractToFile(this ZipArchiveEntry source, string destinationFileName)
	{
		source.ExtractToFile(destinationFileName, overwrite: false);
	}

	public static void ExtractToFile(this ZipArchiveEntry source, string destinationFileName, bool overwrite)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (destinationFileName == null)
		{
			throw new ArgumentNullException("destinationFileName");
		}
		FileMode mode = ((!overwrite) ? FileMode.CreateNew : FileMode.Create);
		using (FileStream destination = new FileStream(destinationFileName, mode, FileAccess.Write, FileShare.None, 4096, useAsync: false))
		{
			using Stream stream = source.Open();
			stream.CopyTo(destination);
		}
		try
		{
			File.SetLastWriteTime(destinationFileName, source.LastWriteTime.DateTime);
		}
		catch (UnauthorizedAccessException)
		{
		}
	}

	internal static void ExtractRelativeToDirectory(this ZipArchiveEntry source, string destinationDirectoryName, bool overwrite)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (destinationDirectoryName == null)
		{
			throw new ArgumentNullException("destinationDirectoryName");
		}
		DirectoryInfo directoryInfo = Directory.CreateDirectory(destinationDirectoryName);
		string text = directoryInfo.FullName;
		if (!text.EndsWith(Path.DirectorySeparatorChar))
		{
			text += Path.DirectorySeparatorChar;
		}
		string fullPath = Path.GetFullPath(Path.Combine(text, source.FullName));
		if (!fullPath.StartsWith(text, System.IO.PathInternal.StringComparison))
		{
			throw new IOException(System.SR.IO_ExtractingResultsInOutside);
		}
		if (Path.GetFileName(fullPath).Length == 0)
		{
			if (source.Length != 0L)
			{
				throw new IOException(System.SR.IO_DirectoryNameWithData);
			}
			Directory.CreateDirectory(fullPath);
		}
		else
		{
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
			source.ExtractToFile(fullPath, overwrite);
		}
	}
}
