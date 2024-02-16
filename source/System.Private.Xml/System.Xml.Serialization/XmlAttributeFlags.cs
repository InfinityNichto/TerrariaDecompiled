namespace System.Xml.Serialization;

internal enum XmlAttributeFlags
{
	Enum = 1,
	Array = 2,
	Text = 4,
	ArrayItems = 8,
	Elements = 0x10,
	Attribute = 0x20,
	Root = 0x40,
	Type = 0x80,
	AnyElements = 0x100,
	AnyAttribute = 0x200,
	ChoiceIdentifier = 0x400,
	XmlnsDeclarations = 0x800
}
