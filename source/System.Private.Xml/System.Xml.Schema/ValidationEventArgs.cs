namespace System.Xml.Schema;

public class ValidationEventArgs : EventArgs
{
	private readonly XmlSchemaException _ex;

	private readonly XmlSeverityType _severity;

	public XmlSeverityType Severity => _severity;

	public XmlSchemaException Exception => _ex;

	public string Message => _ex.Message;

	internal ValidationEventArgs(XmlSchemaException ex)
	{
		_ex = ex;
		_severity = XmlSeverityType.Error;
	}

	internal ValidationEventArgs(XmlSchemaException ex, XmlSeverityType severity)
	{
		_ex = ex;
		_severity = severity;
	}
}
