using System.Collections;
using System.Xml.Serialization;

namespace System.Xml.Schema;

internal class XmlSchemaSubstitutionGroup : XmlSchemaObject
{
	private readonly ArrayList _membersList = new ArrayList();

	private XmlQualifiedName _examplar = XmlQualifiedName.Empty;

	[XmlIgnore]
	internal ArrayList Members => _membersList;

	[XmlIgnore]
	internal XmlQualifiedName Examplar
	{
		get
		{
			return _examplar;
		}
		set
		{
			_examplar = value;
		}
	}
}
