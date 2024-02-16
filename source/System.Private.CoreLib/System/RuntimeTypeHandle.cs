using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Threading;

namespace System;

[NonVersionable]
public struct RuntimeTypeHandle : ISerializable
{
	internal struct IntroducedMethodEnumerator
	{
		private bool _firstCall;

		private RuntimeMethodHandleInternal _handle;

		public RuntimeMethodHandleInternal Current => _handle;

		internal IntroducedMethodEnumerator(RuntimeType type)
		{
			_handle = GetFirstIntroducedMethod(type);
			_firstCall = true;
		}

		public bool MoveNext()
		{
			if (_firstCall)
			{
				_firstCall = false;
			}
			else if (_handle.Value != IntPtr.Zero)
			{
				GetNextIntroducedMethod(ref _handle);
			}
			return !(_handle.Value == IntPtr.Zero);
		}

		public IntroducedMethodEnumerator GetEnumerator()
		{
			return this;
		}
	}

	internal RuntimeType m_type;

	public IntPtr Value
	{
		get
		{
			if (!(m_type != null))
			{
				return IntPtr.Zero;
			}
			return m_type.m_handle;
		}
	}

	internal RuntimeTypeHandle GetNativeHandle()
	{
		RuntimeType type = m_type;
		if (type == null)
		{
			throw new ArgumentNullException(null, SR.Arg_InvalidHandle);
		}
		return new RuntimeTypeHandle(type);
	}

	internal RuntimeType GetTypeChecked()
	{
		RuntimeType type = m_type;
		if (type == null)
		{
			throw new ArgumentNullException(null, SR.Arg_InvalidHandle);
		}
		return type;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsInstanceOfType(RuntimeType type, [NotNullWhen(true)] object o);

	[RequiresUnreferencedCode("MakeGenericType cannot be statically analyzed. It's not possible to guarantee the availability of requirements of the generic type.")]
	internal unsafe static Type GetTypeHelper(Type typeStart, Type[] genericArgs, IntPtr pModifiers, int cModifiers)
	{
		Type type = typeStart;
		if (genericArgs != null)
		{
			type = type.MakeGenericType(genericArgs);
		}
		if (cModifiers > 0)
		{
			int* ptr = (int*)pModifiers.ToPointer();
			for (int i = 0; i < cModifiers; i++)
			{
				type = (((byte)Marshal.ReadInt32((IntPtr)ptr, i * 4) != 15) ? (((byte)Marshal.ReadInt32((IntPtr)ptr, i * 4) != 16) ? (((byte)Marshal.ReadInt32((IntPtr)ptr, i * 4) != 29) ? type.MakeArrayType(Marshal.ReadInt32((IntPtr)ptr, ++i * 4)) : type.MakeArrayType()) : type.MakeByRefType()) : type.MakePointerType());
			}
		}
		return type;
	}

	public static bool operator ==(RuntimeTypeHandle left, object? right)
	{
		return left.Equals(right);
	}

	public static bool operator ==(object? left, RuntimeTypeHandle right)
	{
		return right.Equals(left);
	}

	public static bool operator !=(RuntimeTypeHandle left, object? right)
	{
		return !left.Equals(right);
	}

	public static bool operator !=(object? left, RuntimeTypeHandle right)
	{
		return !right.Equals(left);
	}

	public override int GetHashCode()
	{
		if (!(m_type != null))
		{
			return 0;
		}
		return m_type.GetHashCode();
	}

	public override bool Equals(object? obj)
	{
		if (!(obj is RuntimeTypeHandle runtimeTypeHandle))
		{
			return false;
		}
		return runtimeTypeHandle.m_type == m_type;
	}

	public bool Equals(RuntimeTypeHandle handle)
	{
		return handle.m_type == m_type;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	internal static extern IntPtr GetValueInternal(RuntimeTypeHandle handle);

	internal RuntimeTypeHandle(RuntimeType type)
	{
		m_type = type;
	}

	internal static bool IsTypeDefinition(RuntimeType type)
	{
		CorElementType corElementType = GetCorElementType(type);
		if (((int)corElementType < 1 || (int)corElementType >= 15) && corElementType != CorElementType.ELEMENT_TYPE_VALUETYPE && corElementType != CorElementType.ELEMENT_TYPE_CLASS && corElementType != CorElementType.ELEMENT_TYPE_TYPEDBYREF && corElementType != CorElementType.ELEMENT_TYPE_I && corElementType != CorElementType.ELEMENT_TYPE_U && corElementType != CorElementType.ELEMENT_TYPE_OBJECT)
		{
			return false;
		}
		if (HasInstantiation(type) && !IsGenericTypeDefinition(type))
		{
			return false;
		}
		return true;
	}

	internal static bool IsPrimitive(RuntimeType type)
	{
		return GetCorElementType(type).IsPrimitiveType();
	}

	internal static bool IsByRef(RuntimeType type)
	{
		CorElementType corElementType = GetCorElementType(type);
		return corElementType == CorElementType.ELEMENT_TYPE_BYREF;
	}

	internal static bool IsPointer(RuntimeType type)
	{
		CorElementType corElementType = GetCorElementType(type);
		return corElementType == CorElementType.ELEMENT_TYPE_PTR;
	}

	internal static bool IsArray(RuntimeType type)
	{
		CorElementType corElementType = GetCorElementType(type);
		if (corElementType != CorElementType.ELEMENT_TYPE_ARRAY)
		{
			return corElementType == CorElementType.ELEMENT_TYPE_SZARRAY;
		}
		return true;
	}

	internal static bool IsSZArray(RuntimeType type)
	{
		CorElementType corElementType = GetCorElementType(type);
		return corElementType == CorElementType.ELEMENT_TYPE_SZARRAY;
	}

	internal static bool HasElementType(RuntimeType type)
	{
		CorElementType corElementType = GetCorElementType(type);
		if (corElementType != CorElementType.ELEMENT_TYPE_ARRAY && corElementType != CorElementType.ELEMENT_TYPE_SZARRAY && corElementType != CorElementType.ELEMENT_TYPE_PTR)
		{
			return corElementType == CorElementType.ELEMENT_TYPE_BYREF;
		}
		return true;
	}

	internal static IntPtr[] CopyRuntimeTypeHandles(RuntimeTypeHandle[] inHandles, out int length)
	{
		if (inHandles == null || inHandles.Length == 0)
		{
			length = 0;
			return null;
		}
		IntPtr[] array = new IntPtr[inHandles.Length];
		for (int i = 0; i < inHandles.Length; i++)
		{
			array[i] = inHandles[i].Value;
		}
		length = array.Length;
		return array;
	}

	internal static IntPtr[] CopyRuntimeTypeHandles(Type[] inHandles, out int length)
	{
		if (inHandles == null || inHandles.Length == 0)
		{
			length = 0;
			return null;
		}
		IntPtr[] array = new IntPtr[inHandles.Length];
		for (int i = 0; i < inHandles.Length; i++)
		{
			array[i] = inHandles[i].GetTypeHandleInternal().Value;
		}
		length = array.Length;
		return array;
	}

	internal unsafe static object CreateInstanceForAnotherGenericParameter([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] RuntimeType type, RuntimeType genericParameter)
	{
		object o = null;
		IntPtr value = genericParameter.GetTypeHandleInternal().Value;
		CreateInstanceForAnotherGenericParameter(new QCallTypeHandle(ref type), &value, 1, ObjectHandleOnStack.Create(ref o));
		GC.KeepAlive(genericParameter);
		return o;
	}

	internal unsafe static object CreateInstanceForAnotherGenericParameter([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] RuntimeType type, RuntimeType genericParameter1, RuntimeType genericParameter2)
	{
		object o = null;
		IntPtr* pTypeHandles = stackalloc IntPtr[2]
		{
			genericParameter1.GetTypeHandleInternal().Value,
			genericParameter2.GetTypeHandleInternal().Value
		};
		CreateInstanceForAnotherGenericParameter(new QCallTypeHandle(ref type), pTypeHandles, 2, ObjectHandleOnStack.Create(ref o));
		GC.KeepAlive(genericParameter1);
		GC.KeepAlive(genericParameter2);
		return o;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private unsafe static extern void CreateInstanceForAnotherGenericParameter(QCallTypeHandle baseType, IntPtr* pTypeHandles, int cTypeHandles, ObjectHandleOnStack instantiatedObject);

	internal unsafe static void GetActivationInfo(RuntimeType rt, out delegate*<void*, object> pfnAllocator, out void* vAllocatorFirstArg, out delegate*<object, void> pfnCtor, out bool ctorIsPublic)
	{
		delegate*<void*, object> delegate_002A = default(delegate*<void*, object>);
		void* ptr = default(void*);
		delegate*<object, void> delegate_002A2 = default(delegate*<object, void>);
		Interop.BOOL bOOL = Interop.BOOL.FALSE;
		GetActivationInfo(ObjectHandleOnStack.Create(ref rt), &delegate_002A, &ptr, &delegate_002A2, &bOOL);
		System.Runtime.CompilerServices.Unsafe.As<delegate*<void*, object>, IntPtr>(ref pfnAllocator) = (nint)delegate_002A;
		vAllocatorFirstArg = ptr;
		System.Runtime.CompilerServices.Unsafe.As<delegate*<object, void>, IntPtr>(ref pfnCtor) = (nint)delegate_002A2;
		ctorIsPublic = bOOL != Interop.BOOL.FALSE;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private unsafe static extern void GetActivationInfo(ObjectHandleOnStack pRuntimeType, delegate*<void*, object>* ppfnAllocator, void** pvAllocatorFirstArg, delegate*<object, void>* ppfnCtor, Interop.BOOL* pfCtorIsPublic);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern object AllocateComObject(void* pClassFactory);

	internal RuntimeType GetRuntimeType()
	{
		return m_type;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern CorElementType GetCorElementType(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeAssembly GetAssembly(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeModule GetModule(RuntimeType type);

	public ModuleHandle GetModuleHandle()
	{
		return new ModuleHandle(GetModule(m_type));
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeType GetBaseType(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern TypeAttributes GetAttributes(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeType GetElementType(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool CompareCanonicalHandles(RuntimeType left, RuntimeType right);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int GetArrayRank(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int GetToken(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeMethodHandleInternal GetMethodAt(RuntimeType type, int slot);

	internal static IntroducedMethodEnumerator GetIntroducedMethods(RuntimeType type)
	{
		return new IntroducedMethodEnumerator(type);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern RuntimeMethodHandleInternal GetFirstIntroducedMethod(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void GetNextIntroducedMethod(ref RuntimeMethodHandleInternal method);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal unsafe static extern bool GetFields(RuntimeType type, IntPtr* result, int* count);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern Type[] GetInterfaces(RuntimeType type);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetConstraints(QCallTypeHandle handle, ObjectHandleOnStack types);

	internal Type[] GetConstraints()
	{
		Type[] o = null;
		RuntimeTypeHandle rth = GetNativeHandle();
		GetConstraints(new QCallTypeHandle(ref rth), ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern IntPtr GetGCHandle(QCallTypeHandle handle, GCHandleType type);

	internal IntPtr GetGCHandle(GCHandleType type)
	{
		RuntimeTypeHandle rth = GetNativeHandle();
		return GetGCHandle(new QCallTypeHandle(ref rth), type);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern IntPtr FreeGCHandle(QCallTypeHandle typeHandle, IntPtr objHandle);

	internal IntPtr FreeGCHandle(IntPtr objHandle)
	{
		RuntimeTypeHandle rth = GetNativeHandle();
		return FreeGCHandle(new QCallTypeHandle(ref rth), objHandle);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int GetNumVirtuals(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int GetNumVirtualsAndStaticVirtuals(RuntimeType type);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void VerifyInterfaceIsImplemented(QCallTypeHandle handle, QCallTypeHandle interfaceHandle);

	internal void VerifyInterfaceIsImplemented(RuntimeTypeHandle interfaceHandle)
	{
		RuntimeTypeHandle rth = GetNativeHandle();
		RuntimeTypeHandle rth2 = interfaceHandle.GetNativeHandle();
		VerifyInterfaceIsImplemented(new QCallTypeHandle(ref rth), new QCallTypeHandle(ref rth2));
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern RuntimeMethodHandleInternal GetInterfaceMethodImplementation(QCallTypeHandle handle, QCallTypeHandle interfaceHandle, RuntimeMethodHandleInternal interfaceMethodHandle);

	internal RuntimeMethodHandleInternal GetInterfaceMethodImplementation(RuntimeTypeHandle interfaceHandle, RuntimeMethodHandleInternal interfaceMethodHandle)
	{
		RuntimeTypeHandle rth = GetNativeHandle();
		RuntimeTypeHandle rth2 = interfaceHandle.GetNativeHandle();
		return GetInterfaceMethodImplementation(new QCallTypeHandle(ref rth), new QCallTypeHandle(ref rth2), interfaceMethodHandle);
	}

	internal static bool IsComObject(RuntimeType type, bool isGenericCOM)
	{
		if (isGenericCOM)
		{
			return type.TypeHandle.Value == typeof(__ComObject).TypeHandle.Value;
		}
		return CanCastTo(type, (RuntimeType)typeof(__ComObject));
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsInterface(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsByRefLike(RuntimeType type);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool _IsVisible(QCallTypeHandle typeHandle);

	internal static bool IsVisible(RuntimeType type)
	{
		return _IsVisible(new QCallTypeHandle(ref type));
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsValueType(RuntimeType type);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void ConstructName(QCallTypeHandle handle, TypeNameFormatFlags formatFlags, StringHandleOnStack retString);

	internal string ConstructName(TypeNameFormatFlags formatFlags)
	{
		string s = null;
		RuntimeTypeHandle rth = GetNativeHandle();
		ConstructName(new QCallTypeHandle(ref rth), formatFlags, new StringHandleOnStack(ref s));
		return s;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern void* _GetUtf8Name(RuntimeType type);

	internal unsafe static MdUtf8String GetUtf8Name(RuntimeType type)
	{
		return new MdUtf8String(_GetUtf8Name(type));
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool CanCastTo(RuntimeType type, RuntimeType target);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeType GetDeclaringType(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern IRuntimeMethodInfo GetDeclaringMethod(RuntimeType type);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetTypeByName(string name, bool throwOnError, bool ignoreCase, StackCrawlMarkHandle stackMark, ObjectHandleOnStack assemblyLoadContext, ObjectHandleOnStack type, ObjectHandleOnStack keepalive);

	internal static RuntimeType GetTypeByName(string name, bool throwOnError, bool ignoreCase, ref StackCrawlMark stackMark)
	{
		return GetTypeByName(name, throwOnError, ignoreCase, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
	}

	internal static RuntimeType GetTypeByName(string name, bool throwOnError, bool ignoreCase, ref StackCrawlMark stackMark, AssemblyLoadContext assemblyLoadContext)
	{
		if (string.IsNullOrEmpty(name))
		{
			if (throwOnError)
			{
				throw new TypeLoadException(SR.Arg_TypeLoadNullStr);
			}
			return null;
		}
		RuntimeType o = null;
		object o2 = null;
		AssemblyLoadContext o3 = assemblyLoadContext;
		GetTypeByName(name, throwOnError, ignoreCase, new StackCrawlMarkHandle(ref stackMark), ObjectHandleOnStack.Create(ref o3), ObjectHandleOnStack.Create(ref o), ObjectHandleOnStack.Create(ref o2));
		GC.KeepAlive(o2);
		return o;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetTypeByNameUsingCARules(string name, QCallModule scope, ObjectHandleOnStack type);

	internal static RuntimeType GetTypeByNameUsingCARules(string name, RuntimeModule scope)
	{
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentException(null, "name");
		}
		RuntimeType o = null;
		GetTypeByNameUsingCARules(name, new QCallModule(ref scope), ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern void GetInstantiation(QCallTypeHandle type, ObjectHandleOnStack types, Interop.BOOL fAsRuntimeTypeArray);

	internal RuntimeType[] GetInstantiationInternal()
	{
		RuntimeType[] o = null;
		RuntimeTypeHandle rth = GetNativeHandle();
		GetInstantiation(new QCallTypeHandle(ref rth), ObjectHandleOnStack.Create(ref o), Interop.BOOL.TRUE);
		return o;
	}

	internal Type[] GetInstantiationPublic()
	{
		Type[] o = null;
		RuntimeTypeHandle rth = GetNativeHandle();
		GetInstantiation(new QCallTypeHandle(ref rth), ObjectHandleOnStack.Create(ref o), Interop.BOOL.FALSE);
		return o;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private unsafe static extern void Instantiate(QCallTypeHandle handle, IntPtr* pInst, int numGenericArgs, ObjectHandleOnStack type);

	internal unsafe RuntimeType Instantiate(RuntimeType inst)
	{
		IntPtr value = inst.TypeHandle.Value;
		RuntimeType o = null;
		RuntimeTypeHandle rth = GetNativeHandle();
		Instantiate(new QCallTypeHandle(ref rth), &value, 1, ObjectHandleOnStack.Create(ref o));
		GC.KeepAlive(inst);
		return o;
	}

	internal unsafe RuntimeType Instantiate(Type[] inst)
	{
		int length;
		fixed (IntPtr* pInst = CopyRuntimeTypeHandles(inst, out length))
		{
			RuntimeType o = null;
			RuntimeTypeHandle rth = GetNativeHandle();
			Instantiate(new QCallTypeHandle(ref rth), pInst, length, ObjectHandleOnStack.Create(ref o));
			GC.KeepAlive(inst);
			return o;
		}
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void MakeArray(QCallTypeHandle handle, int rank, ObjectHandleOnStack type);

	internal RuntimeType MakeArray(int rank)
	{
		RuntimeType o = null;
		RuntimeTypeHandle rth = GetNativeHandle();
		MakeArray(new QCallTypeHandle(ref rth), rank, ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void MakeSZArray(QCallTypeHandle handle, ObjectHandleOnStack type);

	internal RuntimeType MakeSZArray()
	{
		RuntimeType o = null;
		RuntimeTypeHandle rth = GetNativeHandle();
		MakeSZArray(new QCallTypeHandle(ref rth), ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void MakeByRef(QCallTypeHandle handle, ObjectHandleOnStack type);

	internal RuntimeType MakeByRef()
	{
		RuntimeType o = null;
		RuntimeTypeHandle rth = GetNativeHandle();
		MakeByRef(new QCallTypeHandle(ref rth), ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void MakePointer(QCallTypeHandle handle, ObjectHandleOnStack type);

	internal RuntimeType MakePointer()
	{
		RuntimeType o = null;
		RuntimeTypeHandle rth = GetNativeHandle();
		MakePointer(new QCallTypeHandle(ref rth), ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern Interop.BOOL IsCollectible(QCallTypeHandle handle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool HasInstantiation(RuntimeType type);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetGenericTypeDefinition(QCallTypeHandle type, ObjectHandleOnStack retType);

	internal static RuntimeType GetGenericTypeDefinition(RuntimeType type)
	{
		RuntimeType o = type;
		if (HasInstantiation(o) && !IsGenericTypeDefinition(o))
		{
			RuntimeTypeHandle rth = o.GetTypeHandleInternal();
			GetGenericTypeDefinition(new QCallTypeHandle(ref rth), ObjectHandleOnStack.Create(ref o));
		}
		return o;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsGenericTypeDefinition(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsGenericVariable(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern int GetGenericVariableIndex(RuntimeType type);

	internal int GetGenericVariableIndex()
	{
		RuntimeType typeChecked = GetTypeChecked();
		if (!IsGenericVariable(typeChecked))
		{
			throw new InvalidOperationException(SR.Arg_NotGenericParameter);
		}
		return GetGenericVariableIndex(typeChecked);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool ContainsGenericVariables(RuntimeType handle);

	internal bool ContainsGenericVariables()
	{
		return ContainsGenericVariables(GetTypeChecked());
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern bool SatisfiesConstraints(RuntimeType paramType, IntPtr* pTypeContext, int typeContextLength, IntPtr* pMethodContext, int methodContextLength, RuntimeType toType);

	internal unsafe static bool SatisfiesConstraints(RuntimeType paramType, RuntimeType[] typeContext, RuntimeType[] methodContext, RuntimeType toType)
	{
		Type[] inHandles = typeContext;
		int length;
		IntPtr[] array = CopyRuntimeTypeHandles(inHandles, out length);
		inHandles = methodContext;
		int length2;
		IntPtr[] array2 = CopyRuntimeTypeHandles(inHandles, out length2);
		fixed (IntPtr* pTypeContext = array)
		{
			fixed (IntPtr* pMethodContext = array2)
			{
				bool result = SatisfiesConstraints(paramType, pTypeContext, length, pMethodContext, length2, toType);
				GC.KeepAlive(typeContext);
				GC.KeepAlive(methodContext);
				return result;
			}
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern IntPtr _GetMetadataImport(RuntimeType type);

	internal static MetadataImport GetMetadataImport(RuntimeType type)
	{
		return new MetadataImport(_GetMetadataImport(type), type);
	}

	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsEquivalentTo(RuntimeType rtType1, RuntimeType rtType2);
}
