using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Threading;

namespace System;

[NonVersionable]
public struct RuntimeMethodHandle : ISerializable
{
	private readonly IRuntimeMethodInfo m_value;

	public IntPtr Value
	{
		get
		{
			if (m_value == null)
			{
				return IntPtr.Zero;
			}
			return m_value.Value.Value;
		}
	}

	internal static IRuntimeMethodInfo EnsureNonNullMethodInfo(IRuntimeMethodInfo method)
	{
		if (method == null)
		{
			throw new ArgumentNullException(null, SR.Arg_InvalidHandle);
		}
		return method;
	}

	internal RuntimeMethodHandle(IRuntimeMethodInfo method)
	{
		m_value = method;
	}

	internal IRuntimeMethodInfo GetMethodInfo()
	{
		return m_value;
	}

	private static IntPtr GetValueInternal(RuntimeMethodHandle rmh)
	{
		return rmh.Value;
	}

	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	public override int GetHashCode()
	{
		return ValueType.GetHashCodeOfPtr(Value);
	}

	public override bool Equals(object? obj)
	{
		if (!(obj is RuntimeMethodHandle runtimeMethodHandle))
		{
			return false;
		}
		return runtimeMethodHandle.Value == Value;
	}

	public static bool operator ==(RuntimeMethodHandle left, RuntimeMethodHandle right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(RuntimeMethodHandle left, RuntimeMethodHandle right)
	{
		return !left.Equals(right);
	}

	public bool Equals(RuntimeMethodHandle handle)
	{
		return handle.Value == Value;
	}

	internal bool IsNullHandle()
	{
		return m_value == null;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern IntPtr GetFunctionPointer(RuntimeMethodHandleInternal handle);

	public IntPtr GetFunctionPointer()
	{
		IntPtr functionPointer = GetFunctionPointer(EnsureNonNullMethodInfo(m_value).Value);
		GC.KeepAlive(m_value);
		return functionPointer;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern Interop.BOOL GetIsCollectible(RuntimeMethodHandleInternal handle);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern Interop.BOOL IsCAVisibleFromDecoratedType(QCallTypeHandle attrTypeHandle, RuntimeMethodHandleInternal attrCtor, QCallTypeHandle sourceTypeHandle, QCallModule sourceModule);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern IRuntimeMethodInfo _GetCurrentMethod(ref StackCrawlMark stackMark);

	internal static IRuntimeMethodInfo GetCurrentMethod(ref StackCrawlMark stackMark)
	{
		return _GetCurrentMethod(ref stackMark);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern MethodAttributes GetAttributes(RuntimeMethodHandleInternal method);

	internal static MethodAttributes GetAttributes(IRuntimeMethodInfo method)
	{
		MethodAttributes attributes = GetAttributes(method.Value);
		GC.KeepAlive(method);
		return attributes;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern MethodImplAttributes GetImplAttributes(IRuntimeMethodInfo method);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void ConstructInstantiation(RuntimeMethodHandleInternal method, TypeNameFormatFlags format, StringHandleOnStack retString);

	internal static string ConstructInstantiation(IRuntimeMethodInfo method, TypeNameFormatFlags format)
	{
		string s = null;
		IRuntimeMethodInfo runtimeMethodInfo = EnsureNonNullMethodInfo(method);
		ConstructInstantiation(runtimeMethodInfo.Value, format, new StringHandleOnStack(ref s));
		GC.KeepAlive(runtimeMethodInfo);
		return s;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeType GetDeclaringType(RuntimeMethodHandleInternal method);

	internal static RuntimeType GetDeclaringType(IRuntimeMethodInfo method)
	{
		RuntimeType declaringType = GetDeclaringType(method.Value);
		GC.KeepAlive(method);
		return declaringType;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int GetSlot(RuntimeMethodHandleInternal method);

	internal static int GetSlot(IRuntimeMethodInfo method)
	{
		int slot = GetSlot(method.Value);
		GC.KeepAlive(method);
		return slot;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int GetMethodDef(IRuntimeMethodInfo method);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern string GetName(RuntimeMethodHandleInternal method);

	internal static string GetName(IRuntimeMethodInfo method)
	{
		string name = GetName(method.Value);
		GC.KeepAlive(method);
		return name;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern void* _GetUtf8Name(RuntimeMethodHandleInternal method);

	internal unsafe static MdUtf8String GetUtf8Name(RuntimeMethodHandleInternal method)
	{
		return new MdUtf8String(_GetUtf8Name(method));
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool MatchesNameHash(RuntimeMethodHandleInternal method, uint hash);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[DebuggerStepThrough]
	[DebuggerHidden]
	internal static extern object InvokeMethod(object target, in Span<object> arguments, Signature sig, bool constructor, bool wrapExceptions);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetMethodInstantiation(RuntimeMethodHandleInternal method, ObjectHandleOnStack types, Interop.BOOL fAsRuntimeTypeArray);

	internal static RuntimeType[] GetMethodInstantiationInternal(IRuntimeMethodInfo method)
	{
		RuntimeType[] o = null;
		GetMethodInstantiation(EnsureNonNullMethodInfo(method).Value, ObjectHandleOnStack.Create(ref o), Interop.BOOL.TRUE);
		GC.KeepAlive(method);
		return o;
	}

	internal static RuntimeType[] GetMethodInstantiationInternal(RuntimeMethodHandleInternal method)
	{
		RuntimeType[] o = null;
		GetMethodInstantiation(method, ObjectHandleOnStack.Create(ref o), Interop.BOOL.TRUE);
		return o;
	}

	internal static Type[] GetMethodInstantiationPublic(IRuntimeMethodInfo method)
	{
		RuntimeType[] o = null;
		GetMethodInstantiation(EnsureNonNullMethodInfo(method).Value, ObjectHandleOnStack.Create(ref o), Interop.BOOL.FALSE);
		GC.KeepAlive(method);
		return o;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool HasMethodInstantiation(RuntimeMethodHandleInternal method);

	internal static bool HasMethodInstantiation(IRuntimeMethodInfo method)
	{
		bool result = HasMethodInstantiation(method.Value);
		GC.KeepAlive(method);
		return result;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeMethodHandleInternal GetStubIfNeeded(RuntimeMethodHandleInternal method, RuntimeType declaringType, RuntimeType[] methodInstantiation);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeMethodHandleInternal GetMethodFromCanonical(RuntimeMethodHandleInternal method, RuntimeType declaringType);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsGenericMethodDefinition(RuntimeMethodHandleInternal method);

	internal static bool IsGenericMethodDefinition(IRuntimeMethodInfo method)
	{
		bool result = IsGenericMethodDefinition(method.Value);
		GC.KeepAlive(method);
		return result;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsTypicalMethodDefinition(IRuntimeMethodInfo method);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetTypicalMethodDefinition(RuntimeMethodHandleInternal method, ObjectHandleOnStack outMethod);

	internal static IRuntimeMethodInfo GetTypicalMethodDefinition(IRuntimeMethodInfo method)
	{
		if (!IsTypicalMethodDefinition(method))
		{
			GetTypicalMethodDefinition(method.Value, ObjectHandleOnStack.Create(ref method));
			GC.KeepAlive(method);
		}
		return method;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern int GetGenericParameterCount(RuntimeMethodHandleInternal method);

	internal static int GetGenericParameterCount(IRuntimeMethodInfo method)
	{
		return GetGenericParameterCount(method.Value);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void StripMethodInstantiation(RuntimeMethodHandleInternal method, ObjectHandleOnStack outMethod);

	internal static IRuntimeMethodInfo StripMethodInstantiation(IRuntimeMethodInfo method)
	{
		IRuntimeMethodInfo o = method;
		StripMethodInstantiation(method.Value, ObjectHandleOnStack.Create(ref o));
		GC.KeepAlive(method);
		return o;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsDynamicMethod(RuntimeMethodHandleInternal method);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern void Destroy(RuntimeMethodHandleInternal method);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern Resolver GetResolver(RuntimeMethodHandleInternal method);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeMethodBody GetMethodBody(IRuntimeMethodInfo method, RuntimeType declaringType);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool IsConstructor(RuntimeMethodHandleInternal method);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern LoaderAllocator GetLoaderAllocator(RuntimeMethodHandleInternal method);
}
