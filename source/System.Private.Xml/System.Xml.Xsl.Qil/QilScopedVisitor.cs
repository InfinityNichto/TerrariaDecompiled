namespace System.Xml.Xsl.Qil;

internal class QilScopedVisitor : QilVisitor
{
	protected virtual void BeginScope(QilNode node)
	{
	}

	protected virtual void EndScope(QilNode node)
	{
	}

	protected virtual void BeforeVisit(QilNode node)
	{
		switch (node.NodeType)
		{
		case QilNodeType.QilExpression:
		{
			QilExpression qilExpression = (QilExpression)node;
			foreach (QilNode globalParameter in qilExpression.GlobalParameterList)
			{
				BeginScope(globalParameter);
			}
			foreach (QilNode globalVariable in qilExpression.GlobalVariableList)
			{
				BeginScope(globalVariable);
			}
			{
				foreach (QilNode function in qilExpression.FunctionList)
				{
					BeginScope(function);
				}
				break;
			}
		}
		case QilNodeType.Function:
		{
			foreach (QilNode argument in ((QilFunction)node).Arguments)
			{
				BeginScope(argument);
			}
			break;
		}
		case QilNodeType.Loop:
		case QilNodeType.Filter:
		case QilNodeType.Sort:
			BeginScope(((QilLoop)node).Variable);
			break;
		}
	}

	protected virtual void AfterVisit(QilNode node)
	{
		switch (node.NodeType)
		{
		case QilNodeType.QilExpression:
		{
			QilExpression qilExpression = (QilExpression)node;
			foreach (QilNode function in qilExpression.FunctionList)
			{
				EndScope(function);
			}
			foreach (QilNode globalVariable in qilExpression.GlobalVariableList)
			{
				EndScope(globalVariable);
			}
			{
				foreach (QilNode globalParameter in qilExpression.GlobalParameterList)
				{
					EndScope(globalParameter);
				}
				break;
			}
		}
		case QilNodeType.Function:
		{
			foreach (QilNode argument in ((QilFunction)node).Arguments)
			{
				EndScope(argument);
			}
			break;
		}
		case QilNodeType.Loop:
		case QilNodeType.Filter:
		case QilNodeType.Sort:
			EndScope(((QilLoop)node).Variable);
			break;
		}
	}

	protected override QilNode Visit(QilNode n)
	{
		BeforeVisit(n);
		QilNode result = base.Visit(n);
		AfterVisit(n);
		return result;
	}
}
