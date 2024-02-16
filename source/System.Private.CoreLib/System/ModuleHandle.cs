using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System;

public struct ModuleHandle
{
	public static readonly ModuleHandle EmptyHandle;

	private readonly RuntimeModule m_ptr;

	public int MDStreamVersion => GetMDStreamVersion(GetRuntimeModule());

	internal ModuleHandle(RuntimeModule module)
	{
		m_ptr = module;
	}

	internal RuntimeModule GetRuntimeModule()
	{
		return m_ptr;
	}

	public override int GetHashCode()
	{
		if (!(m_ptr != null))
		{
			return 0;
		}
		return m_ptr.GetHashCode();
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is ModuleHandle moduleHandle))
		{
			return false;
		}
		return moduleHandle.m_ptr == m_ptr;
	}

	public bool Equals(ModuleHandle handle)
	{
		return handle.m_ptr == m_ptr;
	}

	public static bool operator ==(ModuleHandle left, ModuleHandle right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ModuleHandle left, ModuleHandle right)
	{
		return !left.Equals(right);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern IRuntimeMethodInfo GetDynamicMethod(DynamicMethod method, RuntimeModule module, string name, byte[] sig, Resolver resolver);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int GetToken(RuntimeModule module);

	private static void ValidateModulePointer(RuntimeModule module)
	{
		if ((object)module == null)
		{
			ThrowInvalidOperationException();
		}
		[StackTraceHidden]
		[DoesNotReturn]
		static void ThrowInvalidOperationException()
		{
			throw new InvalidOperationException(SR.InvalidOperation_NullModuleHandle);
		}
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public RuntimeTypeHandle GetRuntimeTypeHandleFromMetadataToken(int typeToken)
	{
		return ResolveTypeHandle(typeToken);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public RuntimeTypeHandle ResolveTypeHandle(int typeToken)
	{
		return ResolveTypeHandle(typeToken, null, null);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public unsafe RuntimeTypeHandle ResolveTypeHandle(int typeToken, RuntimeTypeHandle[]? typeInstantiationContext, RuntimeTypeHandle[]? methodInstantiationContext)
	{
		RuntimeModule module = GetRuntimeModule();
		ValidateModulePointer(module);
		IntPtr[] array = null;
		int length = 0;
		IntPtr[] array2 = null;
		int length2 = 0;
		if (typeInstantiationContext != null && typeInstantiationContext.Length != 0)
		{
			typeInstantiationContext = (RuntimeTypeHandle[])typeInstantiationContext.Clone();
			array = RuntimeTypeHandle.CopyRuntimeTypeHandles(typeInstantiationContext, out length);
		}
		if (methodInstantiationContext != null && methodInstantiationContext.Length != 0)
		{
			methodInstantiationContext = (RuntimeTypeHandle[])methodInstantiationContext.Clone();
			array2 = RuntimeTypeHandle.CopyRuntimeTypeHandles(methodInstantiationContext, out length2);
		}
		fixed (IntPtr* typeInstArgs = array)
		{
			fixed (IntPtr* methodInstArgs = array2)
			{
				try
				{
					RuntimeType o = null;
					ResolveType(new QCallModule(ref module), typeToken, typeInstArgs, length, methodInstArgs, length2, ObjectHandleOnStack.Create(ref o));
					GC.KeepAlive(typeInstantiationContext);
					GC.KeepAlive(methodInstantiationContext);
					return new RuntimeTypeHandle(o);
				}
				catch (Exception)
				{
					if (!GetMetadataImport(module).IsValidToken(typeToken))
					{
						throw new ArgumentOutOfRangeException("typeToken", SR.Format(SR.Argument_InvalidToken, typeToken, new ModuleHandle(module)));
					}
					throw;
				}
			}
		}
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private unsafe static extern void ResolveType(QCallModule module, int typeToken, IntPtr* typeInstArgs, int typeInstCount, IntPtr* methodInstArgs, int methodInstCount, ObjectHandleOnStack type);

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public RuntimeMethodHandle GetRuntimeMethodHandleFromMetadataToken(int methodToken)
	{
		return ResolveMethodHandle(methodToken);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public RuntimeMethodHandle ResolveMethodHandle(int methodToken)
	{
		return ResolveMethodHandle(methodToken, null, null);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public RuntimeMethodHandle ResolveMethodHandle(int methodToken, RuntimeTypeHandle[]? typeInstantiationContext, RuntimeTypeHandle[]? methodInstantiationContext)
	{
		RuntimeModule runtimeModule = GetRuntimeModule();
		typeInstantiationContext = (RuntimeTypeHandle[])typeInstantiationContext?.Clone();
		methodInstantiationContext = (RuntimeTypeHandle[])methodInstantiationContext?.Clone();
		int length;
		IntPtr[] typeInstantiationContext2 = RuntimeTypeHandle.CopyRuntimeTypeHandles(typeInstantiationContext, out length);
		int length2;
		IntPtr[] methodInstantiationContext2 = RuntimeTypeHandle.CopyRuntimeTypeHandles(methodInstantiationContext, out length2);
		RuntimeMethodHandleInternal runtimeMethodHandleInternal = ResolveMethodHandleInternal(runtimeModule, methodToken, typeInstantiationContext2, length, methodInstantiationContext2, length2);
		IRuntimeMethodInfo method = new RuntimeMethodInfoStub(runtimeMethodHandleInternal, RuntimeMethodHandle.GetLoaderAllocator(runtimeMethodHandleInternal));
		GC.KeepAlive(typeInstantiationContext);
		GC.KeepAlive(methodInstantiationContext);
		return new RuntimeMethodHandle(method);
	}

	internal unsafe static RuntimeMethodHandleInternal ResolveMethodHandleInternal(RuntimeModule module, int methodToken, IntPtr[] typeInstantiationContext, int typeInstCount, IntPtr[] methodInstantiationContext, int methodInstCount)
	{
		ValidateModulePointer(module);
		try
		{
			fixed (IntPtr* typeInstArgs = typeInstantiationContext)
			{
				fixed (IntPtr* methodInstArgs = methodInstantiationContext)
				{
					return ResolveMethod(new QCallModule(ref module), methodToken, typeInstArgs, typeInstCount, methodInstArgs, methodInstCount);
				}
			}
		}
		catch (Exception)
		{
			if (!GetMetadataImport(module).IsValidToken(methodToken))
			{
				throw new ArgumentOutOfRangeException("methodToken", SR.Format(SR.Argument_InvalidToken, methodToken, new ModuleHandle(module)));
			}
			throw;
		}
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private unsafe static extern RuntimeMethodHandleInternal ResolveMethod(QCallModule module, int methodToken, IntPtr* typeInstArgs, int typeInstCount, IntPtr* methodInstArgs, int methodInstCount);

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public RuntimeFieldHandle GetRuntimeFieldHandleFromMetadataToken(int fieldToken)
	{
		return ResolveFieldHandle(fieldToken);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public RuntimeFieldHandle ResolveFieldHandle(int fieldToken)
	{
		return ResolveFieldHandle(fieldToken, null, null);
	}

	[RequiresUnreferencedCode("Trimming changes metadata tokens")]
	public unsafe RuntimeFieldHandle ResolveFieldHandle(int fieldToken, RuntimeTypeHandle[]? typeInstantiationContext, RuntimeTypeHandle[]? methodInstantiationContext)
	{
		RuntimeModule module = GetRuntimeModule();
		ValidateModulePointer(module);
		IntPtr[] array = null;
		int length = 0;
		IntPtr[] array2 = null;
		int length2 = 0;
		if (typeInstantiationContext != null && typeInstantiationContext.Length != 0)
		{
			typeInstantiationContext = (RuntimeTypeHandle[])typeInstantiationContext.Clone();
			array = RuntimeTypeHandle.CopyRuntimeTypeHandles(typeInstantiationContext, out length);
		}
		if (methodInstantiationContext != null && methodInstantiationContext.Length != 0)
		{
			methodInstantiationContext = (RuntimeTypeHandle[])methodInstantiationContext.Clone();
			array2 = RuntimeTypeHandle.CopyRuntimeTypeHandles(methodInstantiationContext, out length2);
		}
		fixed (IntPtr* typeInstArgs = array)
		{
			fixed (IntPtr* methodInstArgs = array2)
			{
				try
				{
					IRuntimeFieldInfo o = null;
					ResolveField(new QCallModule(ref module), fieldToken, typeInstArgs, length, methodInstArgs, length2, ObjectHandleOnStack.Create(ref o));
					GC.KeepAlive(typeInstantiationContext);
					GC.KeepAlive(methodInstantiationContext);
					return new RuntimeFieldHandle(o);
				}
				catch (Exception)
				{
					if (!GetMetadataImport(module).IsValidToken(fieldToken))
					{
						throw new ArgumentOutOfRangeException("fieldToken", SR.Format(SR.Argument_InvalidToken, fieldToken, new ModuleHandle(module)));
					}
					throw;
				}
			}
		}
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private unsafe static extern void ResolveField(QCallModule module, int fieldToken, IntPtr* typeInstArgs, int typeInstCount, IntPtr* methodInstArgs, int methodInstCount, ObjectHandleOnStack retField);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern Interop.BOOL _ContainsPropertyMatchingHash(QCallModule module, int propertyToken, uint hash);

	internal static bool ContainsPropertyMatchingHash(RuntimeModule module, int propertyToken, uint hash)
	{
		return _ContainsPropertyMatchingHash(new QCallModule(ref module), propertyToken, hash) != Interop.BOOL.FALSE;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern void GetModuleType(QCallModule handle, ObjectHandleOnStack type);

	internal static RuntimeType GetModuleType(RuntimeModule module)
	{
		RuntimeType o = null;
		GetModuleType(new QCallModule(ref module), ObjectHandleOnStack.Create(ref o));
		return o;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private unsafe static extern void GetPEKind(QCallModule handle, int* peKind, int* machine);

	internal unsafe static void GetPEKind(RuntimeModule module, out PortableExecutableKinds peKind, out ImageFileMachine machine)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out int num);
		System.Runtime.CompilerServices.Unsafe.SkipInit(out int num2);
		GetPEKind(new QCallModule(ref module), &num, &num2);
		peKind = (PortableExecutableKinds)num;
		machine = (ImageFileMachine)num2;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int GetMDStreamVersion(RuntimeModule module);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern IntPtr _GetMetadataImport(RuntimeModule module);

	internal static MetadataImport GetMetadataImport(RuntimeModule module)
	{
		return new MetadataImport(_GetMetadataImport(module), module);
	}
}
