namespace System.Xml;

internal class PositionInfo : IXmlLineInfo
{
	public virtual int LineNumber => 0;

	public virtual int LinePosition => 0;

	public virtual bool HasLineInfo()
	{
		return false;
	}

	public static PositionInfo GetPositionInfo(object o)
	{
		if (o is IXmlLineInfo lineInfo)
		{
			return new ReaderPositionInfo(lineInfo);
		}
		return new PositionInfo();
	}
}
