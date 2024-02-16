using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Runtime.Loader;

public class AssemblyLoadContext
{
	private enum InternalState
	{
		Alive,
		Unloading
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public struct ContextualReflectionScope : IDisposable
	{
		private readonly AssemblyLoadContext _activated;

		private readonly AssemblyLoadContext _predecessor;

		private readonly bool _initialized;

		internal ContextualReflectionScope(AssemblyLoadContext activating)
		{
			_predecessor = CurrentContextualReflectionContext;
			SetCurrentContextualReflectionContext(activating);
			_activated = activating;
			_initialized = true;
		}

		public void Dispose()
		{
			if (_initialized)
			{
				SetCurrentContextualReflectionContext(_predecessor);
			}
		}
	}

	private const string AssemblyLoadName = "AssemblyLoad";

	private static volatile Dictionary<long, WeakReference<AssemblyLoadContext>> s_allContexts;

	private static long s_nextId;

	private readonly IntPtr _nativeAssemblyLoadContext;

	private readonly object _unloadLock;

	private readonly string _name;

	private readonly long _id;

	private InternalState _state;

	private readonly bool _isCollectible;

	private static AsyncLocal<AssemblyLoadContext> s_asyncLocalCurrent;

	[MemberNotNull("s_allContexts")]
	private static Dictionary<long, WeakReference<AssemblyLoadContext>> AllContexts
	{
		[MemberNotNull("s_allContexts")]
		get
		{
			return s_allContexts ?? Interlocked.CompareExchange(ref s_allContexts, new Dictionary<long, WeakReference<AssemblyLoadContext>>(), null) ?? s_allContexts;
		}
	}

	public IEnumerable<Assembly> Assemblies
	{
		get
		{
			Assembly[] loadedAssemblies = GetLoadedAssemblies();
			foreach (Assembly assembly in loadedAssemblies)
			{
				AssemblyLoadContext loadContext = GetLoadContext(assembly);
				if (loadContext == this)
				{
					yield return assembly;
				}
			}
		}
	}

	public static AssemblyLoadContext Default => DefaultAssemblyLoadContext.s_loadContext;

	public bool IsCollectible => _isCollectible;

	public string? Name => _name;

	public static IEnumerable<AssemblyLoadContext> All
	{
		get
		{
			_ = Default;
			Dictionary<long, WeakReference<AssemblyLoadContext>> dictionary = s_allContexts;
			WeakReference<AssemblyLoadContext>[] array;
			lock (dictionary)
			{
				array = new WeakReference<AssemblyLoadContext>[dictionary.Count];
				int num = 0;
				foreach (KeyValuePair<long, WeakReference<AssemblyLoadContext>> item in dictionary)
				{
					array[num++] = item.Value;
				}
			}
			WeakReference<AssemblyLoadContext>[] array2 = array;
			foreach (WeakReference<AssemblyLoadContext> weakReference in array2)
			{
				if (weakReference.TryGetTarget(out var target))
				{
					yield return target;
				}
			}
		}
	}

	public static AssemblyLoadContext? CurrentContextualReflectionContext => s_asyncLocalCurrent?.Value;

	private event Func<Assembly, string, IntPtr>? _resolvingUnmanagedDll;

	private event Func<AssemblyLoadContext, AssemblyName, Assembly>? _resolving;

	private event Action<AssemblyLoadContext>? _unloading;

	public event Func<Assembly, string, IntPtr>? ResolvingUnmanagedDll
	{
		add
		{
			_resolvingUnmanagedDll += value;
		}
		remove
		{
			_resolvingUnmanagedDll -= value;
		}
	}

	public event Func<AssemblyLoadContext, AssemblyName, Assembly?>? Resolving
	{
		add
		{
			_resolving += value;
		}
		remove
		{
			_resolving -= value;
		}
	}

	public event Action<AssemblyLoadContext>? Unloading
	{
		add
		{
			_unloading += value;
		}
		remove
		{
			_unloading -= value;
		}
	}

	internal static event AssemblyLoadEventHandler? AssemblyLoad;

	internal static event ResolveEventHandler? TypeResolve;

	internal static event ResolveEventHandler? ResourceResolve;

	internal static event ResolveEventHandler? AssemblyResolve;

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern IntPtr InitializeAssemblyLoadContext(IntPtr ptrAssemblyLoadContext, bool fRepresentsTPALoadContext, bool isCollectible);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void PrepareForAssemblyLoadContextRelease(IntPtr ptrNativeAssemblyLoadContext, IntPtr ptrAssemblyLoadContextStrong);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	private static extern void LoadFromStream(IntPtr ptrNativeAssemblyLoadContext, IntPtr ptrAssemblyArray, int iAssemblyArrayLen, IntPtr ptrSymbols, int iSymbolArrayLen, ObjectHandleOnStack retAssembly);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern void InternalSetProfileRoot(string directoryPath);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern void InternalStartProfile(string profile, IntPtr ptrNativeAssemblyLoadContext);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	private static extern void LoadFromPath(IntPtr ptrNativeAssemblyLoadContext, string ilPath, string niPath, ObjectHandleOnStack retAssembly);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern Assembly[] GetLoadedAssemblies();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsTracingEnabled();

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern bool TraceResolvingHandlerInvoked(string assemblyName, string handlerName, string alcName, string resultAssemblyName, string resultAssemblyPath);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern bool TraceAssemblyResolveHandlerInvoked(string assemblyName, string handlerName, string resultAssemblyName, string resultAssemblyPath);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern bool TraceAssemblyLoadFromResolveHandlerInvoked(string assemblyName, bool isTrackedAssembly, string requestingAssemblyPath, string requestedAssemblyPath);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern bool TraceSatelliteSubdirectoryPathProbed(string filePath, int hResult);

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	private Assembly InternalLoadFromPath(string assemblyPath, string nativeImagePath)
	{
		RuntimeAssembly o = null;
		LoadFromPath(_nativeAssemblyLoadContext, assemblyPath, nativeImagePath, ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	internal unsafe Assembly InternalLoad(ReadOnlySpan<byte> arrAssembly, ReadOnlySpan<byte> arrSymbols)
	{
		RuntimeAssembly o = null;
		fixed (byte* value = arrAssembly)
		{
			fixed (byte* value2 = arrSymbols)
			{
				LoadFromStream(_nativeAssemblyLoadContext, new IntPtr(value), arrAssembly.Length, new IntPtr(value2), arrSymbols.Length, ObjectHandleOnStack.Create(ref o));
			}
		}
		return o;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern IntPtr LoadFromInMemoryModuleInternal(IntPtr ptrNativeAssemblyLoadContext, IntPtr hModule, ObjectHandleOnStack retAssembly);

	internal Assembly LoadFromInMemoryModule(IntPtr moduleHandle)
	{
		if (moduleHandle == IntPtr.Zero)
		{
			throw new ArgumentNullException("moduleHandle");
		}
		lock (_unloadLock)
		{
			VerifyIsAlive();
			RuntimeAssembly o = null;
			LoadFromInMemoryModuleInternal(_nativeAssemblyLoadContext, moduleHandle, ObjectHandleOnStack.Create(ref o));
			return o;
		}
	}

	private static Assembly ResolveSatelliteAssembly(IntPtr gchManagedAssemblyLoadContext, AssemblyName assemblyName)
	{
		AssemblyLoadContext assemblyLoadContext = (AssemblyLoadContext)GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target;
		return assemblyLoadContext.ResolveSatelliteAssembly(assemblyName);
	}

	private static IntPtr ResolveUnmanagedDll(string unmanagedDllName, IntPtr gchManagedAssemblyLoadContext)
	{
		AssemblyLoadContext assemblyLoadContext = (AssemblyLoadContext)GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target;
		return assemblyLoadContext.LoadUnmanagedDll(unmanagedDllName);
	}

	private static IntPtr ResolveUnmanagedDllUsingEvent(string unmanagedDllName, Assembly assembly, IntPtr gchManagedAssemblyLoadContext)
	{
		AssemblyLoadContext assemblyLoadContext = (AssemblyLoadContext)GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target;
		return assemblyLoadContext.GetResolvedUnmanagedDll(assembly, unmanagedDllName);
	}

	private static Assembly ResolveUsingResolvingEvent(IntPtr gchManagedAssemblyLoadContext, AssemblyName assemblyName)
	{
		AssemblyLoadContext assemblyLoadContext = (AssemblyLoadContext)GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target;
		return assemblyLoadContext.ResolveUsingEvent(assemblyName);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern IntPtr GetLoadContextForAssembly(QCallAssembly assembly);

	public static AssemblyLoadContext? GetLoadContext(Assembly assembly)
	{
		if (assembly == null)
		{
			throw new ArgumentNullException("assembly");
		}
		AssemblyLoadContext result = null;
		RuntimeAssembly runtimeAssembly = GetRuntimeAssembly(assembly);
		if (runtimeAssembly != null)
		{
			RuntimeAssembly assembly2 = runtimeAssembly;
			IntPtr loadContextForAssembly = GetLoadContextForAssembly(new QCallAssembly(ref assembly2));
			result = ((!(loadContextForAssembly == IntPtr.Zero)) ? ((AssemblyLoadContext)GCHandle.FromIntPtr(loadContextForAssembly).Target) : Default);
		}
		return result;
	}

	public void SetProfileOptimizationRoot(string directoryPath)
	{
		InternalSetProfileRoot(directoryPath);
	}

	public void StartProfileOptimization(string? profile)
	{
		InternalStartProfile(profile, _nativeAssemblyLoadContext);
	}

	private static RuntimeAssembly GetRuntimeAssembly(Assembly asm)
	{
		if (!(asm == null))
		{
			if (!(asm is RuntimeAssembly result))
			{
				if (!(asm is AssemblyBuilder assemblyBuilder))
				{
					return null;
				}
				return assemblyBuilder.InternalAssembly;
			}
			return result;
		}
		return null;
	}

	private static void StartAssemblyLoad(ref Guid activityId, ref Guid relatedActivityId)
	{
		ActivityTracker.Instance.Enable();
		ActivityTracker.Instance.OnStart(NativeRuntimeEventSource.Log.Name, "AssemblyLoad", 0, ref activityId, ref relatedActivityId, EventActivityOptions.Recursive, useTplSource: false);
	}

	private static void StopAssemblyLoad(ref Guid activityId)
	{
		ActivityTracker.Instance.OnStop(NativeRuntimeEventSource.Log.Name, "AssemblyLoad", 0, ref activityId, useTplSource: false);
	}

	private static void InitializeDefaultContext()
	{
		_ = Default;
	}

	protected AssemblyLoadContext()
		: this(representsTPALoadContext: false, isCollectible: false, null)
	{
	}

	protected AssemblyLoadContext(bool isCollectible)
		: this(representsTPALoadContext: false, isCollectible, null)
	{
	}

	public AssemblyLoadContext(string? name, bool isCollectible = false)
		: this(representsTPALoadContext: false, isCollectible, name)
	{
	}

	private protected AssemblyLoadContext(bool representsTPALoadContext, bool isCollectible, string name)
	{
		_isCollectible = isCollectible;
		_name = name;
		_unloadLock = new object();
		if (!isCollectible)
		{
			GC.SuppressFinalize(this);
		}
		_nativeAssemblyLoadContext = InitializeAssemblyLoadContext(GCHandle.ToIntPtr(GCHandle.Alloc(this, IsCollectible ? GCHandleType.WeakTrackResurrection : GCHandleType.Normal)), representsTPALoadContext, isCollectible);
		Dictionary<long, WeakReference<AssemblyLoadContext>> allContexts = AllContexts;
		lock (allContexts)
		{
			_id = s_nextId++;
			allContexts.Add(_id, new WeakReference<AssemblyLoadContext>(this, trackResurrection: true));
		}
	}

	~AssemblyLoadContext()
	{
		if (_unloadLock != null)
		{
			InitiateUnload();
		}
	}

	private void RaiseUnloadEvent()
	{
		Interlocked.Exchange(ref this._unloading, null)?.Invoke(this);
	}

	private void InitiateUnload()
	{
		RaiseUnloadEvent();
		lock (_unloadLock)
		{
			GCHandle value = GCHandle.Alloc(this, GCHandleType.Normal);
			IntPtr ptrAssemblyLoadContextStrong = GCHandle.ToIntPtr(value);
			PrepareForAssemblyLoadContextRelease(_nativeAssemblyLoadContext, ptrAssemblyLoadContextStrong);
			_state = InternalState.Unloading;
		}
		Dictionary<long, WeakReference<AssemblyLoadContext>> allContexts = AllContexts;
		lock (allContexts)
		{
			allContexts.Remove(_id);
		}
	}

	public override string ToString()
	{
		return $"\"{Name}\" {GetType()} #{_id}";
	}

	public static AssemblyName GetAssemblyName(string assemblyPath)
	{
		if (assemblyPath == null)
		{
			throw new ArgumentNullException("assemblyPath");
		}
		return AssemblyName.GetAssemblyName(assemblyPath);
	}

	protected virtual Assembly? Load(AssemblyName assemblyName)
	{
		return null;
	}

	public Assembly LoadFromAssemblyName(AssemblyName assemblyName)
	{
		if (assemblyName == null)
		{
			throw new ArgumentNullException("assemblyName");
		}
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
		return RuntimeAssembly.InternalLoad(assemblyName, ref stackMark, this);
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public Assembly LoadFromAssemblyPath(string assemblyPath)
	{
		if (assemblyPath == null)
		{
			throw new ArgumentNullException("assemblyPath");
		}
		if (PathInternal.IsPartiallyQualified(assemblyPath))
		{
			throw new ArgumentException(SR.Format(SR.Argument_AbsolutePathRequired, assemblyPath), "assemblyPath");
		}
		lock (_unloadLock)
		{
			VerifyIsAlive();
			return InternalLoadFromPath(assemblyPath, null);
		}
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public Assembly LoadFromNativeImagePath(string nativeImagePath, string? assemblyPath)
	{
		if (nativeImagePath == null)
		{
			throw new ArgumentNullException("nativeImagePath");
		}
		if (PathInternal.IsPartiallyQualified(nativeImagePath))
		{
			throw new ArgumentException(SR.Format(SR.Argument_AbsolutePathRequired, nativeImagePath), "nativeImagePath");
		}
		if (assemblyPath != null && PathInternal.IsPartiallyQualified(assemblyPath))
		{
			throw new ArgumentException(SR.Format(SR.Argument_AbsolutePathRequired, assemblyPath), "assemblyPath");
		}
		lock (_unloadLock)
		{
			VerifyIsAlive();
			return InternalLoadFromPath(assemblyPath, nativeImagePath);
		}
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public Assembly LoadFromStream(Stream assembly)
	{
		return LoadFromStream(assembly, null);
	}

	[RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
	public Assembly LoadFromStream(Stream assembly, Stream? assemblySymbols)
	{
		if (assembly == null)
		{
			throw new ArgumentNullException("assembly");
		}
		int num = (int)assembly.Length;
		if (num <= 0)
		{
			throw new BadImageFormatException(SR.BadImageFormat_BadILFormat);
		}
		byte[] array = new byte[num];
		assembly.Read(array, 0, num);
		byte[] array2 = null;
		if (assemblySymbols != null)
		{
			int num2 = (int)assemblySymbols.Length;
			array2 = new byte[num2];
			assemblySymbols.Read(array2, 0, num2);
		}
		lock (_unloadLock)
		{
			VerifyIsAlive();
			return InternalLoad(array, array2);
		}
	}

	protected IntPtr LoadUnmanagedDllFromPath(string unmanagedDllPath)
	{
		if (unmanagedDllPath == null)
		{
			throw new ArgumentNullException("unmanagedDllPath");
		}
		if (unmanagedDllPath.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "unmanagedDllPath");
		}
		if (PathInternal.IsPartiallyQualified(unmanagedDllPath))
		{
			throw new ArgumentException(SR.Format(SR.Argument_AbsolutePathRequired, unmanagedDllPath), "unmanagedDllPath");
		}
		return NativeLibrary.Load(unmanagedDllPath);
	}

	protected virtual IntPtr LoadUnmanagedDll(string unmanagedDllName)
	{
		return IntPtr.Zero;
	}

	public void Unload()
	{
		if (!IsCollectible)
		{
			throw new InvalidOperationException(SR.AssemblyLoadContext_Unload_CannotUnloadIfNotCollectible);
		}
		GC.SuppressFinalize(this);
		InitiateUnload();
	}

	internal static void OnProcessExit()
	{
		Dictionary<long, WeakReference<AssemblyLoadContext>> dictionary = s_allContexts;
		if (dictionary == null)
		{
			return;
		}
		lock (dictionary)
		{
			foreach (KeyValuePair<long, WeakReference<AssemblyLoadContext>> item in dictionary)
			{
				if (item.Value.TryGetTarget(out var target))
				{
					target.RaiseUnloadEvent();
				}
			}
		}
	}

	private void VerifyIsAlive()
	{
		if (_state != 0)
		{
			throw new InvalidOperationException(SR.AssemblyLoadContext_Verify_NotUnloading);
		}
	}

	private static void SetCurrentContextualReflectionContext(AssemblyLoadContext value)
	{
		if (s_asyncLocalCurrent == null)
		{
			Interlocked.CompareExchange(ref s_asyncLocalCurrent, new AsyncLocal<AssemblyLoadContext>(), null);
		}
		s_asyncLocalCurrent.Value = value;
	}

	public ContextualReflectionScope EnterContextualReflection()
	{
		return new ContextualReflectionScope(this);
	}

	public static ContextualReflectionScope EnterContextualReflection(Assembly? activating)
	{
		if (activating == null)
		{
			return new ContextualReflectionScope(null);
		}
		AssemblyLoadContext loadContext = GetLoadContext(activating);
		if (loadContext == null)
		{
			throw new ArgumentException(SR.Arg_MustBeRuntimeAssembly, "activating");
		}
		return loadContext.EnterContextualReflection();
	}

	private static Assembly Resolve(IntPtr gchManagedAssemblyLoadContext, AssemblyName assemblyName)
	{
		AssemblyLoadContext assemblyLoadContext = (AssemblyLoadContext)GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target;
		return assemblyLoadContext.ResolveUsingLoad(assemblyName);
	}

	[UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file", Justification = "The code handles the Assembly.Location equals null")]
	private Assembly GetFirstResolvedAssemblyFromResolvingEvent(AssemblyName assemblyName)
	{
		Assembly assembly = null;
		Func<AssemblyLoadContext, AssemblyName, Assembly> resolving = this._resolving;
		if (resolving != null)
		{
			Delegate[] invocationList = resolving.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				Func<AssemblyLoadContext, AssemblyName, Assembly> func = (Func<AssemblyLoadContext, AssemblyName, Assembly>)invocationList[i];
				assembly = func(this, assemblyName);
				if (IsTracingEnabled())
				{
					TraceResolvingHandlerInvoked(assemblyName.FullName, func.Method.Name, (this != Default) ? ToString() : Name, assembly?.FullName, (assembly != null && !assembly.IsDynamic) ? assembly.Location : null);
				}
				if (assembly != null)
				{
					return assembly;
				}
			}
		}
		return null;
	}

	private static Assembly ValidateAssemblyNameWithSimpleName(Assembly assembly, string requestedSimpleName)
	{
		if (string.IsNullOrEmpty(requestedSimpleName))
		{
			throw new ArgumentException(SR.ArgumentNull_AssemblyNameName);
		}
		string value = null;
		RuntimeAssembly runtimeAssembly = GetRuntimeAssembly(assembly);
		if (runtimeAssembly != null)
		{
			value = runtimeAssembly.GetSimpleName();
		}
		if (string.IsNullOrEmpty(value) || !requestedSimpleName.Equals(value, StringComparison.InvariantCultureIgnoreCase))
		{
			throw new InvalidOperationException(SR.Argument_CustomAssemblyLoadContextRequestedNameMismatch);
		}
		return assembly;
	}

	private Assembly ResolveUsingLoad(AssemblyName assemblyName)
	{
		string name = assemblyName.Name;
		Assembly assembly = Load(assemblyName);
		if (assembly != null)
		{
			assembly = ValidateAssemblyNameWithSimpleName(assembly, name);
		}
		return assembly;
	}

	private Assembly ResolveUsingEvent(AssemblyName assemblyName)
	{
		string name = assemblyName.Name;
		Assembly assembly = GetFirstResolvedAssemblyFromResolvingEvent(assemblyName);
		if (assembly != null)
		{
			assembly = ValidateAssemblyNameWithSimpleName(assembly, name);
		}
		return assembly;
	}

	private static void OnAssemblyLoad(RuntimeAssembly assembly)
	{
		AssemblyLoadContext.AssemblyLoad?.Invoke(AppDomain.CurrentDomain, new AssemblyLoadEventArgs(assembly));
	}

	private static RuntimeAssembly OnResourceResolve(RuntimeAssembly assembly, string resourceName)
	{
		return InvokeResolveEvent(AssemblyLoadContext.ResourceResolve, assembly, resourceName);
	}

	private static RuntimeAssembly OnTypeResolve(RuntimeAssembly assembly, string typeName)
	{
		return InvokeResolveEvent(AssemblyLoadContext.TypeResolve, assembly, typeName);
	}

	private static RuntimeAssembly OnAssemblyResolve(RuntimeAssembly assembly, string assemblyFullName)
	{
		return InvokeResolveEvent(AssemblyLoadContext.AssemblyResolve, assembly, assemblyFullName);
	}

	[UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file", Justification = "The code handles the Assembly.Location equals null")]
	private static RuntimeAssembly InvokeResolveEvent(ResolveEventHandler eventHandler, RuntimeAssembly assembly, string name)
	{
		if (eventHandler == null)
		{
			return null;
		}
		ResolveEventArgs args = new ResolveEventArgs(name, assembly);
		Delegate[] invocationList = eventHandler.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			ResolveEventHandler resolveEventHandler = (ResolveEventHandler)invocationList[i];
			Assembly assembly2 = resolveEventHandler(AppDomain.CurrentDomain, args);
			if (eventHandler == AssemblyLoadContext.AssemblyResolve && IsTracingEnabled())
			{
				TraceAssemblyResolveHandlerInvoked(name, resolveEventHandler.Method.Name, assembly2?.FullName, (assembly2 != null && !assembly2.IsDynamic) ? assembly2.Location : null);
			}
			RuntimeAssembly runtimeAssembly = GetRuntimeAssembly(assembly2);
			if (runtimeAssembly != null)
			{
				return runtimeAssembly;
			}
		}
		return null;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Satellite assemblies have no code in them and loading is not a problem")]
	[UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file", Justification = "This call is fine because native call runs before this and checks BindSatelliteResourceFromBundle")]
	private Assembly ResolveSatelliteAssembly(AssemblyName assemblyName)
	{
		if (assemblyName.Name == null || !assemblyName.Name.EndsWith(".resources", StringComparison.Ordinal))
		{
			return null;
		}
		string assemblyName2 = assemblyName.Name.Substring(0, assemblyName.Name.Length - ".resources".Length);
		Assembly assembly = LoadFromAssemblyName(new AssemblyName(assemblyName2));
		AssemblyLoadContext loadContext = GetLoadContext(assembly);
		string directoryName = Path.GetDirectoryName(assembly.Location);
		if (directoryName == null)
		{
			return null;
		}
		string text = Path.Combine(directoryName, assemblyName.CultureName, assemblyName.Name + ".dll");
		bool flag = FileSystem.FileExists(text);
		if (flag || PathInternal.IsCaseSensitive)
		{
		}
		Assembly result = (flag ? loadContext.LoadFromAssemblyPath(text) : null);
		if (IsTracingEnabled())
		{
			TraceSatelliteSubdirectoryPathProbed(text, (!flag) ? (-2147024894) : 0);
		}
		return result;
	}

	internal IntPtr GetResolvedUnmanagedDll(Assembly assembly, string unmanagedDllName)
	{
		IntPtr zero = IntPtr.Zero;
		Func<Assembly, string, IntPtr> resolvingUnmanagedDll = this._resolvingUnmanagedDll;
		if (resolvingUnmanagedDll != null)
		{
			Delegate[] invocationList = resolvingUnmanagedDll.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				Func<Assembly, string, IntPtr> func = (Func<Assembly, string, IntPtr>)invocationList[i];
				zero = func(assembly, unmanagedDllName);
				if (zero != IntPtr.Zero)
				{
					return zero;
				}
			}
		}
		return IntPtr.Zero;
	}
}
