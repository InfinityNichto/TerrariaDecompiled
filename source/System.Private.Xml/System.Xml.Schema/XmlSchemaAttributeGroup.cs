using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaAttributeGroup : XmlSchemaAnnotated
{
	private string _name;

	private XmlSchemaObjectCollection _attributes = new XmlSchemaObjectCollection();

	private XmlSchemaAnyAttribute _anyAttribute;

	private XmlQualifiedName _qname = XmlQualifiedName.Empty;

	private XmlSchemaAttributeGroup _redefined;

	private XmlSchemaObjectTable _attributeUses;

	private XmlSchemaAnyAttribute _attributeWildcard;

	private int _selfReferenceCount;

	[XmlAttribute("name")]
	public string? Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
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

	[XmlIgnore]
	public XmlQualifiedName QualifiedName => _qname;

	[XmlIgnore]
	internal XmlSchemaObjectTable AttributeUses
	{
		get
		{
			if (_attributeUses == null)
			{
				_attributeUses = new XmlSchemaObjectTable();
			}
			return _attributeUses;
		}
	}

	[XmlIgnore]
	internal XmlSchemaAnyAttribute? AttributeWildcard
	{
		get
		{
			return _attributeWildcard;
		}
		set
		{
			_attributeWildcard = value;
		}
	}

	[XmlIgnore]
	public XmlSchemaAttributeGroup? RedefinedAttributeGroup => _redefined;

	[XmlIgnore]
	internal XmlSchemaAttributeGroup? Redefined
	{
		get
		{
			return _redefined;
		}
		set
		{
			_redefined = value;
		}
	}

	[XmlIgnore]
	internal int SelfReferenceCount
	{
		get
		{
			return _selfReferenceCount;
		}
		set
		{
			_selfReferenceCount = value;
		}
	}

	[XmlIgnore]
	internal override string? NameAttribute
	{
		get
		{
			return Name;
		}
		set
		{
			Name = value;
		}
	}

	internal void SetQualifiedName(XmlQualifiedName value)
	{
		_qname = value;
	}

	internal override XmlSchemaObject Clone()
	{
		XmlSchemaAttributeGroup xmlSchemaAttributeGroup = (XmlSchemaAttributeGroup)MemberwiseClone();
		if (XmlSchemaComplexType.HasAttributeQNameRef(_attributes))
		{
			xmlSchemaAttributeGroup._attributes = XmlSchemaComplexType.CloneAttributes(_attributes);
			xmlSchemaAttributeGroup._attributeUses = null;
		}
		return xmlSchemaAttributeGroup;
	}
}
