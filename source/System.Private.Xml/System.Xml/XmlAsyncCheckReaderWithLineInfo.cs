namespace System.Xml;

internal class XmlAsyncCheckReaderWithLineInfo : XmlAsyncCheckReader, IXmlLineInfo
{
	private readonly IXmlLineInfo _readerAsIXmlLineInfo;

	public virtual int LineNumber => _readerAsIXmlLineInfo.LineNumber;

	public virtual int LinePosition => _readerAsIXmlLineInfo.LinePosition;

	public XmlAsyncCheckReaderWithLineInfo(XmlReader reader)
		: base(reader)
	{
		_readerAsIXmlLineInfo = (IXmlLineInfo)reader;
	}

	public virtual bool HasLineInfo()
	{
		return _readerAsIXmlLineInfo.HasLineInfo();
	}
}
