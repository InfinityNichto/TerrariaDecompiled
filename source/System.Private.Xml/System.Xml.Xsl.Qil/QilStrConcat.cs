namespace System.Xml.Xsl.Qil;

internal sealed class QilStrConcat : QilBinary
{
	public QilNode Delimiter => base.Left;

	public QilNode Values => base.Right;

	public QilStrConcat(QilNodeType nodeType, QilNode delimiter, QilNode values)
		: base(nodeType, delimiter, values)
	{
	}
}
