using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices;

public static class RuntimeHelpers
{
	public delegate void TryCode(object? userData);

	public delegate void CleanupCode(object? userData, bool exceptionThrown);

	public static int OffsetToStringData
	{
		[NonVersionable]
		get
		{
			return 12;
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern void InitializeArray(Array array, RuntimeFieldHandle fldHandle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[return: NotNullIfNotNull("obj")]
	public static extern object? GetObjectValue(object? obj);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void RunClassConstructor(QCallTypeHandle type);

	[RequiresUnreferencedCode("Trimmer can't guarantee existence of class constructor")]
	public static void RunClassConstructor(RuntimeTypeHandle type)
	{
		RuntimeType type2 = type.GetRuntimeType();
		if ((object)type2 == null)
		{
			throw new ArgumentException(SR.InvalidOperation_HandleIsNotInitialized, "type");
		}
		RunClassConstructor(new QCallTypeHandle(ref type2));
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void RunModuleConstructor(QCallModule module);

	public static void RunModuleConstructor(ModuleHandle module)
	{
		RuntimeModule module2 = module.GetRuntimeModule();
		if ((object)module2 == null)
		{
			throw new ArgumentException(SR.InvalidOperation_HandleIsNotInitialized, "module");
		}
		RunModuleConstructor(new QCallModule(ref module2));
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern void CompileMethod(RuntimeMethodHandleInternal method);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private unsafe static extern void PrepareMethod(RuntimeMethodHandleInternal method, IntPtr* pInstantiation, int cInstantiation);

	public static void PrepareMethod(RuntimeMethodHandle method)
	{
		PrepareMethod(method, null);
	}

	public unsafe static void PrepareMethod(RuntimeMethodHandle method, RuntimeTypeHandle[]? instantiation)
	{
		IRuntimeMethodInfo methodInfo = method.GetMethodInfo();
		if (methodInfo == null)
		{
			throw new ArgumentException(SR.InvalidOperation_HandleIsNotInitialized, "method");
		}
		instantiation = (RuntimeTypeHandle[])instantiation?.Clone();
		int length;
		fixed (IntPtr* pInstantiation = RuntimeTypeHandle.CopyRuntimeTypeHandles(instantiation, out length))
		{
			PrepareMethod(methodInfo.Value, pInstantiation, length);
			GC.KeepAlive(instantiation);
			GC.KeepAlive(methodInfo);
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern void PrepareDelegate(Delegate d);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern int GetHashCode(object? o);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public new static extern bool Equals(object? o1, object? o2);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern void EnsureSufficientExecutionStack();

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern bool TryEnsureSufficientExecutionStack();

	public static object GetUninitializedObject([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type)
	{
		RuntimeType type2 = type as RuntimeType;
		if ((object)type2 == null)
		{
			if ((object)type == null)
			{
				throw new ArgumentNullException("type", SR.ArgumentNull_Type);
			}
			throw new SerializationException(SR.Format(SR.Serialization_InvalidType, type));
		}
		object o = null;
		GetUninitializedObject(new QCallTypeHandle(ref type2), ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[DllImport("QCall")]
	private static extern void GetUninitializedObject(QCallTypeHandle type, ObjectHandleOnStack retObject);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern object AllocateUninitializedClone(object obj);

	[Intrinsic]
	public static bool IsReferenceOrContainsReferences<T>()
	{
		throw new InvalidOperationException();
	}

	[Intrinsic]
	internal static bool IsBitwiseEquatable<T>()
	{
		throw new InvalidOperationException();
	}

	[Intrinsic]
	internal static bool EnumEquals<T>(T x, T y) where T : struct, Enum
	{
		return x.Equals(y);
	}

	[Intrinsic]
	internal static int EnumCompareTo<T>(T x, T y) where T : struct, Enum
	{
		return x.CompareTo(y);
	}

	internal static ref byte GetRawData(this object obj)
	{
		return ref Unsafe.As<RawData>(obj).Data;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static nuint GetRawObjectDataSize(object obj)
	{
		MethodTable* methodTable = GetMethodTable(obj);
		nuint num = (nuint)methodTable->BaseSize - (nuint)(2 * sizeof(IntPtr));
		if (methodTable->HasComponentSize)
		{
			num += (nuint)((nint)Unsafe.As<RawArrayData>(obj).Length * (nint)methodTable->ComponentSize);
		}
		GC.KeepAlive(obj);
		return num;
	}

	internal unsafe static ushort GetElementSize(this Array array)
	{
		return GetMethodTable(array)->ComponentSize;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ref int GetMultiDimensionalArrayBounds(Array array)
	{
		return ref Unsafe.As<byte, int>(ref Unsafe.As<RawArrayData>(array).Data);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static int GetMultiDimensionalArrayRank(Array array)
	{
		int multiDimensionalArrayRank = GetMethodTable(array)->MultiDimensionalArrayRank;
		GC.KeepAlive(array);
		return multiDimensionalArrayRank;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static bool ObjectHasComponentSize(object obj)
	{
		return GetMethodTable(obj)->HasComponentSize;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	internal unsafe static MethodTable* GetMethodTable(object obj)
	{
		return (MethodTable*)(void*)Unsafe.Add(ref Unsafe.As<byte, IntPtr>(ref obj.GetRawData()), -1);
	}

	public static IntPtr AllocateTypeAssociatedMemory(Type type, int size)
	{
		RuntimeType type2 = type as RuntimeType;
		if (type2 == null)
		{
			throw new ArgumentException(SR.Arg_MustBeType, "type");
		}
		if (size < 0)
		{
			throw new ArgumentOutOfRangeException("size");
		}
		return AllocateTypeAssociatedMemory(new QCallTypeHandle(ref type2), (uint)size);
	}

	[DllImport("QCall")]
	private static extern IntPtr AllocateTypeAssociatedMemory(QCallTypeHandle type, uint size);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern IntPtr AllocTailCallArgBuffer(int size, IntPtr gcDesc);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern TailCallTls* GetTailCallInfo(IntPtr retAddrSlot, IntPtr* retAddr);

	[StackTraceHidden]
	private unsafe static void DispatchTailCalls(IntPtr callersRetAddrSlot, delegate*<IntPtr, IntPtr, PortableTailCallFrame*, void> callTarget, IntPtr retVal)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out IntPtr intPtr);
		TailCallTls* tailCallInfo = GetTailCallInfo(callersRetAddrSlot, &intPtr);
		PortableTailCallFrame* frame = tailCallInfo->Frame;
		if (intPtr == frame->TailCallAwareReturnAddress)
		{
			frame->NextCall = callTarget;
			return;
		}
		System.Runtime.CompilerServices.Unsafe.SkipInit(out PortableTailCallFrame portableTailCallFrame);
		portableTailCallFrame.NextCall = null;
		try
		{
			tailCallInfo->Frame = &portableTailCallFrame;
			do
			{
				callTarget(tailCallInfo->ArgBuffer, retVal, &portableTailCallFrame);
				callTarget = portableTailCallFrame.NextCall;
			}
			while (callTarget != (delegate*<IntPtr, IntPtr, PortableTailCallFrame*, void>)null);
		}
		finally
		{
			tailCallInfo->Frame = frame;
			if (tailCallInfo->ArgBuffer != IntPtr.Zero && *(int*)(void*)tailCallInfo->ArgBuffer == 1)
			{
				*(int*)(void*)tailCallInfo->ArgBuffer = 2;
			}
		}
	}

	public static T[] GetSubArray<T>(T[] array, Range range)
	{
		if (array == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
		}
		var (elementOffset, num) = range.GetOffsetAndLength(array.Length);
		T[] array2;
		if (typeof(T).IsValueType || typeof(T[]) == array.GetType())
		{
			if (num == 0)
			{
				return Array.Empty<T>();
			}
			array2 = new T[num];
		}
		else
		{
			array2 = Unsafe.As<T[]>(Array.CreateInstance(array.GetType().GetElementType(), num));
		}
		Buffer.Memmove(ref MemoryMarshal.GetArrayDataReference(array2), ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), elementOffset), (uint)num);
		return array2;
	}

	[Obsolete("The Constrained Execution Region (CER) feature is not supported.", DiagnosticId = "SYSLIB0004", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static void ExecuteCodeWithGuaranteedCleanup(TryCode code, CleanupCode backoutCode, object? userData)
	{
		if (code == null)
		{
			throw new ArgumentNullException("code");
		}
		if (backoutCode == null)
		{
			throw new ArgumentNullException("backoutCode");
		}
		bool exceptionThrown = true;
		try
		{
			code(userData);
			exceptionThrown = false;
		}
		finally
		{
			backoutCode(userData, exceptionThrown);
		}
	}

	[Obsolete("The Constrained Execution Region (CER) feature is not supported.", DiagnosticId = "SYSLIB0004", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static void PrepareContractedDelegate(Delegate d)
	{
	}

	[Obsolete("The Constrained Execution Region (CER) feature is not supported.", DiagnosticId = "SYSLIB0004", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static void ProbeForSufficientStack()
	{
	}

	[Obsolete("The Constrained Execution Region (CER) feature is not supported.", DiagnosticId = "SYSLIB0004", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static void PrepareConstrainedRegions()
	{
	}

	[Obsolete("The Constrained Execution Region (CER) feature is not supported.", DiagnosticId = "SYSLIB0004", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static void PrepareConstrainedRegionsNoOP()
	{
	}

	internal static bool IsPrimitiveType(this CorElementType et)
	{
		return ((1 << (int)et) & 0x3003FFC) != 0;
	}
}
