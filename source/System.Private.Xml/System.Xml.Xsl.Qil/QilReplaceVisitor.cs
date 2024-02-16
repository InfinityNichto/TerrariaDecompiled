namespace System.Xml.Xsl.Qil;

internal abstract class QilReplaceVisitor : QilVisitor
{
	protected QilFactory f;

	public QilReplaceVisitor(QilFactory f)
	{
		this.f = f;
	}

	protected override QilNode VisitChildren(QilNode parent)
	{
		XmlQueryType xmlType = parent.XmlType;
		bool flag = false;
		for (int i = 0; i < parent.Count; i++)
		{
			QilNode qilNode = parent[i];
			XmlQueryType xmlQueryType = qilNode?.XmlType;
			QilNode qilNode2 = ((!IsReference(parent, i)) ? Visit(qilNode) : VisitReference(qilNode));
			if (qilNode != qilNode2 || (qilNode2 != null && (object)xmlQueryType != qilNode2.XmlType))
			{
				flag = true;
				parent[i] = qilNode2;
			}
		}
		if (flag)
		{
			RecalculateType(parent, xmlType);
		}
		return parent;
	}

	protected virtual void RecalculateType(QilNode node, XmlQueryType oldType)
	{
		XmlQueryType xmlType = f.TypeChecker.Check(node);
		node.XmlType = xmlType;
	}
}
