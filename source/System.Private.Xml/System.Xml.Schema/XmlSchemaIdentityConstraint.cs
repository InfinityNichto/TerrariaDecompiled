using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaIdentityConstraint : XmlSchemaAnnotated
{
	private string _name;

	private XmlSchemaXPath _selector;

	private readonly XmlSchemaObjectCollection _fields = new XmlSchemaObjectCollection();

	private XmlQualifiedName _qualifiedName = XmlQualifiedName.Empty;

	private CompiledIdentityConstraint _compiledConstraint;

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

	[XmlElement("selector", typeof(XmlSchemaXPath))]
	public XmlSchemaXPath? Selector
	{
		get
		{
			return _selector;
		}
		set
		{
			_selector = value;
		}
	}

	[XmlElement("field", typeof(XmlSchemaXPath))]
	public XmlSchemaObjectCollection Fields => _fields;

	[XmlIgnore]
	public XmlQualifiedName QualifiedName => _qualifiedName;

	[XmlIgnore]
	internal CompiledIdentityConstraint? CompiledConstraint
	{
		get
		{
			return _compiledConstraint;
		}
		set
		{
			_compiledConstraint = value;
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
		_qualifiedName = value;
	}
}
