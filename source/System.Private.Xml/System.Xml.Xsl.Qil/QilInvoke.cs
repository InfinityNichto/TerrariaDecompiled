namespace System.Xml.Xsl.Qil;

internal sealed class QilInvoke : QilBinary
{
	public QilFunction Function => (QilFunction)base.Left;

	public QilList Arguments => (QilList)base.Right;

	public QilInvoke(QilNodeType nodeType, QilNode function, QilNode arguments)
		: base(nodeType, function, arguments)
	{
	}
}
