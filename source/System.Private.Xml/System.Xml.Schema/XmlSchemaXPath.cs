using System.ComponentModel;
using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaXPath : XmlSchemaAnnotated
{
	private string _xpath;

	[XmlAttribute("xpath")]
	[DefaultValue("")]
	public string? XPath
	{
		get
		{
			return _xpath;
		}
		set
		{
			_xpath = value;
		}
	}
}
