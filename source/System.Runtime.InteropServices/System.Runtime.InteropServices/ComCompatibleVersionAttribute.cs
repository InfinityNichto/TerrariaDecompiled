namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
public sealed class ComCompatibleVersionAttribute : Attribute
{
	public int MajorVersion { get; }

	public int MinorVersion { get; }

	public int BuildNumber { get; }

	public int RevisionNumber { get; }

	public ComCompatibleVersionAttribute(int major, int minor, int build, int revision)
	{
		MajorVersion = major;
		MinorVersion = minor;
		BuildNumber = build;
		RevisionNumber = revision;
	}
}
