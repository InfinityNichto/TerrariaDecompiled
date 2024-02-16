using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaInclude : XmlSchemaExternal
{
	private XmlSchemaAnnotation _annotation;

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

	public XmlSchemaInclude()
	{
		base.Compositor = Compositor.Include;
	}

	internal override void AddAnnotation(XmlSchemaAnnotation annotation)
	{
		_annotation = annotation;
	}
}
