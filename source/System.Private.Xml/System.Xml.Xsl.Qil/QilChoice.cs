namespace System.Xml.Xsl.Qil;

internal sealed class QilChoice : QilBinary
{
	public QilNode Expression => base.Left;

	public QilList Branches => (QilList)base.Right;

	public QilChoice(QilNodeType nodeType, QilNode expression, QilNode branches)
		: base(nodeType, expression, branches)
	{
	}
}
