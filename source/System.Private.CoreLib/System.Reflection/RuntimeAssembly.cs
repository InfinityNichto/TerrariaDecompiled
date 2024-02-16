using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Serialization;
using System.Threading;

namespace System.Reflection;

internal class RuntimeAssembly : Assembly
{
	private sealed class ManifestResourceStream : UnmanagedMemoryStream
	{
		private RuntimeAssembly _manifestAssembly;

		internal unsafe ManifestResourceStream(RuntimeAssembly manifestAssembly, byte* pointer, long length, long capacity, FileAccess access)
			: base(pointer, length, capacity, access)
		{
			_manifestAssembly = manifestAssembly;
		}
	}

	private string m_fullname;

	private object m_syncRoot;

	private IntPtr m_assembly;

	internal object SyncRoot
	{
		get
		{
			if (m_syncRoot == null)
			{
				Interlocked.CompareExchange<object>(ref m_syncRoot, new object(), (object)null);
			}
			return m_syncRoot;
		}
	}

	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public override string CodeBase
	{
		get
		{
			string codeBase = GetCodeBase();
			if (codeBase == null)
			{
				throw new NotSupportedException(SR.NotSupported_CodeBase);
			}
			return codeBase;
		}
	}

	public override string FullName
	{
		get
		{
			if (m_fullname == null)
			{
				string s = null;
				RuntimeAssembly assembly = this;
				GetFullName(new QCallAssembly(ref assembly), new StringHandleOnStack(ref s));
				Interlocked.CompareExchange(ref m_fullname, s, null);
			}
			return m_fullname;
		}
	}

	public override MethodInfo EntryPoint
	{
		get
		{
			IRuntimeMethodInfo o = null;
			RuntimeAssembly assembly = this;
			GetEntryPoint(new QCallAssembly(ref assembly), ObjectHandleOnStack.Create(ref o));
			if (o == null)
			{
				return null;
			}
			return (MethodInfo)RuntimeType.GetMethodBase(o);
		}
	}

	public override IEnumerable<TypeInfo> DefinedTypes
	{
		[RequiresUnreferencedCode("Types might be removed")]
		get
		{
			RuntimeModule[] modulesInternal = GetModulesInternal(loadIfNotFound: true, getResourceModules: false);
			if (modulesInternal.Length == 1)
			{
				return modulesInternal[0].GetDefinedTypes();
			}
			List<RuntimeType> list = new List<RuntimeType>();
			for (int i = 0; i < modulesInternal.Length; i++)
			{
				list.AddRange(modulesInternal[i].GetDefinedTypes());
			}
			return list.ToArray();
		}
	}

	public override bool IsCollectible
	{
		get
		{
			RuntimeAssembly assembly = this;
			return GetIsCollectible(new QCallAssembly(ref assembly)) != Interop.BOOL.FALSE;
		}
	}

	public override Module ManifestModule => GetManifestModule(GetNativeHandle());

	public override bool ReflectionOnly => false;

	public override string Location
	{
		get
		{
			string s = null;
			RuntimeAssembly assembly = this;
			GetLocation(new QCallAssembly(ref assembly), new StringHandleOnStack(ref s));
			return s;
		}
	}

	public override string ImageRuntimeVersion
	{
		get
		{
			string s = null;
			RuntimeAssembly assembly = this;
			GetImageRuntimeVersion(new QCallAssembly(ref assembly), new StringHandleOnStack(ref s));
			return s;
		}
	}

	[Obsolete("The Global Assembly Cache is not supported.", DiagnosticId = "SYSLIB0005", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public override bool GlobalAssemblyCache => false;

	public override long HostContext => 0L;

	public override bool IsDynamic => FCallIsDynamic(GetNativeHandle());

	private event ModuleResolveEventHandler _ModuleResolve;

	public override event ModuleResolveEventHandler ModuleResolve
	{
		add
		{
			_ModuleResolve += value;
		}
		remove
		{
			_ModuleResolve -= value;
		}
	}

	internal RuntimeAssembly()
	{
		throw new NotSupportedException();
	}

	internal IntPtr GetUnderlyingNativeHandle()
	{
		return m_assembly;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern bool GetCodeBase(QCallAssembly assembly, StringHandleOnStack retString);

	internal string GetCodeBase()
	{
		string s = null;
		RuntimeAssembly assembly = this;
		if (GetCodeBase(new QCallAssembly(ref assembly), new StringHandleOnStack(ref s)))
		{
			return s;
		}
		return null;
	}

	internal RuntimeAssembly GetNativeHandle()
	{
		return this;
	}

	public override AssemblyName GetName(bool copiedName)
	{
		string codeBase = GetCodeBase();
		AssemblyName assemblyName = new AssemblyName(GetSimpleName(), GetPublicKey(), null, GetVersion(), GetLocale(), GetHashAlgorithm(), AssemblyVersionCompatibility.SameMachine, codeBase, GetFlags() | AssemblyNameFlags.PublicKey);
		Module manifestModule = ManifestModule;
		if (manifestModule.MDStreamVersion > 65536)
		{
			manifestModule.GetPEKind(out var peKind, out var machine);
			assemblyName.SetProcArchIndex(peKind, machine);
		}
		return assemblyName;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetFullName(QCallAssembly assembly, StringHandleOnStack retString);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetEntryPoint(QCallAssembly assembly, ObjectHandleOnStack retMethod);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetType(QCallAssembly assembly, string name, bool throwOnError, bool ignoreCase, ObjectHandleOnStack type, ObjectHandleOnStack keepAlive, ObjectHandleOnStack assemblyLoadContext);

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type GetType(string name, bool throwOnError, bool ignoreCase)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		RuntimeType o = null;
		object o2 = null;
		AssemblyLoadContext o3 = AssemblyLoadContext.CurrentContextualReflectionContext;
		RuntimeAssembly assembly = this;
		GetType(new QCallAssembly(ref assembly), name, throwOnError, ignoreCase, ObjectHandleOnStack.Create(ref o), ObjectHandleOnStack.Create(ref o2), ObjectHandleOnStack.Create(ref o3));
		GC.KeepAlive(o2);
		return o;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetExportedTypes(QCallAssembly assembly, ObjectHandleOnStack retTypes);

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type[] GetExportedTypes()
	{
		Type[] o = null;
		RuntimeAssembly assembly = this;
		GetExportedTypes(new QCallAssembly(ref assembly), ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern Interop.BOOL GetIsCollectible(QCallAssembly assembly);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private unsafe static extern byte* GetResource(QCallAssembly assembly, string resourceName, out uint length);

	public override Stream GetManifestResourceStream(Type type, string name)
	{
		if (type == null && name == null)
		{
			throw new ArgumentNullException("type");
		}
		string text = type?.Namespace;
		char ptr = Type.Delimiter;
		string name2 = ((text != null && name != null) ? string.Concat(text, new ReadOnlySpan<char>(ref ptr, 1), name) : (text + name));
		return GetManifestResourceStream(name2);
	}

	public unsafe override Stream GetManifestResourceStream(string name)
	{
		RuntimeAssembly assembly = this;
		uint length;
		byte* resource = GetResource(new QCallAssembly(ref assembly), name, out length);
		if (resource != null)
		{
			return new ManifestResourceStream(this, resource, length, length, FileAccess.Read);
		}
		return null;
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		RuntimeType runtimeType = attributeType.UnderlyingSystemType as RuntimeType;
		if (runtimeType == null)
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.GetCustomAttributes(this, runtimeType);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		RuntimeType runtimeType = attributeType.UnderlyingSystemType as RuntimeType;
		if (runtimeType == null)
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.IsDefined(this, runtimeType);
	}

	public override IList<CustomAttributeData> GetCustomAttributesData()
	{
		return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
	}

	internal static RuntimeAssembly InternalLoad(string assemblyName, ref StackCrawlMark stackMark, AssemblyLoadContext assemblyLoadContext = null)
	{
		return InternalLoad(new AssemblyName(assemblyName), ref stackMark, assemblyLoadContext);
	}

	internal static RuntimeAssembly InternalLoad(AssemblyName assemblyName, ref StackCrawlMark stackMark, AssemblyLoadContext assemblyLoadContext = null)
	{
		return InternalLoad(assemblyName, null, ref stackMark, throwOnFileNotFound: true, assemblyLoadContext);
	}

	internal static RuntimeAssembly InternalLoad(AssemblyName assemblyName, RuntimeAssembly requestingAssembly, ref StackCrawlMark stackMark, bool throwOnFileNotFound, AssemblyLoadContext assemblyLoadContext = null)
	{
		RuntimeAssembly o = null;
		InternalLoad(ObjectHandleOnStack.Create(ref assemblyName), ObjectHandleOnStack.Create(ref requestingAssembly), new StackCrawlMarkHandle(ref stackMark), throwOnFileNotFound, ObjectHandleOnStack.Create(ref assemblyLoadContext), ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void InternalLoad(ObjectHandleOnStack assemblyName, ObjectHandleOnStack requestingAssembly, StackCrawlMarkHandle stackMark, bool throwOnFileNotFound, ObjectHandleOnStack assemblyLoadContext, ObjectHandleOnStack retAssembly);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetModule(QCallAssembly assembly, string name, ObjectHandleOnStack retModule);

	public override Module GetModule(string name)
	{
		Module o = null;
		RuntimeAssembly assembly = this;
		GetModule(new QCallAssembly(ref assembly), name, ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public override FileStream GetFile(string name)
	{
		if (Location.Length == 0)
		{
			throw new FileNotFoundException(SR.IO_NoFileTableInInMemoryAssemblies);
		}
		RuntimeModule runtimeModule = (RuntimeModule)GetModule(name);
		if (runtimeModule == null)
		{
			return null;
		}
		return new FileStream(runtimeModule.GetFullyQualifiedName(), FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: false);
	}

	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public override FileStream[] GetFiles(bool getResourceModules)
	{
		if (Location.Length == 0)
		{
			throw new FileNotFoundException(SR.IO_NoFileTableInInMemoryAssemblies);
		}
		Module[] modules = GetModules(getResourceModules);
		FileStream[] array = new FileStream[modules.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new FileStream(((RuntimeModule)modules[i]).GetFullyQualifiedName(), FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: false);
		}
		return array;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern string[] GetManifestResourceNames(RuntimeAssembly assembly);

	public override string[] GetManifestResourceNames()
	{
		return GetManifestResourceNames(GetNativeHandle());
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern AssemblyName[] GetReferencedAssemblies(RuntimeAssembly assembly);

	[RequiresUnreferencedCode("Assembly references might be removed")]
	public override AssemblyName[] GetReferencedAssemblies()
	{
		return GetReferencedAssemblies(GetNativeHandle());
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern int GetManifestResourceInfo(QCallAssembly assembly, string resourceName, ObjectHandleOnStack assemblyRef, StringHandleOnStack retFileName);

	public override ManifestResourceInfo GetManifestResourceInfo(string resourceName)
	{
		RuntimeAssembly o = null;
		string s = null;
		RuntimeAssembly assembly = this;
		int manifestResourceInfo = GetManifestResourceInfo(new QCallAssembly(ref assembly), resourceName, ObjectHandleOnStack.Create(ref o), new StringHandleOnStack(ref s));
		if (manifestResourceInfo == -1)
		{
			return null;
		}
		return new ManifestResourceInfo(o, s, (ResourceLocation)manifestResourceInfo);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetLocation(QCallAssembly assembly, StringHandleOnStack retString);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetImageRuntimeVersion(QCallAssembly assembly, StringHandleOnStack retString);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetVersion(QCallAssembly assembly, out int majVer, out int minVer, out int buildNum, out int revNum);

	internal Version GetVersion()
	{
		RuntimeAssembly assembly = this;
		GetVersion(new QCallAssembly(ref assembly), out var majVer, out var minVer, out var buildNum, out var revNum);
		return new Version(majVer, minVer, buildNum, revNum);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetLocale(QCallAssembly assembly, StringHandleOnStack retString);

	internal CultureInfo GetLocale()
	{
		string s = null;
		RuntimeAssembly assembly = this;
		GetLocale(new QCallAssembly(ref assembly), new StringHandleOnStack(ref s));
		if (s == null)
		{
			return CultureInfo.InvariantCulture;
		}
		return CultureInfo.GetCultureInfo(s);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool FCallIsDynamic(RuntimeAssembly assembly);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetSimpleName(QCallAssembly assembly, StringHandleOnStack retSimpleName);

	internal string GetSimpleName()
	{
		RuntimeAssembly assembly = this;
		string s = null;
		GetSimpleName(new QCallAssembly(ref assembly), new StringHandleOnStack(ref s));
		return s;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern AssemblyHashAlgorithm GetHashAlgorithm(QCallAssembly assembly);

	private AssemblyHashAlgorithm GetHashAlgorithm()
	{
		RuntimeAssembly assembly = this;
		return GetHashAlgorithm(new QCallAssembly(ref assembly));
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern AssemblyNameFlags GetFlags(QCallAssembly assembly);

	private AssemblyNameFlags GetFlags()
	{
		RuntimeAssembly assembly = this;
		return GetFlags(new QCallAssembly(ref assembly));
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetPublicKey(QCallAssembly assembly, ObjectHandleOnStack retPublicKey);

	internal byte[] GetPublicKey()
	{
		byte[] o = null;
		RuntimeAssembly assembly = this;
		GetPublicKey(new QCallAssembly(ref assembly), ObjectHandleOnStack.Create(ref o));
		return o;
	}

	public override Assembly GetSatelliteAssembly(CultureInfo culture)
	{
		return GetSatelliteAssembly(culture, null);
	}

	public override Assembly GetSatelliteAssembly(CultureInfo culture, Version version)
	{
		if (culture == null)
		{
			throw new ArgumentNullException("culture");
		}
		return InternalGetSatelliteAssembly(culture, version, throwOnFileNotFound: true);
	}

	internal Assembly InternalGetSatelliteAssembly(CultureInfo culture, Version version, bool throwOnFileNotFound)
	{
		AssemblyName assemblyName = new AssemblyName();
		assemblyName.SetPublicKey(GetPublicKey());
		assemblyName.Flags = GetFlags() | AssemblyNameFlags.PublicKey;
		assemblyName.Version = version ?? GetVersion();
		assemblyName.CultureInfo = culture;
		assemblyName.Name = GetSimpleName() + ".resources";
		StackCrawlMark stackMark = StackCrawlMark.LookForMe;
		RuntimeAssembly runtimeAssembly = InternalLoad(assemblyName, this, ref stackMark, throwOnFileNotFound);
		if (runtimeAssembly == this)
		{
			runtimeAssembly = null;
		}
		if (runtimeAssembly == null && throwOnFileNotFound)
		{
			throw new FileNotFoundException(SR.Format(culture, SR.IO_FileNotFound_FileName, assemblyName.Name));
		}
		return runtimeAssembly;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetModules(QCallAssembly assembly, bool loadIfNotFound, bool getResourceModules, ObjectHandleOnStack retModuleHandles);

	private RuntimeModule[] GetModulesInternal(bool loadIfNotFound, bool getResourceModules)
	{
		RuntimeModule[] o = null;
		RuntimeAssembly assembly = this;
		GetModules(new QCallAssembly(ref assembly), loadIfNotFound, getResourceModules, ObjectHandleOnStack.Create(ref o));
		return o;
	}

	public override Module[] GetModules(bool getResourceModules)
	{
		return GetModulesInternal(loadIfNotFound: true, getResourceModules);
	}

	public override Module[] GetLoadedModules(bool getResourceModules)
	{
		return GetModulesInternal(loadIfNotFound: false, getResourceModules);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeModule GetManifestModule(RuntimeAssembly assembly);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int GetToken(RuntimeAssembly assembly);

	[RequiresUnreferencedCode("Types might be removed")]
	public sealed override Type[] GetForwardedTypes()
	{
		List<Type> list = new List<Type>();
		List<Exception> list2 = new List<Exception>();
		GetManifestModule(GetNativeHandle()).MetadataImport.Enum(MetadataTokenType.ExportedType, 0, out var result);
		RuntimeAssembly assembly = this;
		QCallAssembly assembly2 = new QCallAssembly(ref assembly);
		for (int i = 0; i < result.Length; i++)
		{
			MetadataToken mdtExternalType = result[i];
			Type o = null;
			Exception item = null;
			ObjectHandleOnStack type = ObjectHandleOnStack.Create(ref o);
			try
			{
				GetForwardedType(assembly2, mdtExternalType, type);
				if (o == null)
				{
					continue;
				}
			}
			catch (Exception ex)
			{
				o = null;
				item = ex;
			}
			if (o != null)
			{
				list.Add(o);
				AddPublicNestedTypes(o, list, list2);
			}
			else
			{
				list2.Add(item);
			}
		}
		if (list2.Count != 0)
		{
			int count = list.Count;
			int count2 = list2.Count;
			list.AddRange(new Type[count2]);
			list2.InsertRange(0, new Exception[count]);
			throw new ReflectionTypeLoadException(list.ToArray(), list2.ToArray());
		}
		return list.ToArray();
	}

	[RequiresUnreferencedCode("Types might be removed because recursive nested types can't currently be annotated for dynamic access.")]
	private static void AddPublicNestedTypes(Type type, List<Type> types, List<Exception> exceptions)
	{
		Type[] nestedTypes;
		try
		{
			nestedTypes = type.GetNestedTypes(BindingFlags.Public);
		}
		catch (Exception item)
		{
			exceptions.Add(item);
			return;
		}
		Type[] array = nestedTypes;
		foreach (Type type2 in array)
		{
			types.Add(type2);
			AddPublicNestedTypes(type2, types, exceptions);
		}
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetForwardedType(QCallAssembly assembly, MetadataToken mdtExternalType, ObjectHandleOnStack type);
}
