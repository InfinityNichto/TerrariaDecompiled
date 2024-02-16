namespace System.Xml.Xsl.Qil;

internal sealed class QilSortKey : QilBinary
{
	public QilNode Key => base.Left;

	public QilNode Collation => base.Right;

	public QilSortKey(QilNodeType nodeType, QilNode key, QilNode collation)
		: base(nodeType, key, collation)
	{
	}
}
