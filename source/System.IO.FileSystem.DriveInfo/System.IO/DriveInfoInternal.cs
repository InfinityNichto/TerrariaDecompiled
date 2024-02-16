namespace System.IO;

internal static class DriveInfoInternal
{
	public static string[] GetLogicalDrives()
	{
		int logicalDrives = global::Interop.Kernel32.GetLogicalDrives();
		if (logicalDrives == 0)
		{
			throw System.IO.Win32Marshal.GetExceptionForLastWin32Error();
		}
		uint num = (uint)logicalDrives;
		int num2 = 0;
		while (num != 0)
		{
			if ((num & (true ? 1u : 0u)) != 0)
			{
				num2++;
			}
			num >>= 1;
		}
		string[] array = new string[num2];
		Span<char> span = stackalloc char[3] { 'A', ':', '\\' };
		num = (uint)logicalDrives;
		num2 = 0;
		while (num != 0)
		{
			if ((num & (true ? 1u : 0u)) != 0)
			{
				array[num2++] = span.ToString();
			}
			num >>= 1;
			span[0] += '\u0001';
		}
		return array;
	}

	public static string NormalizeDriveName(string driveName)
	{
		string text;
		if (driveName.Length == 1)
		{
			text = driveName + ":\\";
		}
		else
		{
			text = Path.GetPathRoot(driveName);
			if (string.IsNullOrEmpty(text) || text.StartsWith("\\\\", StringComparison.Ordinal))
			{
				throw new ArgumentException(System.SR.Arg_MustBeDriveLetterOrRootDir, "driveName");
			}
		}
		if (text.Length == 2 && text[1] == ':')
		{
			text += "\\";
		}
		char c = driveName[0];
		if ((c < 'A' || c > 'Z') && (c < 'a' || c > 'z'))
		{
			throw new ArgumentException(System.SR.Arg_MustBeDriveLetterOrRootDir, "driveName");
		}
		return text;
	}
}
