using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;

namespace Internal.Runtime.InteropServices;

internal sealed class IsolatedComponentLoadContext : AssemblyLoadContext
{
	private readonly AssemblyDependencyResolver _resolver;

	[RequiresUnreferencedCode("The trimmer might remove assemblies that are loaded by this class", Url = "https://aka.ms/dotnet-illink/nativehost")]
	public IsolatedComponentLoadContext(string componentAssemblyPath)
		: base("IsolatedComponentLoadContext(" + componentAssemblyPath + ")")
	{
		_resolver = new AssemblyDependencyResolver(componentAssemblyPath);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The trimmer warning is added to the constructor of this class since this method is a virtual one.")]
	protected override Assembly Load(AssemblyName assemblyName)
	{
		string text = _resolver.ResolveAssemblyToPath(assemblyName);
		if (text != null)
		{
			return LoadFromAssemblyPath(text);
		}
		return null;
	}

	protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
	{
		string text = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
		if (text != null)
		{
			return LoadUnmanagedDllFromPath(text);
		}
		return IntPtr.Zero;
	}
}
