using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaGroup : XmlSchemaAnnotated
{
	private string _name;

	private XmlSchemaGroupBase _particle;

	private XmlSchemaParticle _canonicalParticle;

	private XmlQualifiedName _qname = XmlQualifiedName.Empty;

	private XmlSchemaGroup _redefined;

	private int _selfReferenceCount;

	[XmlAttribute("name")]
	public string? Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
		}
	}

	[XmlElement("choice", typeof(XmlSchemaChoice))]
	[XmlElement("all", typeof(XmlSchemaAll))]
	[XmlElement("sequence", typeof(XmlSchemaSequence))]
	public XmlSchemaGroupBase? Particle
	{
		get
		{
			return _particle;
		}
		set
		{
			_particle = value;
		}
	}

	[XmlIgnore]
	public XmlQualifiedName QualifiedName => _qname;

	[XmlIgnore]
	internal XmlSchemaParticle? CanonicalParticle
	{
		get
		{
			return _canonicalParticle;
		}
		set
		{
			_canonicalParticle = value;
		}
	}

	[XmlIgnore]
	internal XmlSchemaGroup? Redefined
	{
		get
		{
			return _redefined;
		}
		set
		{
			_redefined = value;
		}
	}

	[XmlIgnore]
	internal int SelfReferenceCount
	{
		get
		{
			return _selfReferenceCount;
		}
		set
		{
			_selfReferenceCount = value;
		}
	}

	[XmlIgnore]
	internal override string? NameAttribute
	{
		get
		{
			return Name;
		}
		set
		{
			Name = value;
		}
	}

	internal void SetQualifiedName(XmlQualifiedName value)
	{
		_qname = value;
	}

	internal override XmlSchemaObject Clone()
	{
		return Clone(null);
	}

	internal XmlSchemaObject Clone(XmlSchema parentSchema)
	{
		XmlSchemaGroup xmlSchemaGroup = (XmlSchemaGroup)MemberwiseClone();
		if (XmlSchemaComplexType.HasParticleRef(_particle, parentSchema))
		{
			xmlSchemaGroup._particle = XmlSchemaComplexType.CloneParticle(_particle, parentSchema) as XmlSchemaGroupBase;
		}
		xmlSchemaGroup._canonicalParticle = XmlSchemaParticle.Empty;
		return xmlSchemaGroup;
	}
}
