namespace System.Reflection.Metadata.Ecma335;

[Flags]
internal enum CustomAttributeValueTreatment : byte
{
	None = 0,
	AttributeUsageAllowSingle = 1,
	AttributeUsageAllowMultiple = 2,
	AttributeUsageVersionAttribute = 3,
	AttributeUsageDeprecatedAttribute = 4
}
