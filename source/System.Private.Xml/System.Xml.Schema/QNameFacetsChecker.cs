using System.Collections;

namespace System.Xml.Schema;

internal sealed class QNameFacetsChecker : FacetsChecker
{
	internal override Exception CheckValueFacets(object value, XmlSchemaDatatype datatype)
	{
		XmlQualifiedName value2 = (XmlQualifiedName)datatype.ValueConverter.ChangeType(value, typeof(XmlQualifiedName));
		return CheckValueFacets(value2, datatype);
	}

	internal override Exception CheckValueFacets(XmlQualifiedName value, XmlSchemaDatatype datatype)
	{
		RestrictionFacets restriction = datatype.Restriction;
		RestrictionFlags restrictionFlags = restriction?.Flags ?? ((RestrictionFlags)0);
		if (restrictionFlags != 0)
		{
			string text = value.ToString();
			int length = text.Length;
			if ((restrictionFlags & RestrictionFlags.Length) != 0 && restriction.Length != length)
			{
				return new XmlSchemaException(System.SR.Sch_LengthConstraintFailed, string.Empty);
			}
			if ((restrictionFlags & RestrictionFlags.MinLength) != 0 && length < restriction.MinLength)
			{
				return new XmlSchemaException(System.SR.Sch_MinLengthConstraintFailed, string.Empty);
			}
			if ((restrictionFlags & RestrictionFlags.MaxLength) != 0 && restriction.MaxLength < length)
			{
				return new XmlSchemaException(System.SR.Sch_MaxLengthConstraintFailed, string.Empty);
			}
			if ((restrictionFlags & RestrictionFlags.Enumeration) != 0 && !MatchEnumeration(value, restriction.Enumeration))
			{
				return new XmlSchemaException(System.SR.Sch_EnumerationConstraintFailed, string.Empty);
			}
		}
		return null;
	}

	internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
	{
		return MatchEnumeration((XmlQualifiedName)datatype.ValueConverter.ChangeType(value, typeof(XmlQualifiedName)), enumeration);
	}

	private bool MatchEnumeration(XmlQualifiedName value, ArrayList enumeration)
	{
		for (int i = 0; i < enumeration.Count; i++)
		{
			if (value.Equals((XmlQualifiedName)enumeration[i]))
			{
				return true;
			}
		}
		return false;
	}
}
