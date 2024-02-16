using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Strategies;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO;

public static class File
{
	private static Encoding s_UTF8NoBOM;

	private static Encoding UTF8NoBOM => s_UTF8NoBOM ?? (s_UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));

	public static StreamReader OpenText(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		return new StreamReader(path);
	}

	public static StreamWriter CreateText(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		return new StreamWriter(path, append: false);
	}

	public static StreamWriter AppendText(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		return new StreamWriter(path, append: true);
	}

	public static void Copy(string sourceFileName, string destFileName)
	{
		Copy(sourceFileName, destFileName, overwrite: false);
	}

	public static void Copy(string sourceFileName, string destFileName, bool overwrite)
	{
		if (sourceFileName == null)
		{
			throw new ArgumentNullException("sourceFileName", SR.ArgumentNull_FileName);
		}
		if (destFileName == null)
		{
			throw new ArgumentNullException("destFileName", SR.ArgumentNull_FileName);
		}
		if (sourceFileName.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyFileName, "sourceFileName");
		}
		if (destFileName.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyFileName, "destFileName");
		}
		FileSystem.CopyFile(Path.GetFullPath(sourceFileName), Path.GetFullPath(destFileName), overwrite);
	}

	public static FileStream Create(string path)
	{
		return Create(path, 4096);
	}

	public static FileStream Create(string path, int bufferSize)
	{
		return new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize);
	}

	public static FileStream Create(string path, int bufferSize, FileOptions options)
	{
		return new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize, options);
	}

	public static void Delete(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		FileSystem.DeleteFile(Path.GetFullPath(path));
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
			path = Path.GetFullPath(path);
			if (path.Length > 0 && PathInternal.IsDirectorySeparator(path[path.Length - 1]))
			{
				return false;
			}
			return FileSystem.FileExists(path);
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

	public static FileStream Open(string path, FileMode mode)
	{
		return Open(path, mode, (mode == FileMode.Append) ? FileAccess.Write : FileAccess.ReadWrite, FileShare.None);
	}

	public static FileStream Open(string path, FileMode mode, FileAccess access)
	{
		return Open(path, mode, access, FileShare.None);
	}

	public static FileStream Open(string path, FileMode mode, FileAccess access, FileShare share)
	{
		return new FileStream(path, mode, access, share);
	}

	internal static DateTimeOffset GetUtcDateTimeOffset(DateTime dateTime)
	{
		if (dateTime.Kind == DateTimeKind.Unspecified)
		{
			return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
		}
		return dateTime.ToUniversalTime();
	}

	public static void SetCreationTime(string path, DateTime creationTime)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetCreationTime(fullPath, creationTime, asDirectory: false);
	}

	public static void SetCreationTimeUtc(string path, DateTime creationTimeUtc)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetCreationTime(fullPath, GetUtcDateTimeOffset(creationTimeUtc), asDirectory: false);
	}

	public static DateTime GetCreationTime(string path)
	{
		string fullPath = Path.GetFullPath(path);
		return FileSystem.GetCreationTime(fullPath).LocalDateTime;
	}

	public static DateTime GetCreationTimeUtc(string path)
	{
		string fullPath = Path.GetFullPath(path);
		return FileSystem.GetCreationTime(fullPath).UtcDateTime;
	}

	public static void SetLastAccessTime(string path, DateTime lastAccessTime)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetLastAccessTime(fullPath, lastAccessTime, asDirectory: false);
	}

	public static void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetLastAccessTime(fullPath, GetUtcDateTimeOffset(lastAccessTimeUtc), asDirectory: false);
	}

	public static DateTime GetLastAccessTime(string path)
	{
		string fullPath = Path.GetFullPath(path);
		return FileSystem.GetLastAccessTime(fullPath).LocalDateTime;
	}

	public static DateTime GetLastAccessTimeUtc(string path)
	{
		string fullPath = Path.GetFullPath(path);
		return FileSystem.GetLastAccessTime(fullPath).UtcDateTime;
	}

	public static void SetLastWriteTime(string path, DateTime lastWriteTime)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetLastWriteTime(fullPath, lastWriteTime, asDirectory: false);
	}

	public static void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetLastWriteTime(fullPath, GetUtcDateTimeOffset(lastWriteTimeUtc), asDirectory: false);
	}

	public static DateTime GetLastWriteTime(string path)
	{
		string fullPath = Path.GetFullPath(path);
		return FileSystem.GetLastWriteTime(fullPath).LocalDateTime;
	}

	public static DateTime GetLastWriteTimeUtc(string path)
	{
		string fullPath = Path.GetFullPath(path);
		return FileSystem.GetLastWriteTime(fullPath).UtcDateTime;
	}

	public static FileAttributes GetAttributes(string path)
	{
		string fullPath = Path.GetFullPath(path);
		return FileSystem.GetAttributes(fullPath);
	}

	public static void SetAttributes(string path, FileAttributes fileAttributes)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.SetAttributes(fullPath, fileAttributes);
	}

	public static FileStream OpenRead(string path)
	{
		return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
	}

	public static FileStream OpenWrite(string path)
	{
		return new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
	}

	public static string ReadAllText(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		return InternalReadAllText(path, Encoding.UTF8);
	}

	public static string ReadAllText(string path, Encoding encoding)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		return InternalReadAllText(path, encoding);
	}

	private static string InternalReadAllText(string path, Encoding encoding)
	{
		using StreamReader streamReader = new StreamReader(path, encoding, detectEncodingFromByteOrderMarks: true);
		return streamReader.ReadToEnd();
	}

	public static void WriteAllText(string path, string? contents)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		using StreamWriter streamWriter = new StreamWriter(path);
		streamWriter.Write(contents);
	}

	public static void WriteAllText(string path, string? contents, Encoding encoding)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		using StreamWriter streamWriter = new StreamWriter(path, append: false, encoding);
		streamWriter.Write(contents);
	}

	public static byte[] ReadAllBytes(string path)
	{
		using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1, FileOptions.SequentialScan);
		long length = fileStream.Length;
		if (length > int.MaxValue)
		{
			throw new IOException(SR.IO_FileTooLong2GB);
		}
		if (length == 0L)
		{
			return ReadAllBytesUnknownLength(fileStream);
		}
		int num = 0;
		int num2 = (int)length;
		byte[] array = new byte[num2];
		while (num2 > 0)
		{
			int num3 = fileStream.Read(array, num, num2);
			if (num3 == 0)
			{
				ThrowHelper.ThrowEndOfFileException();
			}
			num += num3;
			num2 -= num3;
		}
		return array;
	}

	public static void WriteAllBytes(string path, byte[] bytes)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path", SR.ArgumentNull_Path);
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes");
		}
		using SafeFileHandle handle = OpenHandle(path, FileMode.Create, FileAccess.Write, FileShare.Read, FileOptions.None, 0L);
		RandomAccess.WriteAtOffset(handle, bytes, 0L);
	}

	public static string[] ReadAllLines(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		return InternalReadAllLines(path, Encoding.UTF8);
	}

	public static string[] ReadAllLines(string path, Encoding encoding)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		return InternalReadAllLines(path, encoding);
	}

	private static string[] InternalReadAllLines(string path, Encoding encoding)
	{
		List<string> list = new List<string>();
		using (StreamReader streamReader = new StreamReader(path, encoding))
		{
			string item;
			while ((item = streamReader.ReadLine()) != null)
			{
				list.Add(item);
			}
		}
		return list.ToArray();
	}

	public static IEnumerable<string> ReadLines(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		return ReadLinesIterator.CreateIterator(path, Encoding.UTF8);
	}

	public static IEnumerable<string> ReadLines(string path, Encoding encoding)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		return ReadLinesIterator.CreateIterator(path, encoding);
	}

	public static void WriteAllLines(string path, string[] contents)
	{
		WriteAllLines(path, (IEnumerable<string>)contents);
	}

	public static void WriteAllLines(string path, IEnumerable<string> contents)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (contents == null)
		{
			throw new ArgumentNullException("contents");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		InternalWriteAllLines(new StreamWriter(path), contents);
	}

	public static void WriteAllLines(string path, string[] contents, Encoding encoding)
	{
		WriteAllLines(path, (IEnumerable<string>)contents, encoding);
	}

	public static void WriteAllLines(string path, IEnumerable<string> contents, Encoding encoding)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (contents == null)
		{
			throw new ArgumentNullException("contents");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		InternalWriteAllLines(new StreamWriter(path, append: false, encoding), contents);
	}

	private static void InternalWriteAllLines(TextWriter writer, IEnumerable<string> contents)
	{
		using (writer)
		{
			foreach (string content in contents)
			{
				writer.WriteLine(content);
			}
		}
	}

	public static void AppendAllText(string path, string? contents)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		using StreamWriter streamWriter = new StreamWriter(path, append: true);
		streamWriter.Write(contents);
	}

	public static void AppendAllText(string path, string? contents, Encoding encoding)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		using StreamWriter streamWriter = new StreamWriter(path, append: true, encoding);
		streamWriter.Write(contents);
	}

	public static void AppendAllLines(string path, IEnumerable<string> contents)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (contents == null)
		{
			throw new ArgumentNullException("contents");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		InternalWriteAllLines(new StreamWriter(path, append: true), contents);
	}

	public static void AppendAllLines(string path, IEnumerable<string> contents, Encoding encoding)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (contents == null)
		{
			throw new ArgumentNullException("contents");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		InternalWriteAllLines(new StreamWriter(path, append: true, encoding), contents);
	}

	public static void Replace(string sourceFileName, string destinationFileName, string? destinationBackupFileName)
	{
		Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors: false);
	}

	public static void Replace(string sourceFileName, string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors)
	{
		if (sourceFileName == null)
		{
			throw new ArgumentNullException("sourceFileName");
		}
		if (destinationFileName == null)
		{
			throw new ArgumentNullException("destinationFileName");
		}
		FileSystem.ReplaceFile(Path.GetFullPath(sourceFileName), Path.GetFullPath(destinationFileName), (destinationBackupFileName != null) ? Path.GetFullPath(destinationBackupFileName) : null, ignoreMetadataErrors);
	}

	public static void Move(string sourceFileName, string destFileName)
	{
		Move(sourceFileName, destFileName, overwrite: false);
	}

	public static void Move(string sourceFileName, string destFileName, bool overwrite)
	{
		if (sourceFileName == null)
		{
			throw new ArgumentNullException("sourceFileName", SR.ArgumentNull_FileName);
		}
		if (destFileName == null)
		{
			throw new ArgumentNullException("destFileName", SR.ArgumentNull_FileName);
		}
		if (sourceFileName.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyFileName, "sourceFileName");
		}
		if (destFileName.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyFileName, "destFileName");
		}
		string fullPath = Path.GetFullPath(sourceFileName);
		string fullPath2 = Path.GetFullPath(destFileName);
		if (!FileSystem.FileExists(fullPath))
		{
			throw new FileNotFoundException(SR.Format(SR.IO_FileNotFound_FileName, fullPath), fullPath);
		}
		FileSystem.MoveFile(fullPath, fullPath2, overwrite);
	}

	[SupportedOSPlatform("windows")]
	public static void Encrypt(string path)
	{
		FileSystem.Encrypt(path ?? throw new ArgumentNullException("path"));
	}

	[SupportedOSPlatform("windows")]
	public static void Decrypt(string path)
	{
		FileSystem.Decrypt(path ?? throw new ArgumentNullException("path"));
	}

	private static StreamReader AsyncStreamReader(string path, Encoding encoding)
	{
		FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
		return new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true);
	}

	private static StreamWriter AsyncStreamWriter(string path, Encoding encoding, bool append)
	{
		FileStream stream = new FileStream(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
		return new StreamWriter(stream, encoding);
	}

	public static Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
	{
		return ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
	}

	public static Task<string> ReadAllTextAsync(string path, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		if (!cancellationToken.IsCancellationRequested)
		{
			return InternalReadAllTextAsync(path, encoding, cancellationToken);
		}
		return Task.FromCanceled<string>(cancellationToken);
	}

	private static async Task<string> InternalReadAllTextAsync(string path, Encoding encoding, CancellationToken cancellationToken)
	{
		char[] buffer = null;
		StreamReader sr = AsyncStreamReader(path, encoding);
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			buffer = ArrayPool<char>.Shared.Rent(sr.CurrentEncoding.GetMaxCharCount(4096));
			StringBuilder sb = new StringBuilder();
			while (true)
			{
				int num = await sr.ReadAsync(new Memory<char>(buffer), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (num == 0)
				{
					break;
				}
				sb.Append(buffer, 0, num);
			}
			return sb.ToString();
		}
		finally
		{
			sr.Dispose();
			if (buffer != null)
			{
				ArrayPool<char>.Shared.Return(buffer);
			}
		}
	}

	public static Task WriteAllTextAsync(string path, string? contents, CancellationToken cancellationToken = default(CancellationToken))
	{
		return WriteAllTextAsync(path, contents, UTF8NoBOM, cancellationToken);
	}

	public static Task WriteAllTextAsync(string path, string? contents, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		if (string.IsNullOrEmpty(contents))
		{
			new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read).Dispose();
			return Task.CompletedTask;
		}
		return InternalWriteAllTextAsync(AsyncStreamWriter(path, encoding, append: false), contents, cancellationToken);
	}

	public static Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<byte[]>(cancellationToken);
		}
		FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1, FileOptions.Asynchronous | FileOptions.SequentialScan);
		bool flag = false;
		try
		{
			long length = fileStream.Length;
			if (length > int.MaxValue)
			{
				IOException ex = new IOException(SR.IO_FileTooLong2GB);
				ExceptionDispatchInfo.SetCurrentStackTrace(ex);
				return Task.FromException<byte[]>(ex);
			}
			flag = true;
			return (length > 0) ? InternalReadAllBytesAsync(fileStream, (int)length, cancellationToken) : InternalReadAllBytesUnknownLengthAsync(fileStream, cancellationToken);
		}
		finally
		{
			if (!flag)
			{
				fileStream.Dispose();
			}
		}
	}

	private static async Task<byte[]> InternalReadAllBytesAsync(FileStream fs, int count, CancellationToken cancellationToken)
	{
		using (fs)
		{
			int index = 0;
			byte[] bytes = new byte[count];
			do
			{
				int num = await fs.ReadAsync(new Memory<byte>(bytes, index, count - index), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (num == 0)
				{
					ThrowHelper.ThrowEndOfFileException();
				}
				index += num;
			}
			while (index < count);
			return bytes;
		}
	}

	private static async Task<byte[]> InternalReadAllBytesUnknownLengthAsync(FileStream fs, CancellationToken cancellationToken)
	{
		byte[] rentedArray = ArrayPool<byte>.Shared.Rent(512);
		try
		{
			int bytesRead = 0;
			while (true)
			{
				if (bytesRead == rentedArray.Length)
				{
					uint num = (uint)(rentedArray.Length * 2);
					if (num > 2147483591)
					{
						num = (uint)Math.Max(2147483591, rentedArray.Length + 1);
					}
					byte[] array = ArrayPool<byte>.Shared.Rent((int)num);
					Buffer.BlockCopy(rentedArray, 0, array, 0, bytesRead);
					byte[] array2 = rentedArray;
					rentedArray = array;
					ArrayPool<byte>.Shared.Return(array2);
				}
				int num2 = await fs.ReadAsync(rentedArray.AsMemory(bytesRead), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (num2 == 0)
				{
					break;
				}
				bytesRead += num2;
			}
			return rentedArray.AsSpan(0, bytesRead).ToArray();
		}
		finally
		{
			fs.Dispose();
			ArrayPool<byte>.Shared.Return(rentedArray);
		}
	}

	public static Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (path == null)
		{
			throw new ArgumentNullException("path", SR.ArgumentNull_Path);
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes");
		}
		if (!cancellationToken.IsCancellationRequested)
		{
			return Core(path, bytes, cancellationToken);
		}
		return Task.FromCanceled(cancellationToken);
		static async Task Core(string path, byte[] bytes, CancellationToken cancellationToken)
		{
			using SafeFileHandle sfh = OpenHandle(path, FileMode.Create, FileAccess.Write, FileShare.Read, FileOptions.Asynchronous | FileOptions.SequentialScan, 0L);
			await RandomAccess.WriteAtOffsetAsync(sfh, bytes, 0L, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public static Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
	{
		return ReadAllLinesAsync(path, Encoding.UTF8, cancellationToken);
	}

	public static Task<string[]> ReadAllLinesAsync(string path, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		if (!cancellationToken.IsCancellationRequested)
		{
			return InternalReadAllLinesAsync(path, encoding, cancellationToken);
		}
		return Task.FromCanceled<string[]>(cancellationToken);
	}

	private static async Task<string[]> InternalReadAllLinesAsync(string path, Encoding encoding, CancellationToken cancellationToken)
	{
		using StreamReader sr = AsyncStreamReader(path, encoding);
		cancellationToken.ThrowIfCancellationRequested();
		List<string> lines = new List<string>();
		string item;
		while ((item = await sr.ReadLineAsync().ConfigureAwait(continueOnCapturedContext: false)) != null)
		{
			lines.Add(item);
			cancellationToken.ThrowIfCancellationRequested();
		}
		return lines.ToArray();
	}

	public static Task WriteAllLinesAsync(string path, IEnumerable<string> contents, CancellationToken cancellationToken = default(CancellationToken))
	{
		return WriteAllLinesAsync(path, contents, UTF8NoBOM, cancellationToken);
	}

	public static Task WriteAllLinesAsync(string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (contents == null)
		{
			throw new ArgumentNullException("contents");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		if (!cancellationToken.IsCancellationRequested)
		{
			return InternalWriteAllLinesAsync(AsyncStreamWriter(path, encoding, append: false), contents, cancellationToken);
		}
		return Task.FromCanceled(cancellationToken);
	}

	private static async Task InternalWriteAllLinesAsync(TextWriter writer, IEnumerable<string> contents, CancellationToken cancellationToken)
	{
		using (writer)
		{
			foreach (string content in contents)
			{
				cancellationToken.ThrowIfCancellationRequested();
				await writer.WriteLineAsync(content).ConfigureAwait(continueOnCapturedContext: false);
			}
			cancellationToken.ThrowIfCancellationRequested();
			await writer.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private static async Task InternalWriteAllTextAsync(StreamWriter sw, string contents, CancellationToken cancellationToken)
	{
		using (sw)
		{
			await sw.WriteAsync(contents.AsMemory(), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			await sw.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public static Task AppendAllTextAsync(string path, string? contents, CancellationToken cancellationToken = default(CancellationToken))
	{
		return AppendAllTextAsync(path, contents, UTF8NoBOM, cancellationToken);
	}

	public static Task AppendAllTextAsync(string path, string? contents, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		if (string.IsNullOrEmpty(contents))
		{
			new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read).Dispose();
			return Task.CompletedTask;
		}
		return InternalWriteAllTextAsync(AsyncStreamWriter(path, encoding, append: true), contents, cancellationToken);
	}

	public static Task AppendAllLinesAsync(string path, IEnumerable<string> contents, CancellationToken cancellationToken = default(CancellationToken))
	{
		return AppendAllLinesAsync(path, contents, UTF8NoBOM, cancellationToken);
	}

	public static Task AppendAllLinesAsync(string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (contents == null)
		{
			throw new ArgumentNullException("contents");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		if (!cancellationToken.IsCancellationRequested)
		{
			return InternalWriteAllLinesAsync(AsyncStreamWriter(path, encoding, append: true), contents, cancellationToken);
		}
		return Task.FromCanceled(cancellationToken);
	}

	public static FileSystemInfo CreateSymbolicLink(string path, string pathToTarget)
	{
		string fullPath = Path.GetFullPath(path);
		FileSystem.VerifyValidPath(pathToTarget, "pathToTarget");
		FileSystem.CreateSymbolicLink(path, pathToTarget, isDirectory: false);
		return new FileInfo(path, fullPath, null, isNormalized: true);
	}

	public static FileSystemInfo? ResolveLinkTarget(string linkPath, bool returnFinalTarget)
	{
		FileSystem.VerifyValidPath(linkPath, "linkPath");
		return FileSystem.ResolveLinkTarget(linkPath, returnFinalTarget, isDirectory: false);
	}

	public static FileStream Open(string path, FileStreamOptions options)
	{
		return new FileStream(path, options);
	}

	public static SafeFileHandle OpenHandle(string path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read, FileOptions options = FileOptions.None, long preallocationSize = 0L)
	{
		FileStreamHelpers.ValidateArguments(path, mode, access, share, 0, options, preallocationSize);
		return SafeFileHandle.Open(Path.GetFullPath(path), mode, access, share, options, preallocationSize);
	}

	private static byte[] ReadAllBytesUnknownLength(FileStream fs)
	{
		byte[] array = null;
		Span<byte> span = stackalloc byte[512];
		try
		{
			int num = 0;
			while (true)
			{
				if (num == span.Length)
				{
					uint num2 = (uint)(span.Length * 2);
					if (num2 > Array.MaxLength)
					{
						num2 = (uint)Math.Max(Array.MaxLength, span.Length + 1);
					}
					byte[] array2 = ArrayPool<byte>.Shared.Rent((int)num2);
					span.CopyTo(array2);
					byte[] array3 = array;
					span = (array = array2);
					if (array3 != null)
					{
						ArrayPool<byte>.Shared.Return(array3);
					}
				}
				int num3 = fs.Read(span.Slice(num));
				if (num3 == 0)
				{
					break;
				}
				num += num3;
			}
			return span.Slice(0, num).ToArray();
		}
		finally
		{
			if (array != null)
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}
	}
}
