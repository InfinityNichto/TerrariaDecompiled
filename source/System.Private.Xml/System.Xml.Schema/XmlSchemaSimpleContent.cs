using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaSimpleContent : XmlSchemaContentModel
{
	private XmlSchemaContent _content;

	[XmlElement("restriction", typeof(XmlSchemaSimpleContentRestriction))]
	[XmlElement("extension", typeof(XmlSchemaSimpleContentExtension))]
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
}
