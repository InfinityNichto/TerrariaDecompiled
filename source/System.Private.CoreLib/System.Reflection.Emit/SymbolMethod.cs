using System.Globalization;

namespace System.Reflection.Emit;

internal sealed class SymbolMethod : MethodInfo
{
	private ModuleBuilder m_module;

	private Type m_containingType;

	private string m_name;

	private CallingConventions m_callingConvention;

	private Type m_returnType;

	private int m_token;

	private Type[] m_parameterTypes;

	public override int MetadataToken => m_token;

	public override Module Module => m_module;

	public override Type ReflectedType => m_containingType;

	public override string Name => m_name;

	public override Type DeclaringType => m_containingType;

	public override MethodAttributes Attributes
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_SymbolMethod);
		}
	}

	public override CallingConventions CallingConvention => m_callingConvention;

	public override RuntimeMethodHandle MethodHandle
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_SymbolMethod);
		}
	}

	public override Type ReturnType => m_returnType;

	public override ICustomAttributeProvider ReturnTypeCustomAttributes => new EmptyCAHolder();

	internal SymbolMethod(ModuleBuilder mod, int token, Type arrayClass, string methodName, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
	{
		m_token = token;
		m_returnType = returnType ?? typeof(void);
		if (parameterTypes != null)
		{
			m_parameterTypes = new Type[parameterTypes.Length];
			Array.Copy(parameterTypes, m_parameterTypes, parameterTypes.Length);
		}
		else
		{
			m_parameterTypes = Type.EmptyTypes;
		}
		m_module = mod;
		m_containingType = arrayClass;
		m_name = methodName;
		m_callingConvention = callingConvention;
		SignatureHelper.GetMethodSigHelper(mod, callingConvention, returnType, null, null, parameterTypes, null, null);
	}

	internal override Type[] GetParameterTypes()
	{
		return m_parameterTypes;
	}

	internal int GetToken(ModuleBuilder mod)
	{
		return mod.GetArrayMethodToken(m_containingType, m_name, m_callingConvention, m_returnType, m_parameterTypes);
	}

	public override ParameterInfo[] GetParameters()
	{
		throw new NotSupportedException(SR.NotSupported_SymbolMethod);
	}

	public override MethodImplAttributes GetMethodImplementationFlags()
	{
		throw new NotSupportedException(SR.NotSupported_SymbolMethod);
	}

	public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
	{
		throw new NotSupportedException(SR.NotSupported_SymbolMethod);
	}

	public override MethodInfo GetBaseDefinition()
	{
		return this;
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		throw new NotSupportedException(SR.NotSupported_SymbolMethod);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		throw new NotSupportedException(SR.NotSupported_SymbolMethod);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		throw new NotSupportedException(SR.NotSupported_SymbolMethod);
	}

	public Module GetModule()
	{
		return m_module;
	}
}
