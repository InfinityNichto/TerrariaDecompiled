using System;
using System.Globalization;
using System.IO;

namespace Microsoft.Xna.Framework;

public static class TitleContainer
{
	private static char[] badCharacters = new char[7] { ':', '*', '?', '"', '<', '>', '|' };

	public static Stream OpenStream(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		name = GetCleanPath(name);
		if (IsCleanPathAbsolute(name))
		{
			throw new ArgumentException(FrameworkResources.InvalidTitleContainerName);
		}
		try
		{
			string uriString = name.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			new Uri(uriString, UriKind.Relative);
		}
		catch (Exception innerException)
		{
			throw new ArgumentException(FrameworkResources.InvalidTitleContainerName, innerException);
		}
		try
		{
			string path = Path.Combine(TitleLocation.Path, name);
			return File.OpenRead(path);
		}
		catch (Exception ex)
		{
			if (ex is FileNotFoundException || ex is DirectoryNotFoundException || ex is ArgumentException)
			{
				throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, FrameworkResources.OpenStreamNotFound, new object[1] { name }));
			}
			throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, FrameworkResources.OpenStreamError, new object[1] { name }), ex);
		}
	}

	internal static bool IsPathAbsolute(string path)
	{
		path = GetCleanPath(path);
		return IsCleanPathAbsolute(path);
	}

	internal static string GetCleanPath(string path)
	{
		path = path.Replace('/', '\\');
		path = path.Replace("\\.\\", "\\");
		while (path.StartsWith(".\\"))
		{
			path = path.Substring(".\\".Length);
		}
		while (path.EndsWith("\\."))
		{
			path = ((path.Length <= "\\.".Length) ? "\\" : path.Substring(0, path.Length - "\\.".Length));
		}
		int num;
		for (num = 1; num < path.Length; num = CollapseParentDirectory(ref path, num, "\\..\\".Length))
		{
			num = path.IndexOf("\\..\\", num);
			if (num < 0)
			{
				break;
			}
		}
		if (path.EndsWith("\\.."))
		{
			num = path.Length - "\\..".Length;
			if (num > 0)
			{
				CollapseParentDirectory(ref path, num, "\\..".Length);
			}
		}
		if (path == ".")
		{
			path = string.Empty;
		}
		return path;
	}

	private static int CollapseParentDirectory(ref string path, int position, int removeLength)
	{
		int num = path.LastIndexOf('\\', position - 1) + 1;
		path = path.Remove(num, position - num + removeLength);
		return Math.Max(num - 1, 1);
	}

	private static bool IsCleanPathAbsolute(string path)
	{
		if (path.IndexOfAny(badCharacters) >= 0)
		{
			return true;
		}
		if (path.StartsWith("\\"))
		{
			return true;
		}
		if (path.StartsWith("..\\") || path.Contains("\\..\\") || path.EndsWith("\\..") || path == "..")
		{
			return true;
		}
		return false;
	}
}
