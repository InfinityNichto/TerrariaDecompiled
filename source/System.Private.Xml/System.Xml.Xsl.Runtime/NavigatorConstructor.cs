using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

internal sealed class NavigatorConstructor
{
	private object _cache;

	public XPathNavigator GetNavigator(XmlEventCache events, XmlNameTable nameTable)
	{
		if (_cache == null)
		{
			XPathDocument xPathDocument = new XPathDocument(nameTable);
			XmlRawWriter xmlRawWriter = xPathDocument.LoadFromWriter(XPathDocument.LoadFlags.AtomizeNames | ((!events.HasRootNode) ? XPathDocument.LoadFlags.Fragment : XPathDocument.LoadFlags.None), events.BaseUri);
			events.EventsToWriter(xmlRawWriter);
			xmlRawWriter.Close();
			_cache = xPathDocument;
		}
		return ((XPathDocument)_cache).CreateNavigator();
	}

	public XPathNavigator GetNavigator(string text, string baseUri, XmlNameTable nameTable)
	{
		if (_cache == null)
		{
			XPathDocument xPathDocument = new XPathDocument(nameTable);
			XmlRawWriter xmlRawWriter = xPathDocument.LoadFromWriter(XPathDocument.LoadFlags.AtomizeNames, baseUri);
			xmlRawWriter.WriteString(text);
			xmlRawWriter.Close();
			_cache = xPathDocument;
		}
		return ((XPathDocument)_cache).CreateNavigator();
	}
}
