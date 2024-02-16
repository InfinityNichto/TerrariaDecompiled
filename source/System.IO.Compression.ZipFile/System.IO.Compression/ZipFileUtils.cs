using System.Buffers;
using System.Collections.Generic;

namespace System.IO.Compression;

internal static class ZipFileUtils
{
	public static string EntryFromPath(string entry, int offset, int length, ref char[] buffer, bool appendPathSeparator = false)
	{
		while (length > 0 && (entry[offset] == Path.DirectorySeparatorChar || entry[offset] == Path.AltDirectorySeparatorChar))
		{
			offset++;
			length--;
		}
		if (length == 0)
		{
			if (!appendPathSeparator)
			{
				return string.Empty;
			}
			return "/";
		}
		int num = (appendPathSeparator ? (length + 1) : length);
		EnsureCapacity(ref buffer, num);
		entry.CopyTo(offset, buffer, 0, length);
		for (int i = 0; i < length; i++)
		{
			char c = buffer[i];
			if (c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar)
			{
				buffer[i] = '/';
			}
		}
		if (appendPathSeparator)
		{
			buffer[length] = '/';
		}
		return new string(buffer, 0, num);
	}

	public static void EnsureCapacity(ref char[] buffer, int min)
	{
		if (buffer.Length < min)
		{
			int num = buffer.Length * 2;
			if (num < min)
			{
				num = min;
			}
			char[] array = buffer;
			buffer = ArrayPool<char>.Shared.Rent(num);
			ArrayPool<char>.Shared.Return(array);
		}
	}

	public static bool IsDirEmpty(DirectoryInfo possiblyEmptyDir)
	{
		using IEnumerator<string> enumerator = Directory.EnumerateFileSystemEntries(possiblyEmptyDir.FullName).GetEnumerator();
		return !enumerator.MoveNext();
	}
}
