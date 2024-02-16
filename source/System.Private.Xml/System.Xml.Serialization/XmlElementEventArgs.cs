namespace System.Xml.Serialization;

public class XmlElementEventArgs : EventArgs
{
	private readonly object _o;

	private readonly XmlElement _elem;

	private readonly string _qnames;

	private readonly int _lineNumber;

	private readonly int _linePosition;

	public object? ObjectBeingDeserialized => _o;

	public XmlElement Element => _elem;

	public int LineNumber => _lineNumber;

	public int LinePosition => _linePosition;

	public string ExpectedElements
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

	internal XmlElementEventArgs(XmlElement elem, int lineNumber, int linePosition, object o, string qnames)
	{
		_elem = elem;
		_o = o;
		_qnames = qnames;
		_lineNumber = lineNumber;
		_linePosition = linePosition;
	}
}
