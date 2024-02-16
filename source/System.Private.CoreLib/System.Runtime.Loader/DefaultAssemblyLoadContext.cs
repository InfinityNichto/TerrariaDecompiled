namespace System.Runtime.Loader;

internal sealed class DefaultAssemblyLoadContext : AssemblyLoadContext
{
	internal static readonly AssemblyLoadContext s_loadContext = new DefaultAssemblyLoadContext();

	internal DefaultAssemblyLoadContext()
		: base(representsTPALoadContext: true, isCollectible: false, "Default")
	{
	}
}
