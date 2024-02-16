using System.Collections;
using System.Globalization;

namespace System.Xml.Schema;

internal sealed class Numeric10FacetsChecker : FacetsChecker
{
	private readonly decimal _maxValue;

	private readonly decimal _minValue;

	internal Numeric10FacetsChecker(decimal minVal, decimal maxVal)
	{
		_minValue = minVal;
		_maxValue = maxVal;
	}

	internal override Exception CheckValueFacets(object value, XmlSchemaDatatype datatype)
	{
		decimal value2 = datatype.ValueConverter.ToDecimal(value);
		return CheckValueFacets(value2, datatype);
	}

	internal override Exception CheckValueFacets(decimal value, XmlSchemaDatatype datatype)
	{
		RestrictionFacets restriction = datatype.Restriction;
		RestrictionFlags restrictionFlags = restriction?.Flags ?? ((RestrictionFlags)0);
		XmlValueConverter valueConverter = datatype.ValueConverter;
		if (value > _maxValue || value < _minValue)
		{
			return new OverflowException(System.SR.Format(System.SR.XmlConvert_Overflow, value.ToString(CultureInfo.InvariantCulture), datatype.TypeCodeString));
		}
		if (restrictionFlags != 0)
		{
			if ((restrictionFlags & RestrictionFlags.MaxInclusive) != 0 && value > valueConverter.ToDecimal(restriction.MaxInclusive))
			{
				return new XmlSchemaException(System.SR.Sch_MaxInclusiveConstraintFailed, string.Empty);
			}
			if ((restrictionFlags & RestrictionFlags.MaxExclusive) != 0 && value >= valueConverter.ToDecimal(restriction.MaxExclusive))
			{
				return new XmlSchemaException(System.SR.Sch_MaxExclusiveConstraintFailed, string.Empty);
			}
			if ((restrictionFlags & RestrictionFlags.MinInclusive) != 0 && value < valueConverter.ToDecimal(restriction.MinInclusive))
			{
				return new XmlSchemaException(System.SR.Sch_MinInclusiveConstraintFailed, string.Empty);
			}
			if ((restrictionFlags & RestrictionFlags.MinExclusive) != 0 && value <= valueConverter.ToDecimal(restriction.MinExclusive))
			{
				return new XmlSchemaException(System.SR.Sch_MinExclusiveConstraintFailed, string.Empty);
			}
			if ((restrictionFlags & RestrictionFlags.Enumeration) != 0 && !MatchEnumeration(value, restriction.Enumeration, valueConverter))
			{
				return new XmlSchemaException(System.SR.Sch_EnumerationConstraintFailed, string.Empty);
			}
			return CheckTotalAndFractionDigits(value, restriction.TotalDigits, restriction.FractionDigits, (restrictionFlags & RestrictionFlags.TotalDigits) != 0, (restrictionFlags & RestrictionFlags.FractionDigits) != 0);
		}
		return null;
	}

	internal override Exception CheckValueFacets(long value, XmlSchemaDatatype datatype)
	{
		decimal value2 = value;
		return CheckValueFacets(value2, datatype);
	}

	internal override Exception CheckValueFacets(int value, XmlSchemaDatatype datatype)
	{
		decimal value2 = value;
		return CheckValueFacets(value2, datatype);
	}

	internal override Exception CheckValueFacets(short value, XmlSchemaDatatype datatype)
	{
		decimal value2 = value;
		return CheckValueFacets(value2, datatype);
	}

	internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
	{
		return MatchEnumeration(datatype.ValueConverter.ToDecimal(value), enumeration, datatype.ValueConverter);
	}

	internal bool MatchEnumeration(decimal value, ArrayList enumeration, XmlValueConverter valueConverter)
	{
		for (int i = 0; i < enumeration.Count; i++)
		{
			if (value == valueConverter.ToDecimal(enumeration[i]))
			{
				return true;
			}
		}
		return false;
	}

	internal Exception CheckTotalAndFractionDigits(decimal value, int totalDigits, int fractionDigits, bool checkTotal, bool checkFraction)
	{
		decimal num = FacetsChecker.Power(10, totalDigits) - 1m;
		int num2 = 0;
		if (value < 0m)
		{
			value = decimal.Negate(value);
		}
		while (decimal.Truncate(value) != value)
		{
			value *= 10m;
			num2++;
		}
		if (checkTotal && (value > num || num2 > totalDigits))
		{
			return new XmlSchemaException(System.SR.Sch_TotalDigitsConstraintFailed, string.Empty);
		}
		if (checkFraction && num2 > fractionDigits)
		{
			return new XmlSchemaException(System.SR.Sch_FractionDigitsConstraintFailed, string.Empty);
		}
		return null;
	}
}
