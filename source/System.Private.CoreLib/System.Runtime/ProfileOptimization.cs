using System.Runtime.Loader;

namespace System.Runtime;

public static class ProfileOptimization
{
	public static void SetProfileRoot(string directoryPath)
	{
		AssemblyLoadContext.Default.SetProfileOptimizationRoot(directoryPath);
	}

	public static void StartProfile(string? profile)
	{
		AssemblyLoadContext.Default.StartProfileOptimization(profile);
	}
}
