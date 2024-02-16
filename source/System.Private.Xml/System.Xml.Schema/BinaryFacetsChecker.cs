using System.Collections;

namespace System.Xml.Schema;

internal sealed class BinaryFacetsChecker : FacetsChecker
{
	internal override Exception CheckValueFacets(object value, XmlSchemaDatatype datatype)
	{
		byte[] value2 = (byte[])value;
		return CheckValueFacets(value2, datatype);
	}

	internal override Exception CheckValueFacets(byte[] value, XmlSchemaDatatype datatype)
	{
		RestrictionFacets restriction = datatype.Restriction;
		int num = value.Length;
		RestrictionFlags restrictionFlags = restriction?.Flags ?? ((RestrictionFlags)0);
		if (restrictionFlags != 0)
		{
			if ((restrictionFlags & RestrictionFlags.Length) != 0 && restriction.Length != num)
			{
				return new XmlSchemaException(System.SR.Sch_LengthConstraintFailed, string.Empty);
			}
			if ((restrictionFlags & RestrictionFlags.MinLength) != 0 && num < restriction.MinLength)
			{
				return new XmlSchemaException(System.SR.Sch_MinLengthConstraintFailed, string.Empty);
			}
			if ((restrictionFlags & RestrictionFlags.MaxLength) != 0 && restriction.MaxLength < num)
			{
				return new XmlSchemaException(System.SR.Sch_MaxLengthConstraintFailed, string.Empty);
			}
			if ((restrictionFlags & RestrictionFlags.Enumeration) != 0 && !MatchEnumeration(value, restriction.Enumeration, datatype))
			{
				return new XmlSchemaException(System.SR.Sch_EnumerationConstraintFailed, string.Empty);
			}
		}
		return null;
	}

	internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
	{
		return MatchEnumeration((byte[])value, enumeration, datatype);
	}

	private bool MatchEnumeration(byte[] value, ArrayList enumeration, XmlSchemaDatatype datatype)
	{
		for (int i = 0; i < enumeration.Count; i++)
		{
			if (datatype.Compare(value, (byte[])enumeration[i]) == 0)
			{
				return true;
			}
		}
		return false;
	}
}
