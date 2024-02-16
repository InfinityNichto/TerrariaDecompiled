using System.Xml.Schema;

namespace System.Xml.Serialization;

public interface IXmlSerializable
{
	XmlSchema? GetSchema();

	void ReadXml(XmlReader reader);

	void WriteXml(XmlWriter writer);
}
