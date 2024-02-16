using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaAnnotation : XmlSchemaObject
{
	private string _id;

	private readonly XmlSchemaObjectCollection _items = new XmlSchemaObjectCollection();

	private XmlAttribute[] _moreAttributes;

	[XmlAttribute("id", DataType = "ID")]
	public string? Id
	{
		get
		{
			return _id;
		}
		set
		{
			_id = value;
		}
	}

	[XmlElement("documentation", typeof(XmlSchemaDocumentation))]
	[XmlElement("appinfo", typeof(XmlSchemaAppInfo))]
	public XmlSchemaObjectCollection Items => _items;

	[XmlAnyAttribute]
	public XmlAttribute[]? UnhandledAttributes
	{
		get
		{
			return _moreAttributes;
		}
		set
		{
			_moreAttributes = value;
		}
	}

	[XmlIgnore]
	internal override string? IdAttribute
	{
		get
		{
			return Id;
		}
		set
		{
			Id = value;
		}
	}

	internal override void SetUnhandledAttributes(XmlAttribute[] moreAttributes)
	{
		_moreAttributes = moreAttributes;
	}
}
