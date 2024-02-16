using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaSimpleContentExtension : XmlSchemaContent
{
	private XmlSchemaObjectCollection _attributes = new XmlSchemaObjectCollection();

	private XmlSchemaAnyAttribute _anyAttribute;

	private XmlQualifiedName _baseTypeName = XmlQualifiedName.Empty;

	[XmlAttribute("base")]
	public XmlQualifiedName BaseTypeName
	{
		get
		{
			return _baseTypeName;
		}
		set
		{
			_baseTypeName = ((value == null) ? XmlQualifiedName.Empty : value);
		}
	}

	[XmlElement("attribute", typeof(XmlSchemaAttribute))]
	[XmlElement("attributeGroup", typeof(XmlSchemaAttributeGroupRef))]
	public XmlSchemaObjectCollection Attributes => _attributes;

	[XmlElement("anyAttribute")]
	public XmlSchemaAnyAttribute? AnyAttribute
	{
		get
		{
			return _anyAttribute;
		}
		set
		{
			_anyAttribute = value;
		}
	}

	internal void SetAttributes(XmlSchemaObjectCollection newAttributes)
	{
		_attributes = newAttributes;
	}
}
