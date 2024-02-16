using System.Collections;

namespace System.Xml.Schema;

internal sealed class DateTimeFacetsChecker : FacetsChecker
{
	internal override Exception CheckValueFacets(object value, XmlSchemaDatatype datatype)
	{
		DateTime value2 = datatype.ValueConverter.ToDateTime(value);
		return CheckValueFacets(value2, datatype);
	}

	internal override Exception CheckValueFacets(DateTime value, XmlSchemaDatatype datatype)
	{
		RestrictionFacets restriction = datatype.Restriction;
		RestrictionFlags restrictionFlags = restriction?.Flags ?? ((RestrictionFlags)0);
		if ((restrictionFlags & RestrictionFlags.MaxInclusive) != 0 && datatype.Compare(value, (DateTime)restriction.MaxInclusive) > 0)
		{
			return new XmlSchemaException(System.SR.Sch_MaxInclusiveConstraintFailed, string.Empty);
		}
		if ((restrictionFlags & RestrictionFlags.MaxExclusive) != 0 && datatype.Compare(value, (DateTime)restriction.MaxExclusive) >= 0)
		{
			return new XmlSchemaException(System.SR.Sch_MaxExclusiveConstraintFailed, string.Empty);
		}
		if ((restrictionFlags & RestrictionFlags.MinInclusive) != 0 && datatype.Compare(value, (DateTime)restriction.MinInclusive) < 0)
		{
			return new XmlSchemaException(System.SR.Sch_MinInclusiveConstraintFailed, string.Empty);
		}
		if ((restrictionFlags & RestrictionFlags.MinExclusive) != 0 && datatype.Compare(value, (DateTime)restriction.MinExclusive) <= 0)
		{
			return new XmlSchemaException(System.SR.Sch_MinExclusiveConstraintFailed, string.Empty);
		}
		if ((restrictionFlags & RestrictionFlags.Enumeration) != 0 && !MatchEnumeration(value, restriction.Enumeration, datatype))
		{
			return new XmlSchemaException(System.SR.Sch_EnumerationConstraintFailed, string.Empty);
		}
		return null;
	}

	internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
	{
		return MatchEnumeration(datatype.ValueConverter.ToDateTime(value), enumeration, datatype);
	}

	private bool MatchEnumeration(DateTime value, ArrayList enumeration, XmlSchemaDatatype datatype)
	{
		for (int i = 0; i < enumeration.Count; i++)
		{
			if (datatype.Compare(value, (DateTime)enumeration[i]) == 0)
			{
				return true;
			}
		}
		return false;
	}
}
