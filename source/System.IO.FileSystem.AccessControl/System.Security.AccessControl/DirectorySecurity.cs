namespace System.Security.AccessControl;

public sealed class DirectorySecurity : FileSystemSecurity
{
	public DirectorySecurity()
		: base(isContainer: true)
	{
	}

	public DirectorySecurity(string name, AccessControlSections includeSections)
		: base(isContainer: true, name, includeSections, isDirectory: true)
	{
	}
}
