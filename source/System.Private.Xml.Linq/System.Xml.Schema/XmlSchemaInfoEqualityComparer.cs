using System.Collections.Generic;

namespace System.Xml.Schema;

internal sealed class XmlSchemaInfoEqualityComparer : IEqualityComparer<XmlSchemaInfo>
{
	public bool Equals(XmlSchemaInfo si1, XmlSchemaInfo si2)
	{
		if (si1 == si2)
		{
			return true;
		}
		if (si1 == null || si2 == null)
		{
			return false;
		}
		if (si1.ContentType == si2.ContentType && si1.IsDefault == si2.IsDefault && si1.IsNil == si2.IsNil && si1.MemberType == si2.MemberType && si1.SchemaAttribute == si2.SchemaAttribute && si1.SchemaElement == si2.SchemaElement && si1.SchemaType == si2.SchemaType)
		{
			return si1.Validity == si2.Validity;
		}
		return false;
	}

	public int GetHashCode(XmlSchemaInfo si)
	{
		if (si == null)
		{
			return 0;
		}
		int num = (int)si.ContentType;
		if (si.IsDefault)
		{
			num ^= 1;
		}
		if (si.IsNil)
		{
			num ^= 1;
		}
		XmlSchemaSimpleType memberType = si.MemberType;
		if (memberType != null)
		{
			num ^= memberType.GetHashCode();
		}
		XmlSchemaAttribute schemaAttribute = si.SchemaAttribute;
		if (schemaAttribute != null)
		{
			num ^= schemaAttribute.GetHashCode();
		}
		XmlSchemaElement schemaElement = si.SchemaElement;
		if (schemaElement != null)
		{
			num ^= schemaElement.GetHashCode();
		}
		XmlSchemaType schemaType = si.SchemaType;
		if (schemaType != null)
		{
			num ^= schemaType.GetHashCode();
		}
		return num ^ (int)si.Validity;
	}
}
