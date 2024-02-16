namespace System.Xml.Xsl.Runtime;

internal sealed class XmlILQueryEventArgs : XsltMessageEncounteredEventArgs
{
	private readonly string _message;

	public override string Message => _message;

	public XmlILQueryEventArgs(string message)
	{
		_message = message;
	}
}
