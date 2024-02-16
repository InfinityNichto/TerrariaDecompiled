using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaSimpleTypeList : XmlSchemaSimpleTypeContent
{
	private XmlQualifiedName _itemTypeName = XmlQualifiedName.Empty;

	private XmlSchemaSimpleType _itemType;

	private XmlSchemaSimpleType _baseItemType;

	[XmlAttribute("itemType")]
	public XmlQualifiedName ItemTypeName
	{
		get
		{
			return _itemTypeName;
		}
		set
		{
			_itemTypeName = ((value == null) ? XmlQualifiedName.Empty : value);
		}
	}

	[XmlElement("simpleType", typeof(XmlSchemaSimpleType))]
	public XmlSchemaSimpleType? ItemType
	{
		get
		{
			return _itemType;
		}
		set
		{
			_itemType = value;
		}
	}

	[XmlIgnore]
	public XmlSchemaSimpleType? BaseItemType
	{
		get
		{
			return _baseItemType;
		}
		set
		{
			_baseItemType = value;
		}
	}

	internal override XmlSchemaObject Clone()
	{
		XmlSchemaSimpleTypeList xmlSchemaSimpleTypeList = (XmlSchemaSimpleTypeList)MemberwiseClone();
		xmlSchemaSimpleTypeList.ItemTypeName = _itemTypeName.Clone();
		return xmlSchemaSimpleTypeList;
	}
}
