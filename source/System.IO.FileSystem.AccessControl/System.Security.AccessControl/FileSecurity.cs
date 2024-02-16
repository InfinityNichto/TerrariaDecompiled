using Microsoft.Win32.SafeHandles;

namespace System.Security.AccessControl;

public sealed class FileSecurity : FileSystemSecurity
{
	public FileSecurity()
		: base(isContainer: false)
	{
	}

	public FileSecurity(string fileName, AccessControlSections includeSections)
		: base(isContainer: false, fileName, includeSections, isDirectory: false)
	{
	}

	internal FileSecurity(SafeFileHandle handle, AccessControlSections includeSections)
		: base(isContainer: false, handle, includeSections, isDirectory: false)
	{
	}
}
