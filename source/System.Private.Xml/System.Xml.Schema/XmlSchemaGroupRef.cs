using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaGroupRef : XmlSchemaParticle
{
	private XmlQualifiedName _refName = XmlQualifiedName.Empty;

	private XmlSchemaGroupBase _particle;

	private XmlSchemaGroup _refined;

	[XmlAttribute("ref")]
	public XmlQualifiedName RefName
	{
		get
		{
			return _refName;
		}
		set
		{
			_refName = ((value == null) ? XmlQualifiedName.Empty : value);
		}
	}

	[XmlIgnore]
	public XmlSchemaGroupBase? Particle => _particle;

	[XmlIgnore]
	internal XmlSchemaGroup? Redefined
	{
		get
		{
			return _refined;
		}
		set
		{
			_refined = value;
		}
	}

	internal void SetParticle(XmlSchemaGroupBase value)
	{
		_particle = value;
	}
}
