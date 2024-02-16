namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Method, AllowMultiple = false)]
public sealed class DefaultDllImportSearchPathsAttribute : Attribute
{
	public DllImportSearchPath Paths { get; }

	public DefaultDllImportSearchPathsAttribute(DllImportSearchPath paths)
	{
		Paths = paths;
	}
}
