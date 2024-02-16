using System.Globalization;
using System.Reflection;

namespace System.Runtime.Serialization;

internal sealed class SerializationFieldInfo : FieldInfo
{
	private readonly FieldInfo m_field;

	private readonly string m_serializationName;

	internal FieldInfo FieldInfo => m_field;

	public override string Name => m_serializationName;

	public override Module Module => m_field.Module;

	public override int MetadataToken => m_field.MetadataToken;

	public override Type DeclaringType => m_field.DeclaringType;

	public override Type ReflectedType => m_field.ReflectedType;

	public override Type FieldType => m_field.FieldType;

	public override RuntimeFieldHandle FieldHandle => m_field.FieldHandle;

	public override FieldAttributes Attributes => m_field.Attributes;

	internal SerializationFieldInfo(FieldInfo field, string namePrefix)
	{
		m_field = field;
		m_serializationName = namePrefix + "+" + m_field.Name;
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

	public override object GetValue(object obj)
	{
		return m_field.GetValue(obj);
	}

	public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
	{
		m_field.SetValue(obj, value, invokeAttr, binder, culture);
	}
}
