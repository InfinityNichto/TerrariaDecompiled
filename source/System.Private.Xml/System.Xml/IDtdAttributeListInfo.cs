using System.Collections.Generic;

namespace System.Xml;

internal interface IDtdAttributeListInfo
{
	string Prefix { get; }

	string LocalName { get; }

	bool HasNonCDataAttributes { get; }

	IDtdAttributeInfo LookupAttribute(string prefix, string localName);

	IEnumerable<IDtdDefaultAttributeInfo> LookupDefaultAttributes();

	IDtdAttributeInfo LookupIdAttribute();
}
