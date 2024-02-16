using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

internal sealed class XmlCachedSequenceWriter : XmlSequenceWriter
{
	private readonly XmlQueryItemSequence _seqTyped;

	private XPathDocument _doc;

	private XmlRawWriter _writer;

	public XmlQueryItemSequence ResultSequence => _seqTyped;

	public XmlCachedSequenceWriter()
	{
		_seqTyped = new XmlQueryItemSequence();
	}

	public override XmlRawWriter StartTree(XPathNodeType rootType, IXmlNamespaceResolver nsResolver, XmlNameTable nameTable)
	{
		_doc = new XPathDocument(nameTable);
		_writer = _doc.LoadFromWriter(XPathDocument.LoadFlags.AtomizeNames | ((rootType != 0) ? XPathDocument.LoadFlags.Fragment : XPathDocument.LoadFlags.None), string.Empty);
		_writer.NamespaceResolver = nsResolver;
		return _writer;
	}

	public override void EndTree()
	{
		_writer.Close();
		_seqTyped.Add(_doc.CreateNavigator());
	}

	public override void WriteItem(XPathItem item)
	{
		_seqTyped.AddClone(item);
	}
}
