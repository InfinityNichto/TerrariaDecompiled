namespace System.Xml.Schema;

internal enum ValidatorState
{
	None,
	Start,
	TopLevelAttribute,
	TopLevelTextOrWS,
	Element,
	Attribute,
	EndOfAttributes,
	Text,
	Whitespace,
	EndElement,
	SkipToEndElement,
	Finish
}
