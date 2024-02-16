using System.Diagnostics.CodeAnalysis;

namespace System.Reflection;

public static class AssemblyExtensions
{
	[RequiresUnreferencedCode("Types might be removed")]
	public static Type[] GetExportedTypes(this Assembly assembly)
	{
		ArgumentNullException.ThrowIfNull(assembly, "assembly");
		return assembly.GetExportedTypes();
	}

	public static Module[] GetModules(this Assembly assembly)
	{
		ArgumentNullException.ThrowIfNull(assembly, "assembly");
		return assembly.GetModules();
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public static Type[] GetTypes(this Assembly assembly)
	{
		ArgumentNullException.ThrowIfNull(assembly, "assembly");
		return assembly.GetTypes();
	}
}
