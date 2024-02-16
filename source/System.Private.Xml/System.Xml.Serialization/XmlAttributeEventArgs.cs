namespace System.Xml.Serialization;

public class XmlAttributeEventArgs : EventArgs
{
	private readonly object _o;

	private readonly XmlAttribute _attr;

	private readonly string _qnames;

	private readonly int _lineNumber;

	private readonly int _linePosition;

	public object? ObjectBeingDeserialized => _o;

	public XmlAttribute Attr => _attr;

	public int LineNumber => _lineNumber;

	public int LinePosition => _linePosition;

	public string ExpectedAttributes
	{
		get
		{
			if (_qnames != null)
			{
				return _qnames;
			}
			return string.Empty;
		}
	}

	internal XmlAttributeEventArgs(XmlAttribute attr, int lineNumber, int linePosition, object o, string qnames)
	{
		_attr = attr;
		_o = o;
		_qnames = qnames;
		_lineNumber = lineNumber;
		_linePosition = linePosition;
	}
}
