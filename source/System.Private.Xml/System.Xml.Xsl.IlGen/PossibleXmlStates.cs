namespace System.Xml.Xsl.IlGen;

internal enum PossibleXmlStates
{
	None,
	WithinSequence,
	EnumAttrs,
	WithinContent,
	WithinAttr,
	WithinComment,
	WithinPI,
	Any
}
