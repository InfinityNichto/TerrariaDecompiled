using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;

namespace System.Reflection.Emit;

public sealed class AssemblyBuilder : Assembly
{
	internal AssemblyBuilderData _assemblyData;

	private readonly InternalAssemblyBuilder _internalAssemblyBuilder;

	private ModuleBuilder _manifestModuleBuilder;

	private bool _isManifestModuleUsedAsDefinedModule;

	private static readonly object s_assemblyBuilderLock = new object();

	internal object SyncRoot => InternalAssembly.SyncRoot;

	internal InternalAssemblyBuilder InternalAssembly => _internalAssemblyBuilder;

	public override string? FullName => InternalAssembly.FullName;

	public override Module ManifestModule => _manifestModuleBuilder.InternalModule;

	public override bool ReflectionOnly => InternalAssembly.ReflectionOnly;

	public override long HostContext => InternalAssembly.HostContext;

	public override bool IsCollectible => InternalAssembly.IsCollectible;

	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public override string? CodeBase
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
		}
	}

	public override string Location
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
		}
	}

	public override MethodInfo? EntryPoint => null;

	public override bool IsDynamic => true;

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern RuntimeModule GetInMemoryAssemblyModule(RuntimeAssembly assembly);

	internal ModuleBuilder GetModuleBuilder(InternalModuleBuilder module)
	{
		lock (SyncRoot)
		{
			if (_manifestModuleBuilder.InternalModule == module)
			{
				return _manifestModuleBuilder;
			}
			throw new ArgumentException(null, "module");
		}
	}

	internal AssemblyBuilder(AssemblyName name, AssemblyBuilderAccess access, ref StackCrawlMark stackMark, AssemblyLoadContext assemblyLoadContext, IEnumerable<CustomAttributeBuilder> unsafeAssemblyAttributes)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (access != AssemblyBuilderAccess.Run && access != AssemblyBuilderAccess.RunAndCollect)
		{
			throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)access), "access");
		}
		name = (AssemblyName)name.Clone();
		List<CustomAttributeBuilder> list = null;
		if (unsafeAssemblyAttributes != null)
		{
			list = new List<CustomAttributeBuilder>(unsafeAssemblyAttributes);
		}
		Assembly o = null;
		CreateDynamicAssembly(ObjectHandleOnStack.Create(ref name), new StackCrawlMarkHandle(ref stackMark), (int)access, ObjectHandleOnStack.Create(ref assemblyLoadContext), ObjectHandleOnStack.Create(ref o));
		_internalAssemblyBuilder = (InternalAssemblyBuilder)o;
		_assemblyData = new AssemblyBuilderData(access);
		InitManifestModule();
		if (list == null)
		{
			return;
		}
		foreach (CustomAttributeBuilder item in list)
		{
			SetCustomAttribute(item);
		}
	}

	[MemberNotNull("_manifestModuleBuilder")]
	private void InitManifestModule()
	{
		InternalModuleBuilder internalModuleBuilder = (InternalModuleBuilder)GetInMemoryAssemblyModule(InternalAssembly);
		_manifestModuleBuilder = new ModuleBuilder(this, internalModuleBuilder);
		_manifestModuleBuilder.Init("RefEmit_InMemoryManifestModule");
		_isManifestModuleUsedAsDefinedModule = false;
	}

	public static AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access)
	{
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
		return InternalDefineDynamicAssembly(name, access, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext, null);
	}

	public static AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, IEnumerable<CustomAttributeBuilder>? assemblyAttributes)
	{
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
		return InternalDefineDynamicAssembly(name, access, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext, assemblyAttributes);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void CreateDynamicAssembly(ObjectHandleOnStack name, StackCrawlMarkHandle stackMark, int access, ObjectHandleOnStack assemblyLoadContext, ObjectHandleOnStack retAssembly);

	internal static AssemblyBuilder InternalDefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, ref StackCrawlMark stackMark, AssemblyLoadContext assemblyLoadContext, IEnumerable<CustomAttributeBuilder> unsafeAssemblyAttributes)
	{
		lock (s_assemblyBuilderLock)
		{
			return new AssemblyBuilder(name, access, ref stackMark, assemblyLoadContext, unsafeAssemblyAttributes);
		}
	}

	public ModuleBuilder DefineDynamicModule(string name)
	{
		lock (SyncRoot)
		{
			return DefineDynamicModuleInternalNoLock(name);
		}
	}

	private ModuleBuilder DefineDynamicModuleInternalNoLock(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyName, "name");
		}
		if (name[0] == '\0')
		{
			throw new ArgumentException(SR.Argument_InvalidName, "name");
		}
		if (_isManifestModuleUsedAsDefinedModule)
		{
			throw new InvalidOperationException(SR.InvalidOperation_NoMultiModuleAssembly);
		}
		ModuleBuilder manifestModuleBuilder = _manifestModuleBuilder;
		_assemblyData._moduleBuilderList.Add(manifestModuleBuilder);
		if (manifestModuleBuilder == _manifestModuleBuilder)
		{
			_isManifestModuleUsedAsDefinedModule = true;
		}
		return manifestModuleBuilder;
	}

	internal static void CheckContext(params Type[][] typess)
	{
		if (typess == null)
		{
			return;
		}
		foreach (Type[] array in typess)
		{
			if (array != null)
			{
				CheckContext(array);
			}
		}
	}

	internal static void CheckContext(params Type[] types)
	{
		if (types == null)
		{
			return;
		}
		foreach (Type type in types)
		{
			if (!(type == null))
			{
				if (type.Module == null || type.Module.Assembly == null)
				{
					throw new ArgumentException(SR.Argument_TypeNotValid);
				}
				_ = type.Module.Assembly == typeof(object).Module.Assembly;
			}
		}
	}

	public override bool Equals(object? obj)
	{
		return InternalAssembly.Equals(obj);
	}

	public override int GetHashCode()
	{
		return InternalAssembly.GetHashCode();
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return InternalAssembly.GetCustomAttributes(inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return InternalAssembly.GetCustomAttributes(attributeType, inherit);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		return InternalAssembly.IsDefined(attributeType, inherit);
	}

	public override IList<CustomAttributeData> GetCustomAttributesData()
	{
		return InternalAssembly.GetCustomAttributesData();
	}

	public override AssemblyName GetName(bool copiedName)
	{
		return InternalAssembly.GetName(copiedName);
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type? GetType(string name, bool throwOnError, bool ignoreCase)
	{
		return InternalAssembly.GetType(name, throwOnError, ignoreCase);
	}

	public override Module? GetModule(string name)
	{
		return InternalAssembly.GetModule(name);
	}

	[RequiresUnreferencedCode("Assembly references might be removed")]
	public override AssemblyName[] GetReferencedAssemblies()
	{
		return InternalAssembly.GetReferencedAssemblies();
	}

	public override Module[] GetModules(bool getResourceModules)
	{
		return InternalAssembly.GetModules(getResourceModules);
	}

	public override Module[] GetLoadedModules(bool getResourceModules)
	{
		return InternalAssembly.GetLoadedModules(getResourceModules);
	}

	public override Assembly GetSatelliteAssembly(CultureInfo culture)
	{
		return InternalAssembly.GetSatelliteAssembly(culture, null);
	}

	public override Assembly GetSatelliteAssembly(CultureInfo culture, Version? version)
	{
		return InternalAssembly.GetSatelliteAssembly(culture, version);
	}

	public ModuleBuilder? GetDynamicModule(string name)
	{
		lock (SyncRoot)
		{
			return GetDynamicModuleNoLock(name);
		}
	}

	private ModuleBuilder GetDynamicModuleNoLock(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyName, "name");
		}
		for (int i = 0; i < _assemblyData._moduleBuilderList.Count; i++)
		{
			ModuleBuilder moduleBuilder = _assemblyData._moduleBuilderList[i];
			if (moduleBuilder._moduleData._moduleName.Equals(name))
			{
				return moduleBuilder;
			}
		}
		return null;
	}

	public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
	{
		if (con == null)
		{
			throw new ArgumentNullException("con");
		}
		if (binaryAttribute == null)
		{
			throw new ArgumentNullException("binaryAttribute");
		}
		lock (SyncRoot)
		{
			TypeBuilder.DefineCustomAttribute(_manifestModuleBuilder, 536870913, _manifestModuleBuilder.GetConstructorToken(con), binaryAttribute);
		}
	}

	public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
	{
		if (customBuilder == null)
		{
			throw new ArgumentNullException("customBuilder");
		}
		lock (SyncRoot)
		{
			customBuilder.CreateCustomAttribute(_manifestModuleBuilder, 536870913);
		}
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type[] GetExportedTypes()
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public override FileStream GetFile(string name)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public override FileStream[] GetFiles(bool getResourceModules)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	public override ManifestResourceInfo? GetManifestResourceInfo(string resourceName)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	public override string[] GetManifestResourceNames()
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	public override Stream? GetManifestResourceStream(string name)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	public override Stream? GetManifestResourceStream(Type type, string name)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}
}
