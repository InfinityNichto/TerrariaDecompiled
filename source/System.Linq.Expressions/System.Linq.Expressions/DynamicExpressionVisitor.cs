using System.Dynamic.Utils;

namespace System.Linq.Expressions;

public class DynamicExpressionVisitor : ExpressionVisitor
{
	protected internal override Expression VisitDynamic(DynamicExpression node)
	{
		Expression[] array = ExpressionVisitorUtils.VisitArguments(this, node);
		if (array == null)
		{
			return node;
		}
		return node.Rewrite(array);
	}
}
