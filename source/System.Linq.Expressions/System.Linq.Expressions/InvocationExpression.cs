using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;

namespace System.Linq.Expressions;

[DebuggerTypeProxy(typeof(InvocationExpressionProxy))]
public class InvocationExpression : Expression, IArgumentProvider
{
	public sealed override Type Type { get; }

	public sealed override ExpressionType NodeType => ExpressionType.Invoke;

	public Expression Expression { get; }

	public ReadOnlyCollection<Expression> Arguments => GetOrMakeArguments();

	[ExcludeFromCodeCoverage(Justification = "Unreachable")]
	public virtual int ArgumentCount
	{
		get
		{
			throw ContractUtils.Unreachable;
		}
	}

	internal LambdaExpression? LambdaOperand
	{
		get
		{
			if (Expression.NodeType != ExpressionType.Quote)
			{
				return Expression as LambdaExpression;
			}
			return (LambdaExpression)((UnaryExpression)Expression).Operand;
		}
	}

	internal InvocationExpression(Expression expression, Type returnType)
	{
		Expression = expression;
		Type = returnType;
	}

	public InvocationExpression Update(Expression expression, IEnumerable<Expression>? arguments)
	{
		if (expression == Expression && arguments != null && ExpressionUtils.SameElements(ref arguments, Arguments))
		{
			return this;
		}
		return System.Linq.Expressions.Expression.Invoke(expression, arguments);
	}

	[ExcludeFromCodeCoverage(Justification = "Unreachable")]
	internal virtual ReadOnlyCollection<Expression> GetOrMakeArguments()
	{
		throw ContractUtils.Unreachable;
	}

	[ExcludeFromCodeCoverage(Justification = "Unreachable")]
	public virtual Expression GetArgument(int index)
	{
		throw ContractUtils.Unreachable;
	}

	protected internal override Expression Accept(ExpressionVisitor visitor)
	{
		return visitor.VisitInvocation(this);
	}

	[ExcludeFromCodeCoverage(Justification = "Unreachable")]
	internal virtual InvocationExpression Rewrite(Expression lambda, Expression[] arguments)
	{
		throw ContractUtils.Unreachable;
	}
}
