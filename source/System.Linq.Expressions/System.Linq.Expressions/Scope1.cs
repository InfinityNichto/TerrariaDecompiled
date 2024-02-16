using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic.Utils;

namespace System.Linq.Expressions;

internal sealed class Scope1 : ScopeExpression
{
	private object _body;

	internal override int ExpressionCount => 1;

	internal Scope1(IReadOnlyList<ParameterExpression> variables, Expression body)
		: this(variables, (object)body)
	{
	}

	private Scope1(IReadOnlyList<ParameterExpression> variables, object body)
		: base(variables)
	{
		_body = body;
	}

	internal override bool SameExpressions(ICollection<Expression> expressions)
	{
		if (expressions.Count == 1)
		{
			if (_body is ReadOnlyCollection<Expression> current)
			{
				return ExpressionUtils.SameElements(expressions, current);
			}
			using IEnumerator<Expression> enumerator = expressions.GetEnumerator();
			enumerator.MoveNext();
			return ExpressionUtils.ReturnObject<Expression>(_body) == enumerator.Current;
		}
		return false;
	}

	internal override Expression GetExpression(int index)
	{
		if (index == 0)
		{
			return ExpressionUtils.ReturnObject<Expression>(_body);
		}
		throw Error.ArgumentOutOfRange("index");
	}

	internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
	{
		return BlockExpression.ReturnReadOnlyExpressions(this, ref _body);
	}

	internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
	{
		if (args == null)
		{
			Expression.ValidateVariables(variables, "variables");
			return new Scope1(variables, _body);
		}
		return new Scope1(ReuseOrValidateVariables(variables), args[0]);
	}
}
