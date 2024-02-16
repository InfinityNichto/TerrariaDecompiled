using System.Collections;

namespace System.Xml.Schema;

internal sealed class DurationFacetsChecker : FacetsChecker
{
	internal override Exception CheckValueFacets(object value, XmlSchemaDatatype datatype)
	{
		TimeSpan value2 = (TimeSpan)datatype.ValueConverter.ChangeType(value, typeof(TimeSpan));
		return CheckValueFacets(value2, datatype);
	}

	internal override Exception CheckValueFacets(TimeSpan value, XmlSchemaDatatype datatype)
	{
		RestrictionFacets restriction = datatype.Restriction;
		RestrictionFlags restrictionFlags = restriction?.Flags ?? ((RestrictionFlags)0);
		if ((restrictionFlags & RestrictionFlags.MaxInclusive) != 0 && TimeSpan.Compare(value, (TimeSpan)restriction.MaxInclusive) > 0)
		{
			return new XmlSchemaException(System.SR.Sch_MaxInclusiveConstraintFailed, string.Empty);
		}
		if ((restrictionFlags & RestrictionFlags.MaxExclusive) != 0 && TimeSpan.Compare(value, (TimeSpan)restriction.MaxExclusive) >= 0)
		{
			return new XmlSchemaException(System.SR.Sch_MaxExclusiveConstraintFailed, string.Empty);
		}
		if ((restrictionFlags & RestrictionFlags.MinInclusive) != 0 && TimeSpan.Compare(value, (TimeSpan)restriction.MinInclusive) < 0)
		{
			return new XmlSchemaException(System.SR.Sch_MinInclusiveConstraintFailed, string.Empty);
		}
		if ((restrictionFlags & RestrictionFlags.MinExclusive) != 0 && TimeSpan.Compare(value, (TimeSpan)restriction.MinExclusive) <= 0)
		{
			return new XmlSchemaException(System.SR.Sch_MinExclusiveConstraintFailed, string.Empty);
		}
		if ((restrictionFlags & RestrictionFlags.Enumeration) != 0 && !MatchEnumeration(value, restriction.Enumeration))
		{
			return new XmlSchemaException(System.SR.Sch_EnumerationConstraintFailed, string.Empty);
		}
		return null;
	}

	internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
	{
		return MatchEnumeration((TimeSpan)value, enumeration);
	}

	private bool MatchEnumeration(TimeSpan value, ArrayList enumeration)
	{
		for (int i = 0; i < enumeration.Count; i++)
		{
			if (TimeSpan.Compare(value, (TimeSpan)enumeration[i]) == 0)
			{
				return true;
			}
		}
		return false;
	}
}
