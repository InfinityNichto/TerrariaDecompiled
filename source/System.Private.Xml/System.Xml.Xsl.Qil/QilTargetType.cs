namespace System.Xml.Xsl.Qil;

internal sealed class QilTargetType : QilBinary
{
	public QilNode Source => base.Left;

	public XmlQueryType TargetType => (XmlQueryType)((QilLiteral)base.Right).Value;

	public QilTargetType(QilNodeType nodeType, QilNode expr, QilNode targetType)
		: base(nodeType, expr, targetType)
	{
	}
}
