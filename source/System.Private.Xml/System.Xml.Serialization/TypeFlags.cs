namespace System.Xml.Serialization;

internal enum TypeFlags
{
	None = 0,
	Abstract = 1,
	Reference = 2,
	Special = 4,
	CanBeAttributeValue = 8,
	CanBeTextValue = 0x10,
	CanBeElementValue = 0x20,
	HasCustomFormatter = 0x40,
	AmbiguousDataType = 0x80,
	IgnoreDefault = 0x200,
	HasIsEmpty = 0x400,
	HasDefaultConstructor = 0x800,
	XmlEncodingNotRequired = 0x1000,
	UseReflection = 0x4000,
	CollapseWhitespace = 0x8000,
	OptionalValue = 0x10000,
	CtorInaccessible = 0x20000,
	UsePrivateImplementation = 0x40000,
	GenericInterface = 0x80000,
	Unsupported = 0x100000
}
