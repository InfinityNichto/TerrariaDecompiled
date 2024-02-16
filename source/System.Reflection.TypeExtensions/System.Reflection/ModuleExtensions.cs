namespace System.Reflection;

public static class ModuleExtensions
{
	public static bool HasModuleVersionId(this Module module)
	{
		ArgumentNullException.ThrowIfNull(module, "module");
		return true;
	}

	public static Guid GetModuleVersionId(this Module module)
	{
		ArgumentNullException.ThrowIfNull(module, "module");
		return module.ModuleVersionId;
	}
}
