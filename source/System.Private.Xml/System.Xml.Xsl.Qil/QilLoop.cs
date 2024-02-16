namespace System.Xml.Xsl.Qil;

internal sealed class QilLoop : QilBinary
{
	public QilIterator Variable => (QilIterator)base.Left;

	public QilNode Body
	{
		get
		{
			return base.Right;
		}
		set
		{
			base.Right = value;
		}
	}

	public QilLoop(QilNodeType nodeType, QilNode variable, QilNode body)
		: base(nodeType, variable, body)
	{
	}
}
