namespace System.Xml.Xsl;

[Flags]
internal enum XmlNodeKindFlags
{
	None = 0,
	Document = 1,
	Element = 2,
	Attribute = 4,
	Text = 8,
	Comment = 0x10,
	PI = 0x20,
	Namespace = 0x40,
	Content = 0x3A,
	Any = 0x7F
}
