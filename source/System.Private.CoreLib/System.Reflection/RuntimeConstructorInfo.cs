using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace System.Reflection;

internal sealed class RuntimeConstructorInfo : ConstructorInfo, IRuntimeMethodInfo
{
	private volatile RuntimeType m_declaringType;

	private RuntimeType.RuntimeTypeCache m_reflectedTypeCache;

	private string m_toString;

	private ParameterInfo[] m_parameters;

	private object _empty1;

	private object _empty2;

	private object _empty3;

	private IntPtr m_handle;

	private MethodAttributes m_methodAttributes;

	private BindingFlags m_bindingFlags;

	private Signature m_signature;

	private INVOCATION_FLAGS m_invocationFlags;

	internal INVOCATION_FLAGS InvocationFlags
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			INVOCATION_FLAGS iNVOCATION_FLAGS = m_invocationFlags;
			if ((iNVOCATION_FLAGS & INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED) == 0)
			{
				iNVOCATION_FLAGS = LazyCreateInvocationFlags();
			}
			return iNVOCATION_FLAGS;
			[MethodImpl(MethodImplOptions.NoInlining)]
			INVOCATION_FLAGS LazyCreateInvocationFlags()
			{
				INVOCATION_FLAGS iNVOCATION_FLAGS2 = INVOCATION_FLAGS.INVOCATION_FLAGS_IS_CTOR;
				Type declaringType = DeclaringType;
				if (declaringType == typeof(void) || (declaringType != null && declaringType.ContainsGenericParameters) || (CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
				{
					iNVOCATION_FLAGS2 |= INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE;
				}
				else if (base.IsStatic)
				{
					iNVOCATION_FLAGS2 |= INVOCATION_FLAGS.INVOCATION_FLAGS_RUN_CLASS_CONSTRUCTOR | INVOCATION_FLAGS.INVOCATION_FLAGS_NO_CTOR_INVOKE;
				}
				else if (declaringType != null && declaringType.IsAbstract)
				{
					iNVOCATION_FLAGS2 |= INVOCATION_FLAGS.INVOCATION_FLAGS_NO_CTOR_INVOKE;
				}
				else
				{
					if (declaringType != null && declaringType.IsByRefLike)
					{
						iNVOCATION_FLAGS2 |= INVOCATION_FLAGS.INVOCATION_FLAGS_CONTAINS_STACK_POINTERS;
					}
					if (typeof(Delegate).IsAssignableFrom(DeclaringType))
					{
						iNVOCATION_FLAGS2 |= INVOCATION_FLAGS.INVOCATION_FLAGS_IS_DELEGATE_CTOR;
					}
				}
				return m_invocationFlags = iNVOCATION_FLAGS2 | INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED;
			}
		}
	}

	RuntimeMethodHandleInternal IRuntimeMethodInfo.Value => new RuntimeMethodHandleInternal(m_handle);

	private Signature Signature
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return m_signature ?? LazyCreateSignature();
			[MethodImpl(MethodImplOptions.NoInlining)]
			Signature LazyCreateSignature()
			{
				Signature signature = new Signature(this, m_declaringType);
				Volatile.Write(ref m_signature, signature);
				return signature;
			}
		}
	}

	private RuntimeType ReflectedTypeInternal => m_reflectedTypeCache.GetRuntimeType();

	internal BindingFlags BindingFlags => m_bindingFlags;

	public override string Name => RuntimeMethodHandle.GetName(this);

	public override MemberTypes MemberType => MemberTypes.Constructor;

	public override Type DeclaringType
	{
		get
		{
			if (!m_reflectedTypeCache.IsGlobal)
			{
				return m_declaringType;
			}
			return null;
		}
	}

	public override Type ReflectedType
	{
		get
		{
			if (!m_reflectedTypeCache.IsGlobal)
			{
				return ReflectedTypeInternal;
			}
			return null;
		}
	}

	public override int MetadataToken => RuntimeMethodHandle.GetMethodDef(this);

	public override Module Module => GetRuntimeModule();

	public override RuntimeMethodHandle MethodHandle => new RuntimeMethodHandle(this);

	public override MethodAttributes Attributes => m_methodAttributes;

	public override CallingConventions CallingConvention => Signature.CallingConvention;

	public override bool IsSecurityCritical => true;

	public override bool IsSecuritySafeCritical => false;

	public override bool IsSecurityTransparent => false;

	public override bool ContainsGenericParameters
	{
		get
		{
			if (DeclaringType != null)
			{
				return DeclaringType.ContainsGenericParameters;
			}
			return false;
		}
	}

	internal RuntimeConstructorInfo(RuntimeMethodHandleInternal handle, RuntimeType declaringType, RuntimeType.RuntimeTypeCache reflectedTypeCache, MethodAttributes methodAttributes, BindingFlags bindingFlags)
	{
		m_bindingFlags = bindingFlags;
		m_reflectedTypeCache = reflectedTypeCache;
		m_declaringType = declaringType;
		m_handle = handle.Value;
		m_methodAttributes = methodAttributes;
	}

	internal override bool CacheEquals(object o)
	{
		if (o is RuntimeConstructorInfo runtimeConstructorInfo)
		{
			return runtimeConstructorInfo.m_handle == m_handle;
		}
		return false;
	}

	private void CheckConsistency(object target)
	{
		if ((target != null || !base.IsStatic) && !m_declaringType.IsInstanceOfType(target))
		{
			if (target == null)
			{
				throw new TargetException(SR.RFLCT_Targ_StatMethReqTarg);
			}
			throw new TargetException(SR.RFLCT_Targ_ITargMismatch);
		}
	}

	public override string ToString()
	{
		if (m_toString == null)
		{
			ValueStringBuilder sbParamList = new ValueStringBuilder(100);
			sbParamList.Append("Void ");
			sbParamList.Append(Name);
			sbParamList.Append('(');
			MethodBase.AppendParameters(ref sbParamList, GetParameterTypes(), CallingConvention);
			sbParamList.Append(')');
			m_toString = sbParamList.ToString();
		}
		return m_toString;
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

	public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
	{
		return HasSameMetadataDefinitionAsCore<RuntimeConstructorInfo>(other);
	}

	internal RuntimeType GetRuntimeType()
	{
		return m_declaringType;
	}

	internal RuntimeModule GetRuntimeModule()
	{
		return RuntimeTypeHandle.GetModule(m_declaringType);
	}

	internal override Type GetReturnType()
	{
		return Signature.ReturnType;
	}

	internal override ParameterInfo[] GetParametersNoCopy()
	{
		return m_parameters ?? (m_parameters = RuntimeParameterInfo.GetParameters(this, this, Signature));
	}

	public override ParameterInfo[] GetParameters()
	{
		ParameterInfo[] parametersNoCopy = GetParametersNoCopy();
		if (parametersNoCopy.Length == 0)
		{
			return parametersNoCopy;
		}
		ParameterInfo[] array = new ParameterInfo[parametersNoCopy.Length];
		Array.Copy(parametersNoCopy, array, parametersNoCopy.Length);
		return array;
	}

	public override MethodImplAttributes GetMethodImplementationFlags()
	{
		return RuntimeMethodHandle.GetImplAttributes(this);
	}

	internal static void CheckCanCreateInstance(Type declaringType, bool isVarArg)
	{
		if (declaringType == null)
		{
			throw new ArgumentNullException("declaringType");
		}
		if (declaringType.IsInterface)
		{
			throw new MemberAccessException(SR.Format(SR.Acc_CreateInterfaceEx, declaringType));
		}
		if (declaringType.IsAbstract)
		{
			throw new MemberAccessException(SR.Format(SR.Acc_CreateAbstEx, declaringType));
		}
		if (declaringType.GetRootElementType() == typeof(ArgIterator))
		{
			throw new NotSupportedException();
		}
		if (isVarArg)
		{
			throw new NotSupportedException();
		}
		if (declaringType.ContainsGenericParameters)
		{
			throw new MemberAccessException(SR.Format(SR.Acc_CreateGenericEx, declaringType));
		}
		if (declaringType == typeof(void))
		{
			throw new MemberAccessException(SR.Access_Void);
		}
	}

	[DoesNotReturn]
	internal void ThrowNoInvokeException()
	{
		CheckCanCreateInstance(DeclaringType, (CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs);
		if ((Attributes & MethodAttributes.Static) == MethodAttributes.Static)
		{
			throw new MemberAccessException(SR.Acc_NotClassInit);
		}
		throw new TargetException();
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2059:RunClassConstructor", Justification = "This ConstructorInfo instance represents the static constructor itself, so if this object was created, the static constructor exists.")]
	public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		INVOCATION_FLAGS invocationFlags = InvocationFlags;
		if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE) != 0)
		{
			ThrowNoInvokeException();
		}
		CheckConsistency(obj);
		if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_RUN_CLASS_CONSTRUCTOR) != 0)
		{
			Type declaringType = DeclaringType;
			if (declaringType != null)
			{
				RuntimeHelpers.RunClassConstructor(declaringType.TypeHandle);
			}
			else
			{
				RuntimeHelpers.RunModuleConstructor(Module.ModuleHandle);
			}
			return null;
		}
		Signature signature = Signature;
		int num = signature.Arguments.Length;
		int num2 = ((parameters != null) ? parameters.Length : 0);
		if (num != num2)
		{
			throw new TargetParameterCountException(SR.Arg_ParmCnt);
		}
		bool wrapExceptions = (invokeAttr & BindingFlags.DoNotWrapExceptions) == 0;
		StackAllocedArguments stackArgs = default(StackAllocedArguments);
		Span<object> arguments = default(Span<object>);
		if (num2 != 0)
		{
			arguments = CheckArguments(ref stackArgs, parameters, binder, invokeAttr, culture, signature);
		}
		object result = RuntimeMethodHandle.InvokeMethod(obj, in arguments, signature, constructor: false, wrapExceptions);
		for (int i = 0; i < arguments.Length; i++)
		{
			parameters[i] = arguments[i];
		}
		return result;
	}

	[RequiresUnreferencedCode("Trimming may change method bodies. For example it can change some instructions, remove branches or local variables.")]
	public override MethodBody GetMethodBody()
	{
		RuntimeMethodBody methodBody = RuntimeMethodHandle.GetMethodBody(this, ReflectedTypeInternal);
		if (methodBody != null)
		{
			methodBody._methodBase = this;
		}
		return methodBody;
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		INVOCATION_FLAGS invocationFlags = InvocationFlags;
		if ((invocationFlags & (INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE | INVOCATION_FLAGS.INVOCATION_FLAGS_NO_CTOR_INVOKE | INVOCATION_FLAGS.INVOCATION_FLAGS_CONTAINS_STACK_POINTERS)) != 0)
		{
			ThrowNoInvokeException();
		}
		Signature signature = Signature;
		int num = signature.Arguments.Length;
		int num2 = ((parameters != null) ? parameters.Length : 0);
		if (num != num2)
		{
			throw new TargetParameterCountException(SR.Arg_ParmCnt);
		}
		bool wrapExceptions = (invokeAttr & BindingFlags.DoNotWrapExceptions) == 0;
		StackAllocedArguments stackArgs = default(StackAllocedArguments);
		Span<object> arguments = default(Span<object>);
		if (num2 != 0)
		{
			arguments = CheckArguments(ref stackArgs, parameters, binder, invokeAttr, culture, signature);
		}
		object result = RuntimeMethodHandle.InvokeMethod(null, in arguments, signature, constructor: true, wrapExceptions);
		for (int i = 0; i < arguments.Length; i++)
		{
			parameters[i] = arguments[i];
		}
		return result;
	}
}
