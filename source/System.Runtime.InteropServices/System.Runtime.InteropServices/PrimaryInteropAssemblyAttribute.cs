namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
public sealed class PrimaryInteropAssemblyAttribute : Attribute
{
	public int MajorVersion { get; }

	public int MinorVersion { get; }

	public PrimaryInteropAssemblyAttribute(int major, int minor)
	{
		MajorVersion = major;
		MinorVersion = minor;
	}
}
