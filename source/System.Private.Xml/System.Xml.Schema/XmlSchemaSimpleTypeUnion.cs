using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaSimpleTypeUnion : XmlSchemaSimpleTypeContent
{
	private readonly XmlSchemaObjectCollection _baseTypes = new XmlSchemaObjectCollection();

	private XmlQualifiedName[] _memberTypes;

	private XmlSchemaSimpleType[] _baseMemberTypes;

	[XmlElement("simpleType", typeof(XmlSchemaSimpleType))]
	public XmlSchemaObjectCollection BaseTypes => _baseTypes;

	[XmlAttribute("memberTypes")]
	public XmlQualifiedName[]? MemberTypes
	{
		get
		{
			return _memberTypes;
		}
		set
		{
			_memberTypes = value;
		}
	}

	[XmlIgnore]
	public XmlSchemaSimpleType[]? BaseMemberTypes => _baseMemberTypes;

	internal void SetBaseMemberTypes(XmlSchemaSimpleType[] baseMemberTypes)
	{
		_baseMemberTypes = baseMemberTypes;
	}

	internal override XmlSchemaObject Clone()
	{
		if (_memberTypes != null && _memberTypes.Length != 0)
		{
			XmlSchemaSimpleTypeUnion xmlSchemaSimpleTypeUnion = (XmlSchemaSimpleTypeUnion)MemberwiseClone();
			XmlQualifiedName[] array = new XmlQualifiedName[_memberTypes.Length];
			for (int i = 0; i < _memberTypes.Length; i++)
			{
				array[i] = _memberTypes[i].Clone();
			}
			xmlSchemaSimpleTypeUnion.MemberTypes = array;
			return xmlSchemaSimpleTypeUnion;
		}
		return this;
	}
}
