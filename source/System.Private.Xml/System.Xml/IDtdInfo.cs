using System.Collections.Generic;

namespace System.Xml;

internal interface IDtdInfo
{
	XmlQualifiedName Name { get; }

	string InternalDtdSubset { get; }

	bool HasDefaultAttributes { get; }

	bool HasNonCDataAttributes { get; }

	IDtdAttributeListInfo LookupAttributeList(string prefix, string localName);

	IEnumerable<IDtdAttributeListInfo> GetAttributeLists();

	IDtdEntityInfo LookupEntity(string name);
}
