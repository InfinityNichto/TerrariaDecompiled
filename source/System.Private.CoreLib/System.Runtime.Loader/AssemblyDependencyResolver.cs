using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace System.Runtime.Loader;

[UnsupportedOSPlatform("android")]
[UnsupportedOSPlatform("browser")]
[UnsupportedOSPlatform("ios")]
[UnsupportedOSPlatform("tvos")]
public sealed class AssemblyDependencyResolver
{
	private readonly Dictionary<string, string> _assemblyPaths;

	private readonly string[] _nativeSearchPaths;

	private readonly string[] _resourceSearchPaths;

	private readonly string[] _assemblyDirectorySearchPaths;

	public AssemblyDependencyResolver(string componentAssemblyPath)
	{
		if (componentAssemblyPath == null)
		{
			throw new ArgumentNullException("componentAssemblyPath");
		}
		string assemblyPathsList = null;
		string nativeSearchPathsList = null;
		string resourceSearchPathsList = null;
		int num = 0;
		StringBuilder errorMessage = new StringBuilder();
		try
		{
			Interop.HostPolicy.corehost_error_writer_fn corehost_error_writer_fn = delegate(string message)
			{
				errorMessage.AppendLine(message);
			};
			IntPtr functionPointerForDelegate = Marshal.GetFunctionPointerForDelegate(corehost_error_writer_fn);
			IntPtr errorWriter = Interop.HostPolicy.corehost_set_error_writer(functionPointerForDelegate);
			try
			{
				num = Interop.HostPolicy.corehost_resolve_component_dependencies(componentAssemblyPath, delegate(string assemblyPaths, string nativeSearchPaths, string resourceSearchPaths)
				{
					assemblyPathsList = assemblyPaths;
					nativeSearchPathsList = nativeSearchPaths;
					resourceSearchPathsList = resourceSearchPaths;
				});
			}
			finally
			{
				Interop.HostPolicy.corehost_set_error_writer(errorWriter);
				GC.KeepAlive(corehost_error_writer_fn);
			}
		}
		catch (EntryPointNotFoundException innerException)
		{
			throw new InvalidOperationException(SR.AssemblyDependencyResolver_FailedToLoadHostpolicy, innerException);
		}
		catch (DllNotFoundException innerException2)
		{
			throw new InvalidOperationException(SR.AssemblyDependencyResolver_FailedToLoadHostpolicy, innerException2);
		}
		if (num != 0)
		{
			throw new InvalidOperationException(SR.Format(SR.AssemblyDependencyResolver_FailedToResolveDependencies, componentAssemblyPath, num, errorMessage));
		}
		string[] array = SplitPathsList(assemblyPathsList);
		_assemblyPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		string[] array2 = array;
		foreach (string text in array2)
		{
			_assemblyPaths.TryAdd(Path.GetFileNameWithoutExtension(text), text);
		}
		_nativeSearchPaths = SplitPathsList(nativeSearchPathsList);
		_resourceSearchPaths = SplitPathsList(resourceSearchPathsList);
		_assemblyDirectorySearchPaths = new string[1] { Path.GetDirectoryName(componentAssemblyPath) };
	}

	public string? ResolveAssemblyToPath(AssemblyName assemblyName)
	{
		if (assemblyName == null)
		{
			throw new ArgumentNullException("assemblyName");
		}
		string value;
		if (!string.IsNullOrEmpty(assemblyName.CultureName) && !string.Equals(assemblyName.CultureName, "neutral", StringComparison.OrdinalIgnoreCase))
		{
			string[] resourceSearchPaths = _resourceSearchPaths;
			foreach (string path in resourceSearchPaths)
			{
				string text = Path.Combine(path, assemblyName.CultureName, assemblyName.Name + ".dll");
				if (File.Exists(text))
				{
					return text;
				}
			}
		}
		else if (assemblyName.Name != null && _assemblyPaths.TryGetValue(assemblyName.Name, out value) && File.Exists(value))
		{
			return value;
		}
		return null;
	}

	public string? ResolveUnmanagedDllToPath(string unmanagedDllName)
	{
		if (unmanagedDllName == null)
		{
			throw new ArgumentNullException("unmanagedDllName");
		}
		string[] array = ((!unmanagedDllName.Contains(Path.DirectorySeparatorChar)) ? _nativeSearchPaths : _assemblyDirectorySearchPaths);
		bool isRelativePath = !Path.IsPathFullyQualified(unmanagedDllName);
		foreach (LibraryNameVariation item in LibraryNameVariation.DetermineLibraryNameVariations(unmanagedDllName, isRelativePath))
		{
			string path = item.Prefix + unmanagedDllName + item.Suffix;
			string[] array2 = array;
			foreach (string path2 in array2)
			{
				string text = Path.Combine(path2, path);
				if (File.Exists(text))
				{
					return text;
				}
			}
		}
		return null;
	}

	private static string[] SplitPathsList(string pathsList)
	{
		if (pathsList == null)
		{
			return Array.Empty<string>();
		}
		return pathsList.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
	}
}
