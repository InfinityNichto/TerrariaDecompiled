using System.IO;

namespace System.ComponentModel;

public static class SyntaxCheck
{
	public static bool CheckMachineName(string value)
	{
		if (value == null)
		{
			return false;
		}
		value = value.Trim();
		if (value.Equals(string.Empty))
		{
			return false;
		}
		return !value.Contains('\\');
	}

	public static bool CheckPath(string value)
	{
		if (value == null)
		{
			return false;
		}
		value = value.TrimStart();
		if (value.Equals(string.Empty))
		{
			return false;
		}
		return value.StartsWith("\\\\", StringComparison.Ordinal);
	}

	public static bool CheckRootedPath(string value)
	{
		if (value == null)
		{
			return false;
		}
		value = value.Trim();
		if (value.Equals(string.Empty))
		{
			return false;
		}
		return Path.IsPathRooted(value);
	}
}
