using System.Globalization;

namespace System.Reflection.Emit;

internal sealed class FieldOnTypeBuilderInstantiation : FieldInfo
{
	private FieldInfo m_field;

	private TypeBuilderInstantiation m_type;

	internal FieldInfo FieldInfo => m_field;

	public override MemberTypes MemberType => MemberTypes.Field;

	public override string Name => m_field.Name;

	public override Type DeclaringType => m_type;

	public override Type ReflectedType => m_type;

	public override int MetadataToken
	{
		get
		{
			FieldBuilder fieldBuilder = m_field as FieldBuilder;
			if (fieldBuilder != null)
			{
				return fieldBuilder.MetadataToken;
			}
			return m_field.MetadataToken;
		}
	}

	public override Module Module => m_field.Module;

	public override RuntimeFieldHandle FieldHandle
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override Type FieldType
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override FieldAttributes Attributes => m_field.Attributes;

	internal static FieldInfo GetField(FieldInfo Field, TypeBuilderInstantiation type)
	{
		FieldInfo fieldInfo;
		if (type.m_hashtable.Contains(Field))
		{
			fieldInfo = type.m_hashtable[Field] as FieldInfo;
		}
		else
		{
			fieldInfo = new FieldOnTypeBuilderInstantiation(Field, type);
			type.m_hashtable[Field] = fieldInfo;
		}
		return fieldInfo;
	}

	internal FieldOnTypeBuilderInstantiation(FieldInfo field, TypeBuilderInstantiation type)
	{
		m_field = field;
		m_type = type;
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return m_field.GetCustomAttributes(inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return m_field.GetCustomAttributes(attributeType, inherit);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		return m_field.IsDefined(attributeType, inherit);
	}

	public override Type[] GetRequiredCustomModifiers()
	{
		return m_field.GetRequiredCustomModifiers();
	}

	public override Type[] GetOptionalCustomModifiers()
	{
		return m_field.GetOptionalCustomModifiers();
	}

	public override void SetValueDirect(TypedReference obj, object value)
	{
		throw new NotImplementedException();
	}

	public override object GetValueDirect(TypedReference obj)
	{
		throw new NotImplementedException();
	}

	public override object GetValue(object obj)
	{
		throw new InvalidOperationException();
	}

	public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
	{
		throw new InvalidOperationException();
	}
}
