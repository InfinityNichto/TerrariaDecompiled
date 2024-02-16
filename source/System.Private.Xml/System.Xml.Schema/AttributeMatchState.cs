namespace System.Xml.Schema;

internal enum AttributeMatchState
{
	AttributeFound,
	AnyIdAttributeFound,
	UndeclaredElementAndAttribute,
	UndeclaredAttribute,
	AnyAttributeLax,
	AnyAttributeSkip,
	ProhibitedAnyAttribute,
	ProhibitedAttribute,
	AttributeNameMismatch,
	ValidateAttributeInvalidCall
}
