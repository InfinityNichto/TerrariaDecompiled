using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Reflection.Emit;

public sealed class MethodBuilder : MethodInfo
{
	internal string m_strName;

	private int m_token;

	private readonly ModuleBuilder m_module;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	internal TypeBuilder m_containingType;

	private int[] m_mdMethodFixups;

	private byte[] m_localSignature;

	internal LocalSymInfo m_localSymInfo;

	internal ILGenerator m_ilGenerator;

	private byte[] m_ubBody;

	private ExceptionHandler[] m_exceptions;

	internal bool m_bIsBaked;

	private bool m_fInitLocals;

	private readonly MethodAttributes m_iAttributes;

	private readonly CallingConventions m_callingConvention;

	private MethodImplAttributes m_dwMethodImplFlags;

	private SignatureHelper m_signature;

	internal Type[] m_parameterTypes;

	private Type m_returnType;

	private Type[] m_returnTypeRequiredCustomModifiers;

	private Type[] m_returnTypeOptionalCustomModifiers;

	private Type[][] m_parameterTypeRequiredCustomModifiers;

	private Type[][] m_parameterTypeOptionalCustomModifiers;

	private GenericTypeParameterBuilder[] m_inst;

	private bool m_bIsGenMethDef;

	internal bool m_canBeRuntimeImpl;

	internal bool m_isDllImport;

	internal int ExceptionHandlerCount
	{
		get
		{
			if (m_exceptions == null)
			{
				return 0;
			}
			return m_exceptions.Length;
		}
	}

	public override string Name => m_strName;

	public override int MetadataToken => GetToken();

	public override Module Module => m_containingType.Module;

	public override Type? DeclaringType
	{
		get
		{
			if (m_containingType.m_isHiddenGlobalType)
			{
				return null;
			}
			return m_containingType;
		}
	}

	public override ICustomAttributeProvider ReturnTypeCustomAttributes => new EmptyCAHolder();

	public override Type? ReflectedType => DeclaringType;

	public override MethodAttributes Attributes => m_iAttributes;

	public override CallingConventions CallingConvention => m_callingConvention;

	public override RuntimeMethodHandle MethodHandle
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_DynamicModule);
		}
	}

	public override bool IsSecurityCritical => true;

	public override bool IsSecuritySafeCritical => false;

	public override bool IsSecurityTransparent => false;

	public override Type ReturnType => m_returnType;

	public override ParameterInfo ReturnParameter
	{
		get
		{
			if (!m_bIsBaked || m_containingType == null || m_containingType.BakedRuntimeType == null)
			{
				throw new InvalidOperationException(SR.InvalidOperation_TypeNotCreated);
			}
			MethodInfo method = m_containingType.GetMethod(m_strName, m_parameterTypes);
			return method.ReturnParameter;
		}
	}

	public override bool IsGenericMethodDefinition => m_bIsGenMethDef;

	public override bool ContainsGenericParameters
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public override bool IsGenericMethod => m_inst != null;

	public bool InitLocals
	{
		get
		{
			ThrowIfGeneric();
			return m_fInitLocals;
		}
		set
		{
			ThrowIfGeneric();
			m_fInitLocals = value;
		}
	}

	internal MethodBuilder(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers, ModuleBuilder mod, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TypeBuilder type)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyName, "name");
		}
		if (name[0] == '\0')
		{
			throw new ArgumentException(SR.Argument_IllegalName, "name");
		}
		if (mod == null)
		{
			throw new ArgumentNullException("mod");
		}
		if (parameterTypes != null)
		{
			foreach (Type type2 in parameterTypes)
			{
				if (type2 == null)
				{
					throw new ArgumentNullException("parameterTypes");
				}
			}
		}
		m_strName = name;
		m_module = mod;
		m_containingType = type;
		m_returnType = returnType ?? typeof(void);
		if ((attributes & MethodAttributes.Static) == 0)
		{
			callingConvention |= CallingConventions.HasThis;
		}
		else if ((attributes & MethodAttributes.Virtual) != 0 && (attributes & MethodAttributes.Abstract) == 0)
		{
			throw new ArgumentException(SR.Arg_NoStaticVirtual);
		}
		m_callingConvention = callingConvention;
		if (parameterTypes != null)
		{
			m_parameterTypes = new Type[parameterTypes.Length];
			Array.Copy(parameterTypes, m_parameterTypes, parameterTypes.Length);
		}
		else
		{
			m_parameterTypes = null;
		}
		m_returnTypeRequiredCustomModifiers = returnTypeRequiredCustomModifiers;
		m_returnTypeOptionalCustomModifiers = returnTypeOptionalCustomModifiers;
		m_parameterTypeRequiredCustomModifiers = parameterTypeRequiredCustomModifiers;
		m_parameterTypeOptionalCustomModifiers = parameterTypeOptionalCustomModifiers;
		m_iAttributes = attributes;
		m_bIsBaked = false;
		m_fInitLocals = true;
		m_localSymInfo = new LocalSymInfo();
		m_ubBody = null;
		m_ilGenerator = null;
		m_dwMethodImplFlags = MethodImplAttributes.IL;
	}

	internal void CreateMethodBodyHelper(ILGenerator il)
	{
		if (il == null)
		{
			throw new ArgumentNullException("il");
		}
		int num = 0;
		ModuleBuilder module = m_module;
		m_containingType.ThrowIfCreated();
		if (m_bIsBaked)
		{
			throw new InvalidOperationException(SR.InvalidOperation_MethodHasBody);
		}
		if (il.m_methodBuilder != this && il.m_methodBuilder != null)
		{
			throw new InvalidOperationException(SR.InvalidOperation_BadILGeneratorUsage);
		}
		ThrowIfShouldNotHaveBody();
		if (il.m_ScopeTree.m_iOpenScopeCount != 0)
		{
			throw new InvalidOperationException(SR.InvalidOperation_OpenLocalVariableScope);
		}
		m_ubBody = il.BakeByteArray();
		m_mdMethodFixups = il.GetTokenFixups();
		__ExceptionInfo[] exceptions = il.GetExceptions();
		int num2 = CalculateNumberOfExceptions(exceptions);
		if (num2 > 0)
		{
			m_exceptions = new ExceptionHandler[num2];
			for (int i = 0; i < exceptions.Length; i++)
			{
				int[] filterAddresses = exceptions[i].GetFilterAddresses();
				int[] catchAddresses = exceptions[i].GetCatchAddresses();
				int[] catchEndAddresses = exceptions[i].GetCatchEndAddresses();
				Type[] catchClass = exceptions[i].GetCatchClass();
				int numberOfCatches = exceptions[i].GetNumberOfCatches();
				int startAddress = exceptions[i].GetStartAddress();
				int endAddress = exceptions[i].GetEndAddress();
				int[] exceptionTypes = exceptions[i].GetExceptionTypes();
				for (int j = 0; j < numberOfCatches; j++)
				{
					int exceptionTypeToken = 0;
					if (catchClass[j] != null)
					{
						exceptionTypeToken = module.GetTypeTokenInternal(catchClass[j]);
					}
					switch (exceptionTypes[j])
					{
					case 0:
					case 1:
					case 4:
						m_exceptions[num++] = new ExceptionHandler(startAddress, endAddress, filterAddresses[j], catchAddresses[j], catchEndAddresses[j], exceptionTypes[j], exceptionTypeToken);
						break;
					case 2:
						m_exceptions[num++] = new ExceptionHandler(startAddress, exceptions[i].GetFinallyEndAddress(), filterAddresses[j], catchAddresses[j], catchEndAddresses[j], exceptionTypes[j], exceptionTypeToken);
						break;
					}
				}
			}
		}
		m_bIsBaked = true;
	}

	internal void ReleaseBakedStructures()
	{
		if (m_bIsBaked)
		{
			m_ubBody = null;
			m_localSymInfo = null;
			m_mdMethodFixups = null;
			m_localSignature = null;
			m_exceptions = null;
		}
	}

	internal override Type[] GetParameterTypes()
	{
		return m_parameterTypes ?? (m_parameterTypes = Type.EmptyTypes);
	}

	internal static Type GetMethodBaseReturnType(MethodBase method)
	{
		if (method is MethodInfo methodInfo)
		{
			return methodInfo.ReturnType;
		}
		if (method is ConstructorInfo constructorInfo)
		{
			return constructorInfo.GetReturnType();
		}
		return null;
	}

	internal void SetToken(int token)
	{
		m_token = token;
	}

	internal byte[] GetBody()
	{
		return m_ubBody;
	}

	internal int[] GetTokenFixups()
	{
		return m_mdMethodFixups;
	}

	internal SignatureHelper GetMethodSignature()
	{
		if (m_parameterTypes == null)
		{
			m_parameterTypes = Type.EmptyTypes;
		}
		m_signature = SignatureHelper.GetMethodSigHelper(m_module, m_callingConvention, (m_inst != null) ? m_inst.Length : 0, m_returnType, m_returnTypeRequiredCustomModifiers, m_returnTypeOptionalCustomModifiers, m_parameterTypes, m_parameterTypeRequiredCustomModifiers, m_parameterTypeOptionalCustomModifiers);
		return m_signature;
	}

	internal byte[] GetLocalSignature(out int signatureLength)
	{
		if (m_localSignature != null)
		{
			signatureLength = m_localSignature.Length;
			return m_localSignature;
		}
		if (m_ilGenerator != null && m_ilGenerator.m_localCount != 0)
		{
			return m_ilGenerator.m_localSignature.InternalGetSignature(out signatureLength);
		}
		return SignatureHelper.GetLocalVarSigHelper(m_module).InternalGetSignature(out signatureLength);
	}

	internal int GetMaxStack()
	{
		if (m_ilGenerator != null)
		{
			return m_ilGenerator.GetMaxStackSize() + ExceptionHandlerCount;
		}
		return 16;
	}

	internal ExceptionHandler[] GetExceptionHandlers()
	{
		return m_exceptions;
	}

	internal static int CalculateNumberOfExceptions(__ExceptionInfo[] excp)
	{
		int num = 0;
		if (excp == null)
		{
			return 0;
		}
		for (int i = 0; i < excp.Length; i++)
		{
			num += excp[i].GetNumberOfCatches();
		}
		return num;
	}

	internal bool IsTypeCreated()
	{
		if (m_containingType != null)
		{
			return m_containingType.IsCreated();
		}
		return false;
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	internal TypeBuilder GetTypeBuilder()
	{
		return m_containingType;
	}

	internal ModuleBuilder GetModuleBuilder()
	{
		return m_module;
	}

	public override bool Equals(object? obj)
	{
		if (!(obj is MethodBuilder))
		{
			return false;
		}
		if (!m_strName.Equals(((MethodBuilder)obj).m_strName))
		{
			return false;
		}
		if (m_iAttributes != ((MethodBuilder)obj).m_iAttributes)
		{
			return false;
		}
		SignatureHelper methodSignature = ((MethodBuilder)obj).GetMethodSignature();
		if (methodSignature.Equals(GetMethodSignature()))
		{
			return true;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return m_strName.GetHashCode();
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder(1000);
		stringBuilder.Append("Name: ").Append(m_strName).AppendLine(" ");
		stringBuilder.Append("Attributes: ").Append((int)m_iAttributes).AppendLine();
		stringBuilder.Append("Method Signature: ").Append(GetMethodSignature()).AppendLine();
		stringBuilder.AppendLine();
		return stringBuilder.ToString();
	}

	public override object Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override MethodImplAttributes GetMethodImplementationFlags()
	{
		return m_dwMethodImplFlags;
	}

	public override MethodInfo GetBaseDefinition()
	{
		return this;
	}

	public override ParameterInfo[] GetParameters()
	{
		if (!m_bIsBaked || m_containingType == null || m_containingType.BakedRuntimeType == null)
		{
			throw new NotSupportedException(SR.InvalidOperation_TypeNotCreated);
		}
		MethodInfo method = m_containingType.GetMethod(m_strName, m_parameterTypes);
		return method.GetParameters();
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override MethodInfo GetGenericMethodDefinition()
	{
		if (!IsGenericMethod)
		{
			throw new InvalidOperationException();
		}
		return this;
	}

	public override Type[] GetGenericArguments()
	{
		Type[] inst = m_inst;
		return inst ?? Type.EmptyTypes;
	}

	[RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
	public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
	{
		return MethodBuilderInstantiation.MakeGenericMethod(this, typeArguments);
	}

	public GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names)
	{
		if (names == null)
		{
			throw new ArgumentNullException("names");
		}
		if (names.Length == 0)
		{
			throw new ArgumentException(SR.Arg_EmptyArray, "names");
		}
		if (m_inst != null)
		{
			throw new InvalidOperationException(SR.InvalidOperation_GenericParametersAlreadySet);
		}
		for (int i = 0; i < names.Length; i++)
		{
			if (names[i] == null)
			{
				throw new ArgumentNullException("names");
			}
		}
		if (m_token != 0)
		{
			throw new InvalidOperationException(SR.InvalidOperation_MethodBuilderBaked);
		}
		m_bIsGenMethDef = true;
		m_inst = new GenericTypeParameterBuilder[names.Length];
		for (int j = 0; j < names.Length; j++)
		{
			m_inst[j] = new GenericTypeParameterBuilder(new TypeBuilder(names[j], j, this));
		}
		return m_inst;
	}

	internal void ThrowIfGeneric()
	{
		if (IsGenericMethod && !IsGenericMethodDefinition)
		{
			throw new InvalidOperationException();
		}
	}

	private int GetToken()
	{
		if (m_token != 0)
		{
			return m_token;
		}
		MethodBuilder methodBuilder = null;
		int result = 0;
		lock (m_containingType.m_listMethods)
		{
			if (m_token != 0)
			{
				return m_token;
			}
			int i;
			for (i = m_containingType.m_lastTokenizedMethod + 1; i < m_containingType.m_listMethods.Count; i++)
			{
				methodBuilder = m_containingType.m_listMethods[i];
				result = methodBuilder.GetTokenNoLock();
				if (methodBuilder == this)
				{
					break;
				}
			}
			m_containingType.m_lastTokenizedMethod = i;
			return result;
		}
	}

	private int GetTokenNoLock()
	{
		int length;
		byte[] signature = GetMethodSignature().InternalGetSignature(out length);
		ModuleBuilder module = m_module;
		int tkMethod = (m_token = TypeBuilder.DefineMethod(new QCallModule(ref module), m_containingType.MetadataToken, m_strName, signature, length, Attributes));
		if (m_inst != null)
		{
			GenericTypeParameterBuilder[] inst = m_inst;
			foreach (GenericTypeParameterBuilder genericTypeParameterBuilder in inst)
			{
				if (!genericTypeParameterBuilder.m_type.IsCreated())
				{
					genericTypeParameterBuilder.m_type.CreateType();
				}
			}
		}
		TypeBuilder.SetMethodImpl(new QCallModule(ref module), tkMethod, m_dwMethodImplFlags);
		return m_token;
	}

	public void SetParameters(params Type[] parameterTypes)
	{
		AssemblyBuilder.CheckContext(parameterTypes);
		SetSignature(null, null, null, parameterTypes, null, null);
	}

	public void SetReturnType(Type? returnType)
	{
		AssemblyBuilder.CheckContext(returnType);
		SetSignature(returnType, null, null, null, null, null);
	}

	public void SetSignature(Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
	{
		if (m_token == 0)
		{
			AssemblyBuilder.CheckContext(returnType);
			AssemblyBuilder.CheckContext(returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes);
			AssemblyBuilder.CheckContext(parameterTypeRequiredCustomModifiers);
			AssemblyBuilder.CheckContext(parameterTypeOptionalCustomModifiers);
			ThrowIfGeneric();
			if (returnType != null)
			{
				m_returnType = returnType;
			}
			if (parameterTypes != null)
			{
				m_parameterTypes = new Type[parameterTypes.Length];
				Array.Copy(parameterTypes, m_parameterTypes, parameterTypes.Length);
			}
			m_returnTypeRequiredCustomModifiers = returnTypeRequiredCustomModifiers;
			m_returnTypeOptionalCustomModifiers = returnTypeOptionalCustomModifiers;
			m_parameterTypeRequiredCustomModifiers = parameterTypeRequiredCustomModifiers;
			m_parameterTypeOptionalCustomModifiers = parameterTypeOptionalCustomModifiers;
		}
	}

	public ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, string? strParamName)
	{
		if (position < 0)
		{
			throw new ArgumentOutOfRangeException(SR.ArgumentOutOfRange_ParamSequence);
		}
		ThrowIfGeneric();
		m_containingType.ThrowIfCreated();
		if (position > 0 && (m_parameterTypes == null || position > m_parameterTypes.Length))
		{
			throw new ArgumentOutOfRangeException(SR.ArgumentOutOfRange_ParamSequence);
		}
		attributes &= ~ParameterAttributes.ReservedMask;
		return new ParameterBuilder(this, position, attributes, strParamName);
	}

	public void SetImplementationFlags(MethodImplAttributes attributes)
	{
		ThrowIfGeneric();
		m_containingType.ThrowIfCreated();
		m_dwMethodImplFlags = attributes;
		m_canBeRuntimeImpl = true;
		ModuleBuilder module = m_module;
		TypeBuilder.SetMethodImpl(new QCallModule(ref module), MetadataToken, attributes);
	}

	public ILGenerator GetILGenerator()
	{
		ThrowIfGeneric();
		ThrowIfShouldNotHaveBody();
		return m_ilGenerator ?? (m_ilGenerator = new ILGenerator(this));
	}

	public ILGenerator GetILGenerator(int size)
	{
		ThrowIfGeneric();
		ThrowIfShouldNotHaveBody();
		return m_ilGenerator ?? (m_ilGenerator = new ILGenerator(this, size));
	}

	private void ThrowIfShouldNotHaveBody()
	{
		if ((m_dwMethodImplFlags & MethodImplAttributes.CodeTypeMask) != 0 || (m_dwMethodImplFlags & MethodImplAttributes.ManagedMask) != 0 || (m_iAttributes & MethodAttributes.PinvokeImpl) != 0 || m_isDllImport)
		{
			throw new InvalidOperationException(SR.InvalidOperation_ShouldNotHaveMethodBody);
		}
	}

	internal Module GetModule()
	{
		return GetModuleBuilder();
	}

	public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
	{
		if ((object)con == null)
		{
			throw new ArgumentNullException("con");
		}
		if (binaryAttribute == null)
		{
			throw new ArgumentNullException("binaryAttribute");
		}
		ThrowIfGeneric();
		TypeBuilder.DefineCustomAttribute(m_module, MetadataToken, m_module.GetConstructorToken(con), binaryAttribute);
		if (IsKnownCA(con))
		{
			ParseCA(con);
		}
	}

	public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
	{
		if (customBuilder == null)
		{
			throw new ArgumentNullException("customBuilder");
		}
		ThrowIfGeneric();
		customBuilder.CreateCustomAttribute(m_module, MetadataToken);
		if (IsKnownCA(customBuilder.m_con))
		{
			ParseCA(customBuilder.m_con);
		}
	}

	private static bool IsKnownCA(ConstructorInfo con)
	{
		Type declaringType = con.DeclaringType;
		if (!(declaringType == typeof(MethodImplAttribute)))
		{
			return declaringType == typeof(DllImportAttribute);
		}
		return true;
	}

	private void ParseCA(ConstructorInfo con)
	{
		Type declaringType = con.DeclaringType;
		if (declaringType == typeof(MethodImplAttribute))
		{
			m_canBeRuntimeImpl = true;
		}
		else if (declaringType == typeof(DllImportAttribute))
		{
			m_canBeRuntimeImpl = true;
			m_isDllImport = true;
		}
	}
}
