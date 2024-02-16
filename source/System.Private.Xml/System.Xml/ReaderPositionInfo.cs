namespace System.Xml;

internal sealed class ReaderPositionInfo : PositionInfo
{
	private readonly IXmlLineInfo _lineInfo;

	public override int LineNumber => _lineInfo.LineNumber;

	public override int LinePosition => _lineInfo.LinePosition;

	public ReaderPositionInfo(IXmlLineInfo lineInfo)
	{
		_lineInfo = lineInfo;
	}

	public override bool HasLineInfo()
	{
		return _lineInfo.HasLineInfo();
	}
}
