using System;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Internal.Runtime.InteropServices;

public static class InMemoryAssemblyLoader
{
	private static bool IsSupported { get; } = InitializeIsSupported();


	private static bool InitializeIsSupported()
	{
		if (!AppContext.TryGetSwitch("System.Runtime.InteropServices.EnableCppCLIHostActivation", out var isEnabled))
		{
			return true;
		}
		return isEnabled;
	}

	public static void LoadInMemoryAssembly(IntPtr moduleHandle, IntPtr assemblyPath)
	{
		if (!IsSupported)
		{
			throw new NotSupportedException("This API is not enabled in trimmed scenarios. see https://aka.ms/dotnet-illink/nativehost for more details");
		}
		string text = Marshal.PtrToStringUni(assemblyPath);
		if (text == null)
		{
			throw new ArgumentOutOfRangeException("assemblyPath");
		}
		AssemblyLoadContext assemblyLoadContext = new IsolatedComponentLoadContext(text);
		assemblyLoadContext.LoadFromInMemoryModule(moduleHandle);
	}
}
