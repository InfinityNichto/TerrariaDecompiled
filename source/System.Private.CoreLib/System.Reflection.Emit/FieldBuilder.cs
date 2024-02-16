using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Reflection.Emit;

public sealed class FieldBuilder : FieldInfo
{
	private int m_fieldTok;

	private TypeBuilder m_typeBuilder;

	private string m_fieldName;

	private FieldAttributes m_Attributes;

	private Type m_fieldType;

	public override int MetadataToken => m_fieldTok;

	public override Module Module => m_typeBuilder.Module;

	public override string Name => m_fieldName;

	public override Type? DeclaringType
	{
		get
		{
			if (m_typeBuilder.m_isHiddenGlobalType)
			{
				return null;
			}
			return m_typeBuilder;
		}
	}

	public override Type? ReflectedType
	{
		get
		{
			if (m_typeBuilder.m_isHiddenGlobalType)
			{
				return null;
			}
			return m_typeBuilder;
		}
	}

	public override Type FieldType => m_fieldType;

	public override RuntimeFieldHandle FieldHandle
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_DynamicModule);
		}
	}

	public override FieldAttributes Attributes => m_Attributes;

	internal FieldBuilder(TypeBuilder typeBuilder, string fieldName, Type type, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers, FieldAttributes attributes)
	{
		if (fieldName == null)
		{
			throw new ArgumentNullException("fieldName");
		}
		if (fieldName.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyName, "fieldName");
		}
		if (fieldName[0] == '\0')
		{
			throw new ArgumentException(SR.Argument_IllegalName, "fieldName");
		}
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (type == typeof(void))
		{
			throw new ArgumentException(SR.Argument_BadFieldType);
		}
		m_fieldName = fieldName;
		m_typeBuilder = typeBuilder;
		m_fieldType = type;
		m_Attributes = attributes & ~FieldAttributes.ReservedMask;
		SignatureHelper fieldSigHelper = SignatureHelper.GetFieldSigHelper(m_typeBuilder.Module);
		fieldSigHelper.AddArgument(type, requiredCustomModifiers, optionalCustomModifiers);
		int length;
		byte[] signature = fieldSigHelper.InternalGetSignature(out length);
		ModuleBuilder module = m_typeBuilder.GetModuleBuilder();
		m_fieldTok = TypeBuilder.DefineField(new QCallModule(ref module), typeBuilder.TypeToken, fieldName, signature, length, m_Attributes);
	}

	internal void SetData(byte[] data, int size)
	{
		ModuleBuilder module = m_typeBuilder.GetModuleBuilder();
		ModuleBuilder.SetFieldRVAContent(new QCallModule(ref module), m_fieldTok, data, size);
	}

	public override object? GetValue(object? obj)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicModule);
	}

	public override void SetValue(object? obj, object? val, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
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

	public void SetOffset(int iOffset)
	{
		m_typeBuilder.ThrowIfCreated();
		ModuleBuilder module = m_typeBuilder.GetModuleBuilder();
		TypeBuilder.SetFieldLayoutOffset(new QCallModule(ref module), m_fieldTok, iOffset);
	}

	public void SetConstant(object? defaultValue)
	{
		m_typeBuilder.ThrowIfCreated();
		if (defaultValue == null && m_fieldType.IsValueType && (!m_fieldType.IsGenericType || !(m_fieldType.GetGenericTypeDefinition() == typeof(Nullable<>))))
		{
			throw new ArgumentException(SR.Argument_ConstantNull);
		}
		TypeBuilder.SetConstantValue(m_typeBuilder.GetModuleBuilder(), m_fieldTok, m_fieldType, defaultValue);
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
		ModuleBuilder moduleBuilder = m_typeBuilder.Module as ModuleBuilder;
		m_typeBuilder.ThrowIfCreated();
		TypeBuilder.DefineCustomAttribute(moduleBuilder, m_fieldTok, moduleBuilder.GetConstructorToken(con), binaryAttribute);
	}

	public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
	{
		if (customBuilder == null)
		{
			throw new ArgumentNullException("customBuilder");
		}
		m_typeBuilder.ThrowIfCreated();
		ModuleBuilder mod = m_typeBuilder.Module as ModuleBuilder;
		customBuilder.CreateCustomAttribute(mod, m_fieldTok);
	}
}
