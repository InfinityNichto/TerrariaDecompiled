using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Threading;

namespace System.Linq.Expressions;

[DebuggerTypeProxy(typeof(BlockExpressionProxy))]
public class BlockExpression : Expression
{
	public ReadOnlyCollection<Expression> Expressions => GetOrMakeExpressions();

	public ReadOnlyCollection<ParameterExpression> Variables => GetOrMakeVariables();

	public Expression Result => GetExpression(ExpressionCount - 1);

	public sealed override ExpressionType NodeType => ExpressionType.Block;

	public override Type Type => GetExpression(ExpressionCount - 1).Type;

	[ExcludeFromCodeCoverage(Justification = "Unreachable")]
	internal virtual int ExpressionCount
	{
		get
		{
			throw ContractUtils.Unreachable;
		}
	}

	internal BlockExpression()
	{
	}

	protected internal override Expression Accept(ExpressionVisitor visitor)
	{
		return visitor.VisitBlock(this);
	}

	public BlockExpression Update(IEnumerable<ParameterExpression>? variables, IEnumerable<Expression> expressions)
	{
		if (expressions != null)
		{
			ICollection<ParameterExpression> collection;
			if (variables == null)
			{
				collection = null;
			}
			else
			{
				collection = variables as ICollection<ParameterExpression>;
				if (collection == null)
				{
					variables = (collection = variables.ToReadOnly());
				}
			}
			if (SameVariables(collection))
			{
				ICollection<Expression> collection2 = expressions as ICollection<Expression>;
				if (collection2 == null)
				{
					expressions = (collection2 = expressions.ToReadOnly());
				}
				if (SameExpressions(collection2))
				{
					return this;
				}
			}
		}
		return Expression.Block(Type, variables, expressions);
	}

	internal virtual bool SameVariables(ICollection<ParameterExpression> variables)
	{
		if (variables != null)
		{
			return variables.Count == 0;
		}
		return true;
	}

	[ExcludeFromCodeCoverage(Justification = "Unreachable")]
	internal virtual bool SameExpressions(ICollection<Expression> expressions)
	{
		throw ContractUtils.Unreachable;
	}

	[ExcludeFromCodeCoverage(Justification = "Unreachable")]
	internal virtual Expression GetExpression(int index)
	{
		throw ContractUtils.Unreachable;
	}

	[ExcludeFromCodeCoverage(Justification = "Unreachable")]
	internal virtual ReadOnlyCollection<Expression> GetOrMakeExpressions()
	{
		throw ContractUtils.Unreachable;
	}

	internal virtual ReadOnlyCollection<ParameterExpression> GetOrMakeVariables()
	{
		return EmptyReadOnlyCollection<ParameterExpression>.Instance;
	}

	[ExcludeFromCodeCoverage(Justification = "Unreachable")]
	internal virtual BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
	{
		throw ContractUtils.Unreachable;
	}

	internal static ReadOnlyCollection<Expression> ReturnReadOnlyExpressions(BlockExpression provider, ref object collection)
	{
		if (collection is Expression expression)
		{
			Interlocked.CompareExchange(ref collection, new ReadOnlyCollection<Expression>(new BlockExpressionList(provider, expression)), expression);
		}
		return (ReadOnlyCollection<Expression>)collection;
	}
}
