using System.Reflection;

namespace System.Xml.Xsl.Qil;

internal sealed class QilInvokeEarlyBound : QilTernary
{
	public QilName Name => (QilName)base.Left;

	public MethodInfo ClrMethod => (MethodInfo)((QilLiteral)base.Center).Value;

	public QilList Arguments => (QilList)base.Right;

	public QilInvokeEarlyBound(QilNodeType nodeType, QilNode name, QilNode method, QilNode arguments, XmlQueryType resultType)
		: base(nodeType, name, method, arguments)
	{
		xmlType = resultType;
	}
}
