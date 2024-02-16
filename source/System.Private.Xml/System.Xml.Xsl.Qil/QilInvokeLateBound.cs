namespace System.Xml.Xsl.Qil;

internal sealed class QilInvokeLateBound : QilBinary
{
	public QilName Name => (QilName)base.Left;

	public QilList Arguments => (QilList)base.Right;

	public QilInvokeLateBound(QilNodeType nodeType, QilNode name, QilNode arguments)
		: base(nodeType, name, arguments)
	{
	}
}
