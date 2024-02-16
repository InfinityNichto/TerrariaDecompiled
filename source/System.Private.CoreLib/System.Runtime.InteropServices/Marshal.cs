using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using System.Security;
using System.StubHelpers;
using System.Text;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices;

public static class Marshal
{
	internal static Guid IID_IUnknown = new Guid(0, 0, 0, 192, 0, 0, 0, 0, 0, 0, 70);

	public static readonly int SystemDefaultCharSize = 2;

	public static readonly int SystemMaxDBCSCharSize = GetSystemMaxDBCSCharSize();

	internal static bool IsBuiltInComSupported { get; } = IsBuiltInComSupportedInternal();


	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int SizeOfHelper(Type t, bool throwIfNotMarshalable);

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "Trimming doesn't affect types eligible for marshalling. Different exception for invalid inputs doesn't matter.")]
	public static IntPtr OffsetOf(Type t, string fieldName)
	{
		if ((object)t == null)
		{
			throw new ArgumentNullException("t");
		}
		FieldInfo field = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if ((object)field == null)
		{
			throw new ArgumentException(SR.Format(SR.Argument_OffsetOfFieldNotFound, t.FullName), "fieldName");
		}
		if (!(field is RtFieldInfo f))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeFieldInfo, "fieldName");
		}
		return OffsetOfHelper(f);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern IntPtr OffsetOfHelper(IRuntimeFieldInfo f);

	public static byte ReadByte(object ptr, int ofs)
	{
		return ReadValueSlow(ptr, ofs, (IntPtr nativeHome, int offset) => ReadByte(nativeHome, offset));
	}

	public static short ReadInt16(object ptr, int ofs)
	{
		return ReadValueSlow(ptr, ofs, (IntPtr nativeHome, int offset) => ReadInt16(nativeHome, offset));
	}

	public static int ReadInt32(object ptr, int ofs)
	{
		return ReadValueSlow(ptr, ofs, (IntPtr nativeHome, int offset) => ReadInt32(nativeHome, offset));
	}

	public static long ReadInt64([In][MarshalAs(UnmanagedType.AsAny)] object ptr, int ofs)
	{
		return ReadValueSlow(ptr, ofs, (IntPtr nativeHome, int offset) => ReadInt64(nativeHome, offset));
	}

	private unsafe static T ReadValueSlow<T>(object ptr, int ofs, Func<IntPtr, int, T> readValueHelper)
	{
		if (ptr == null)
		{
			throw new AccessViolationException();
		}
		MngdNativeArrayMarshaler.MarshalerState marshalerState = default(MngdNativeArrayMarshaler.MarshalerState);
		AsAnyMarshaler asAnyMarshaler = new AsAnyMarshaler(new IntPtr(&marshalerState));
		IntPtr intPtr = IntPtr.Zero;
		try
		{
			intPtr = asAnyMarshaler.ConvertToNative(ptr, 285147391);
			return readValueHelper(intPtr, ofs);
		}
		finally
		{
			asAnyMarshaler.ClearNative(intPtr);
		}
	}

	public static void WriteByte(object ptr, int ofs, byte val)
	{
		WriteValueSlow(ptr, ofs, val, delegate(IntPtr nativeHome, int offset, byte value)
		{
			WriteByte(nativeHome, offset, value);
		});
	}

	public static void WriteInt16(object ptr, int ofs, short val)
	{
		WriteValueSlow(ptr, ofs, val, delegate(IntPtr nativeHome, int offset, short value)
		{
			WriteInt16(nativeHome, offset, value);
		});
	}

	public static void WriteInt32(object ptr, int ofs, int val)
	{
		WriteValueSlow(ptr, ofs, val, delegate(IntPtr nativeHome, int offset, int value)
		{
			WriteInt32(nativeHome, offset, value);
		});
	}

	public static void WriteInt64(object ptr, int ofs, long val)
	{
		WriteValueSlow(ptr, ofs, val, delegate(IntPtr nativeHome, int offset, long value)
		{
			WriteInt64(nativeHome, offset, value);
		});
	}

	private unsafe static void WriteValueSlow<T>(object ptr, int ofs, T val, Action<IntPtr, int, T> writeValueHelper)
	{
		if (ptr == null)
		{
			throw new AccessViolationException();
		}
		MngdNativeArrayMarshaler.MarshalerState marshalerState = default(MngdNativeArrayMarshaler.MarshalerState);
		AsAnyMarshaler asAnyMarshaler = new AsAnyMarshaler(new IntPtr(&marshalerState));
		IntPtr intPtr = IntPtr.Zero;
		try
		{
			intPtr = asAnyMarshaler.ConvertToNative(ptr, 822018303);
			writeValueHelper(intPtr, ofs, val);
			asAnyMarshaler.ConvertToManaged(ptr, intPtr);
		}
		finally
		{
			asAnyMarshaler.ClearNative(intPtr);
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern int GetLastPInvokeError();

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern void SetLastPInvokeError(int error);

	private static void PrelinkCore(MethodInfo m)
	{
		if (!(m is RuntimeMethodInfo runtimeMethodInfo))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeMethodInfo, "m");
		}
		InternalPrelink(((IRuntimeMethodInfo)runtimeMethodInfo).Value);
		GC.KeepAlive(runtimeMethodInfo);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void InternalPrelink(RuntimeMethodHandleInternal m);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern IntPtr GetExceptionPointers();

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern int GetExceptionCode();

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern void StructureToPtr(object structure, IntPtr ptr, bool fDeleteOld);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void PtrToStructureHelper(IntPtr ptr, object structure, bool allowValueClasses);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern void DestroyStructure(IntPtr ptr, Type structuretype);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsPinnable(object obj);

	[DllImport("QCall")]
	private static extern bool IsBuiltInComSupportedInternal();

	public static IntPtr GetHINSTANCE(Module m)
	{
		if ((object)m == null)
		{
			throw new ArgumentNullException("m");
		}
		RuntimeModule module = m as RuntimeModule;
		if ((object)module != null)
		{
			return GetHINSTANCE(new QCallModule(ref module));
		}
		return (IntPtr)(-1);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern IntPtr GetHINSTANCE(QCallModule m);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern Exception GetExceptionForHRInternal(int errorCode, IntPtr errorInfo);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern int GetHRForException(Exception? e);

	[SupportedOSPlatform("windows")]
	public static string GetTypeInfoName(ITypeInfo typeInfo)
	{
		if (typeInfo == null)
		{
			throw new ArgumentNullException("typeInfo");
		}
		typeInfo.GetDocumentation(-1, out string strName, out string _, out int _, out string _);
		return strName;
	}

	internal static Type GetTypeFromCLSID(Guid clsid, string server, bool throwOnError)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		Type o = null;
		GetTypeFromCLSID(in clsid, server, ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetTypeFromCLSID(in Guid clsid, string server, ObjectHandleOnStack retType);

	[SupportedOSPlatform("windows")]
	public static IntPtr GetIUnknownForObject(object o)
	{
		if (o == null)
		{
			throw new ArgumentNullException("o");
		}
		return GetIUnknownForObjectNative(o);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern IntPtr GetIUnknownForObjectNative(object o);

	[SupportedOSPlatform("windows")]
	public static IntPtr GetIDispatchForObject(object o)
	{
		if (o == null)
		{
			throw new ArgumentNullException("o");
		}
		return GetIDispatchForObjectNative(o);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern IntPtr GetIDispatchForObjectNative(object o);

	[SupportedOSPlatform("windows")]
	public static IntPtr GetComInterfaceForObject(object o, Type T)
	{
		if (o == null)
		{
			throw new ArgumentNullException("o");
		}
		if ((object)T == null)
		{
			throw new ArgumentNullException("T");
		}
		return GetComInterfaceForObjectNative(o, T, fEnableCustomizedQueryInterface: true);
	}

	[SupportedOSPlatform("windows")]
	public static IntPtr GetComInterfaceForObject<T, TInterface>([DisallowNull] T o)
	{
		return GetComInterfaceForObject(o, typeof(TInterface));
	}

	[SupportedOSPlatform("windows")]
	public static IntPtr GetComInterfaceForObject(object o, Type T, CustomQueryInterfaceMode mode)
	{
		if (o == null)
		{
			throw new ArgumentNullException("o");
		}
		if ((object)T == null)
		{
			throw new ArgumentNullException("T");
		}
		bool fEnableCustomizedQueryInterface = mode == CustomQueryInterfaceMode.Allow;
		return GetComInterfaceForObjectNative(o, T, fEnableCustomizedQueryInterface);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern IntPtr GetComInterfaceForObjectNative(object o, Type t, bool fEnableCustomizedQueryInterface);

	[SupportedOSPlatform("windows")]
	public static object GetObjectForIUnknown(IntPtr pUnk)
	{
		if (pUnk == IntPtr.Zero)
		{
			throw new ArgumentNullException("pUnk");
		}
		return GetObjectForIUnknownNative(pUnk);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern object GetObjectForIUnknownNative(IntPtr pUnk);

	[SupportedOSPlatform("windows")]
	public static object GetUniqueObjectForIUnknown(IntPtr unknown)
	{
		if (unknown == IntPtr.Zero)
		{
			throw new ArgumentNullException("unknown");
		}
		return GetUniqueObjectForIUnknownNative(unknown);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern object GetUniqueObjectForIUnknownNative(IntPtr unknown);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[SupportedOSPlatform("windows")]
	public static extern object GetTypedObjectForIUnknown(IntPtr pUnk, Type t);

	[SupportedOSPlatform("windows")]
	public static IntPtr CreateAggregatedObject(IntPtr pOuter, object o)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		return CreateAggregatedObjectNative(pOuter, o);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern IntPtr CreateAggregatedObjectNative(IntPtr pOuter, object o);

	[SupportedOSPlatform("windows")]
	public static IntPtr CreateAggregatedObject<T>(IntPtr pOuter, T o) where T : notnull
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		return CreateAggregatedObject(pOuter, (object)o);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern void CleanupUnusedObjectsInCurrentContext();

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern bool AreComObjectsAvailableForCleanup();

	public static bool IsComObject(object o)
	{
		if (o == null)
		{
			throw new ArgumentNullException("o");
		}
		return o is __ComObject;
	}

	[SupportedOSPlatform("windows")]
	public static int ReleaseComObject(object o)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		if (o == null)
		{
			throw new NullReferenceException();
		}
		if (!(o is __ComObject _ComObject))
		{
			throw new ArgumentException(SR.Argument_ObjNotComObject, "o");
		}
		return _ComObject.ReleaseSelf();
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int InternalReleaseComObject(object o);

	[SupportedOSPlatform("windows")]
	public static int FinalReleaseComObject(object o)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		if (o == null)
		{
			throw new ArgumentNullException("o");
		}
		if (!(o is __ComObject _ComObject))
		{
			throw new ArgumentException(SR.Argument_ObjNotComObject, "o");
		}
		_ComObject.FinalReleaseSelf();
		return 0;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void InternalFinalReleaseComObject(object o);

	[SupportedOSPlatform("windows")]
	public static object? GetComObjectData(object obj, object key)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (!(obj is __ComObject _ComObject))
		{
			throw new ArgumentException(SR.Argument_ObjNotComObject, "obj");
		}
		return _ComObject.GetData(key);
	}

	[SupportedOSPlatform("windows")]
	public static bool SetComObjectData(object obj, object key, object? data)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (!(obj is __ComObject _ComObject))
		{
			throw new ArgumentException(SR.Argument_ObjNotComObject, "obj");
		}
		return _ComObject.SetData(key, data);
	}

	[SupportedOSPlatform("windows")]
	[return: NotNullIfNotNull("o")]
	public static object? CreateWrapperOfType(object? o, Type t)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		if ((object)t == null)
		{
			throw new ArgumentNullException("t");
		}
		if (!t.IsCOMObject)
		{
			throw new ArgumentException(SR.Argument_TypeNotComObject, "t");
		}
		if (t.IsGenericType)
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "t");
		}
		if (o == null)
		{
			return null;
		}
		if (!o.GetType().IsCOMObject)
		{
			throw new ArgumentException(SR.Argument_ObjNotComObject, "o");
		}
		if (o.GetType() == t)
		{
			return o;
		}
		object obj = GetComObjectData(o, t);
		if (obj == null)
		{
			obj = InternalCreateWrapperOfType(o, t);
			if (!SetComObjectData(o, t, obj))
			{
				obj = GetComObjectData(o, t);
			}
		}
		return obj;
	}

	[SupportedOSPlatform("windows")]
	public static TWrapper CreateWrapperOfType<T, TWrapper>(T? o)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		return (TWrapper)CreateWrapperOfType(o, typeof(TWrapper));
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern object InternalCreateWrapperOfType(object o, Type t);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern bool IsTypeVisibleFromCom(Type t);

	[SupportedOSPlatform("windows")]
	public static void GetNativeVariantForObject(object? obj, IntPtr pDstNativeVariant)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		GetNativeVariantForObjectNative(obj, pDstNativeVariant);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void GetNativeVariantForObjectNative(object obj, IntPtr pDstNativeVariant);

	[SupportedOSPlatform("windows")]
	public static void GetNativeVariantForObject<T>(T? obj, IntPtr pDstNativeVariant)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		GetNativeVariantForObject((object?)obj, pDstNativeVariant);
	}

	[SupportedOSPlatform("windows")]
	public static object? GetObjectForNativeVariant(IntPtr pSrcNativeVariant)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		return GetObjectForNativeVariantNative(pSrcNativeVariant);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern object GetObjectForNativeVariantNative(IntPtr pSrcNativeVariant);

	[SupportedOSPlatform("windows")]
	public static T? GetObjectForNativeVariant<T>(IntPtr pSrcNativeVariant)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		return (T)GetObjectForNativeVariant(pSrcNativeVariant);
	}

	[SupportedOSPlatform("windows")]
	public static object?[] GetObjectsForNativeVariants(IntPtr aSrcNativeVariant, int cVars)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		return GetObjectsForNativeVariantsNative(aSrcNativeVariant, cVars);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern object[] GetObjectsForNativeVariantsNative(IntPtr aSrcNativeVariant, int cVars);

	[SupportedOSPlatform("windows")]
	public static T[] GetObjectsForNativeVariants<T>(IntPtr aSrcNativeVariant, int cVars)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		object[] objectsForNativeVariants = GetObjectsForNativeVariants(aSrcNativeVariant, cVars);
		T[] array = new T[objectsForNativeVariants.Length];
		Array.Copy(objectsForNativeVariants, array, objectsForNativeVariants.Length);
		return array;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	[SupportedOSPlatform("windows")]
	public static extern int GetStartComSlot(Type t);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[SupportedOSPlatform("windows")]
	public static extern int GetEndComSlot(Type t);

	[RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
	[SupportedOSPlatform("windows")]
	public static object BindToMoniker(string monikerName)
	{
		if (!IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		CreateBindCtx(0u, out var ppbc);
		MkParseDisplayName(ppbc, monikerName, out var _, out var ppmk);
		BindMoniker(ppmk, 0u, ref IID_IUnknown, out var ppvResult);
		return ppvResult;
	}

	[DllImport("ole32.dll", PreserveSig = false)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2050:UnrecognizedReflectionPattern", Justification = "The calling method is annotated with RequiresUnreferencedCode")]
	private static extern void CreateBindCtx(uint reserved, out IBindCtx ppbc);

	[DllImport("ole32.dll", PreserveSig = false)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2050:UnrecognizedReflectionPattern", Justification = "The calling method is annotated with RequiresUnreferencedCode")]
	private static extern void MkParseDisplayName(IBindCtx pbc, [MarshalAs(UnmanagedType.LPWStr)] string szUserName, out uint pchEaten, out IMoniker ppmk);

	[DllImport("ole32.dll", PreserveSig = false)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2050:UnrecognizedReflectionPattern", Justification = "The calling method is annotated with RequiresUnreferencedCode")]
	private static extern void BindMoniker(IMoniker pmk, uint grfOpt, ref Guid iidResult, [MarshalAs(UnmanagedType.Interface)] out object ppvResult);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[SupportedOSPlatform("windows")]
	public static extern void ChangeWrapperHandleStrength(object otp, bool fIsWeak);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern Delegate GetDelegateForFunctionPointerInternal(IntPtr ptr, Type t);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern IntPtr GetFunctionPointerForDelegateInternal(Delegate d);

	public static IntPtr AllocHGlobal(int cb)
	{
		return AllocHGlobal((IntPtr)cb);
	}

	public unsafe static string? PtrToStringAnsi(IntPtr ptr)
	{
		if (IsNullOrWin32Atom(ptr))
		{
			return null;
		}
		return new string((sbyte*)(void*)ptr);
	}

	public unsafe static string PtrToStringAnsi(IntPtr ptr, int len)
	{
		if (ptr == IntPtr.Zero)
		{
			throw new ArgumentNullException("ptr");
		}
		if (len < 0)
		{
			throw new ArgumentOutOfRangeException("len", len, SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		return new string((sbyte*)(void*)ptr, 0, len);
	}

	public unsafe static string? PtrToStringUni(IntPtr ptr)
	{
		if (IsNullOrWin32Atom(ptr))
		{
			return null;
		}
		return new string((char*)(void*)ptr);
	}

	public unsafe static string PtrToStringUni(IntPtr ptr, int len)
	{
		if (ptr == IntPtr.Zero)
		{
			throw new ArgumentNullException("ptr");
		}
		if (len < 0)
		{
			throw new ArgumentOutOfRangeException("len", len, SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		return new string((char*)(void*)ptr, 0, len);
	}

	public unsafe static string? PtrToStringUTF8(IntPtr ptr)
	{
		if (IsNullOrWin32Atom(ptr))
		{
			return null;
		}
		int byteLength = string.strlen((byte*)(void*)ptr);
		return string.CreateStringFromEncoding((byte*)(void*)ptr, byteLength, Encoding.UTF8);
	}

	public unsafe static string PtrToStringUTF8(IntPtr ptr, int byteLen)
	{
		if (ptr == IntPtr.Zero)
		{
			throw new ArgumentNullException("ptr");
		}
		if (byteLen < 0)
		{
			throw new ArgumentOutOfRangeException("byteLen", byteLen, SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		return string.CreateStringFromEncoding((byte*)(void*)ptr, byteLen, Encoding.UTF8);
	}

	public static int SizeOf(object structure)
	{
		if (structure == null)
		{
			throw new ArgumentNullException("structure");
		}
		return SizeOfHelper(structure.GetType(), throwIfNotMarshalable: true);
	}

	public static int SizeOf<T>(T structure)
	{
		if (structure == null)
		{
			throw new ArgumentNullException("structure");
		}
		return SizeOfHelper(structure.GetType(), throwIfNotMarshalable: true);
	}

	public static int SizeOf(Type t)
	{
		if ((object)t == null)
		{
			throw new ArgumentNullException("t");
		}
		if (!t.IsRuntimeImplemented())
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType, "t");
		}
		if (t.IsGenericType)
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "t");
		}
		return SizeOfHelper(t, throwIfNotMarshalable: true);
	}

	public static int SizeOf<T>()
	{
		Type typeFromHandle = typeof(T);
		if (typeFromHandle.IsGenericType)
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "T");
		}
		return SizeOfHelper(typeFromHandle, throwIfNotMarshalable: true);
	}

	public unsafe static int QueryInterface(IntPtr pUnk, ref Guid iid, out IntPtr ppv)
	{
		if (pUnk == IntPtr.Zero)
		{
			throw new ArgumentNullException("pUnk");
		}
		fixed (Guid* ptr = &iid)
		{
			fixed (IntPtr* ptr2 = &ppv)
			{
				return ((delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)(*(*(IntPtr**)(void*)pUnk)))(pUnk, ptr, ptr2);
			}
		}
	}

	public unsafe static int AddRef(IntPtr pUnk)
	{
		if (pUnk == IntPtr.Zero)
		{
			throw new ArgumentNullException("pUnk");
		}
		return ((delegate* unmanaged<IntPtr, int>)(*(IntPtr*)((nint)(*(IntPtr*)(void*)pUnk) + sizeof(void*))))(pUnk);
	}

	public unsafe static int Release(IntPtr pUnk)
	{
		if (pUnk == IntPtr.Zero)
		{
			throw new ArgumentNullException("pUnk");
		}
		return ((delegate* unmanaged<IntPtr, int>)(*(IntPtr*)((nint)(*(IntPtr*)(void*)pUnk) + (nint)2 * (nint)sizeof(void*))))(pUnk);
	}

	public unsafe static IntPtr UnsafeAddrOfPinnedArrayElement(Array arr, int index)
	{
		if (arr == null)
		{
			throw new ArgumentNullException("arr");
		}
		void* ptr = Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(arr));
		return (IntPtr)((byte*)ptr + (nuint)((nint)(uint)index * (nint)arr.GetElementSize()));
	}

	public unsafe static IntPtr UnsafeAddrOfPinnedArrayElement<T>(T[] arr, int index)
	{
		if (arr == null)
		{
			throw new ArgumentNullException("arr");
		}
		void* ptr = Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(arr));
		return (IntPtr)((byte*)ptr + (nuint)((nint)(uint)index * (nint)Unsafe.SizeOf<T>()));
	}

	public static IntPtr OffsetOf<T>(string fieldName)
	{
		return OffsetOf(typeof(T), fieldName);
	}

	public static void Copy(int[] source, int startIndex, IntPtr destination, int length)
	{
		CopyToNative(source, startIndex, destination, length);
	}

	public static void Copy(char[] source, int startIndex, IntPtr destination, int length)
	{
		CopyToNative(source, startIndex, destination, length);
	}

	public static void Copy(short[] source, int startIndex, IntPtr destination, int length)
	{
		CopyToNative(source, startIndex, destination, length);
	}

	public static void Copy(long[] source, int startIndex, IntPtr destination, int length)
	{
		CopyToNative(source, startIndex, destination, length);
	}

	public static void Copy(float[] source, int startIndex, IntPtr destination, int length)
	{
		CopyToNative(source, startIndex, destination, length);
	}

	public static void Copy(double[] source, int startIndex, IntPtr destination, int length)
	{
		CopyToNative(source, startIndex, destination, length);
	}

	public static void Copy(byte[] source, int startIndex, IntPtr destination, int length)
	{
		CopyToNative(source, startIndex, destination, length);
	}

	public static void Copy(IntPtr[] source, int startIndex, IntPtr destination, int length)
	{
		CopyToNative(source, startIndex, destination, length);
	}

	private unsafe static void CopyToNative<T>(T[] source, int startIndex, IntPtr destination, int length)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (destination == IntPtr.Zero)
		{
			throw new ArgumentNullException("destination");
		}
		new Span<T>(source, startIndex, length).CopyTo(new Span<T>((void*)destination, length));
	}

	public static void Copy(IntPtr source, int[] destination, int startIndex, int length)
	{
		CopyToManaged(source, destination, startIndex, length);
	}

	public static void Copy(IntPtr source, char[] destination, int startIndex, int length)
	{
		CopyToManaged(source, destination, startIndex, length);
	}

	public static void Copy(IntPtr source, short[] destination, int startIndex, int length)
	{
		CopyToManaged(source, destination, startIndex, length);
	}

	public static void Copy(IntPtr source, long[] destination, int startIndex, int length)
	{
		CopyToManaged(source, destination, startIndex, length);
	}

	public static void Copy(IntPtr source, float[] destination, int startIndex, int length)
	{
		CopyToManaged(source, destination, startIndex, length);
	}

	public static void Copy(IntPtr source, double[] destination, int startIndex, int length)
	{
		CopyToManaged(source, destination, startIndex, length);
	}

	public static void Copy(IntPtr source, byte[] destination, int startIndex, int length)
	{
		CopyToManaged(source, destination, startIndex, length);
	}

	public static void Copy(IntPtr source, IntPtr[] destination, int startIndex, int length)
	{
		CopyToManaged(source, destination, startIndex, length);
	}

	private unsafe static void CopyToManaged<T>(IntPtr source, T[] destination, int startIndex, int length)
	{
		if (source == IntPtr.Zero)
		{
			throw new ArgumentNullException("source");
		}
		if (destination == null)
		{
			throw new ArgumentNullException("destination");
		}
		if (startIndex < 0)
		{
			throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_StartIndex);
		}
		if (length < 0)
		{
			throw new ArgumentOutOfRangeException("length", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		new Span<T>((void*)source, length).CopyTo(new Span<T>(destination, startIndex, length));
	}

	public unsafe static byte ReadByte(IntPtr ptr, int ofs)
	{
		try
		{
			byte* ptr2 = (byte*)(void*)ptr + ofs;
			return *ptr2;
		}
		catch (NullReferenceException)
		{
			throw new AccessViolationException();
		}
	}

	public static byte ReadByte(IntPtr ptr)
	{
		return ReadByte(ptr, 0);
	}

	public unsafe static short ReadInt16(IntPtr ptr, int ofs)
	{
		try
		{
			byte* ptr2 = (byte*)(void*)ptr + ofs;
			if (((int)ptr2 & 1) == 0)
			{
				return *(short*)ptr2;
			}
			return Unsafe.ReadUnaligned<short>(ptr2);
		}
		catch (NullReferenceException)
		{
			throw new AccessViolationException();
		}
	}

	public static short ReadInt16(IntPtr ptr)
	{
		return ReadInt16(ptr, 0);
	}

	public unsafe static int ReadInt32(IntPtr ptr, int ofs)
	{
		try
		{
			byte* ptr2 = (byte*)(void*)ptr + ofs;
			if (((int)ptr2 & 3) == 0)
			{
				return *(int*)ptr2;
			}
			return Unsafe.ReadUnaligned<int>(ptr2);
		}
		catch (NullReferenceException)
		{
			throw new AccessViolationException();
		}
	}

	public static int ReadInt32(IntPtr ptr)
	{
		return ReadInt32(ptr, 0);
	}

	public static IntPtr ReadIntPtr(object ptr, int ofs)
	{
		return (IntPtr)ReadInt64(ptr, ofs);
	}

	public static IntPtr ReadIntPtr(IntPtr ptr, int ofs)
	{
		return (IntPtr)ReadInt64(ptr, ofs);
	}

	public static IntPtr ReadIntPtr(IntPtr ptr)
	{
		return ReadIntPtr(ptr, 0);
	}

	public unsafe static long ReadInt64(IntPtr ptr, int ofs)
	{
		try
		{
			byte* ptr2 = (byte*)(void*)ptr + ofs;
			if (((int)ptr2 & 7) == 0)
			{
				return *(long*)ptr2;
			}
			return Unsafe.ReadUnaligned<long>(ptr2);
		}
		catch (NullReferenceException)
		{
			throw new AccessViolationException();
		}
	}

	public static long ReadInt64(IntPtr ptr)
	{
		return ReadInt64(ptr, 0);
	}

	public unsafe static void WriteByte(IntPtr ptr, int ofs, byte val)
	{
		try
		{
			byte* ptr2 = (byte*)(void*)ptr + ofs;
			*ptr2 = val;
		}
		catch (NullReferenceException)
		{
			throw new AccessViolationException();
		}
	}

	public static void WriteByte(IntPtr ptr, byte val)
	{
		WriteByte(ptr, 0, val);
	}

	public unsafe static void WriteInt16(IntPtr ptr, int ofs, short val)
	{
		try
		{
			byte* ptr2 = (byte*)(void*)ptr + ofs;
			if (((int)ptr2 & 1) == 0)
			{
				*(short*)ptr2 = val;
			}
			else
			{
				Unsafe.WriteUnaligned(ptr2, val);
			}
		}
		catch (NullReferenceException)
		{
			throw new AccessViolationException();
		}
	}

	public static void WriteInt16(IntPtr ptr, short val)
	{
		WriteInt16(ptr, 0, val);
	}

	public static void WriteInt16(IntPtr ptr, int ofs, char val)
	{
		WriteInt16(ptr, ofs, (short)val);
	}

	public static void WriteInt16([In][Out] object ptr, int ofs, char val)
	{
		WriteInt16(ptr, ofs, (short)val);
	}

	public static void WriteInt16(IntPtr ptr, char val)
	{
		WriteInt16(ptr, 0, (short)val);
	}

	public unsafe static void WriteInt32(IntPtr ptr, int ofs, int val)
	{
		try
		{
			byte* ptr2 = (byte*)(void*)ptr + ofs;
			if (((int)ptr2 & 3) == 0)
			{
				*(int*)ptr2 = val;
			}
			else
			{
				Unsafe.WriteUnaligned(ptr2, val);
			}
		}
		catch (NullReferenceException)
		{
			throw new AccessViolationException();
		}
	}

	public static void WriteInt32(IntPtr ptr, int val)
	{
		WriteInt32(ptr, 0, val);
	}

	public static void WriteIntPtr(IntPtr ptr, int ofs, IntPtr val)
	{
		WriteInt64(ptr, ofs, (long)val);
	}

	public static void WriteIntPtr(object ptr, int ofs, IntPtr val)
	{
		WriteInt64(ptr, ofs, (long)val);
	}

	public static void WriteIntPtr(IntPtr ptr, IntPtr val)
	{
		WriteIntPtr(ptr, 0, val);
	}

	public unsafe static void WriteInt64(IntPtr ptr, int ofs, long val)
	{
		try
		{
			byte* ptr2 = (byte*)(void*)ptr + ofs;
			if (((int)ptr2 & 7) == 0)
			{
				*(long*)ptr2 = val;
			}
			else
			{
				Unsafe.WriteUnaligned(ptr2, val);
			}
		}
		catch (NullReferenceException)
		{
			throw new AccessViolationException();
		}
	}

	public static void WriteInt64(IntPtr ptr, long val)
	{
		WriteInt64(ptr, 0, val);
	}

	public static void Prelink(MethodInfo m)
	{
		if ((object)m == null)
		{
			throw new ArgumentNullException("m");
		}
		PrelinkCore(m);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "This only needs to prelink methods that are actually used")]
	public static void PrelinkAll(Type c)
	{
		if ((object)c == null)
		{
			throw new ArgumentNullException("c");
		}
		MethodInfo[] methods = c.GetMethods();
		for (int i = 0; i < methods.Length; i++)
		{
			Prelink(methods[i]);
		}
	}

	public static void StructureToPtr<T>([DisallowNull] T structure, IntPtr ptr, bool fDeleteOld)
	{
		StructureToPtr((object)structure, ptr, fDeleteOld);
	}

	public static object? PtrToStructure(IntPtr ptr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type structureType)
	{
		if (ptr == IntPtr.Zero)
		{
			return null;
		}
		if ((object)structureType == null)
		{
			throw new ArgumentNullException("structureType");
		}
		if (structureType.IsGenericType)
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "structureType");
		}
		if (!structureType.IsRuntimeImplemented())
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType, "structureType");
		}
		object obj = Activator.CreateInstance(structureType, nonPublic: true);
		PtrToStructureHelper(ptr, obj, allowValueClasses: true);
		return obj;
	}

	public static void PtrToStructure(IntPtr ptr, object structure)
	{
		PtrToStructureHelper(ptr, structure, allowValueClasses: false);
	}

	public static void PtrToStructure<T>(IntPtr ptr, [DisallowNull] T structure)
	{
		PtrToStructureHelper(ptr, structure, allowValueClasses: false);
	}

	public static T? PtrToStructure<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(IntPtr ptr)
	{
		if (ptr == IntPtr.Zero)
		{
			return (T)(object)null;
		}
		Type typeFromHandle = typeof(T);
		if (typeFromHandle.IsGenericType)
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "T");
		}
		object obj = Activator.CreateInstance(typeFromHandle, nonPublic: true);
		PtrToStructureHelper(ptr, obj, allowValueClasses: true);
		return (T)obj;
	}

	public static void DestroyStructure<T>(IntPtr ptr)
	{
		DestroyStructure(ptr, typeof(T));
	}

	public static Exception? GetExceptionForHR(int errorCode)
	{
		return GetExceptionForHR(errorCode, IntPtr.Zero);
	}

	public static Exception? GetExceptionForHR(int errorCode, IntPtr errorInfo)
	{
		if (errorCode >= 0)
		{
			return null;
		}
		return GetExceptionForHRInternal(errorCode, errorInfo);
	}

	public static void ThrowExceptionForHR(int errorCode)
	{
		if (errorCode < 0)
		{
			throw GetExceptionForHR(errorCode);
		}
	}

	public static void ThrowExceptionForHR(int errorCode, IntPtr errorInfo)
	{
		if (errorCode < 0)
		{
			throw GetExceptionForHR(errorCode, errorInfo);
		}
	}

	public static IntPtr SecureStringToBSTR(SecureString s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return s.MarshalToBSTR();
	}

	public static IntPtr SecureStringToCoTaskMemAnsi(SecureString s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return s.MarshalToString(globalAlloc: false, unicode: false);
	}

	public static IntPtr SecureStringToCoTaskMemUnicode(SecureString s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return s.MarshalToString(globalAlloc: false, unicode: true);
	}

	public static IntPtr SecureStringToGlobalAllocAnsi(SecureString s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return s.MarshalToString(globalAlloc: true, unicode: false);
	}

	public static IntPtr SecureStringToGlobalAllocUnicode(SecureString s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return s.MarshalToString(globalAlloc: true, unicode: true);
	}

	public unsafe static IntPtr StringToHGlobalAnsi(string? s)
	{
		if (s == null)
		{
			return IntPtr.Zero;
		}
		long num = (long)(s.Length + 1) * (long)SystemMaxDBCSCharSize;
		int num2 = (int)num;
		if (num2 != num)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.s);
		}
		IntPtr intPtr = AllocHGlobal((IntPtr)num2);
		StringToAnsiString(s, (byte*)(void*)intPtr, num2);
		return intPtr;
	}

	public unsafe static IntPtr StringToHGlobalUni(string? s)
	{
		if (s == null)
		{
			return IntPtr.Zero;
		}
		int num = (s.Length + 1) * 2;
		if (num < s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.s);
		}
		IntPtr intPtr = AllocHGlobal((IntPtr)num);
		s.CopyTo(new Span<char>((void*)intPtr, s.Length));
		*(short*)((byte*)(void*)intPtr + (nint)s.Length * (nint)2) = 0;
		return intPtr;
	}

	public unsafe static IntPtr StringToCoTaskMemUni(string? s)
	{
		if (s == null)
		{
			return IntPtr.Zero;
		}
		int num = (s.Length + 1) * 2;
		if (num < s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.s);
		}
		IntPtr intPtr = AllocCoTaskMem(num);
		s.CopyTo(new Span<char>((void*)intPtr, s.Length));
		*(short*)((byte*)(void*)intPtr + (nint)s.Length * (nint)2) = 0;
		return intPtr;
	}

	public unsafe static IntPtr StringToCoTaskMemUTF8(string? s)
	{
		if (s == null)
		{
			return IntPtr.Zero;
		}
		int maxByteCount = Encoding.UTF8.GetMaxByteCount(s.Length);
		IntPtr intPtr = AllocCoTaskMem(maxByteCount + 1);
		byte* ptr = (byte*)(void*)intPtr;
		int bytes;
		fixed (char* chars = s)
		{
			bytes = Encoding.UTF8.GetBytes(chars, s.Length, ptr, maxByteCount);
		}
		ptr[bytes] = 0;
		return intPtr;
	}

	public unsafe static IntPtr StringToCoTaskMemAnsi(string? s)
	{
		if (s == null)
		{
			return IntPtr.Zero;
		}
		long num = (long)(s.Length + 1) * (long)SystemMaxDBCSCharSize;
		int num2 = (int)num;
		if (num2 != num)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.s);
		}
		IntPtr intPtr = AllocCoTaskMem(num2);
		StringToAnsiString(s, (byte*)(void*)intPtr, num2);
		return intPtr;
	}

	public static Guid GenerateGuidForType(Type type)
	{
		if ((object)type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (!type.IsRuntimeImplemented())
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType, "type");
		}
		return type.GUID;
	}

	public static string? GenerateProgIdForType(Type type)
	{
		if ((object)type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (type.IsImport)
		{
			throw new ArgumentException(SR.Argument_TypeMustNotBeComImport, "type");
		}
		if (type.IsGenericType)
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "type");
		}
		ProgIdAttribute customAttribute = type.GetCustomAttribute<ProgIdAttribute>();
		if (customAttribute != null)
		{
			return customAttribute.Value ?? string.Empty;
		}
		return type.FullName;
	}

	public static Delegate GetDelegateForFunctionPointer(IntPtr ptr, Type t)
	{
		if (ptr == IntPtr.Zero)
		{
			throw new ArgumentNullException("ptr");
		}
		if ((object)t == null)
		{
			throw new ArgumentNullException("t");
		}
		if (!t.IsRuntimeImplemented())
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType, "t");
		}
		if (t.IsGenericType)
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "t");
		}
		if (t.BaseType != typeof(MulticastDelegate) && t != typeof(MulticastDelegate))
		{
			throw new ArgumentException(SR.Arg_MustBeDelegate, "t");
		}
		return GetDelegateForFunctionPointerInternal(ptr, t);
	}

	public static TDelegate GetDelegateForFunctionPointer<TDelegate>(IntPtr ptr)
	{
		if (ptr == IntPtr.Zero)
		{
			throw new ArgumentNullException("ptr");
		}
		Type typeFromHandle = typeof(TDelegate);
		if (typeFromHandle.IsGenericType)
		{
			throw new ArgumentException(SR.Argument_NeedNonGenericType, "TDelegate");
		}
		if (typeFromHandle.BaseType != typeof(MulticastDelegate) && typeFromHandle != typeof(MulticastDelegate))
		{
			throw new ArgumentException(SR.Arg_MustBeDelegate, "TDelegate");
		}
		return (TDelegate)(object)GetDelegateForFunctionPointerInternal(ptr, typeFromHandle);
	}

	public static IntPtr GetFunctionPointerForDelegate(Delegate d)
	{
		if ((object)d == null)
		{
			throw new ArgumentNullException("d");
		}
		return GetFunctionPointerForDelegateInternal(d);
	}

	public static IntPtr GetFunctionPointerForDelegate<TDelegate>(TDelegate d) where TDelegate : notnull
	{
		return GetFunctionPointerForDelegate((Delegate)(object)d);
	}

	public static int GetHRForLastWin32Error()
	{
		int lastPInvokeError = GetLastPInvokeError();
		if ((lastPInvokeError & 0x80000000u) == 2147483648u)
		{
			return lastPInvokeError;
		}
		return (lastPInvokeError & 0xFFFF) | -2147024896;
	}

	public unsafe static void ZeroFreeBSTR(IntPtr s)
	{
		if (!(s == IntPtr.Zero))
		{
			Buffer.ZeroMemory((byte*)(void*)s, SysStringByteLen(s));
			FreeBSTR(s);
		}
	}

	public static void ZeroFreeCoTaskMemAnsi(IntPtr s)
	{
		ZeroFreeCoTaskMemUTF8(s);
	}

	public unsafe static void ZeroFreeCoTaskMemUnicode(IntPtr s)
	{
		if (!(s == IntPtr.Zero))
		{
			Buffer.ZeroMemory((byte*)(void*)s, (nuint)string.wcslen((char*)(void*)s) * (nuint)2u);
			FreeCoTaskMem(s);
		}
	}

	public unsafe static void ZeroFreeCoTaskMemUTF8(IntPtr s)
	{
		if (!(s == IntPtr.Zero))
		{
			Buffer.ZeroMemory((byte*)(void*)s, (nuint)string.strlen((byte*)(void*)s));
			FreeCoTaskMem(s);
		}
	}

	public unsafe static void ZeroFreeGlobalAllocAnsi(IntPtr s)
	{
		if (!(s == IntPtr.Zero))
		{
			Buffer.ZeroMemory((byte*)(void*)s, (nuint)string.strlen((byte*)(void*)s));
			FreeHGlobal(s);
		}
	}

	public unsafe static void ZeroFreeGlobalAllocUnicode(IntPtr s)
	{
		if (!(s == IntPtr.Zero))
		{
			Buffer.ZeroMemory((byte*)(void*)s, (nuint)string.wcslen((char*)(void*)s) * (nuint)2u);
			FreeHGlobal(s);
		}
	}

	public unsafe static IntPtr StringToBSTR(string? s)
	{
		if (s == null)
		{
			return IntPtr.Zero;
		}
		IntPtr intPtr = AllocBSTR(s.Length);
		s.CopyTo(new Span<char>((void*)intPtr, s.Length));
		return intPtr;
	}

	public static string PtrToStringBSTR(IntPtr ptr)
	{
		if (ptr == IntPtr.Zero)
		{
			throw new ArgumentNullException("ptr");
		}
		return PtrToStringUni(ptr, (int)(SysStringByteLen(ptr) / 2));
	}

	internal unsafe static uint SysStringByteLen(IntPtr s)
	{
		return *(uint*)((byte*)(void*)s - 4);
	}

	[SupportedOSPlatform("windows")]
	public static Type? GetTypeFromCLSID(Guid clsid)
	{
		return GetTypeFromCLSID(clsid, null, throwOnError: false);
	}

	public static void InitHandle(SafeHandle safeHandle, IntPtr handle)
	{
		safeHandle.SetHandle(handle);
	}

	public static int GetLastWin32Error()
	{
		return GetLastPInvokeError();
	}

	public static string? PtrToStringAuto(IntPtr ptr, int len)
	{
		return PtrToStringUni(ptr, len);
	}

	public static string? PtrToStringAuto(IntPtr ptr)
	{
		return PtrToStringUni(ptr);
	}

	public static IntPtr StringToHGlobalAuto(string? s)
	{
		return StringToHGlobalUni(s);
	}

	public static IntPtr StringToCoTaskMemAuto(string? s)
	{
		return StringToCoTaskMemUni(s);
	}

	private unsafe static int GetSystemMaxDBCSCharSize()
	{
		Interop.Kernel32.CPINFO cPINFO = default(Interop.Kernel32.CPINFO);
		if (Interop.Kernel32.GetCPInfo(0u, &cPINFO) == Interop.BOOL.FALSE)
		{
			return 2;
		}
		return cPINFO.MaxCharSize;
	}

	private static bool IsNullOrWin32Atom(IntPtr ptr)
	{
		long num = (long)ptr;
		return (num & -65536) == 0;
	}

	internal unsafe static int StringToAnsiString(string s, byte* buffer, int bufferLength, bool bestFit = false, bool throwOnUnmappableChar = false)
	{
		uint dwFlags = ((!bestFit) ? 1024u : 0u);
		uint num = 0u;
		int num2;
		fixed (char* lpWideCharStr = s)
		{
			num2 = Interop.Kernel32.WideCharToMultiByte(0u, dwFlags, lpWideCharStr, s.Length, buffer, bufferLength, IntPtr.Zero, throwOnUnmappableChar ? new IntPtr(&num) : IntPtr.Zero);
		}
		if (num != 0)
		{
			throw new ArgumentException(SR.Interop_Marshal_Unmappable_Char);
		}
		buffer[num2] = 0;
		return num2;
	}

	internal unsafe static int GetAnsiStringByteCount(ReadOnlySpan<char> chars)
	{
		int num;
		if (chars.Length == 0)
		{
			num = 0;
		}
		else
		{
			fixed (char* lpWideCharStr = chars)
			{
				num = Interop.Kernel32.WideCharToMultiByte(0u, 1024u, lpWideCharStr, chars.Length, null, 0, IntPtr.Zero, IntPtr.Zero);
				if (num <= 0)
				{
					throw new ArgumentException();
				}
			}
		}
		return checked(num + 1);
	}

	internal unsafe static void GetAnsiStringBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
	{
		int num;
		if (chars.Length == 0)
		{
			num = 0;
		}
		else
		{
			fixed (char* lpWideCharStr = chars)
			{
				fixed (byte* lpMultiByteStr = bytes)
				{
					num = Interop.Kernel32.WideCharToMultiByte(0u, 1024u, lpWideCharStr, chars.Length, lpMultiByteStr, bytes.Length, IntPtr.Zero, IntPtr.Zero);
					if (num <= 0)
					{
						throw new ArgumentException();
					}
				}
			}
		}
		bytes[num] = 0;
	}

	public static IntPtr AllocHGlobal(IntPtr cb)
	{
		IntPtr intPtr = Interop.Kernel32.LocalAlloc(0u, (nuint)(nint)cb);
		if (intPtr == IntPtr.Zero)
		{
			throw new OutOfMemoryException();
		}
		return intPtr;
	}

	public static void FreeHGlobal(IntPtr hglobal)
	{
		if (!IsNullOrWin32Atom(hglobal))
		{
			Interop.Kernel32.LocalFree(hglobal);
		}
	}

	public static IntPtr ReAllocHGlobal(IntPtr pv, IntPtr cb)
	{
		if (pv == IntPtr.Zero)
		{
			return AllocHGlobal(cb);
		}
		IntPtr intPtr = Interop.Kernel32.LocalReAlloc(pv, (nuint)(nint)cb, 2u);
		if (intPtr == IntPtr.Zero)
		{
			throw new OutOfMemoryException();
		}
		return intPtr;
	}

	public static IntPtr AllocCoTaskMem(int cb)
	{
		IntPtr intPtr = Interop.Ole32.CoTaskMemAlloc((uint)cb);
		if (intPtr == IntPtr.Zero)
		{
			throw new OutOfMemoryException();
		}
		return intPtr;
	}

	public static void FreeCoTaskMem(IntPtr ptr)
	{
		if (!IsNullOrWin32Atom(ptr))
		{
			Interop.Ole32.CoTaskMemFree(ptr);
		}
	}

	public static IntPtr ReAllocCoTaskMem(IntPtr pv, int cb)
	{
		IntPtr intPtr = Interop.Ole32.CoTaskMemRealloc(pv, (uint)cb);
		if (intPtr == IntPtr.Zero && cb != 0)
		{
			throw new OutOfMemoryException();
		}
		return intPtr;
	}

	internal static IntPtr AllocBSTR(int length)
	{
		IntPtr intPtr = Interop.OleAut32.SysAllocStringLen(IntPtr.Zero, (uint)length);
		if (intPtr == IntPtr.Zero)
		{
			throw new OutOfMemoryException();
		}
		return intPtr;
	}

	internal static IntPtr AllocBSTRByteLen(uint length)
	{
		IntPtr intPtr = Interop.OleAut32.SysAllocStringByteLen(null, length);
		if (intPtr == IntPtr.Zero)
		{
			throw new OutOfMemoryException();
		}
		return intPtr;
	}

	public static void FreeBSTR(IntPtr ptr)
	{
		if (!IsNullOrWin32Atom(ptr))
		{
			Interop.OleAut32.SysFreeString(ptr);
		}
	}

	internal static Type GetTypeFromProgID(string progID, string server, bool throwOnError)
	{
		if (progID == null)
		{
			throw new ArgumentNullException("progID");
		}
		Guid lpclsid;
		int num = Interop.Ole32.CLSIDFromProgID(progID, out lpclsid);
		if (num < 0)
		{
			if (throwOnError)
			{
				throw GetExceptionForHR(num, new IntPtr(-1));
			}
			return null;
		}
		return GetTypeFromCLSID(lpclsid, server, throwOnError);
	}

	public static int GetLastSystemError()
	{
		return Interop.Kernel32.GetLastError();
	}

	public static void SetLastSystemError(int error)
	{
		Interop.Kernel32.SetLastError(error);
	}
}
