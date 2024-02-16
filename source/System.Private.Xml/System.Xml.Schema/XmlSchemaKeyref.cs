using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaKeyref : XmlSchemaIdentityConstraint
{
	private XmlQualifiedName _refer = XmlQualifiedName.Empty;

	[XmlAttribute("refer")]
	public XmlQualifiedName Refer
	{
		get
		{
			return _refer;
		}
		set
		{
			_refer = ((value == null) ? XmlQualifiedName.Empty : value);
		}
	}
}
