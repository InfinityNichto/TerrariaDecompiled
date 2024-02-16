using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

namespace System.Xml.Schema;

internal abstract class FacetsChecker
{
	private struct FacetsCompiler
	{
		private struct Map
		{
			internal char match;

			internal string replacement;

			internal Map(char m, string r)
			{
				match = m;
				replacement = r;
			}
		}

		private readonly DatatypeImplementation _datatype;

		private readonly RestrictionFacets _derivedRestriction;

		private readonly RestrictionFlags _baseFlags;

		private readonly RestrictionFlags _baseFixedFlags;

		private readonly RestrictionFlags _validRestrictionFlags;

		private readonly XmlSchemaDatatype _nonNegativeInt;

		private readonly XmlSchemaDatatype _builtInType;

		private readonly XmlTypeCode _builtInEnum;

		private bool _firstPattern;

		private StringBuilder _regStr;

		private XmlSchemaPatternFacet _pattern_facet;

		private static readonly Map[] s_map = new Map[8]
		{
			new Map('c', "\\p{_xmlC}"),
			new Map('C', "\\P{_xmlC}"),
			new Map('d', "\\p{_xmlD}"),
			new Map('D', "\\P{_xmlD}"),
			new Map('i', "\\p{_xmlI}"),
			new Map('I', "\\P{_xmlI}"),
			new Map('w', "\\p{_xmlW}"),
			new Map('W', "\\P{_xmlW}")
		};

		public FacetsCompiler(DatatypeImplementation baseDatatype, RestrictionFacets restriction)
		{
			_firstPattern = true;
			_regStr = null;
			_pattern_facet = null;
			_datatype = baseDatatype;
			_derivedRestriction = restriction;
			_baseFlags = ((_datatype.Restriction != null) ? _datatype.Restriction.Flags : ((RestrictionFlags)0));
			_baseFixedFlags = ((_datatype.Restriction != null) ? _datatype.Restriction.FixedFlags : ((RestrictionFlags)0));
			_validRestrictionFlags = _datatype.ValidRestrictionFlags;
			_nonNegativeInt = DatatypeImplementation.GetSimpleTypeFromTypeCode(XmlTypeCode.NonNegativeInteger).Datatype;
			_builtInEnum = ((!(_datatype is Datatype_union) && !(_datatype is Datatype_List)) ? _datatype.TypeCode : XmlTypeCode.None);
			_builtInType = ((_builtInEnum > XmlTypeCode.None) ? DatatypeImplementation.GetSimpleTypeFromTypeCode(_builtInEnum).Datatype : _datatype);
		}

		internal void CompileLengthFacet(XmlSchemaFacet facet)
		{
			CheckProhibitedFlag(facet, RestrictionFlags.Length, System.SR.Sch_LengthFacetProhibited);
			CheckDupFlag(facet, RestrictionFlags.Length, System.SR.Sch_DupLengthFacet);
			_derivedRestriction.Length = XmlBaseConverter.DecimalToInt32((decimal)ParseFacetValue(_nonNegativeInt, facet, System.SR.Sch_LengthFacetInvalid, null, null));
			if ((_baseFixedFlags & RestrictionFlags.Length) != 0 && !_datatype.IsEqual(_datatype.Restriction.Length, _derivedRestriction.Length))
			{
				throw new XmlSchemaException(System.SR.Sch_FacetBaseFixed, facet);
			}
			if ((_baseFlags & RestrictionFlags.Length) != 0 && _datatype.Restriction.Length < _derivedRestriction.Length)
			{
				throw new XmlSchemaException(System.SR.Sch_LengthGtBaseLength, facet);
			}
			if ((_baseFlags & RestrictionFlags.MinLength) != 0 && _datatype.Restriction.MinLength > _derivedRestriction.Length)
			{
				throw new XmlSchemaException(System.SR.Sch_MaxMinLengthBaseLength, facet);
			}
			if ((_baseFlags & RestrictionFlags.MaxLength) != 0 && _datatype.Restriction.MaxLength < _derivedRestriction.Length)
			{
				throw new XmlSchemaException(System.SR.Sch_MaxMinLengthBaseLength, facet);
			}
			SetFlag(facet, RestrictionFlags.Length);
		}

		internal void CompileMinLengthFacet(XmlSchemaFacet facet)
		{
			CheckProhibitedFlag(facet, RestrictionFlags.MinLength, System.SR.Sch_MinLengthFacetProhibited);
			CheckDupFlag(facet, RestrictionFlags.MinLength, System.SR.Sch_DupMinLengthFacet);
			_derivedRestriction.MinLength = XmlBaseConverter.DecimalToInt32((decimal)ParseFacetValue(_nonNegativeInt, facet, System.SR.Sch_MinLengthFacetInvalid, null, null));
			if ((_baseFixedFlags & RestrictionFlags.MinLength) != 0 && !_datatype.IsEqual(_datatype.Restriction.MinLength, _derivedRestriction.MinLength))
			{
				throw new XmlSchemaException(System.SR.Sch_FacetBaseFixed, facet);
			}
			if ((_baseFlags & RestrictionFlags.MinLength) != 0 && _datatype.Restriction.MinLength > _derivedRestriction.MinLength)
			{
				throw new XmlSchemaException(System.SR.Sch_MinLengthGtBaseMinLength, facet);
			}
			if ((_baseFlags & RestrictionFlags.Length) != 0 && _datatype.Restriction.Length < _derivedRestriction.MinLength)
			{
				throw new XmlSchemaException(System.SR.Sch_MaxMinLengthBaseLength, facet);
			}
			SetFlag(facet, RestrictionFlags.MinLength);
		}

		internal void CompileMaxLengthFacet(XmlSchemaFacet facet)
		{
			CheckProhibitedFlag(facet, RestrictionFlags.MaxLength, System.SR.Sch_MaxLengthFacetProhibited);
			CheckDupFlag(facet, RestrictionFlags.MaxLength, System.SR.Sch_DupMaxLengthFacet);
			_derivedRestriction.MaxLength = XmlBaseConverter.DecimalToInt32((decimal)ParseFacetValue(_nonNegativeInt, facet, System.SR.Sch_MaxLengthFacetInvalid, null, null));
			if ((_baseFixedFlags & RestrictionFlags.MaxLength) != 0 && !_datatype.IsEqual(_datatype.Restriction.MaxLength, _derivedRestriction.MaxLength))
			{
				throw new XmlSchemaException(System.SR.Sch_FacetBaseFixed, facet);
			}
			if ((_baseFlags & RestrictionFlags.MaxLength) != 0 && _datatype.Restriction.MaxLength < _derivedRestriction.MaxLength)
			{
				throw new XmlSchemaException(System.SR.Sch_MaxLengthGtBaseMaxLength, facet);
			}
			if ((_baseFlags & RestrictionFlags.Length) != 0 && _datatype.Restriction.Length > _derivedRestriction.MaxLength)
			{
				throw new XmlSchemaException(System.SR.Sch_MaxMinLengthBaseLength, facet);
			}
			SetFlag(facet, RestrictionFlags.MaxLength);
		}

		internal void CompilePatternFacet(XmlSchemaPatternFacet facet)
		{
			CheckProhibitedFlag(facet, RestrictionFlags.Pattern, System.SR.Sch_PatternFacetProhibited);
			if (_firstPattern)
			{
				_regStr = new StringBuilder();
				_regStr.Append('(');
				_regStr.Append(facet.Value);
				_pattern_facet = facet;
				_firstPattern = false;
			}
			else
			{
				_regStr.Append(")|(");
				_regStr.Append(facet.Value);
			}
			SetFlag(facet, RestrictionFlags.Pattern);
		}

		internal void CompileEnumerationFacet(XmlSchemaFacet facet, IXmlNamespaceResolver nsmgr, XmlNameTable nameTable)
		{
			CheckProhibitedFlag(facet, RestrictionFlags.Enumeration, System.SR.Sch_EnumerationFacetProhibited);
			if (_derivedRestriction.Enumeration == null)
			{
				_derivedRestriction.Enumeration = new ArrayList();
			}
			_derivedRestriction.Enumeration.Add(ParseFacetValue(_datatype, facet, System.SR.Sch_EnumerationFacetInvalid, nsmgr, nameTable));
			SetFlag(facet, RestrictionFlags.Enumeration);
		}

		internal void CompileWhitespaceFacet(XmlSchemaFacet facet)
		{
			CheckProhibitedFlag(facet, RestrictionFlags.WhiteSpace, System.SR.Sch_WhiteSpaceFacetProhibited);
			CheckDupFlag(facet, RestrictionFlags.WhiteSpace, System.SR.Sch_DupWhiteSpaceFacet);
			if (facet.Value == "preserve")
			{
				_derivedRestriction.WhiteSpace = XmlSchemaWhiteSpace.Preserve;
			}
			else if (facet.Value == "replace")
			{
				_derivedRestriction.WhiteSpace = XmlSchemaWhiteSpace.Replace;
			}
			else
			{
				if (!(facet.Value == "collapse"))
				{
					throw new XmlSchemaException(System.SR.Sch_InvalidWhiteSpace, facet.Value, facet);
				}
				_derivedRestriction.WhiteSpace = XmlSchemaWhiteSpace.Collapse;
			}
			if ((_baseFixedFlags & RestrictionFlags.WhiteSpace) != 0 && !_datatype.IsEqual(_datatype.Restriction.WhiteSpace, _derivedRestriction.WhiteSpace))
			{
				throw new XmlSchemaException(System.SR.Sch_FacetBaseFixed, facet);
			}
			XmlSchemaWhiteSpace xmlSchemaWhiteSpace = (((_baseFlags & RestrictionFlags.WhiteSpace) == 0) ? _datatype.BuiltInWhitespaceFacet : _datatype.Restriction.WhiteSpace);
			if (xmlSchemaWhiteSpace == XmlSchemaWhiteSpace.Collapse && (_derivedRestriction.WhiteSpace == XmlSchemaWhiteSpace.Replace || _derivedRestriction.WhiteSpace == XmlSchemaWhiteSpace.Preserve))
			{
				throw new XmlSchemaException(System.SR.Sch_WhiteSpaceRestriction1, facet);
			}
			if (xmlSchemaWhiteSpace == XmlSchemaWhiteSpace.Replace && _derivedRestriction.WhiteSpace == XmlSchemaWhiteSpace.Preserve)
			{
				throw new XmlSchemaException(System.SR.Sch_WhiteSpaceRestriction2, facet);
			}
			SetFlag(facet, RestrictionFlags.WhiteSpace);
		}

		internal void CompileMaxInclusiveFacet(XmlSchemaFacet facet)
		{
			CheckProhibitedFlag(facet, RestrictionFlags.MaxInclusive, System.SR.Sch_MaxInclusiveFacetProhibited);
			CheckDupFlag(facet, RestrictionFlags.MaxInclusive, System.SR.Sch_DupMaxInclusiveFacet);
			_derivedRestriction.MaxInclusive = ParseFacetValue(_builtInType, facet, System.SR.Sch_MaxInclusiveFacetInvalid, null, null);
			if ((_baseFixedFlags & RestrictionFlags.MaxInclusive) != 0 && !_datatype.IsEqual(_datatype.Restriction.MaxInclusive, _derivedRestriction.MaxInclusive))
			{
				throw new XmlSchemaException(System.SR.Sch_FacetBaseFixed, facet);
			}
			CheckValue(_derivedRestriction.MaxInclusive, facet);
			SetFlag(facet, RestrictionFlags.MaxInclusive);
		}

		internal void CompileMaxExclusiveFacet(XmlSchemaFacet facet)
		{
			CheckProhibitedFlag(facet, RestrictionFlags.MaxExclusive, System.SR.Sch_MaxExclusiveFacetProhibited);
			CheckDupFlag(facet, RestrictionFlags.MaxExclusive, System.SR.Sch_DupMaxExclusiveFacet);
			_derivedRestriction.MaxExclusive = ParseFacetValue(_builtInType, facet, System.SR.Sch_MaxExclusiveFacetInvalid, null, null);
			if ((_baseFixedFlags & RestrictionFlags.MaxExclusive) != 0 && !_datatype.IsEqual(_datatype.Restriction.MaxExclusive, _derivedRestriction.MaxExclusive))
			{
				throw new XmlSchemaException(System.SR.Sch_FacetBaseFixed, facet);
			}
			CheckValue(_derivedRestriction.MaxExclusive, facet);
			SetFlag(facet, RestrictionFlags.MaxExclusive);
		}

		internal void CompileMinInclusiveFacet(XmlSchemaFacet facet)
		{
			CheckProhibitedFlag(facet, RestrictionFlags.MinInclusive, System.SR.Sch_MinInclusiveFacetProhibited);
			CheckDupFlag(facet, RestrictionFlags.MinInclusive, System.SR.Sch_DupMinInclusiveFacet);
			_derivedRestriction.MinInclusive = ParseFacetValue(_builtInType, facet, System.SR.Sch_MinInclusiveFacetInvalid, null, null);
			if ((_baseFixedFlags & RestrictionFlags.MinInclusive) != 0 && !_datatype.IsEqual(_datatype.Restriction.MinInclusive, _derivedRestriction.MinInclusive))
			{
				throw new XmlSchemaException(System.SR.Sch_FacetBaseFixed, facet);
			}
			CheckValue(_derivedRestriction.MinInclusive, facet);
			SetFlag(facet, RestrictionFlags.MinInclusive);
		}

		internal void CompileMinExclusiveFacet(XmlSchemaFacet facet)
		{
			CheckProhibitedFlag(facet, RestrictionFlags.MinExclusive, System.SR.Sch_MinExclusiveFacetProhibited);
			CheckDupFlag(facet, RestrictionFlags.MinExclusive, System.SR.Sch_DupMinExclusiveFacet);
			_derivedRestriction.MinExclusive = ParseFacetValue(_builtInType, facet, System.SR.Sch_MinExclusiveFacetInvalid, null, null);
			if ((_baseFixedFlags & RestrictionFlags.MinExclusive) != 0 && !_datatype.IsEqual(_datatype.Restriction.MinExclusive, _derivedRestriction.MinExclusive))
			{
				throw new XmlSchemaException(System.SR.Sch_FacetBaseFixed, facet);
			}
			CheckValue(_derivedRestriction.MinExclusive, facet);
			SetFlag(facet, RestrictionFlags.MinExclusive);
		}

		internal void CompileTotalDigitsFacet(XmlSchemaFacet facet)
		{
			CheckProhibitedFlag(facet, RestrictionFlags.TotalDigits, System.SR.Sch_TotalDigitsFacetProhibited);
			CheckDupFlag(facet, RestrictionFlags.TotalDigits, System.SR.Sch_DupTotalDigitsFacet);
			XmlSchemaDatatype datatype = DatatypeImplementation.GetSimpleTypeFromTypeCode(XmlTypeCode.PositiveInteger).Datatype;
			_derivedRestriction.TotalDigits = XmlBaseConverter.DecimalToInt32((decimal)ParseFacetValue(datatype, facet, System.SR.Sch_TotalDigitsFacetInvalid, null, null));
			if ((_baseFixedFlags & RestrictionFlags.TotalDigits) != 0 && _datatype.Restriction.TotalDigits != _derivedRestriction.TotalDigits)
			{
				throw new XmlSchemaException(System.SR.Sch_FacetBaseFixed, facet);
			}
			if ((_baseFlags & RestrictionFlags.TotalDigits) != 0 && _derivedRestriction.TotalDigits > _datatype.Restriction.TotalDigits)
			{
				throw new XmlSchemaException(System.SR.Sch_TotalDigitsMismatch, string.Empty);
			}
			SetFlag(facet, RestrictionFlags.TotalDigits);
		}

		internal void CompileFractionDigitsFacet(XmlSchemaFacet facet)
		{
			CheckProhibitedFlag(facet, RestrictionFlags.FractionDigits, System.SR.Sch_FractionDigitsFacetProhibited);
			CheckDupFlag(facet, RestrictionFlags.FractionDigits, System.SR.Sch_DupFractionDigitsFacet);
			_derivedRestriction.FractionDigits = XmlBaseConverter.DecimalToInt32((decimal)ParseFacetValue(_nonNegativeInt, facet, System.SR.Sch_FractionDigitsFacetInvalid, null, null));
			if (_derivedRestriction.FractionDigits != 0 && _datatype.TypeCode != XmlTypeCode.Decimal)
			{
				throw new XmlSchemaException(System.SR.Sch_FractionDigitsFacetInvalid, System.SR.Sch_FractionDigitsNotOnDecimal, facet);
			}
			if ((_baseFixedFlags & RestrictionFlags.FractionDigits) != 0 && _datatype.Restriction.FractionDigits != _derivedRestriction.FractionDigits)
			{
				throw new XmlSchemaException(System.SR.Sch_FacetBaseFixed, facet);
			}
			if ((_baseFlags & RestrictionFlags.FractionDigits) != 0 && _derivedRestriction.FractionDigits > _datatype.Restriction.FractionDigits)
			{
				throw new XmlSchemaException(System.SR.Sch_FractionDigitsMismatch, string.Empty);
			}
			SetFlag(facet, RestrictionFlags.FractionDigits);
		}

		internal void FinishFacetCompile()
		{
			if (_firstPattern)
			{
				return;
			}
			if (_derivedRestriction.Patterns == null)
			{
				_derivedRestriction.Patterns = new ArrayList();
			}
			try
			{
				_regStr.Append(')');
				string text = _regStr.ToString();
				if (text.Contains('|'))
				{
					_regStr.Insert(0, '(');
					_regStr.Append(')');
				}
				_derivedRestriction.Patterns.Add(new Regex(Preprocess(_regStr.ToString())));
			}
			catch (Exception ex)
			{
				throw new XmlSchemaException(System.SR.Sch_PatternFacetInvalid, new string[1] { ex.Message }, ex, _pattern_facet.SourceUri, _pattern_facet.LineNumber, _pattern_facet.LinePosition, _pattern_facet);
			}
		}

		private void CheckValue(object value, XmlSchemaFacet facet)
		{
			RestrictionFacets restriction = _datatype.Restriction;
			switch (facet.FacetType)
			{
			case FacetType.MaxInclusive:
				if ((_baseFlags & RestrictionFlags.MaxInclusive) != 0 && _datatype.Compare(value, restriction.MaxInclusive) > 0)
				{
					throw new XmlSchemaException(System.SR.Sch_MaxInclusiveMismatch, string.Empty);
				}
				if ((_baseFlags & RestrictionFlags.MaxExclusive) != 0 && _datatype.Compare(value, restriction.MaxExclusive) >= 0)
				{
					throw new XmlSchemaException(System.SR.Sch_MaxIncExlMismatch, string.Empty);
				}
				break;
			case FacetType.MaxExclusive:
				if ((_baseFlags & RestrictionFlags.MaxExclusive) != 0 && _datatype.Compare(value, restriction.MaxExclusive) > 0)
				{
					throw new XmlSchemaException(System.SR.Sch_MaxExclusiveMismatch, string.Empty);
				}
				if ((_baseFlags & RestrictionFlags.MaxInclusive) != 0 && _datatype.Compare(value, restriction.MaxInclusive) > 0)
				{
					throw new XmlSchemaException(System.SR.Sch_MaxExlIncMismatch, string.Empty);
				}
				break;
			case FacetType.MinInclusive:
				if ((_baseFlags & RestrictionFlags.MinInclusive) != 0 && _datatype.Compare(value, restriction.MinInclusive) < 0)
				{
					throw new XmlSchemaException(System.SR.Sch_MinInclusiveMismatch, string.Empty);
				}
				if ((_baseFlags & RestrictionFlags.MinExclusive) != 0 && _datatype.Compare(value, restriction.MinExclusive) < 0)
				{
					throw new XmlSchemaException(System.SR.Sch_MinIncExlMismatch, string.Empty);
				}
				if ((_baseFlags & RestrictionFlags.MaxExclusive) != 0 && _datatype.Compare(value, restriction.MaxExclusive) >= 0)
				{
					throw new XmlSchemaException(System.SR.Sch_MinIncMaxExlMismatch, string.Empty);
				}
				break;
			case FacetType.MinExclusive:
				if ((_baseFlags & RestrictionFlags.MinExclusive) != 0 && _datatype.Compare(value, restriction.MinExclusive) < 0)
				{
					throw new XmlSchemaException(System.SR.Sch_MinExclusiveMismatch, string.Empty);
				}
				if ((_baseFlags & RestrictionFlags.MinInclusive) != 0 && _datatype.Compare(value, restriction.MinInclusive) < 0)
				{
					throw new XmlSchemaException(System.SR.Sch_MinExlIncMismatch, string.Empty);
				}
				if ((_baseFlags & RestrictionFlags.MaxExclusive) != 0 && _datatype.Compare(value, restriction.MaxExclusive) >= 0)
				{
					throw new XmlSchemaException(System.SR.Sch_MinExlMaxExlMismatch, string.Empty);
				}
				break;
			}
		}

		internal void CompileFacetCombinations()
		{
			if ((_derivedRestriction.Flags & RestrictionFlags.MaxInclusive) != 0 && (_derivedRestriction.Flags & RestrictionFlags.MaxExclusive) != 0)
			{
				throw new XmlSchemaException(System.SR.Sch_MaxInclusiveExclusive, string.Empty);
			}
			if ((_derivedRestriction.Flags & RestrictionFlags.MinInclusive) != 0 && (_derivedRestriction.Flags & RestrictionFlags.MinExclusive) != 0)
			{
				throw new XmlSchemaException(System.SR.Sch_MinInclusiveExclusive, string.Empty);
			}
			if ((_derivedRestriction.Flags & RestrictionFlags.Length) != 0 && (_derivedRestriction.Flags & (RestrictionFlags.MinLength | RestrictionFlags.MaxLength)) != 0)
			{
				throw new XmlSchemaException(System.SR.Sch_LengthAndMinMax, string.Empty);
			}
			CopyFacetsFromBaseType();
			if ((_derivedRestriction.Flags & RestrictionFlags.MinLength) != 0 && (_derivedRestriction.Flags & RestrictionFlags.MaxLength) != 0 && _derivedRestriction.MinLength > _derivedRestriction.MaxLength)
			{
				throw new XmlSchemaException(System.SR.Sch_MinLengthGtMaxLength, string.Empty);
			}
			if ((_derivedRestriction.Flags & RestrictionFlags.MinInclusive) != 0 && (_derivedRestriction.Flags & RestrictionFlags.MaxInclusive) != 0 && _datatype.Compare(_derivedRestriction.MinInclusive, _derivedRestriction.MaxInclusive) > 0)
			{
				throw new XmlSchemaException(System.SR.Sch_MinInclusiveGtMaxInclusive, string.Empty);
			}
			if ((_derivedRestriction.Flags & RestrictionFlags.MinInclusive) != 0 && (_derivedRestriction.Flags & RestrictionFlags.MaxExclusive) != 0 && _datatype.Compare(_derivedRestriction.MinInclusive, _derivedRestriction.MaxExclusive) > 0)
			{
				throw new XmlSchemaException(System.SR.Sch_MinInclusiveGtMaxExclusive, string.Empty);
			}
			if ((_derivedRestriction.Flags & RestrictionFlags.MinExclusive) != 0 && (_derivedRestriction.Flags & RestrictionFlags.MaxExclusive) != 0 && _datatype.Compare(_derivedRestriction.MinExclusive, _derivedRestriction.MaxExclusive) > 0)
			{
				throw new XmlSchemaException(System.SR.Sch_MinExclusiveGtMaxExclusive, string.Empty);
			}
			if ((_derivedRestriction.Flags & RestrictionFlags.MinExclusive) != 0 && (_derivedRestriction.Flags & RestrictionFlags.MaxInclusive) != 0 && _datatype.Compare(_derivedRestriction.MinExclusive, _derivedRestriction.MaxInclusive) > 0)
			{
				throw new XmlSchemaException(System.SR.Sch_MinExclusiveGtMaxInclusive, string.Empty);
			}
			if ((_derivedRestriction.Flags & (RestrictionFlags.TotalDigits | RestrictionFlags.FractionDigits)) == (RestrictionFlags.TotalDigits | RestrictionFlags.FractionDigits) && _derivedRestriction.FractionDigits > _derivedRestriction.TotalDigits)
			{
				throw new XmlSchemaException(System.SR.Sch_FractionDigitsGtTotalDigits, string.Empty);
			}
		}

		private void CopyFacetsFromBaseType()
		{
			RestrictionFacets restriction = _datatype.Restriction;
			if ((_derivedRestriction.Flags & RestrictionFlags.Length) == 0 && (_baseFlags & RestrictionFlags.Length) != 0)
			{
				_derivedRestriction.Length = restriction.Length;
				SetFlag(RestrictionFlags.Length);
			}
			if ((_derivedRestriction.Flags & RestrictionFlags.MinLength) == 0 && (_baseFlags & RestrictionFlags.MinLength) != 0)
			{
				_derivedRestriction.MinLength = restriction.MinLength;
				SetFlag(RestrictionFlags.MinLength);
			}
			if ((_derivedRestriction.Flags & RestrictionFlags.MaxLength) == 0 && (_baseFlags & RestrictionFlags.MaxLength) != 0)
			{
				_derivedRestriction.MaxLength = restriction.MaxLength;
				SetFlag(RestrictionFlags.MaxLength);
			}
			if ((_baseFlags & RestrictionFlags.Pattern) != 0)
			{
				if (_derivedRestriction.Patterns == null)
				{
					_derivedRestriction.Patterns = restriction.Patterns;
				}
				else
				{
					_derivedRestriction.Patterns.AddRange(restriction.Patterns);
				}
				SetFlag(RestrictionFlags.Pattern);
			}
			if ((_baseFlags & RestrictionFlags.Enumeration) != 0)
			{
				if (_derivedRestriction.Enumeration == null)
				{
					_derivedRestriction.Enumeration = restriction.Enumeration;
				}
				SetFlag(RestrictionFlags.Enumeration);
			}
			if ((_derivedRestriction.Flags & RestrictionFlags.WhiteSpace) == 0 && (_baseFlags & RestrictionFlags.WhiteSpace) != 0)
			{
				_derivedRestriction.WhiteSpace = restriction.WhiteSpace;
				SetFlag(RestrictionFlags.WhiteSpace);
			}
			if ((_derivedRestriction.Flags & RestrictionFlags.MaxInclusive) == 0 && (_baseFlags & RestrictionFlags.MaxInclusive) != 0)
			{
				_derivedRestriction.MaxInclusive = restriction.MaxInclusive;
				SetFlag(RestrictionFlags.MaxInclusive);
			}
			if ((_derivedRestriction.Flags & RestrictionFlags.MaxExclusive) == 0 && (_baseFlags & RestrictionFlags.MaxExclusive) != 0)
			{
				_derivedRestriction.MaxExclusive = restriction.MaxExclusive;
				SetFlag(RestrictionFlags.MaxExclusive);
			}
			if ((_derivedRestriction.Flags & RestrictionFlags.MinInclusive) == 0 && (_baseFlags & RestrictionFlags.MinInclusive) != 0)
			{
				_derivedRestriction.MinInclusive = restriction.MinInclusive;
				SetFlag(RestrictionFlags.MinInclusive);
			}
			if ((_derivedRestriction.Flags & RestrictionFlags.MinExclusive) == 0 && (_baseFlags & RestrictionFlags.MinExclusive) != 0)
			{
				_derivedRestriction.MinExclusive = restriction.MinExclusive;
				SetFlag(RestrictionFlags.MinExclusive);
			}
			if ((_derivedRestriction.Flags & RestrictionFlags.TotalDigits) == 0 && (_baseFlags & RestrictionFlags.TotalDigits) != 0)
			{
				_derivedRestriction.TotalDigits = restriction.TotalDigits;
				SetFlag(RestrictionFlags.TotalDigits);
			}
			if ((_derivedRestriction.Flags & RestrictionFlags.FractionDigits) == 0 && (_baseFlags & RestrictionFlags.FractionDigits) != 0)
			{
				_derivedRestriction.FractionDigits = restriction.FractionDigits;
				SetFlag(RestrictionFlags.FractionDigits);
			}
		}

		private object ParseFacetValue(XmlSchemaDatatype datatype, XmlSchemaFacet facet, string code, IXmlNamespaceResolver nsmgr, XmlNameTable nameTable)
		{
			object typedValue;
			Exception ex = datatype.TryParseValue(facet.Value, nameTable, nsmgr, out typedValue);
			if (ex == null)
			{
				return typedValue;
			}
			throw new XmlSchemaException(code, new string[1] { ex.Message }, ex, facet.SourceUri, facet.LineNumber, facet.LinePosition, facet);
		}

		private static string Preprocess(string pattern)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append('^');
			char[] array = pattern.ToCharArray();
			int length = pattern.Length;
			int num = 0;
			for (int i = 0; i < length - 2; i++)
			{
				if (array[i] != '\\')
				{
					continue;
				}
				if (array[i + 1] == '\\')
				{
					i++;
					continue;
				}
				char c = array[i + 1];
				for (int j = 0; j < s_map.Length; j++)
				{
					if (s_map[j].match == c)
					{
						if (num < i)
						{
							stringBuilder.Append(array, num, i - num);
						}
						stringBuilder.Append(s_map[j].replacement);
						i++;
						num = i + 1;
						break;
					}
				}
			}
			if (num < length)
			{
				stringBuilder.Append(array, num, length - num);
			}
			stringBuilder.Append('$');
			return stringBuilder.ToString();
		}

		private void CheckProhibitedFlag(XmlSchemaFacet facet, RestrictionFlags flag, string errorCode)
		{
			if ((_validRestrictionFlags & flag) == 0)
			{
				throw new XmlSchemaException(errorCode, _datatype.TypeCodeString, facet);
			}
		}

		private void CheckDupFlag(XmlSchemaFacet facet, RestrictionFlags flag, string errorCode)
		{
			if ((_derivedRestriction.Flags & flag) != 0)
			{
				throw new XmlSchemaException(errorCode, facet);
			}
		}

		private void SetFlag(XmlSchemaFacet facet, RestrictionFlags flag)
		{
			_derivedRestriction.Flags |= flag;
			if (facet.IsFixed)
			{
				_derivedRestriction.FixedFlags |= flag;
			}
		}

		private void SetFlag(RestrictionFlags flag)
		{
			_derivedRestriction.Flags |= flag;
			if ((_baseFixedFlags & flag) != 0)
			{
				_derivedRestriction.FixedFlags |= flag;
			}
		}
	}

	internal virtual Exception CheckLexicalFacets(ref string parseString, XmlSchemaDatatype datatype)
	{
		CheckWhitespaceFacets(ref parseString, datatype);
		return CheckPatternFacets(datatype.Restriction, parseString);
	}

	internal virtual Exception CheckValueFacets(object value, XmlSchemaDatatype datatype)
	{
		return null;
	}

	internal virtual Exception CheckValueFacets(decimal value, XmlSchemaDatatype datatype)
	{
		return null;
	}

	internal virtual Exception CheckValueFacets(long value, XmlSchemaDatatype datatype)
	{
		return null;
	}

	internal virtual Exception CheckValueFacets(int value, XmlSchemaDatatype datatype)
	{
		return null;
	}

	internal virtual Exception CheckValueFacets(short value, XmlSchemaDatatype datatype)
	{
		return null;
	}

	internal virtual Exception CheckValueFacets(DateTime value, XmlSchemaDatatype datatype)
	{
		return null;
	}

	internal virtual Exception CheckValueFacets(double value, XmlSchemaDatatype datatype)
	{
		return null;
	}

	internal virtual Exception CheckValueFacets(float value, XmlSchemaDatatype datatype)
	{
		return null;
	}

	internal virtual Exception CheckValueFacets(string value, XmlSchemaDatatype datatype)
	{
		return null;
	}

	internal virtual Exception CheckValueFacets(byte[] value, XmlSchemaDatatype datatype)
	{
		return null;
	}

	internal virtual Exception CheckValueFacets(TimeSpan value, XmlSchemaDatatype datatype)
	{
		return null;
	}

	internal virtual Exception CheckValueFacets(XmlQualifiedName value, XmlSchemaDatatype datatype)
	{
		return null;
	}

	internal void CheckWhitespaceFacets(ref string s, XmlSchemaDatatype datatype)
	{
		RestrictionFacets restriction = datatype.Restriction;
		switch (datatype.Variety)
		{
		case XmlSchemaDatatypeVariety.List:
			s = s.Trim();
			break;
		case XmlSchemaDatatypeVariety.Atomic:
			if (datatype.BuiltInWhitespaceFacet == XmlSchemaWhiteSpace.Collapse)
			{
				s = XmlComplianceUtil.NonCDataNormalize(s);
			}
			else if (datatype.BuiltInWhitespaceFacet == XmlSchemaWhiteSpace.Replace)
			{
				s = XmlComplianceUtil.CDataNormalize(s);
			}
			else if (restriction != null && (restriction.Flags & RestrictionFlags.WhiteSpace) != 0)
			{
				if (restriction.WhiteSpace == XmlSchemaWhiteSpace.Replace)
				{
					s = XmlComplianceUtil.CDataNormalize(s);
				}
				else if (restriction.WhiteSpace == XmlSchemaWhiteSpace.Collapse)
				{
					s = XmlComplianceUtil.NonCDataNormalize(s);
				}
			}
			break;
		}
	}

	internal Exception CheckPatternFacets(RestrictionFacets restriction, string value)
	{
		if (restriction != null && (restriction.Flags & RestrictionFlags.Pattern) != 0)
		{
			for (int i = 0; i < restriction.Patterns.Count; i++)
			{
				Regex regex = (Regex)restriction.Patterns[i];
				if (!regex.IsMatch(value))
				{
					return new XmlSchemaException(System.SR.Sch_PatternConstraintFailed, string.Empty);
				}
			}
		}
		return null;
	}

	internal virtual bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
	{
		return false;
	}

	internal virtual RestrictionFacets ConstructRestriction(DatatypeImplementation datatype, XmlSchemaObjectCollection facets, XmlNameTable nameTable)
	{
		RestrictionFacets restrictionFacets = new RestrictionFacets();
		FacetsCompiler facetsCompiler = new FacetsCompiler(datatype, restrictionFacets);
		for (int i = 0; i < facets.Count; i++)
		{
			XmlSchemaFacet xmlSchemaFacet = (XmlSchemaFacet)facets[i];
			if (xmlSchemaFacet.Value == null)
			{
				throw new XmlSchemaException(System.SR.Sch_InvalidFacet, xmlSchemaFacet);
			}
			IXmlNamespaceResolver nsmgr = new SchemaNamespaceManager(xmlSchemaFacet);
			switch (xmlSchemaFacet.FacetType)
			{
			case FacetType.Length:
				facetsCompiler.CompileLengthFacet(xmlSchemaFacet);
				break;
			case FacetType.MinLength:
				facetsCompiler.CompileMinLengthFacet(xmlSchemaFacet);
				break;
			case FacetType.MaxLength:
				facetsCompiler.CompileMaxLengthFacet(xmlSchemaFacet);
				break;
			case FacetType.Pattern:
				facetsCompiler.CompilePatternFacet(xmlSchemaFacet as XmlSchemaPatternFacet);
				break;
			case FacetType.Enumeration:
				facetsCompiler.CompileEnumerationFacet(xmlSchemaFacet, nsmgr, nameTable);
				break;
			case FacetType.Whitespace:
				facetsCompiler.CompileWhitespaceFacet(xmlSchemaFacet);
				break;
			case FacetType.MinInclusive:
				facetsCompiler.CompileMinInclusiveFacet(xmlSchemaFacet);
				break;
			case FacetType.MinExclusive:
				facetsCompiler.CompileMinExclusiveFacet(xmlSchemaFacet);
				break;
			case FacetType.MaxInclusive:
				facetsCompiler.CompileMaxInclusiveFacet(xmlSchemaFacet);
				break;
			case FacetType.MaxExclusive:
				facetsCompiler.CompileMaxExclusiveFacet(xmlSchemaFacet);
				break;
			case FacetType.TotalDigits:
				facetsCompiler.CompileTotalDigitsFacet(xmlSchemaFacet);
				break;
			case FacetType.FractionDigits:
				facetsCompiler.CompileFractionDigitsFacet(xmlSchemaFacet);
				break;
			default:
				throw new XmlSchemaException(System.SR.Sch_UnknownFacet, xmlSchemaFacet);
			}
		}
		facetsCompiler.FinishFacetCompile();
		facetsCompiler.CompileFacetCombinations();
		return restrictionFacets;
	}

	internal static decimal Power(int x, int y)
	{
		decimal result = 1m;
		decimal num = x;
		if (y > 28)
		{
			return decimal.MaxValue;
		}
		for (int i = 0; i < y; i++)
		{
			result *= num;
		}
		return result;
	}
}
