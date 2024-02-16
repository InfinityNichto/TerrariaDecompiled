namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
public sealed class TypeLibVersionAttribute : Attribute
{
	public int MajorVersion { get; }

	public int MinorVersion { get; }

	public TypeLibVersionAttribute(int major, int minor)
	{
		MajorVersion = major;
		MinorVersion = minor;
	}
}
