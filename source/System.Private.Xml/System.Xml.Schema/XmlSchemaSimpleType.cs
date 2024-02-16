using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaSimpleType : XmlSchemaType
{
	private XmlSchemaSimpleTypeContent _content;

	[XmlElement("restriction", typeof(XmlSchemaSimpleTypeRestriction))]
	[XmlElement("list", typeof(XmlSchemaSimpleTypeList))]
	[XmlElement("union", typeof(XmlSchemaSimpleTypeUnion))]
	public XmlSchemaSimpleTypeContent? Content
	{
		get
		{
			return _content;
		}
		set
		{
			_content = value;
		}
	}

	internal override XmlQualifiedName DerivedFrom
	{
		get
		{
			if (_content == null)
			{
				return XmlQualifiedName.Empty;
			}
			if (_content is XmlSchemaSimpleTypeRestriction)
			{
				return ((XmlSchemaSimpleTypeRestriction)_content).BaseTypeName;
			}
			return XmlQualifiedName.Empty;
		}
	}

	internal override XmlSchemaObject Clone()
	{
		XmlSchemaSimpleType xmlSchemaSimpleType = (XmlSchemaSimpleType)MemberwiseClone();
		if (_content != null)
		{
			xmlSchemaSimpleType.Content = (XmlSchemaSimpleTypeContent)_content.Clone();
		}
		return xmlSchemaSimpleType;
	}
}
