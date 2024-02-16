using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaImport : XmlSchemaExternal
{
	private string _ns;

	private XmlSchemaAnnotation _annotation;

	[XmlAttribute("namespace", DataType = "anyURI")]
	public string? Namespace
	{
		get
		{
			return _ns;
		}
		set
		{
			_ns = value;
		}
	}

	[XmlElement("annotation", typeof(XmlSchemaAnnotation))]
	public XmlSchemaAnnotation? Annotation
	{
		get
		{
			return _annotation;
		}
		set
		{
			_annotation = value;
		}
	}

	public XmlSchemaImport()
	{
		base.Compositor = Compositor.Import;
	}

	internal override void AddAnnotation(XmlSchemaAnnotation annotation)
	{
		_annotation = annotation;
	}
}
