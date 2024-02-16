using System.Configuration.Assemblies;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Runtime.Remoting;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;
using System.Threading;

namespace System;

public sealed class AppDomain : MarshalByRefObject
{
	private static readonly AppDomain s_domain = new AppDomain();

	private IPrincipal _defaultPrincipal;

	private PrincipalPolicy _principalPolicy = PrincipalPolicy.NoPrincipal;

	private Func<IPrincipal> s_getWindowsPrincipal;

	private Func<IPrincipal> s_getUnauthenticatedPrincipal;

	public static AppDomain CurrentDomain => s_domain;

	public string BaseDirectory => AppContext.BaseDirectory;

	public string? RelativeSearchPath => null;

	public AppDomainSetup SetupInformation => new AppDomainSetup();

	[Obsolete("Code Access Security is not supported or honored by the runtime.", DiagnosticId = "SYSLIB0003", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public PermissionSet PermissionSet => new PermissionSet(PermissionState.Unrestricted);

	public string? DynamicDirectory => null;

	public string FriendlyName
	{
		get
		{
			Assembly entryAssembly = Assembly.GetEntryAssembly();
			if (!(entryAssembly != null))
			{
				return "DefaultDomain";
			}
			return entryAssembly.GetName().Name;
		}
	}

	public int Id => 1;

	public bool IsFullyTrusted => true;

	public bool IsHomogenous => true;

	public static bool MonitoringIsEnabled
	{
		get
		{
			return true;
		}
		set
		{
			if (!value)
			{
				throw new ArgumentException(SR.Arg_MustBeTrue);
			}
		}
	}

	public long MonitoringSurvivedMemorySize => MonitoringSurvivedProcessMemorySize;

	public static long MonitoringSurvivedProcessMemorySize
	{
		get
		{
			GCMemoryInfo gCMemoryInfo = GC.GetGCMemoryInfo();
			return gCMemoryInfo.HeapSizeBytes - gCMemoryInfo.FragmentedBytes;
		}
	}

	public long MonitoringTotalAllocatedMemorySize => GC.GetTotalAllocatedBytes();

	public bool ShadowCopyFiles => false;

	public TimeSpan MonitoringTotalProcessorTime
	{
		get
		{
			if (!Interop.Kernel32.GetProcessTimes(Interop.Kernel32.GetCurrentProcess(), out var _, out var _, out var _, out var user))
			{
				return TimeSpan.Zero;
			}
			return new TimeSpan(user);
		}
	}

	public event UnhandledExceptionEventHandler? UnhandledException
	{
		add
		{
			AppContext.UnhandledException += value;
		}
		remove
		{
			AppContext.UnhandledException -= value;
		}
	}

	public event EventHandler? DomainUnload;

	public event EventHandler<FirstChanceExceptionEventArgs>? FirstChanceException
	{
		add
		{
			AppContext.FirstChanceException += value;
		}
		remove
		{
			AppContext.FirstChanceException -= value;
		}
	}

	public event EventHandler? ProcessExit
	{
		add
		{
			AppContext.ProcessExit += value;
		}
		remove
		{
			AppContext.ProcessExit -= value;
		}
	}

	public event AssemblyLoadEventHandler? AssemblyLoad
	{
		add
		{
			AssemblyLoadContext.AssemblyLoad += value;
		}
		remove
		{
			AssemblyLoadContext.AssemblyLoad -= value;
		}
	}

	public event ResolveEventHandler? AssemblyResolve
	{
		add
		{
			AssemblyLoadContext.AssemblyResolve += value;
		}
		remove
		{
			AssemblyLoadContext.AssemblyResolve -= value;
		}
	}

	public event ResolveEventHandler? ReflectionOnlyAssemblyResolve;

	public event ResolveEventHandler? TypeResolve
	{
		add
		{
			AssemblyLoadContext.TypeResolve += value;
		}
		remove
		{
			AssemblyLoadContext.TypeResolve -= value;
		}
	}

	public event ResolveEventHandler? ResourceResolve
	{
		add
		{
			AssemblyLoadContext.ResourceResolve += value;
		}
		remove
		{
			AssemblyLoadContext.ResourceResolve -= value;
		}
	}

	private AppDomain()
	{
	}

	[Obsolete("AppDomain.SetDynamicBase has been deprecated and is not supported.")]
	public void SetDynamicBase(string? path)
	{
	}

	public string ApplyPolicy(string assemblyName)
	{
		if (assemblyName == null)
		{
			throw new ArgumentNullException("assemblyName");
		}
		if (assemblyName.Length == 0 || assemblyName[0] == '\0')
		{
			throw new ArgumentException(SR.Argument_StringZeroLength, "assemblyName");
		}
		return assemblyName;
	}

	[Obsolete("Creating and unloading AppDomains is not supported and throws an exception.", DiagnosticId = "SYSLIB0024", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static AppDomain CreateDomain(string friendlyName)
	{
		if (friendlyName == null)
		{
			throw new ArgumentNullException("friendlyName");
		}
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_AppDomains);
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public int ExecuteAssembly(string assemblyFile)
	{
		return ExecuteAssembly(assemblyFile, null);
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public int ExecuteAssembly(string assemblyFile, string?[]? args)
	{
		if (assemblyFile == null)
		{
			throw new ArgumentNullException("assemblyFile");
		}
		string fullPath = Path.GetFullPath(assemblyFile);
		Assembly assembly = Assembly.LoadFile(fullPath);
		return ExecuteAssembly(assembly, args);
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	[Obsolete("Code Access Security is not supported or honored by the runtime.", DiagnosticId = "SYSLIB0003", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public int ExecuteAssembly(string assemblyFile, string?[]? args, byte[]? hashValue, AssemblyHashAlgorithm hashAlgorithm)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS);
	}

	private static int ExecuteAssembly(Assembly assembly, string[] args)
	{
		MethodInfo entryPoint = assembly.EntryPoint;
		if (entryPoint == null)
		{
			throw new MissingMethodException(SR.Arg_EntryPointNotFoundException);
		}
		object obj = entryPoint.Invoke(null, BindingFlags.DoNotWrapExceptions, null, (entryPoint.GetParameters().Length == 0) ? null : new object[1] { args }, null);
		if (obj == null)
		{
			return 0;
		}
		return (int)obj;
	}

	public int ExecuteAssemblyByName(AssemblyName assemblyName, params string?[]? args)
	{
		return ExecuteAssembly(Assembly.Load(assemblyName), args);
	}

	public int ExecuteAssemblyByName(string assemblyName)
	{
		return ExecuteAssemblyByName(assemblyName, (string?[]?)null);
	}

	public int ExecuteAssemblyByName(string assemblyName, params string?[]? args)
	{
		return ExecuteAssembly(Assembly.Load(assemblyName), args);
	}

	public object? GetData(string name)
	{
		return AppContext.GetData(name);
	}

	public void SetData(string name, object? data)
	{
		AppContext.SetData(name, data);
	}

	public bool? IsCompatibilitySwitchSet(string value)
	{
		if (!AppContext.TryGetSwitch(value, out var isEnabled))
		{
			return null;
		}
		return isEnabled;
	}

	public bool IsDefaultAppDomain()
	{
		return true;
	}

	public bool IsFinalizingForUnload()
	{
		return false;
	}

	public override string ToString()
	{
		return SR.AppDomain_Name + FriendlyName + "\r\n" + SR.AppDomain_NoContextPolicies;
	}

	[Obsolete("Creating and unloading AppDomains is not supported and throws an exception.", DiagnosticId = "SYSLIB0024", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static void Unload(AppDomain domain)
	{
		if (domain == null)
		{
			throw new ArgumentNullException("domain");
		}
		throw new CannotUnloadAppDomainException(SR.Arg_PlatformNotSupported);
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public Assembly Load(byte[] rawAssembly)
	{
		return Assembly.Load(rawAssembly);
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public Assembly Load(byte[] rawAssembly, byte[]? rawSymbolStore)
	{
		return Assembly.Load(rawAssembly, rawSymbolStore);
	}

	public Assembly Load(AssemblyName assemblyRef)
	{
		return Assembly.Load(assemblyRef);
	}

	public Assembly Load(string assemblyString)
	{
		return Assembly.Load(assemblyString);
	}

	public Assembly[] ReflectionOnlyGetAssemblies()
	{
		return Array.Empty<Assembly>();
	}

	[Obsolete("AppDomain.GetCurrentThreadId has been deprecated because it does not provide a stable Id when managed threads are running on fibers (aka lightweight threads). To get a stable identifier for a managed thread, use the ManagedThreadId property on Thread instead.")]
	public static int GetCurrentThreadId()
	{
		return Environment.CurrentManagedThreadId;
	}

	[Obsolete("AppDomain.AppendPrivatePath has been deprecated and is not supported.")]
	public void AppendPrivatePath(string? path)
	{
	}

	[Obsolete("AppDomain.ClearPrivatePath has been deprecated and is not supported.")]
	public void ClearPrivatePath()
	{
	}

	[Obsolete("AppDomain.ClearShadowCopyPath has been deprecated and is not supported.")]
	public void ClearShadowCopyPath()
	{
	}

	[Obsolete("AppDomain.SetCachePath has been deprecated and is not supported.")]
	public void SetCachePath(string? path)
	{
	}

	[Obsolete("AppDomain.SetShadowCopyFiles has been deprecated and is not supported.")]
	public void SetShadowCopyFiles()
	{
	}

	[Obsolete("AppDomain.SetShadowCopyPath has been deprecated and is not supported.")]
	public void SetShadowCopyPath(string? path)
	{
	}

	public Assembly[] GetAssemblies()
	{
		return AssemblyLoadContext.GetLoadedAssemblies();
	}

	public void SetPrincipalPolicy(PrincipalPolicy policy)
	{
		_principalPolicy = policy;
	}

	public void SetThreadPrincipal(IPrincipal principal)
	{
		if (principal == null)
		{
			throw new ArgumentNullException("principal");
		}
		if (Interlocked.CompareExchange(ref _defaultPrincipal, principal, null) != null)
		{
			throw new SystemException(SR.AppDomain_Policy_PrincipalTwice);
		}
	}

	[RequiresUnreferencedCode("Type and its constructor could be removed")]
	public ObjectHandle? CreateInstance(string assemblyName, string typeName)
	{
		if (assemblyName == null)
		{
			throw new ArgumentNullException("assemblyName");
		}
		return Activator.CreateInstance(assemblyName, typeName);
	}

	[RequiresUnreferencedCode("Type and its constructor could be removed")]
	public ObjectHandle? CreateInstance(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes)
	{
		if (assemblyName == null)
		{
			throw new ArgumentNullException("assemblyName");
		}
		return Activator.CreateInstance(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes);
	}

	[RequiresUnreferencedCode("Type and its constructor could be removed")]
	public ObjectHandle? CreateInstance(string assemblyName, string typeName, object?[]? activationAttributes)
	{
		if (assemblyName == null)
		{
			throw new ArgumentNullException("assemblyName");
		}
		return Activator.CreateInstance(assemblyName, typeName, activationAttributes);
	}

	[RequiresUnreferencedCode("Type and its constructor could be removed")]
	public object? CreateInstanceAndUnwrap(string assemblyName, string typeName)
	{
		return CreateInstance(assemblyName, typeName)?.Unwrap();
	}

	[RequiresUnreferencedCode("Type and its constructor could be removed")]
	public object? CreateInstanceAndUnwrap(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes)
	{
		return CreateInstance(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes)?.Unwrap();
	}

	[RequiresUnreferencedCode("Type and its constructor could be removed")]
	public object? CreateInstanceAndUnwrap(string assemblyName, string typeName, object?[]? activationAttributes)
	{
		return CreateInstance(assemblyName, typeName, activationAttributes)?.Unwrap();
	}

	[RequiresUnreferencedCode("Type and its constructor could be removed")]
	public ObjectHandle? CreateInstanceFrom(string assemblyFile, string typeName)
	{
		return Activator.CreateInstanceFrom(assemblyFile, typeName);
	}

	[RequiresUnreferencedCode("Type and its constructor could be removed")]
	public ObjectHandle? CreateInstanceFrom(string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes)
	{
		return Activator.CreateInstanceFrom(assemblyFile, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes);
	}

	[RequiresUnreferencedCode("Type and its constructor could be removed")]
	public ObjectHandle? CreateInstanceFrom(string assemblyFile, string typeName, object?[]? activationAttributes)
	{
		return Activator.CreateInstanceFrom(assemblyFile, typeName, activationAttributes);
	}

	[RequiresUnreferencedCode("Type and its constructor could be removed")]
	public object? CreateInstanceFromAndUnwrap(string assemblyFile, string typeName)
	{
		return CreateInstanceFrom(assemblyFile, typeName)?.Unwrap();
	}

	[RequiresUnreferencedCode("Type and its constructor could be removed")]
	public object? CreateInstanceFromAndUnwrap(string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes)
	{
		return CreateInstanceFrom(assemblyFile, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes)?.Unwrap();
	}

	[RequiresUnreferencedCode("Type and its constructor could be removed")]
	public object? CreateInstanceFromAndUnwrap(string assemblyFile, string typeName, object?[]? activationAttributes)
	{
		return CreateInstanceFrom(assemblyFile, typeName, activationAttributes)?.Unwrap();
	}

	internal IPrincipal GetThreadPrincipal()
	{
		IPrincipal principal = _defaultPrincipal;
		if (principal == null)
		{
			switch (_principalPolicy)
			{
			case PrincipalPolicy.UnauthenticatedPrincipal:
				if (s_getUnauthenticatedPrincipal == null)
				{
					Type type2 = Type.GetType("System.Security.Principal.GenericPrincipal, System.Security.Claims", throwOnError: true);
					MethodInfo method2 = type2.GetMethod("GetDefaultInstance", BindingFlags.Static | BindingFlags.NonPublic);
					Volatile.Write(ref s_getUnauthenticatedPrincipal, method2.CreateDelegate<Func<IPrincipal>>());
				}
				principal = s_getUnauthenticatedPrincipal();
				break;
			case PrincipalPolicy.WindowsPrincipal:
				if (s_getWindowsPrincipal == null)
				{
					Type type = Type.GetType("System.Security.Principal.WindowsPrincipal, System.Security.Principal.Windows", throwOnError: true);
					MethodInfo method = type.GetMethod("GetDefaultInstance", BindingFlags.Static | BindingFlags.NonPublic);
					if (method == null)
					{
						throw new PlatformNotSupportedException(SR.PlatformNotSupported_Principal);
					}
					Volatile.Write(ref s_getWindowsPrincipal, method.CreateDelegate<Func<IPrincipal>>());
				}
				principal = s_getWindowsPrincipal();
				break;
			}
		}
		return principal;
	}
}
