using System.Xml.Serialization;

namespace System.Xml.Schema;

internal sealed class XmlSchemaSubstitutionGroupV1Compat : XmlSchemaSubstitutionGroup
{
	private readonly XmlSchemaChoice _choice = new XmlSchemaChoice();

	[XmlIgnore]
	internal XmlSchemaChoice Choice => _choice;
}
