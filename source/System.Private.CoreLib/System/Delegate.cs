using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System;

[ClassInterface(ClassInterfaceType.None)]
[ComVisible(true)]
public abstract class Delegate : ICloneable, ISerializable
{
	internal object _target;

	internal object _methodBase;

	internal IntPtr _methodPtr;

	internal IntPtr _methodPtrAux;

	public object? Target => GetTarget();

	public MethodInfo Method => GetMethodImpl();

	[RequiresUnreferencedCode("The target method might be removed")]
	protected Delegate(object target, string method)
	{
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		if (method == null)
		{
			throw new ArgumentNullException("method");
		}
		if (!BindToMethodName(target, (RuntimeType)target.GetType(), method, (DelegateBindingFlags)10))
		{
			throw new ArgumentException(SR.Arg_DlgtTargMeth);
		}
	}

	protected Delegate([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type target, string method)
	{
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		if (target.ContainsGenericParameters)
		{
			throw new ArgumentException(SR.Arg_UnboundGenParam, "target");
		}
		if (method == null)
		{
			throw new ArgumentNullException("method");
		}
		if (!(target is RuntimeType methodType))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType, "target");
		}
		BindToMethodName(null, methodType, method, (DelegateBindingFlags)37);
	}

	protected virtual object? DynamicInvokeImpl(object?[]? args)
	{
		RuntimeMethodInfo runtimeMethodInfo = (RuntimeMethodInfo)RuntimeType.GetMethodBase(methodHandle: new RuntimeMethodHandleInternal(GetInvokeMethod()), reflectedType: (RuntimeType)GetType());
		return runtimeMethodInfo.Invoke(this, BindingFlags.Default, null, args, null);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == null || !InternalEqualTypes(this, obj))
		{
			return false;
		}
		Delegate @delegate = (Delegate)obj;
		if (_target == @delegate._target && _methodPtr == @delegate._methodPtr && _methodPtrAux == @delegate._methodPtrAux)
		{
			return true;
		}
		if (_methodPtrAux == IntPtr.Zero)
		{
			if (@delegate._methodPtrAux != IntPtr.Zero)
			{
				return false;
			}
			if (_target != @delegate._target)
			{
				return false;
			}
		}
		else
		{
			if (@delegate._methodPtrAux == IntPtr.Zero)
			{
				return false;
			}
			if (_methodPtrAux == @delegate._methodPtrAux)
			{
				return true;
			}
		}
		if (_methodBase == null || @delegate._methodBase == null || !(_methodBase is MethodInfo) || !(@delegate._methodBase is MethodInfo))
		{
			return InternalEqualMethodHandles(this, @delegate);
		}
		return _methodBase.Equals(@delegate._methodBase);
	}

	public override int GetHashCode()
	{
		if (_methodPtrAux == IntPtr.Zero)
		{
			return ((_target != null) ? (RuntimeHelpers.GetHashCode(_target) * 33) : 0) + GetType().GetHashCode();
		}
		return GetType().GetHashCode();
	}

	protected virtual MethodInfo GetMethodImpl()
	{
		if (_methodBase == null || !(_methodBase is MethodInfo))
		{
			IRuntimeMethodInfo runtimeMethodInfo = FindMethodHandle();
			RuntimeType runtimeType = RuntimeMethodHandle.GetDeclaringType(runtimeMethodInfo);
			if ((RuntimeTypeHandle.IsGenericTypeDefinition(runtimeType) || RuntimeTypeHandle.HasInstantiation(runtimeType)) && (RuntimeMethodHandle.GetAttributes(runtimeMethodInfo) & MethodAttributes.Static) == 0)
			{
				if (_methodPtrAux == IntPtr.Zero)
				{
					Type type = _target.GetType();
					Type genericTypeDefinition = runtimeType.GetGenericTypeDefinition();
					while (type != null)
					{
						if (type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition)
						{
							runtimeType = type as RuntimeType;
							break;
						}
						type = type.BaseType;
					}
				}
				else
				{
					MethodInfo method = GetType().GetMethod("Invoke");
					runtimeType = (RuntimeType)method.GetParameters()[0].ParameterType;
				}
			}
			_methodBase = (MethodInfo)RuntimeType.GetMethodBase(runtimeType, runtimeMethodInfo);
		}
		return (MethodInfo)_methodBase;
	}

	[RequiresUnreferencedCode("The target method might be removed")]
	public static Delegate? CreateDelegate(Type type, object target, string method, bool ignoreCase, bool throwOnBindFailure)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		if (method == null)
		{
			throw new ArgumentNullException("method");
		}
		if (!(type is RuntimeType runtimeType))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType, "type");
		}
		if (!runtimeType.IsDelegate())
		{
			throw new ArgumentException(SR.Arg_MustBeDelegate, "type");
		}
		Delegate @delegate = InternalAlloc(runtimeType);
		if (!@delegate.BindToMethodName(target, (RuntimeType)target.GetType(), method, (DelegateBindingFlags)26 | (ignoreCase ? DelegateBindingFlags.CaselessMatching : ((DelegateBindingFlags)0))))
		{
			if (throwOnBindFailure)
			{
				throw new ArgumentException(SR.Arg_DlgtTargMeth);
			}
			return null;
		}
		return @delegate;
	}

	public static Delegate? CreateDelegate(Type type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type target, string method, bool ignoreCase, bool throwOnBindFailure)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		if (target.ContainsGenericParameters)
		{
			throw new ArgumentException(SR.Arg_UnboundGenParam, "target");
		}
		if (method == null)
		{
			throw new ArgumentNullException("method");
		}
		if (!(type is RuntimeType runtimeType))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType, "type");
		}
		if (!(target is RuntimeType methodType))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType, "target");
		}
		if (!runtimeType.IsDelegate())
		{
			throw new ArgumentException(SR.Arg_MustBeDelegate, "type");
		}
		Delegate @delegate = InternalAlloc(runtimeType);
		if (!@delegate.BindToMethodName(null, methodType, method, (DelegateBindingFlags)5 | (ignoreCase ? DelegateBindingFlags.CaselessMatching : ((DelegateBindingFlags)0))))
		{
			if (throwOnBindFailure)
			{
				throw new ArgumentException(SR.Arg_DlgtTargMeth);
			}
			return null;
		}
		return @delegate;
	}

	public static Delegate? CreateDelegate(Type type, MethodInfo method, bool throwOnBindFailure)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (method == null)
		{
			throw new ArgumentNullException("method");
		}
		if (!(type is RuntimeType runtimeType))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType, "type");
		}
		if (!(method is RuntimeMethodInfo rtMethod))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeMethodInfo, "method");
		}
		if (!runtimeType.IsDelegate())
		{
			throw new ArgumentException(SR.Arg_MustBeDelegate, "type");
		}
		Delegate @delegate = CreateDelegateInternal(runtimeType, rtMethod, null, (DelegateBindingFlags)68);
		if ((object)@delegate == null && throwOnBindFailure)
		{
			throw new ArgumentException(SR.Arg_DlgtTargMeth);
		}
		return @delegate;
	}

	public static Delegate? CreateDelegate(Type type, object? firstArgument, MethodInfo method, bool throwOnBindFailure)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (method == null)
		{
			throw new ArgumentNullException("method");
		}
		if (!(type is RuntimeType runtimeType))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType, "type");
		}
		if (!(method is RuntimeMethodInfo rtMethod))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeMethodInfo, "method");
		}
		if (!runtimeType.IsDelegate())
		{
			throw new ArgumentException(SR.Arg_MustBeDelegate, "type");
		}
		Delegate @delegate = CreateDelegateInternal(runtimeType, rtMethod, firstArgument, DelegateBindingFlags.RelaxedSignature);
		if ((object)@delegate == null && throwOnBindFailure)
		{
			throw new ArgumentException(SR.Arg_DlgtTargMeth);
		}
		return @delegate;
	}

	internal static Delegate CreateDelegateNoSecurityCheck(Type type, object target, RuntimeMethodHandle method)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (method.IsNullHandle())
		{
			throw new ArgumentNullException("method");
		}
		if (!(type is RuntimeType runtimeType))
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType, "type");
		}
		if (!runtimeType.IsDelegate())
		{
			throw new ArgumentException(SR.Arg_MustBeDelegate, "type");
		}
		Delegate @delegate = InternalAlloc(runtimeType);
		if (!@delegate.BindToMethodInfo(target, method.GetMethodInfo(), RuntimeMethodHandle.GetDeclaringType(method.GetMethodInfo()), DelegateBindingFlags.RelaxedSignature))
		{
			throw new ArgumentException(SR.Arg_DlgtTargMeth);
		}
		return @delegate;
	}

	internal static Delegate CreateDelegateInternal(RuntimeType rtType, RuntimeMethodInfo rtMethod, object firstArgument, DelegateBindingFlags flags)
	{
		Delegate @delegate = InternalAlloc(rtType);
		if (@delegate.BindToMethodInfo(firstArgument, rtMethod, rtMethod.GetDeclaringTypeInternal(), flags))
		{
			return @delegate;
		}
		return null;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern bool BindToMethodName(object target, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] RuntimeType methodType, string method, DelegateBindingFlags flags);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern bool BindToMethodInfo(object target, IRuntimeMethodInfo method, RuntimeType methodType, DelegateBindingFlags flags);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern MulticastDelegate InternalAlloc(RuntimeType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern MulticastDelegate InternalAllocLike(Delegate d);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool InternalEqualTypes(object a, object b);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern void DelegateConstruct(object target, IntPtr slot);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal extern IntPtr GetMulticastInvoke();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal extern IntPtr GetInvokeMethod();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal extern IRuntimeMethodInfo FindMethodHandle();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool InternalEqualMethodHandles(Delegate left, Delegate right);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal extern IntPtr AdjustTarget(object target, IntPtr methodPtr);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal extern IntPtr GetCallStub(IntPtr methodPtr);

	internal virtual object GetTarget()
	{
		if (!(_methodPtrAux == IntPtr.Zero))
		{
			return null;
		}
		return _target;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern bool CompareUnmanagedFunctionPtrs(Delegate d1, Delegate d2);

	public virtual object Clone()
	{
		return MemberwiseClone();
	}

	[return: NotNullIfNotNull("a")]
	[return: NotNullIfNotNull("b")]
	public static Delegate? Combine(Delegate? a, Delegate? b)
	{
		if ((object)a == null)
		{
			return b;
		}
		return a.CombineImpl(b);
	}

	public static Delegate? Combine(params Delegate?[]? delegates)
	{
		if (delegates == null || delegates.Length == 0)
		{
			return null;
		}
		Delegate @delegate = delegates[0];
		for (int i = 1; i < delegates.Length; i++)
		{
			@delegate = Combine(@delegate, delegates[i]);
		}
		return @delegate;
	}

	public static Delegate CreateDelegate(Type type, object? firstArgument, MethodInfo method)
	{
		return CreateDelegate(type, firstArgument, method, throwOnBindFailure: true);
	}

	public static Delegate CreateDelegate(Type type, MethodInfo method)
	{
		return CreateDelegate(type, method, throwOnBindFailure: true);
	}

	[RequiresUnreferencedCode("The target method might be removed")]
	public static Delegate CreateDelegate(Type type, object target, string method)
	{
		return CreateDelegate(type, target, method, ignoreCase: false, throwOnBindFailure: true);
	}

	[RequiresUnreferencedCode("The target method might be removed")]
	public static Delegate CreateDelegate(Type type, object target, string method, bool ignoreCase)
	{
		return CreateDelegate(type, target, method, ignoreCase, throwOnBindFailure: true);
	}

	public static Delegate CreateDelegate(Type type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type target, string method)
	{
		return CreateDelegate(type, target, method, ignoreCase: false, throwOnBindFailure: true);
	}

	public static Delegate CreateDelegate(Type type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type target, string method, bool ignoreCase)
	{
		return CreateDelegate(type, target, method, ignoreCase, throwOnBindFailure: true);
	}

	protected virtual Delegate CombineImpl(Delegate? d)
	{
		throw new MulticastNotSupportedException(SR.Multicast_Combine);
	}

	protected virtual Delegate? RemoveImpl(Delegate d)
	{
		if (!d.Equals(this))
		{
			return this;
		}
		return null;
	}

	public virtual Delegate[] GetInvocationList()
	{
		return new Delegate[1] { this };
	}

	public object? DynamicInvoke(params object?[]? args)
	{
		return DynamicInvokeImpl(args);
	}

	public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	public static Delegate? Remove(Delegate? source, Delegate? value)
	{
		if ((object)source == null)
		{
			return null;
		}
		if ((object)value == null)
		{
			return source;
		}
		if (!InternalEqualTypes(source, value))
		{
			throw new ArgumentException(SR.Arg_DlgtTypeMis);
		}
		return source.RemoveImpl(value);
	}

	public static Delegate? RemoveAll(Delegate? source, Delegate? value)
	{
		Delegate @delegate;
		do
		{
			@delegate = source;
			source = Remove(source, value);
		}
		while (@delegate != source);
		return @delegate;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Delegate? d1, Delegate? d2)
	{
		if ((object)d2 == null)
		{
			if ((object)d1 != null)
			{
				return false;
			}
			return true;
		}
		if ((object)d2 != d1)
		{
			return d2.Equals(d1);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Delegate? d1, Delegate? d2)
	{
		if ((object)d2 == null)
		{
			if ((object)d1 != null)
			{
				return true;
			}
			return false;
		}
		if ((object)d2 != d1)
		{
			return !d2.Equals(d1);
		}
		return false;
	}
}
