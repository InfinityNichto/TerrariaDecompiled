using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Runtime.InteropServices;

public static class NativeLibrary
{
	private static ConditionalWeakTable<Assembly, DllImportResolver> s_nativeDllResolveMap;

	internal static IntPtr LoadLibraryByName(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, bool throwOnError)
	{
		RuntimeAssembly assembly2 = (RuntimeAssembly)assembly;
		return LoadByName(libraryName, new QCallAssembly(ref assembly2), searchPath.HasValue, (uint)searchPath.GetValueOrDefault(), throwOnError);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern IntPtr LoadFromPath(string libraryName, bool throwOnError);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern IntPtr LoadByName(string libraryName, QCallAssembly callingAssembly, bool hasDllImportSearchPathFlag, uint dllImportSearchPathFlag, bool throwOnError);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern void FreeLib(IntPtr handle);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern IntPtr GetSymbol(IntPtr handle, string symbolName, bool throwOnError);

	public static IntPtr Load(string libraryPath)
	{
		if (libraryPath == null)
		{
			throw new ArgumentNullException("libraryPath");
		}
		return LoadFromPath(libraryPath, throwOnError: true);
	}

	public static bool TryLoad(string libraryPath, out IntPtr handle)
	{
		if (libraryPath == null)
		{
			throw new ArgumentNullException("libraryPath");
		}
		handle = LoadFromPath(libraryPath, throwOnError: false);
		return handle != IntPtr.Zero;
	}

	public static IntPtr Load(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
	{
		if (libraryName == null)
		{
			throw new ArgumentNullException("libraryName");
		}
		if (assembly == null)
		{
			throw new ArgumentNullException("assembly");
		}
		if (!assembly.IsRuntimeImplemented())
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);
		}
		return LoadLibraryByName(libraryName, assembly, searchPath, throwOnError: true);
	}

	public static bool TryLoad(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, out IntPtr handle)
	{
		if (libraryName == null)
		{
			throw new ArgumentNullException("libraryName");
		}
		if (assembly == null)
		{
			throw new ArgumentNullException("assembly");
		}
		if (!assembly.IsRuntimeImplemented())
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);
		}
		handle = LoadLibraryByName(libraryName, assembly, searchPath, throwOnError: false);
		return handle != IntPtr.Zero;
	}

	public static void Free(IntPtr handle)
	{
		if (!(handle == IntPtr.Zero))
		{
			FreeLib(handle);
		}
	}

	public static IntPtr GetExport(IntPtr handle, string name)
	{
		if (handle == IntPtr.Zero)
		{
			throw new ArgumentNullException("handle");
		}
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		return GetSymbol(handle, name, throwOnError: true);
	}

	public static bool TryGetExport(IntPtr handle, string name, out IntPtr address)
	{
		if (handle == IntPtr.Zero)
		{
			throw new ArgumentNullException("handle");
		}
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		address = GetSymbol(handle, name, throwOnError: false);
		return address != IntPtr.Zero;
	}

	public static void SetDllImportResolver(Assembly assembly, DllImportResolver resolver)
	{
		if (assembly == null)
		{
			throw new ArgumentNullException("assembly");
		}
		if (resolver == null)
		{
			throw new ArgumentNullException("resolver");
		}
		if (!assembly.IsRuntimeImplemented())
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);
		}
		if (s_nativeDllResolveMap == null)
		{
			Interlocked.CompareExchange(ref s_nativeDllResolveMap, new ConditionalWeakTable<Assembly, DllImportResolver>(), null);
		}
		try
		{
			s_nativeDllResolveMap.Add(assembly, resolver);
		}
		catch (ArgumentException)
		{
			throw new InvalidOperationException(SR.InvalidOperation_CannotRegisterSecondResolver);
		}
	}

	internal static IntPtr LoadLibraryCallbackStub(string libraryName, Assembly assembly, bool hasDllImportSearchPathFlags, uint dllImportSearchPathFlags)
	{
		if (s_nativeDllResolveMap == null)
		{
			return IntPtr.Zero;
		}
		if (!s_nativeDllResolveMap.TryGetValue(assembly, out var value))
		{
			return IntPtr.Zero;
		}
		return value(libraryName, assembly, hasDllImportSearchPathFlags ? new DllImportSearchPath?((DllImportSearchPath)dllImportSearchPathFlags) : null);
	}
}
