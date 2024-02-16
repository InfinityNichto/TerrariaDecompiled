namespace System.Xml.Xsl.Qil;

internal class QilCloneVisitor : QilScopedVisitor
{
	private readonly QilFactory _fac;

	private readonly SubstitutionList _subs;

	public QilCloneVisitor(QilFactory fac)
		: this(fac, new SubstitutionList())
	{
	}

	public QilCloneVisitor(QilFactory fac, SubstitutionList subs)
	{
		_fac = fac;
		_subs = subs;
	}

	public QilNode Clone(QilNode node)
	{
		QilDepthChecker.Check(node);
		return VisitAssumeReference(node);
	}

	protected override QilNode Visit(QilNode oldNode)
	{
		QilNode qilNode = null;
		if (oldNode == null)
		{
			return null;
		}
		if (oldNode is QilReference)
		{
			qilNode = FindClonedReference(oldNode);
		}
		if (qilNode == null)
		{
			qilNode = oldNode.ShallowClone(_fac);
		}
		return base.Visit(qilNode);
	}

	protected override QilNode VisitChildren(QilNode parent)
	{
		for (int i = 0; i < parent.Count; i++)
		{
			QilNode qilNode = parent[i];
			if (IsReference(parent, i))
			{
				parent[i] = VisitReference(qilNode);
				if (parent[i] == null)
				{
					parent[i] = qilNode;
				}
			}
			else
			{
				parent[i] = Visit(qilNode);
			}
		}
		return parent;
	}

	protected override QilNode VisitReference(QilNode oldNode)
	{
		QilNode qilNode = FindClonedReference(oldNode);
		return base.VisitReference((qilNode == null) ? oldNode : qilNode);
	}

	protected override void BeginScope(QilNode node)
	{
		_subs.AddSubstitutionPair(node, node.ShallowClone(_fac));
	}

	protected override void EndScope(QilNode node)
	{
		_subs.RemoveLastSubstitutionPair();
	}

	protected QilNode FindClonedReference(QilNode node)
	{
		return _subs.FindReplacement(node);
	}
}
