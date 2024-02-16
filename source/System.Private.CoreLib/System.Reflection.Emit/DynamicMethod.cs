using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace System.Reflection.Emit;

public sealed class DynamicMethod : MethodInfo
{
	internal sealed class RTDynamicMethod : MethodInfo
	{
		internal DynamicMethod m_owner;

		private RuntimeParameterInfo[] m_parameters;

		private string m_name;

		private MethodAttributes m_attributes;

		private CallingConventions m_callingConvention;

		public override string Name => m_name;

		public override Type DeclaringType => null;

		public override Type ReflectedType => null;

		public override Module Module => m_owner.m_module;

		public override RuntimeMethodHandle MethodHandle
		{
			get
			{
				throw new InvalidOperationException(SR.InvalidOperation_NotAllowedInDynamicMethod);
			}
		}

		public override MethodAttributes Attributes => m_attributes;

		public override CallingConventions CallingConvention => m_callingConvention;

		public override bool IsSecurityCritical => m_owner.IsSecurityCritical;

		public override bool IsSecuritySafeCritical => m_owner.IsSecuritySafeCritical;

		public override bool IsSecurityTransparent => m_owner.IsSecurityTransparent;

		public override Type ReturnType => m_owner.m_returnType;

		public override ParameterInfo ReturnParameter => new RuntimeParameterInfo(this, null, m_owner.m_returnType, -1);

		public override ICustomAttributeProvider ReturnTypeCustomAttributes => new EmptyCAHolder();

		internal RTDynamicMethod(DynamicMethod owner, string name, MethodAttributes attributes, CallingConventions callingConvention)
		{
			m_owner = owner;
			m_name = name;
			m_attributes = attributes;
			m_callingConvention = callingConvention;
		}

		public override string ToString()
		{
			ValueStringBuilder sbParamList = new ValueStringBuilder(100);
			sbParamList.Append(ReturnType.FormatTypeName());
			sbParamList.Append(' ');
			sbParamList.Append(Name);
			sbParamList.Append('(');
			MethodBase.AppendParameters(ref sbParamList, GetParameterTypes(), CallingConvention);
			sbParamList.Append(')');
			return sbParamList.ToString();
		}

		public override MethodInfo GetBaseDefinition()
		{
			return this;
		}

		public override ParameterInfo[] GetParameters()
		{
			ParameterInfo[] array = LoadParameters();
			ParameterInfo[] array2 = array;
			ParameterInfo[] array3 = new ParameterInfo[array2.Length];
			Array.Copy(array2, array3, array2.Length);
			return array3;
		}

		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return MethodImplAttributes.NoInlining;
		}

		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeMethodInfo, "this");
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			if (attributeType.IsAssignableFrom(typeof(MethodImplAttribute)))
			{
				return new object[1]
				{
					new MethodImplAttribute((MethodImplOptions)GetMethodImplementationFlags())
				};
			}
			return Array.Empty<object>();
		}

		public override object[] GetCustomAttributes(bool inherit)
		{
			return new object[1]
			{
				new MethodImplAttribute((MethodImplOptions)GetMethodImplementationFlags())
			};
		}

		public override bool IsDefined(Type attributeType, bool inherit)
		{
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			if (attributeType.IsAssignableFrom(typeof(MethodImplAttribute)))
			{
				return true;
			}
			return false;
		}

		internal RuntimeParameterInfo[] LoadParameters()
		{
			if (m_parameters == null)
			{
				Type[] parameterTypes = m_owner.m_parameterTypes;
				Type[] array = parameterTypes;
				RuntimeParameterInfo[] array2 = new RuntimeParameterInfo[array.Length];
				for (int i = 0; i < array.Length; i++)
				{
					array2[i] = new RuntimeParameterInfo(this, null, array[i], i);
				}
				if (m_parameters == null)
				{
					m_parameters = array2;
				}
			}
			return m_parameters;
		}
	}

	private RuntimeType[] m_parameterTypes;

	internal IRuntimeMethodInfo m_methodHandle;

	private RuntimeType m_returnType;

	private DynamicILGenerator m_ilGenerator;

	private DynamicILInfo m_DynamicILInfo;

	private bool m_fInitLocals;

	private RuntimeModule m_module;

	internal bool m_skipVisibility;

	internal RuntimeType m_typeOwner;

	private RTDynamicMethod m_dynMethod;

	internal DynamicResolver m_resolver;

	internal bool m_restrictedSkipVisibility;

	private static volatile InternalModuleBuilder s_anonymouslyHostedDynamicMethodsModule;

	private static readonly object s_anonymouslyHostedDynamicMethodsModuleLock = new object();

	public override string Name => m_dynMethod.Name;

	public override Type? DeclaringType => m_dynMethod.DeclaringType;

	public override Type? ReflectedType => m_dynMethod.ReflectedType;

	public override Module Module => m_dynMethod.Module;

	public override RuntimeMethodHandle MethodHandle
	{
		get
		{
			throw new InvalidOperationException(SR.InvalidOperation_NotAllowedInDynamicMethod);
		}
	}

	public override MethodAttributes Attributes => m_dynMethod.Attributes;

	public override CallingConventions CallingConvention => m_dynMethod.CallingConvention;

	public override bool IsSecurityCritical => true;

	public override bool IsSecuritySafeCritical => false;

	public override bool IsSecurityTransparent => false;

	public override Type ReturnType => m_dynMethod.ReturnType;

	public override ParameterInfo ReturnParameter => m_dynMethod.ReturnParameter;

	public override ICustomAttributeProvider ReturnTypeCustomAttributes => m_dynMethod.ReturnTypeCustomAttributes;

	public bool InitLocals
	{
		get
		{
			return m_fInitLocals;
		}
		set
		{
			m_fInitLocals = value;
		}
	}

	public DynamicMethod(string name, Type? returnType, Type[]? parameterTypes)
	{
		Init(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, null, null, skipVisibility: false, transparentMethod: true);
	}

	public DynamicMethod(string name, Type? returnType, Type[]? parameterTypes, bool restrictedSkipVisibility)
	{
		Init(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, null, null, restrictedSkipVisibility, transparentMethod: true);
	}

	public DynamicMethod(string name, Type? returnType, Type[]? parameterTypes, Module m)
	{
		if (m == null)
		{
			throw new ArgumentNullException("m");
		}
		Init(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, null, m, skipVisibility: false, transparentMethod: false);
	}

	public DynamicMethod(string name, Type? returnType, Type[]? parameterTypes, Module m, bool skipVisibility)
	{
		if (m == null)
		{
			throw new ArgumentNullException("m");
		}
		Init(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, null, m, skipVisibility, transparentMethod: false);
	}

	public DynamicMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, Module m, bool skipVisibility)
	{
		if (m == null)
		{
			throw new ArgumentNullException("m");
		}
		Init(name, attributes, callingConvention, returnType, parameterTypes, null, m, skipVisibility, transparentMethod: false);
	}

	public DynamicMethod(string name, Type? returnType, Type[]? parameterTypes, Type owner)
	{
		if (owner == null)
		{
			throw new ArgumentNullException("owner");
		}
		Init(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, owner, null, skipVisibility: false, transparentMethod: false);
	}

	public DynamicMethod(string name, Type? returnType, Type[]? parameterTypes, Type owner, bool skipVisibility)
	{
		if (owner == null)
		{
			throw new ArgumentNullException("owner");
		}
		Init(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, owner, null, skipVisibility, transparentMethod: false);
	}

	public DynamicMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, Type owner, bool skipVisibility)
	{
		if (owner == null)
		{
			throw new ArgumentNullException("owner");
		}
		Init(name, attributes, callingConvention, returnType, parameterTypes, owner, null, skipVisibility, transparentMethod: false);
	}

	private static void CheckConsistency(MethodAttributes attributes, CallingConventions callingConvention)
	{
		if ((attributes & ~MethodAttributes.MemberAccessMask) != MethodAttributes.Static)
		{
			throw new NotSupportedException(SR.NotSupported_DynamicMethodFlags);
		}
		if ((attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public)
		{
			throw new NotSupportedException(SR.NotSupported_DynamicMethodFlags);
		}
		if (callingConvention != CallingConventions.Standard && callingConvention != CallingConventions.VarArgs)
		{
			throw new NotSupportedException(SR.NotSupported_DynamicMethodFlags);
		}
		if (callingConvention == CallingConventions.VarArgs)
		{
			throw new NotSupportedException(SR.NotSupported_DynamicMethodFlags);
		}
	}

	private static RuntimeModule GetDynamicMethodsModule()
	{
		if (s_anonymouslyHostedDynamicMethodsModule != null)
		{
			return s_anonymouslyHostedDynamicMethodsModule;
		}
		lock (s_anonymouslyHostedDynamicMethodsModuleLock)
		{
			if (s_anonymouslyHostedDynamicMethodsModule != null)
			{
				return s_anonymouslyHostedDynamicMethodsModule;
			}
			AssemblyName name = new AssemblyName("Anonymously Hosted DynamicMethods Assembly");
			StackCrawlMark stackMark = StackCrawlMark.LookForMe;
			AssemblyBuilder assemblyBuilder = AssemblyBuilder.InternalDefineDynamicAssembly(name, AssemblyBuilderAccess.Run, ref stackMark, null, null);
			s_anonymouslyHostedDynamicMethodsModule = (InternalModuleBuilder)assemblyBuilder.ManifestModule;
		}
		return s_anonymouslyHostedDynamicMethodsModule;
	}

	[MemberNotNull("m_parameterTypes")]
	[MemberNotNull("m_returnType")]
	[MemberNotNull("m_dynMethod")]
	private void Init(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] signature, Type owner, Module m, bool skipVisibility, bool transparentMethod)
	{
		CheckConsistency(attributes, callingConvention);
		if (signature != null)
		{
			m_parameterTypes = new RuntimeType[signature.Length];
			for (int i = 0; i < signature.Length; i++)
			{
				if (signature[i] == null)
				{
					throw new ArgumentException(SR.Arg_InvalidTypeInSignature);
				}
				m_parameterTypes[i] = signature[i].UnderlyingSystemType as RuntimeType;
				if (m_parameterTypes[i] == null || m_parameterTypes[i] == typeof(void))
				{
					throw new ArgumentException(SR.Arg_InvalidTypeInSignature);
				}
			}
		}
		else
		{
			m_parameterTypes = Array.Empty<RuntimeType>();
		}
		m_returnType = ((returnType == null) ? ((RuntimeType)typeof(void)) : (returnType.UnderlyingSystemType as RuntimeType));
		if (m_returnType == null)
		{
			throw new NotSupportedException(SR.Arg_InvalidTypeInRetType);
		}
		if (transparentMethod)
		{
			m_module = GetDynamicMethodsModule();
			if (skipVisibility)
			{
				m_restrictedSkipVisibility = true;
			}
		}
		else
		{
			if (m != null)
			{
				m_module = m.ModuleHandle.GetRuntimeModule();
			}
			else
			{
				RuntimeType runtimeType = null;
				if (owner != null)
				{
					runtimeType = owner.UnderlyingSystemType as RuntimeType;
				}
				if (runtimeType != null)
				{
					if (runtimeType.HasElementType || runtimeType.ContainsGenericParameters || runtimeType.IsGenericParameter || runtimeType.IsInterface)
					{
						throw new ArgumentException(SR.Argument_InvalidTypeForDynamicMethod);
					}
					m_typeOwner = runtimeType;
					m_module = runtimeType.GetRuntimeModule();
				}
			}
			m_skipVisibility = skipVisibility;
		}
		m_ilGenerator = null;
		m_fInitLocals = true;
		m_methodHandle = null;
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		m_dynMethod = new RTDynamicMethod(this, name, attributes, callingConvention);
	}

	public sealed override Delegate CreateDelegate(Type delegateType)
	{
		if (m_restrictedSkipVisibility)
		{
			GetMethodDescriptor();
			IRuntimeMethodInfo methodHandle = m_methodHandle;
			RuntimeHelpers.CompileMethod(methodHandle?.Value ?? RuntimeMethodHandleInternal.EmptyHandle);
			GC.KeepAlive(methodHandle);
		}
		MulticastDelegate multicastDelegate = (MulticastDelegate)Delegate.CreateDelegateNoSecurityCheck(delegateType, null, GetMethodDescriptor());
		multicastDelegate.StoreDynamicMethod(GetMethodInfo());
		return multicastDelegate;
	}

	public sealed override Delegate CreateDelegate(Type delegateType, object? target)
	{
		if (m_restrictedSkipVisibility)
		{
			GetMethodDescriptor();
			IRuntimeMethodInfo methodHandle = m_methodHandle;
			RuntimeHelpers.CompileMethod(methodHandle?.Value ?? RuntimeMethodHandleInternal.EmptyHandle);
			GC.KeepAlive(methodHandle);
		}
		MulticastDelegate multicastDelegate = (MulticastDelegate)Delegate.CreateDelegateNoSecurityCheck(delegateType, target, GetMethodDescriptor());
		multicastDelegate.StoreDynamicMethod(GetMethodInfo());
		return multicastDelegate;
	}

	internal RuntimeMethodHandle GetMethodDescriptor()
	{
		if (m_methodHandle == null)
		{
			lock (this)
			{
				if (m_methodHandle == null)
				{
					if (m_DynamicILInfo != null)
					{
						m_DynamicILInfo.GetCallableMethod(m_module, this);
					}
					else
					{
						if (m_ilGenerator == null || m_ilGenerator.ILOffset == 0)
						{
							throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadEmptyMethodBody, Name));
						}
						m_ilGenerator.GetCallableMethod(m_module, this);
					}
				}
			}
		}
		return new RuntimeMethodHandle(m_methodHandle);
	}

	public override string ToString()
	{
		return m_dynMethod.ToString();
	}

	public override MethodInfo GetBaseDefinition()
	{
		return this;
	}

	public override ParameterInfo[] GetParameters()
	{
		return m_dynMethod.GetParameters();
	}

	public override MethodImplAttributes GetMethodImplementationFlags()
	{
		return m_dynMethod.GetMethodImplementationFlags();
	}

	public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
	{
		if ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
		{
			throw new NotSupportedException(SR.NotSupported_CallToVarArg);
		}
		GetMethodDescriptor();
		Signature signature = new Signature(m_methodHandle, m_parameterTypes, m_returnType, CallingConvention);
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
		object result = RuntimeMethodHandle.InvokeMethod(null, in arguments, signature, constructor: false, wrapExceptions);
		for (int i = 0; i < arguments.Length; i++)
		{
			parameters[i] = arguments[i];
		}
		GC.KeepAlive(this);
		return result;
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return m_dynMethod.GetCustomAttributes(attributeType, inherit);
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return m_dynMethod.GetCustomAttributes(inherit);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		return m_dynMethod.IsDefined(attributeType, inherit);
	}

	public ParameterBuilder? DefineParameter(int position, ParameterAttributes attributes, string? parameterName)
	{
		if (position < 0 || position > m_parameterTypes.Length)
		{
			throw new ArgumentOutOfRangeException(SR.ArgumentOutOfRange_ParamSequence);
		}
		position--;
		if (position >= 0)
		{
			RuntimeParameterInfo[] array = m_dynMethod.LoadParameters();
			array[position].SetName(parameterName);
			array[position].SetAttributes(attributes);
		}
		return null;
	}

	public DynamicILInfo GetDynamicILInfo()
	{
		if (m_DynamicILInfo == null)
		{
			CallingConventions callingConvention = CallingConvention;
			Type returnType = ReturnType;
			Type[] parameterTypes = m_parameterTypes;
			byte[] signature = SignatureHelper.GetMethodSigHelper(null, callingConvention, returnType, null, null, parameterTypes, null, null).GetSignature(appendEndOfSig: true);
			m_DynamicILInfo = new DynamicILInfo(this, signature);
		}
		return m_DynamicILInfo;
	}

	public ILGenerator GetILGenerator()
	{
		return GetILGenerator(64);
	}

	public ILGenerator GetILGenerator(int streamSize)
	{
		if (m_ilGenerator == null)
		{
			CallingConventions callingConvention = CallingConvention;
			Type returnType = ReturnType;
			Type[] parameterTypes = m_parameterTypes;
			byte[] signature = SignatureHelper.GetMethodSigHelper(null, callingConvention, returnType, null, null, parameterTypes, null, null).GetSignature(appendEndOfSig: true);
			m_ilGenerator = new DynamicILGenerator(this, signature, streamSize);
		}
		return m_ilGenerator;
	}

	internal MethodInfo GetMethodInfo()
	{
		return m_dynMethod;
	}
}
