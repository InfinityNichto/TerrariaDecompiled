using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaRedefine : XmlSchemaExternal
{
	private readonly XmlSchemaObjectCollection _items = new XmlSchemaObjectCollection();

	private readonly XmlSchemaObjectTable _attributeGroups = new XmlSchemaObjectTable();

	private readonly XmlSchemaObjectTable _types = new XmlSchemaObjectTable();

	private readonly XmlSchemaObjectTable _groups = new XmlSchemaObjectTable();

	[XmlElement("annotation", typeof(XmlSchemaAnnotation))]
	[XmlElement("attributeGroup", typeof(XmlSchemaAttributeGroup))]
	[XmlElement("complexType", typeof(XmlSchemaComplexType))]
	[XmlElement("group", typeof(XmlSchemaGroup))]
	[XmlElement("simpleType", typeof(XmlSchemaSimpleType))]
	public XmlSchemaObjectCollection Items => _items;

	[XmlIgnore]
	public XmlSchemaObjectTable AttributeGroups => _attributeGroups;

	[XmlIgnore]
	public XmlSchemaObjectTable SchemaTypes => _types;

	[XmlIgnore]
	public XmlSchemaObjectTable Groups => _groups;

	public XmlSchemaRedefine()
	{
		base.Compositor = Compositor.Redefine;
	}

	internal override void AddAnnotation(XmlSchemaAnnotation annotation)
	{
		_items.Add(annotation);
	}
}
