using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading;

namespace System.Reflection;

internal sealed class RuntimeMethodInfo : MethodInfo, IRuntimeMethodInfo
{
	private IntPtr m_handle;

	private RuntimeType.RuntimeTypeCache m_reflectedTypeCache;

	private string m_name;

	private string m_toString;

	private ParameterInfo[] m_parameters;

	private ParameterInfo m_returnParameter;

	private BindingFlags m_bindingFlags;

	private MethodAttributes m_methodAttributes;

	private Signature m_signature;

	private RuntimeType m_declaringType;

	private object m_keepalive;

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
				INVOCATION_FLAGS iNVOCATION_FLAGS2 = INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN;
				Type declaringType = DeclaringType;
				if (ContainsGenericParameters || IsDisallowedByRefType(ReturnType) || (declaringType != null && declaringType.ContainsGenericParameters) || (CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
				{
					iNVOCATION_FLAGS2 = INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE;
				}
				else if ((declaringType != null && declaringType.IsByRefLike) || ReturnType.IsByRefLike)
				{
					iNVOCATION_FLAGS2 |= INVOCATION_FLAGS.INVOCATION_FLAGS_CONTAINS_STACK_POINTERS;
				}
				return m_invocationFlags = iNVOCATION_FLAGS2 | INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED;
			}
		}
	}

	RuntimeMethodHandleInternal IRuntimeMethodInfo.Value => new RuntimeMethodHandleInternal(m_handle);

	private RuntimeType ReflectedTypeInternal => m_reflectedTypeCache.GetRuntimeType();

	internal Signature Signature
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

	internal BindingFlags BindingFlags => m_bindingFlags;

	internal sealed override int GenericParameterCount => RuntimeMethodHandle.GetGenericParameterCount(this);

	public override string Name => m_name ?? (m_name = RuntimeMethodHandle.GetName(this));

	public override Type DeclaringType
	{
		get
		{
			if (m_reflectedTypeCache.IsGlobal)
			{
				return null;
			}
			return m_declaringType;
		}
	}

	public override Type ReflectedType
	{
		get
		{
			if (m_reflectedTypeCache.IsGlobal)
			{
				return null;
			}
			return m_reflectedTypeCache.GetRuntimeType();
		}
	}

	public override MemberTypes MemberType => MemberTypes.Method;

	public override int MetadataToken => RuntimeMethodHandle.GetMethodDef(this);

	public override Module Module => GetRuntimeModule();

	public override bool IsSecurityCritical => true;

	public override bool IsSecuritySafeCritical => false;

	public override bool IsSecurityTransparent => false;

	public override RuntimeMethodHandle MethodHandle => new RuntimeMethodHandle(this);

	public override MethodAttributes Attributes => m_methodAttributes;

	public override CallingConventions CallingConvention => Signature.CallingConvention;

	public override Type ReturnType => Signature.ReturnType;

	public override ICustomAttributeProvider ReturnTypeCustomAttributes => ReturnParameter;

	public override ParameterInfo ReturnParameter => FetchReturnParameter();

	public override bool IsCollectible => RuntimeMethodHandle.GetIsCollectible(new RuntimeMethodHandleInternal(m_handle)) != Interop.BOOL.FALSE;

	public override bool IsGenericMethod => RuntimeMethodHandle.HasMethodInstantiation(this);

	public override bool IsGenericMethodDefinition => RuntimeMethodHandle.IsGenericMethodDefinition(this);

	public override bool ContainsGenericParameters
	{
		get
		{
			if (DeclaringType != null && DeclaringType.ContainsGenericParameters)
			{
				return true;
			}
			if (!IsGenericMethod)
			{
				return false;
			}
			Type[] genericArguments = GetGenericArguments();
			for (int i = 0; i < genericArguments.Length; i++)
			{
				if (genericArguments[i].ContainsGenericParameters)
				{
					return true;
				}
			}
			return false;
		}
	}

	private static bool IsDisallowedByRefType(Type type)
	{
		if (!type.IsByRef)
		{
			return false;
		}
		Type elementType = type.GetElementType();
		if (!elementType.IsByRefLike)
		{
			return elementType == typeof(void);
		}
		return true;
	}

	internal RuntimeMethodInfo(RuntimeMethodHandleInternal handle, RuntimeType declaringType, RuntimeType.RuntimeTypeCache reflectedTypeCache, MethodAttributes methodAttributes, BindingFlags bindingFlags, object keepalive)
	{
		m_bindingFlags = bindingFlags;
		m_declaringType = declaringType;
		m_keepalive = keepalive;
		m_handle = handle.Value;
		m_reflectedTypeCache = reflectedTypeCache;
		m_methodAttributes = methodAttributes;
	}

	private ParameterInfo[] FetchNonReturnParameters()
	{
		return m_parameters ?? (m_parameters = RuntimeParameterInfo.GetParameters(this, this, Signature));
	}

	private ParameterInfo FetchReturnParameter()
	{
		return m_returnParameter ?? (m_returnParameter = RuntimeParameterInfo.GetReturnParameter(this, this, Signature));
	}

	internal override bool CacheEquals(object o)
	{
		if (o is RuntimeMethodInfo runtimeMethodInfo)
		{
			return runtimeMethodInfo.m_handle == m_handle;
		}
		return false;
	}

	internal RuntimeMethodInfo GetParentDefinition()
	{
		if (!base.IsVirtual || m_declaringType.IsInterface)
		{
			return null;
		}
		RuntimeType runtimeType = (RuntimeType)m_declaringType.BaseType;
		if (runtimeType == null)
		{
			return null;
		}
		int slot = RuntimeMethodHandle.GetSlot(this);
		if (RuntimeTypeHandle.GetNumVirtuals(runtimeType) <= slot)
		{
			return null;
		}
		return (RuntimeMethodInfo)RuntimeType.GetMethodBase(runtimeType, RuntimeTypeHandle.GetMethodAt(runtimeType, slot));
	}

	internal RuntimeType GetDeclaringTypeInternal()
	{
		return m_declaringType;
	}

	public override string ToString()
	{
		if (m_toString == null)
		{
			ValueStringBuilder sbParamList = new ValueStringBuilder(100);
			sbParamList.Append(ReturnType.FormatTypeName());
			sbParamList.Append(' ');
			sbParamList.Append(Name);
			if (IsGenericMethod)
			{
				sbParamList.Append(RuntimeMethodHandle.ConstructInstantiation(this, TypeNameFormatFlags.FormatBasic));
			}
			sbParamList.Append('(');
			MethodBase.AppendParameters(ref sbParamList, GetParameterTypes(), CallingConvention);
			sbParamList.Append(')');
			m_toString = sbParamList.ToString();
		}
		return m_toString;
	}

	public override int GetHashCode()
	{
		if (IsGenericMethod)
		{
			return ValueType.GetHashCodeOfPtr(m_handle);
		}
		return base.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (!IsGenericMethod)
		{
			return obj == this;
		}
		RuntimeMethodInfo runtimeMethodInfo = obj as RuntimeMethodInfo;
		if (runtimeMethodInfo == null || !runtimeMethodInfo.IsGenericMethod)
		{
			return false;
		}
		IRuntimeMethodInfo runtimeMethodInfo2 = RuntimeMethodHandle.StripMethodInstantiation(this);
		IRuntimeMethodInfo runtimeMethodInfo3 = RuntimeMethodHandle.StripMethodInstantiation(runtimeMethodInfo);
		if (runtimeMethodInfo2.Value.Value != runtimeMethodInfo3.Value.Value)
		{
			return false;
		}
		Type[] genericArguments = GetGenericArguments();
		Type[] genericArguments2 = runtimeMethodInfo.GetGenericArguments();
		if (genericArguments.Length != genericArguments2.Length)
		{
			return false;
		}
		for (int i = 0; i < genericArguments.Length; i++)
		{
			if (genericArguments[i] != genericArguments2[i])
			{
				return false;
			}
		}
		if (DeclaringType != runtimeMethodInfo.DeclaringType)
		{
			return false;
		}
		if (ReflectedType != runtimeMethodInfo.ReflectedType)
		{
			return false;
		}
		return true;
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType, inherit);
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
		return CustomAttribute.GetCustomAttributes(this, runtimeType, inherit);
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
		return CustomAttribute.IsDefined(this, runtimeType, inherit);
	}

	public override IList<CustomAttributeData> GetCustomAttributesData()
	{
		return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
	}

	public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
	{
		return HasSameMetadataDefinitionAsCore<RuntimeMethodInfo>(other);
	}

	internal RuntimeType GetRuntimeType()
	{
		return m_declaringType;
	}

	internal RuntimeModule GetRuntimeModule()
	{
		return m_declaringType.GetRuntimeModule();
	}

	internal override ParameterInfo[] GetParametersNoCopy()
	{
		return FetchNonReturnParameters();
	}

	public override ParameterInfo[] GetParameters()
	{
		ParameterInfo[] array = FetchNonReturnParameters();
		if (array.Length == 0)
		{
			return array;
		}
		ParameterInfo[] array2 = new ParameterInfo[array.Length];
		Array.Copy(array, array2, array.Length);
		return array2;
	}

	public override MethodImplAttributes GetMethodImplementationFlags()
	{
		return RuntimeMethodHandle.GetImplAttributes(this);
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void CheckConsistency(object target)
	{
		if ((m_methodAttributes & MethodAttributes.Static) == 0 && !m_declaringType.IsInstanceOfType(target))
		{
			if (target == null)
			{
				throw new TargetException(SR.RFLCT_Targ_StatMethReqTarg);
			}
			throw new TargetException(SR.RFLCT_Targ_ITargMismatch);
		}
	}

	[DoesNotReturn]
	private void ThrowNoInvokeException()
	{
		if ((InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_CONTAINS_STACK_POINTERS) != 0)
		{
			throw new NotSupportedException();
		}
		if ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
		{
			throw new NotSupportedException();
		}
		if (DeclaringType.ContainsGenericParameters || ContainsGenericParameters)
		{
			throw new InvalidOperationException(SR.Arg_UnboundGenParam);
		}
		if (base.IsAbstract)
		{
			throw new MemberAccessException();
		}
		if (ReturnType.IsByRef)
		{
			Type elementType = ReturnType.GetElementType();
			if (elementType.IsByRefLike)
			{
				throw new NotSupportedException(SR.NotSupported_ByRefToByRefLikeReturn);
			}
			if (elementType == typeof(void))
			{
				throw new NotSupportedException(SR.NotSupported_ByRefToVoidReturn);
			}
		}
		throw new TargetException();
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		if ((InvocationFlags & (INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE | INVOCATION_FLAGS.INVOCATION_FLAGS_CONTAINS_STACK_POINTERS)) != 0)
		{
			ThrowNoInvokeException();
		}
		CheckConsistency(obj);
		Signature signature = Signature;
		int num = ((parameters != null) ? parameters.Length : 0);
		if (signature.Arguments.Length != num)
		{
			throw new TargetParameterCountException(SR.Arg_ParmCnt);
		}
		StackAllocedArguments stackArgs = default(StackAllocedArguments);
		Span<object> arguments = default(Span<object>);
		if (num != 0)
		{
			arguments = CheckArguments(ref stackArgs, parameters, binder, invokeAttr, culture, signature);
		}
		bool wrapExceptions = (invokeAttr & BindingFlags.DoNotWrapExceptions) == 0;
		object result = RuntimeMethodHandle.InvokeMethod(obj, in arguments, Signature, constructor: false, wrapExceptions);
		for (int i = 0; i < arguments.Length; i++)
		{
			parameters[i] = arguments[i];
		}
		return result;
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	internal object InvokeOneParameter(object obj, BindingFlags invokeAttr, Binder binder, object parameter, CultureInfo culture)
	{
		if ((InvocationFlags & (INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE | INVOCATION_FLAGS.INVOCATION_FLAGS_CONTAINS_STACK_POINTERS)) != 0)
		{
			ThrowNoInvokeException();
		}
		CheckConsistency(obj);
		Signature signature = Signature;
		if (signature.Arguments.Length != 1)
		{
			throw new TargetParameterCountException(SR.Arg_ParmCnt);
		}
		StackAllocedArguments stackArgs = default(StackAllocedArguments);
		Span<object> arguments = CheckArguments(ref stackArgs, new ReadOnlySpan<object>(ref parameter, 1), binder, invokeAttr, culture, signature);
		bool wrapExceptions = (invokeAttr & BindingFlags.DoNotWrapExceptions) == 0;
		return RuntimeMethodHandle.InvokeMethod(obj, in arguments, Signature, constructor: false, wrapExceptions);
	}

	public override MethodInfo GetBaseDefinition()
	{
		if (!base.IsVirtual || base.IsStatic || m_declaringType == null || m_declaringType.IsInterface)
		{
			return this;
		}
		int slot = RuntimeMethodHandle.GetSlot(this);
		RuntimeType runtimeType = (RuntimeType)DeclaringType;
		RuntimeType reflectedType = runtimeType;
		RuntimeMethodHandleInternal methodHandle = default(RuntimeMethodHandleInternal);
		do
		{
			int numVirtuals = RuntimeTypeHandle.GetNumVirtuals(runtimeType);
			if (numVirtuals <= slot)
			{
				break;
			}
			methodHandle = RuntimeTypeHandle.GetMethodAt(runtimeType, slot);
			reflectedType = runtimeType;
			runtimeType = (RuntimeType)runtimeType.BaseType;
		}
		while (runtimeType != null);
		return (MethodInfo)RuntimeType.GetMethodBase(reflectedType, methodHandle);
	}

	public override Delegate CreateDelegate(Type delegateType)
	{
		return CreateDelegateInternal(delegateType, null, (DelegateBindingFlags)68);
	}

	public override Delegate CreateDelegate(Type delegateType, object target)
	{
		return CreateDelegateInternal(delegateType, target, DelegateBindingFlags.RelaxedSignature);
	}

	private Delegate CreateDelegateInternal(Type delegateType, object firstArgument, DelegateBindingFlags bindingFlags)
	{
		if (delegateType == null)
		{
			throw new ArgumentNullException("delegateType");
		}
		RuntimeType runtimeType = delegateType as RuntimeType;
		if (runtimeType == null)
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType, "delegateType");
		}
		if (!runtimeType.IsDelegate())
		{
			throw new ArgumentException(SR.Arg_MustBeDelegate, "delegateType");
		}
		Delegate @delegate = Delegate.CreateDelegateInternal(runtimeType, this, firstArgument, bindingFlags);
		if ((object)@delegate == null)
		{
			throw new ArgumentException(SR.Arg_DlgtTargMeth);
		}
		return @delegate;
	}

	[RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
	public override MethodInfo MakeGenericMethod(params Type[] methodInstantiation)
	{
		if (methodInstantiation == null)
		{
			throw new ArgumentNullException("methodInstantiation");
		}
		RuntimeType[] array = new RuntimeType[methodInstantiation.Length];
		if (!IsGenericMethodDefinition)
		{
			throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericMethodDefinition, this));
		}
		for (int i = 0; i < methodInstantiation.Length; i++)
		{
			Type type = methodInstantiation[i];
			if (type == null)
			{
				throw new ArgumentNullException();
			}
			RuntimeType runtimeType = type as RuntimeType;
			if (runtimeType == null)
			{
				Type[] array2 = new Type[methodInstantiation.Length];
				for (int j = 0; j < methodInstantiation.Length; j++)
				{
					array2[j] = methodInstantiation[j];
				}
				methodInstantiation = array2;
				return MethodBuilderInstantiation.MakeGenericMethod(this, methodInstantiation);
			}
			array[i] = runtimeType;
		}
		RuntimeType[] genericArgumentsInternal = GetGenericArgumentsInternal();
		RuntimeType.SanityCheckGenericArguments(array, genericArgumentsInternal);
		MethodInfo methodInfo = null;
		try
		{
			return RuntimeType.GetMethodBase(ReflectedTypeInternal, RuntimeMethodHandle.GetStubIfNeeded(new RuntimeMethodHandleInternal(m_handle), m_declaringType, array)) as MethodInfo;
		}
		catch (VerificationException e)
		{
			RuntimeType.ValidateGenericArguments(this, array, e);
			throw;
		}
	}

	internal RuntimeType[] GetGenericArgumentsInternal()
	{
		return RuntimeMethodHandle.GetMethodInstantiationInternal(this);
	}

	public override Type[] GetGenericArguments()
	{
		return RuntimeMethodHandle.GetMethodInstantiationPublic(this) ?? Type.EmptyTypes;
	}

	public override MethodInfo GetGenericMethodDefinition()
	{
		if (!IsGenericMethod)
		{
			throw new InvalidOperationException();
		}
		return RuntimeType.GetMethodBase(m_declaringType, RuntimeMethodHandle.StripMethodInstantiation(this)) as MethodInfo;
	}

	internal static MethodBase InternalGetCurrentMethod(ref StackCrawlMark stackMark)
	{
		IRuntimeMethodInfo currentMethod = RuntimeMethodHandle.GetCurrentMethod(ref stackMark);
		if (currentMethod == null)
		{
			return null;
		}
		return RuntimeType.GetMethodBase(currentMethod);
	}
}
