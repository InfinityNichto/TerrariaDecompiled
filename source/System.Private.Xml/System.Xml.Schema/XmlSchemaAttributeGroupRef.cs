using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaAttributeGroupRef : XmlSchemaAnnotated
{
	private XmlQualifiedName _refName = XmlQualifiedName.Empty;

	[XmlAttribute("ref")]
	public XmlQualifiedName RefName
	{
		get
		{
			return _refName;
		}
		[param: AllowNull]
		set
		{
			_refName = ((value == null) ? XmlQualifiedName.Empty : value);
		}
	}
}
