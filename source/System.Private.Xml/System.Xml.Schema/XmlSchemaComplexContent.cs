using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaComplexContent : XmlSchemaContentModel
{
	private XmlSchemaContent _content;

	private bool _isMixed;

	private bool _hasMixedAttribute;

	[XmlAttribute("mixed")]
	public bool IsMixed
	{
		get
		{
			return _isMixed;
		}
		set
		{
			_isMixed = value;
			_hasMixedAttribute = true;
		}
	}

	[XmlElement("restriction", typeof(XmlSchemaComplexContentRestriction))]
	[XmlElement("extension", typeof(XmlSchemaComplexContentExtension))]
	public override XmlSchemaContent? Content
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

	[XmlIgnore]
	internal bool HasMixedAttribute => _hasMixedAttribute;
}
