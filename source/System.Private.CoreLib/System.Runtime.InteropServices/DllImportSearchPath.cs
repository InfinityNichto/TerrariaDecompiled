namespace System.Runtime.InteropServices;

[Flags]
public enum DllImportSearchPath
{
	UseDllDirectoryForDependencies = 0x100,
	ApplicationDirectory = 0x200,
	UserDirectories = 0x400,
	System32 = 0x800,
	SafeDirectories = 0x1000,
	AssemblyDirectory = 2,
	LegacyBehavior = 0
}
