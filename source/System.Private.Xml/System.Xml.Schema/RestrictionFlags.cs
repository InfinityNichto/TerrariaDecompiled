namespace System.Xml.Schema;

[Flags]
internal enum RestrictionFlags
{
	Length = 1,
	MinLength = 2,
	MaxLength = 4,
	Pattern = 8,
	Enumeration = 0x10,
	WhiteSpace = 0x20,
	MaxInclusive = 0x40,
	MaxExclusive = 0x80,
	MinInclusive = 0x100,
	MinExclusive = 0x200,
	TotalDigits = 0x400,
	FractionDigits = 0x800
}
