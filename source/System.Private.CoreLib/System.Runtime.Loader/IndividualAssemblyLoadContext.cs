namespace System.Runtime.Loader;

internal sealed class IndividualAssemblyLoadContext : AssemblyLoadContext
{
	internal IndividualAssemblyLoadContext(string name)
		: base(representsTPALoadContext: false, isCollectible: false, name)
	{
	}
}
