using System.Collections;

namespace System.Xml.Schema;

internal sealed class RestrictionFacets
{
	internal int Length;

	internal int MinLength;

	internal int MaxLength;

	internal ArrayList Patterns;

	internal ArrayList Enumeration;

	internal XmlSchemaWhiteSpace WhiteSpace;

	internal object MaxInclusive;

	internal object MaxExclusive;

	internal object MinInclusive;

	internal object MinExclusive;

	internal int TotalDigits;

	internal int FractionDigits;

	internal RestrictionFlags Flags;

	internal RestrictionFlags FixedFlags;
}
