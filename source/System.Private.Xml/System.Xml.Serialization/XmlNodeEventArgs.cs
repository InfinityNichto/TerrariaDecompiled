namespace System.Xml.Serialization;

public class XmlNodeEventArgs : EventArgs
{
	private readonly object _o;

	private readonly XmlNode _xmlNode;

	private readonly int _lineNumber;

	private readonly int _linePosition;

	public object? ObjectBeingDeserialized => _o;

	public XmlNodeType NodeType => _xmlNode.NodeType;

	public string Name => _xmlNode.Name;

	public string LocalName => _xmlNode.LocalName;

	public string NamespaceURI => _xmlNode.NamespaceURI;

	public string? Text => _xmlNode.Value;

	public int LineNumber => _lineNumber;

	public int LinePosition => _linePosition;

	internal XmlNodeEventArgs(XmlNode xmlNode, int lineNumber, int linePosition, object o)
	{
		_o = o;
		_xmlNode = xmlNode;
		_lineNumber = lineNumber;
		_linePosition = linePosition;
	}
}
