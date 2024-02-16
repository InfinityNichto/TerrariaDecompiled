using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Reflection.Emit;

public sealed class PropertyBuilder : PropertyInfo
{
	private string m_name;

	private int m_tkProperty;

	private ModuleBuilder m_moduleBuilder;

	private PropertyAttributes m_attributes;

	private Type m_returnType;

	private MethodInfo m_getMethod;

	private MethodInfo m_setMethod;

	private TypeBuilder m_containingType;

	public override Module Module => m_containingType.Module;

	public override Type PropertyType => m_returnType;

	public override PropertyAttributes Attributes => m_attributes;

	public override bool CanRead
	{
		get
		{
			if (m_getMethod != null)
			{
				return true;
			}
			return false;
		}
	}

	public override bool CanWrite
	{
		get
		{
			if (m_setMethod != null)
			{
				return true;
			}
			return false;
		}
	}

	public override string Name => m_name;

	public override Type? DeclaringType => m_containingType;

	public override Type? ReflectedType => m_containingType;

	internal PropertyBuilder(ModuleBuilder mod, string name, SignatureHelper sig, PropertyAttributes attr, Type returnType, int prToken, TypeBuilder containingType)
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
		m_name = name;
		m_moduleBuilder = mod;
		m_attributes = attr;
		m_returnType = returnType;
		m_tkProperty = prToken;
		m_containingType = containingType;
	}

	public void SetConstant(object? defaultValue)
	{
		m_containingType.ThrowIfCreated();
		TypeBuilder.SetConstantValue(m_moduleBuilder, m_tkProperty, m_returnType, defaultValue);
	}

	private void SetMethodSemantics(MethodBuilder mdBuilder, MethodSemanticsAttributes semantics)
	{
		if (mdBuilder == null)
		{
			throw new ArgumentNullException("mdBuilder");
		}
		m_containingType.ThrowIfCreated();
		ModuleBuilder module = m_moduleBuilder;
		TypeBuilder.DefineMethodSemantics(new QCallModule(ref module), m_tkProperty, semantics, mdBuilder.MetadataToken);
	}

	public void SetGetMethod(MethodBuilder mdBuilder)
	{
		SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Getter);
		m_getMethod = mdBuilder;
	}

	public void SetSetMethod(MethodBuilder mdBuilder)
	{
		SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Setter);
		m_setMethod = mdBuilder;
	}

	public void AddOtherMethod(MethodBuilder mdBuilder)
	{
		SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Other);
	}

	public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
	{
		if (con == null)
		{
			throw new ArgumentNullException("con");
		}
		if (binaryAttribute == null)
		{
			throw new ArgumentNullException("binaryAttribute");
		}
		m_containingType.ThrowIfCreated();
		TypeBuilder.DefineCustomAttribute(m_moduleBuilder, m_tkProperty, m_moduleBuilder.GetConstructorToken(con), binaryAttribute);
	}

	public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
	{
		if (customBuilder == null)
		{
			throw new ArgumentNullException("customBuilder");
		}
		m_containingType.ThrowIfCreated();
		customBuilder.CreateCustomAttribute(m_moduleBuilder, m_tkProperty);
	}

	public override object GetValue(object? obj, object?[]? index)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override object GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override void SetValue(object? obj, object? value, object?[]? index)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override MethodInfo[] GetAccessors(bool nonPublic)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override MethodInfo? GetGetMethod(bool nonPublic)
	{
		if (nonPublic || m_getMethod == null)
		{
			return m_getMethod;
		}
		if ((m_getMethod.Attributes & MethodAttributes.Public) == MethodAttributes.Public)
		{
			return m_getMethod;
		}
		return null;
	}

	public override MethodInfo? GetSetMethod(bool nonPublic)
	{
		if (nonPublic || m_setMethod == null)
		{
			return m_setMethod;
		}
		if ((m_setMethod.Attributes & MethodAttributes.Public) == MethodAttributes.Public)
		{
			return m_setMethod;
		}
		return null;
	}

	public override ParameterInfo[] GetIndexParameters()
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
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
}
