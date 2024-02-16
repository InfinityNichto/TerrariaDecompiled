using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;

namespace System.Reflection;

public abstract class Assembly : ICustomAttributeProvider, ISerializable
{
	private static readonly Dictionary<string, Assembly> s_loadfile = new Dictionary<string, Assembly>();

	private static readonly List<string> s_loadFromAssemblyList = new List<string>();

	private static bool s_loadFromHandlerSet;

	private static int s_cachedSerializationSwitch;

	private static bool s_forceNullEntryPoint;

	public virtual IEnumerable<TypeInfo> DefinedTypes
	{
		[RequiresUnreferencedCode("Types might be removed")]
		get
		{
			Type[] types = GetTypes();
			TypeInfo[] array = new TypeInfo[types.Length];
			for (int i = 0; i < types.Length; i++)
			{
				TypeInfo typeInfo = types[i].GetTypeInfo();
				if (typeInfo == null)
				{
					throw new NotSupportedException(SR.Format(SR.NotSupported_NoTypeInfo, types[i].FullName));
				}
				array[i] = typeInfo;
			}
			return array;
		}
	}

	public virtual IEnumerable<Type> ExportedTypes
	{
		[RequiresUnreferencedCode("Types might be removed")]
		get
		{
			return GetExportedTypes();
		}
	}

	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public virtual string? CodeBase
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual MethodInfo? EntryPoint
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual string? FullName
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual string ImageRuntimeVersion
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual bool IsDynamic => false;

	public virtual string Location
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual bool ReflectionOnly
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual bool IsCollectible => true;

	public bool IsFullyTrusted => true;

	public virtual IEnumerable<CustomAttributeData> CustomAttributes => GetCustomAttributesData();

	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public virtual string EscapedCodeBase => AssemblyName.EscapeCodeBase(CodeBase);

	public virtual Module ManifestModule
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual IEnumerable<Module> Modules => GetLoadedModules(getResourceModules: true);

	[Obsolete("The Global Assembly Cache is not supported.", DiagnosticId = "SYSLIB0005", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public virtual bool GlobalAssemblyCache
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual long HostContext
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual SecurityRuleSet SecurityRuleSet => SecurityRuleSet.None;

	public virtual event ModuleResolveEventHandler? ModuleResolve
	{
		add
		{
			throw NotImplemented.ByDesign;
		}
		remove
		{
			throw NotImplemented.ByDesign;
		}
	}

	public static Assembly Load(string assemblyString)
	{
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
		return RuntimeAssembly.InternalLoad(assemblyString, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
	}

	[Obsolete("Assembly.LoadWithPartialName has been deprecated. Use Assembly.Load() instead.")]
	public static Assembly? LoadWithPartialName(string partialName)
	{
		if (partialName == null)
		{
			throw new ArgumentNullException("partialName");
		}
		if (partialName.Length == 0 || partialName[0] == '\0')
		{
			throw new ArgumentException(SR.Format_StringZeroLength, "partialName");
		}
		try
		{
			StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.InternalLoad(partialName, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
		}
		catch (FileNotFoundException)
		{
			return null;
		}
	}

	public static Assembly Load(AssemblyName assemblyRef)
	{
		if (assemblyRef == null)
		{
			throw new ArgumentNullException("assemblyRef");
		}
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
		return RuntimeAssembly.InternalLoad(assemblyRef, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetExecutingAssemblyNative(StackCrawlMarkHandle stackMark, ObjectHandleOnStack retAssembly);

	internal static RuntimeAssembly GetExecutingAssembly(ref StackCrawlMark stackMark)
	{
		RuntimeAssembly o = null;
		GetExecutingAssemblyNative(new StackCrawlMarkHandle(ref stackMark), ObjectHandleOnStack.Create(ref o));
		return o;
	}

	public static Assembly GetExecutingAssembly()
	{
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
		return GetExecutingAssembly(ref stackMark);
	}

	public static Assembly GetCallingAssembly()
	{
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCallersCaller;
		return GetExecutingAssembly(ref stackMark);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetEntryAssemblyNative(ObjectHandleOnStack retAssembly);

	private static Assembly GetEntryAssemblyInternal()
	{
		RuntimeAssembly o = null;
		GetEntryAssemblyNative(ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool IsRuntimeImplemented()
	{
		return this is RuntimeAssembly;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern uint GetAssemblyCount();

	[RequiresUnreferencedCode("Types might be removed")]
	public virtual Type[] GetTypes()
	{
		Module[] modules = GetModules(getResourceModules: false);
		if (modules.Length == 1)
		{
			return modules[0].GetTypes();
		}
		int num = 0;
		Type[][] array = new Type[modules.Length][];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = modules[i].GetTypes();
			num += array[i].Length;
		}
		int num2 = 0;
		Type[] array2 = new Type[num];
		for (int j = 0; j < array.Length; j++)
		{
			int num3 = array[j].Length;
			Array.Copy(array[j], 0, array2, num2, num3);
			num2 += num3;
		}
		return array2;
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public virtual Type[] GetExportedTypes()
	{
		throw NotImplemented.ByDesign;
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public virtual Type[] GetForwardedTypes()
	{
		throw NotImplemented.ByDesign;
	}

	public virtual ManifestResourceInfo? GetManifestResourceInfo(string resourceName)
	{
		throw NotImplemented.ByDesign;
	}

	public virtual string[] GetManifestResourceNames()
	{
		throw NotImplemented.ByDesign;
	}

	public virtual Stream? GetManifestResourceStream(string name)
	{
		throw NotImplemented.ByDesign;
	}

	public virtual Stream? GetManifestResourceStream(Type type, string name)
	{
		throw NotImplemented.ByDesign;
	}

	public virtual AssemblyName GetName()
	{
		return GetName(copiedName: false);
	}

	public virtual AssemblyName GetName(bool copiedName)
	{
		throw NotImplemented.ByDesign;
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public virtual Type? GetType(string name)
	{
		return GetType(name, throwOnError: false, ignoreCase: false);
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public virtual Type? GetType(string name, bool throwOnError)
	{
		return GetType(name, throwOnError, ignoreCase: false);
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public virtual Type? GetType(string name, bool throwOnError, bool ignoreCase)
	{
		throw NotImplemented.ByDesign;
	}

	public virtual bool IsDefined(Type attributeType, bool inherit)
	{
		throw NotImplemented.ByDesign;
	}

	public virtual IList<CustomAttributeData> GetCustomAttributesData()
	{
		throw NotImplemented.ByDesign;
	}

	public virtual object[] GetCustomAttributes(bool inherit)
	{
		throw NotImplemented.ByDesign;
	}

	public virtual object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		throw NotImplemented.ByDesign;
	}

	[RequiresUnreferencedCode("Assembly.CreateInstance is not supported with trimming. Use Type.GetType instead.")]
	public object? CreateInstance(string typeName)
	{
		return CreateInstance(typeName, ignoreCase: false, BindingFlags.Instance | BindingFlags.Public, null, null, null, null);
	}

	[RequiresUnreferencedCode("Assembly.CreateInstance is not supported with trimming. Use Type.GetType instead.")]
	public object? CreateInstance(string typeName, bool ignoreCase)
	{
		return CreateInstance(typeName, ignoreCase, BindingFlags.Instance | BindingFlags.Public, null, null, null, null);
	}

	[RequiresUnreferencedCode("Assembly.CreateInstance is not supported with trimming. Use Type.GetType instead.")]
	public virtual object? CreateInstance(string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object[]? args, CultureInfo? culture, object[]? activationAttributes)
	{
		Type type = GetType(typeName, throwOnError: false, ignoreCase);
		if (type == null)
		{
			return null;
		}
		return Activator.CreateInstance(type, bindingAttr, binder, args, culture, activationAttributes);
	}

	public virtual Module? GetModule(string name)
	{
		throw NotImplemented.ByDesign;
	}

	public Module[] GetModules()
	{
		return GetModules(getResourceModules: false);
	}

	public virtual Module[] GetModules(bool getResourceModules)
	{
		throw NotImplemented.ByDesign;
	}

	public Module[] GetLoadedModules()
	{
		return GetLoadedModules(getResourceModules: false);
	}

	public virtual Module[] GetLoadedModules(bool getResourceModules)
	{
		throw NotImplemented.ByDesign;
	}

	[RequiresUnreferencedCode("Assembly references might be removed")]
	public virtual AssemblyName[] GetReferencedAssemblies()
	{
		throw NotImplemented.ByDesign;
	}

	public virtual Assembly GetSatelliteAssembly(CultureInfo culture)
	{
		throw NotImplemented.ByDesign;
	}

	public virtual Assembly GetSatelliteAssembly(CultureInfo culture, Version? version)
	{
		throw NotImplemented.ByDesign;
	}

	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public virtual FileStream? GetFile(string name)
	{
		throw NotImplemented.ByDesign;
	}

	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public virtual FileStream[] GetFiles()
	{
		return GetFiles(getResourceModules: false);
	}

	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public virtual FileStream[] GetFiles(bool getResourceModules)
	{
		throw NotImplemented.ByDesign;
	}

	public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw NotImplemented.ByDesign;
	}

	public override string ToString()
	{
		return FullName ?? base.ToString();
	}

	public override bool Equals(object? o)
	{
		return base.Equals(o);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Assembly? left, Assembly? right)
	{
		if ((object)right == null)
		{
			if ((object)left != null)
			{
				return false;
			}
			return true;
		}
		if ((object)left == right)
		{
			return true;
		}
		return left?.Equals(right) ?? false;
	}

	public static bool operator !=(Assembly? left, Assembly? right)
	{
		return !(left == right);
	}

	public static string CreateQualifiedName(string? assemblyName, string? typeName)
	{
		return typeName + ", " + assemblyName;
	}

	public static Assembly? GetAssembly(Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		Module module = type.Module;
		if (module == null)
		{
			return null;
		}
		return module.Assembly;
	}

	public static Assembly? GetEntryAssembly()
	{
		if (s_forceNullEntryPoint)
		{
			return null;
		}
		return GetEntryAssemblyInternal();
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public static Assembly Load(byte[] rawAssembly)
	{
		return Load(rawAssembly, null);
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public static Assembly Load(byte[] rawAssembly, byte[]? rawSymbolStore)
	{
		if (rawAssembly == null)
		{
			throw new ArgumentNullException("rawAssembly");
		}
		if (rawAssembly.Length == 0)
		{
			throw new BadImageFormatException(SR.BadImageFormat_BadILFormat);
		}
		SerializationInfo.ThrowIfDeserializationInProgress("AllowAssembliesFromByteArrays", ref s_cachedSerializationSwitch);
		AssemblyLoadContext assemblyLoadContext = new IndividualAssemblyLoadContext("Assembly.Load(byte[], ...)");
		return assemblyLoadContext.InternalLoad(rawAssembly, rawSymbolStore);
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public static Assembly LoadFile(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (PathInternal.IsPartiallyQualified(path))
		{
			throw new ArgumentException(SR.Format(SR.Argument_AbsolutePathRequired, path), "path");
		}
		string fullPath = Path.GetFullPath(path);
		lock (s_loadfile)
		{
			if (s_loadfile.TryGetValue(fullPath, out var value))
			{
				return value;
			}
			AssemblyLoadContext assemblyLoadContext = new IndividualAssemblyLoadContext("Assembly.LoadFile(" + fullPath + ")");
			value = assemblyLoadContext.LoadFromAssemblyPath(fullPath);
			s_loadfile.Add(fullPath, value);
			return value;
		}
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	[UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file", Justification = "The assembly is loaded by specifying a path outside of the single-file bundle, the location of the path will not be empty if the path exist, otherwise it will be handled as null")]
	private static Assembly LoadFromResolveHandler(object sender, ResolveEventArgs args)
	{
		Assembly requestingAssembly = args.RequestingAssembly;
		if (requestingAssembly == null)
		{
			return null;
		}
		if (AssemblyLoadContext.Default != AssemblyLoadContext.GetLoadContext(requestingAssembly))
		{
			return null;
		}
		string fullPath = Path.GetFullPath(requestingAssembly.Location);
		if (string.IsNullOrEmpty(fullPath))
		{
			return null;
		}
		lock (s_loadFromAssemblyList)
		{
			if (!s_loadFromAssemblyList.Contains(fullPath))
			{
				if (AssemblyLoadContext.IsTracingEnabled())
				{
					AssemblyLoadContext.TraceAssemblyLoadFromResolveHandlerInvoked(args.Name, isTrackedAssembly: false, fullPath, null);
				}
				return null;
			}
		}
		AssemblyName assemblyName = new AssemblyName(args.Name);
		string text = Path.Combine(Path.GetDirectoryName(fullPath), assemblyName.Name + ".dll");
		if (AssemblyLoadContext.IsTracingEnabled())
		{
			AssemblyLoadContext.TraceAssemblyLoadFromResolveHandlerInvoked(args.Name, isTrackedAssembly: true, fullPath, text);
		}
		try
		{
			return LoadFrom(text);
		}
		catch (FileNotFoundException)
		{
			return null;
		}
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public static Assembly LoadFrom(string assemblyFile)
	{
		if (assemblyFile == null)
		{
			throw new ArgumentNullException("assemblyFile");
		}
		string fullPath = Path.GetFullPath(assemblyFile);
		if (!s_loadFromHandlerSet)
		{
			lock (s_loadFromAssemblyList)
			{
				if (!s_loadFromHandlerSet)
				{
					AssemblyLoadContext.AssemblyResolve += LoadFromResolveHandler;
					s_loadFromHandlerSet = true;
				}
			}
		}
		lock (s_loadFromAssemblyList)
		{
			if (!s_loadFromAssemblyList.Contains(fullPath))
			{
				s_loadFromAssemblyList.Add(fullPath);
			}
		}
		return AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public static Assembly LoadFrom(string assemblyFile, byte[]? hashValue, AssemblyHashAlgorithm hashAlgorithm)
	{
		throw new NotSupportedException(SR.NotSupported_AssemblyLoadFromHash);
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public static Assembly UnsafeLoadFrom(string assemblyFile)
	{
		return LoadFrom(assemblyFile);
	}

	[RequiresUnreferencedCode("Types and members the loaded module depends on might be removed")]
	public Module LoadModule(string moduleName, byte[]? rawModule)
	{
		return LoadModule(moduleName, rawModule, null);
	}

	[RequiresUnreferencedCode("Types and members the loaded module depends on might be removed")]
	public virtual Module LoadModule(string moduleName, byte[]? rawModule, byte[]? rawSymbolStore)
	{
		throw NotImplemented.ByDesign;
	}

	[Obsolete("ReflectionOnly loading is not supported and throws PlatformNotSupportedException.", DiagnosticId = "SYSLIB0018", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public static Assembly ReflectionOnlyLoad(byte[] rawAssembly)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_ReflectionOnly);
	}

	[Obsolete("ReflectionOnly loading is not supported and throws PlatformNotSupportedException.", DiagnosticId = "SYSLIB0018", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public static Assembly ReflectionOnlyLoad(string assemblyString)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_ReflectionOnly);
	}

	[Obsolete("ReflectionOnly loading is not supported and throws PlatformNotSupportedException.", DiagnosticId = "SYSLIB0018", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public static Assembly ReflectionOnlyLoadFrom(string assemblyFile)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_ReflectionOnly);
	}
}
