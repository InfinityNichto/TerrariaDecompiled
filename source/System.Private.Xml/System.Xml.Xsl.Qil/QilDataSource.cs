namespace System.Xml.Xsl.Qil;

internal sealed class QilDataSource : QilBinary
{
	public QilNode Name => base.Left;

	public QilNode BaseUri => base.Right;

	public QilDataSource(QilNodeType nodeType, QilNode name, QilNode baseUri)
		: base(nodeType, name, baseUri)
	{
	}
}
