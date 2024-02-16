namespace System.Xml.Xsl.XsltOld;

internal sealed class OutKeywords
{
	private readonly string _AtomEmpty;

	private readonly string _AtomLang;

	private readonly string _AtomSpace;

	private readonly string _AtomXmlns;

	private readonly string _AtomXml;

	private readonly string _AtomXmlNamespace;

	private readonly string _AtomXmlnsNamespace;

	internal string Empty => _AtomEmpty;

	internal string Lang => _AtomLang;

	internal string Space => _AtomSpace;

	internal string Xmlns => _AtomXmlns;

	internal string Xml => _AtomXml;

	internal string XmlNamespace => _AtomXmlNamespace;

	internal string XmlnsNamespace => _AtomXmlnsNamespace;

	internal OutKeywords(XmlNameTable nameTable)
	{
		_AtomEmpty = nameTable.Add(string.Empty);
		_AtomLang = nameTable.Add("lang");
		_AtomSpace = nameTable.Add("space");
		_AtomXmlns = nameTable.Add("xmlns");
		_AtomXml = nameTable.Add("xml");
		_AtomXmlNamespace = nameTable.Add("http://www.w3.org/XML/1998/namespace");
		_AtomXmlnsNamespace = nameTable.Add("http://www.w3.org/2000/xmlns/");
	}
}
