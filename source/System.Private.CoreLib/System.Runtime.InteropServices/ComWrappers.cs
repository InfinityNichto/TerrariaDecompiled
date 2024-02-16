using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices;

[UnsupportedOSPlatform("android")]
[UnsupportedOSPlatform("browser")]
[UnsupportedOSPlatform("ios")]
[UnsupportedOSPlatform("tvos")]
[CLSCompliant(false)]
public abstract class ComWrappers
{
	public struct ComInterfaceDispatch
	{
		private struct ComInterfaceInstance
		{
			public IntPtr GcHandle;
		}

		public IntPtr Vtable;

		public unsafe static T GetInstance<T>(ComInterfaceDispatch* dispatchPtr) where T : class
		{
			ComInterfaceInstance* ptr = *(ComInterfaceInstance**)((ulong)dispatchPtr & 0xFFFFFFFFFFFFFFF0uL);
			return Unsafe.As<T>(GCHandle.InternalGet(ptr->GcHandle));
		}
	}

	public struct ComInterfaceEntry
	{
		public Guid IID;

		public IntPtr Vtable;
	}

	private static ComWrappers s_globalInstanceForTrackerSupport;

	private static ComWrappers s_globalInstanceForMarshalling;

	private static long s_instanceCounter;

	private readonly long id = Interlocked.Increment(ref s_instanceCounter);

	public IntPtr GetOrCreateComInterfaceForObject(object instance, CreateComInterfaceFlags flags)
	{
		if (!TryGetOrCreateComInterfaceForObjectInternal(this, instance, flags, out var retValue))
		{
			throw new ArgumentException(null, "instance");
		}
		return retValue;
	}

	private static bool TryGetOrCreateComInterfaceForObjectInternal(ComWrappers impl, object instance, CreateComInterfaceFlags flags, out IntPtr retValue)
	{
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		return TryGetOrCreateComInterfaceForObjectInternal(ObjectHandleOnStack.Create(ref impl), impl.id, ObjectHandleOnStack.Create(ref instance), flags, out retValue);
	}

	[DllImport("QCall")]
	private static extern bool TryGetOrCreateComInterfaceForObjectInternal(ObjectHandleOnStack comWrappersImpl, long wrapperId, ObjectHandleOnStack instance, CreateComInterfaceFlags flags, out IntPtr retValue);

	internal unsafe static void* CallComputeVtables(ComWrappersScenario scenario, ComWrappers comWrappersImpl, object obj, CreateComInterfaceFlags flags, out int count)
	{
		ComWrappers comWrappers = null;
		switch (scenario)
		{
		case ComWrappersScenario.Instance:
			comWrappers = comWrappersImpl;
			break;
		case ComWrappersScenario.TrackerSupportGlobalInstance:
			comWrappers = s_globalInstanceForTrackerSupport;
			break;
		case ComWrappersScenario.MarshallingGlobalInstance:
			comWrappers = s_globalInstanceForMarshalling;
			break;
		}
		if (comWrappers == null)
		{
			count = -1;
			return null;
		}
		return comWrappers.ComputeVtables(obj, flags, out count);
	}

	public object GetOrCreateObjectForComInstance(IntPtr externalComObject, CreateObjectFlags flags)
	{
		if (!TryGetOrCreateObjectForComInstanceInternal(this, externalComObject, IntPtr.Zero, flags, null, out var retValue))
		{
			throw new ArgumentNullException("externalComObject");
		}
		return retValue;
	}

	internal static object CallCreateObject(ComWrappersScenario scenario, ComWrappers comWrappersImpl, IntPtr externalComObject, CreateObjectFlags flags)
	{
		ComWrappers comWrappers = null;
		switch (scenario)
		{
		case ComWrappersScenario.Instance:
			comWrappers = comWrappersImpl;
			break;
		case ComWrappersScenario.TrackerSupportGlobalInstance:
			comWrappers = s_globalInstanceForTrackerSupport;
			break;
		case ComWrappersScenario.MarshallingGlobalInstance:
			comWrappers = s_globalInstanceForMarshalling;
			break;
		}
		return comWrappers?.CreateObject(externalComObject, flags);
	}

	public object GetOrRegisterObjectForComInstance(IntPtr externalComObject, CreateObjectFlags flags, object wrapper)
	{
		return GetOrRegisterObjectForComInstance(externalComObject, flags, wrapper, IntPtr.Zero);
	}

	public object GetOrRegisterObjectForComInstance(IntPtr externalComObject, CreateObjectFlags flags, object wrapper, IntPtr inner)
	{
		if (wrapper == null)
		{
			throw new ArgumentNullException("wrapper");
		}
		if (!TryGetOrCreateObjectForComInstanceInternal(this, externalComObject, inner, flags, wrapper, out var retValue))
		{
			throw new ArgumentNullException("externalComObject");
		}
		return retValue;
	}

	private static bool TryGetOrCreateObjectForComInstanceInternal(ComWrappers impl, IntPtr externalComObject, IntPtr innerMaybe, CreateObjectFlags flags, object wrapperMaybe, out object retValue)
	{
		if (externalComObject == IntPtr.Zero)
		{
			throw new ArgumentNullException("externalComObject");
		}
		if (innerMaybe != IntPtr.Zero && !flags.HasFlag(CreateObjectFlags.Aggregation))
		{
			throw new InvalidOperationException(SR.InvalidOperation_SuppliedInnerMustBeMarkedAggregation);
		}
		object o = wrapperMaybe;
		retValue = null;
		return TryGetOrCreateObjectForComInstanceInternal(ObjectHandleOnStack.Create(ref impl), impl.id, externalComObject, innerMaybe, flags, ObjectHandleOnStack.Create(ref o), ObjectHandleOnStack.Create(ref retValue));
	}

	[DllImport("QCall")]
	private static extern bool TryGetOrCreateObjectForComInstanceInternal(ObjectHandleOnStack comWrappersImpl, long wrapperId, IntPtr externalComObject, IntPtr innerMaybe, CreateObjectFlags flags, ObjectHandleOnStack wrapper, ObjectHandleOnStack retValue);

	internal static void CallReleaseObjects(ComWrappers comWrappersImpl, IEnumerable objects)
	{
		(comWrappersImpl ?? s_globalInstanceForTrackerSupport).ReleaseObjects(objects);
	}

	public static void RegisterForTrackerSupport(ComWrappers instance)
	{
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		if (Interlocked.CompareExchange(ref s_globalInstanceForTrackerSupport, instance, null) != null)
		{
			throw new InvalidOperationException(SR.InvalidOperation_ResetGlobalComWrappersInstance);
		}
		SetGlobalInstanceRegisteredForTrackerSupport(instance.id);
	}

	[DllImport("QCall")]
	[SuppressGCTransition]
	private static extern void SetGlobalInstanceRegisteredForTrackerSupport(long id);

	[SupportedOSPlatform("windows")]
	public static void RegisterForMarshalling(ComWrappers instance)
	{
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		if (Interlocked.CompareExchange(ref s_globalInstanceForMarshalling, instance, null) != null)
		{
			throw new InvalidOperationException(SR.InvalidOperation_ResetGlobalComWrappersInstance);
		}
		SetGlobalInstanceRegisteredForMarshalling(instance.id);
	}

	[DllImport("QCall")]
	[SuppressGCTransition]
	private static extern void SetGlobalInstanceRegisteredForMarshalling(long id);

	protected static void GetIUnknownImpl(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease)
	{
		GetIUnknownImplInternal(out fpQueryInterface, out fpAddRef, out fpRelease);
	}

	[DllImport("QCall")]
	private static extern void GetIUnknownImplInternal(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease);

	internal static int CallICustomQueryInterface(object customQueryInterfaceMaybe, ref Guid iid, out IntPtr ppObject)
	{
		if (!(customQueryInterfaceMaybe is ICustomQueryInterface customQueryInterface))
		{
			ppObject = IntPtr.Zero;
			return -1;
		}
		return (int)customQueryInterface.GetInterface(ref iid, out ppObject);
	}

	protected unsafe abstract ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count);

	protected abstract object? CreateObject(IntPtr externalComObject, CreateObjectFlags flags);

	protected abstract void ReleaseObjects(IEnumerable objects);
}
