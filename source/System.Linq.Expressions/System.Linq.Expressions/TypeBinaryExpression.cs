using System.Diagnostics;
using System.Dynamic.Utils;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions;

[DebuggerTypeProxy(typeof(TypeBinaryExpressionProxy))]
public sealed class TypeBinaryExpression : Expression
{
	public sealed override Type Type => typeof(bool);

	public sealed override ExpressionType NodeType { get; }

	public Expression Expression { get; }

	public Type TypeOperand { get; }

	internal TypeBinaryExpression(Expression expression, Type typeOperand, ExpressionType nodeType)
	{
		Expression = expression;
		TypeOperand = typeOperand;
		NodeType = nodeType;
	}

	internal Expression ReduceTypeEqual()
	{
		Type type = Expression.Type;
		if (type.IsValueType || TypeOperand.IsPointer)
		{
			if (type.IsNullableType())
			{
				if (type.GetNonNullableType() != TypeOperand.GetNonNullableType())
				{
					return System.Linq.Expressions.Expression.Block(Expression, Utils.Constant(value: false));
				}
				return System.Linq.Expressions.Expression.NotEqual(Expression, System.Linq.Expressions.Expression.Constant(null, Expression.Type));
			}
			return System.Linq.Expressions.Expression.Block(Expression, Utils.Constant(type == TypeOperand.GetNonNullableType()));
		}
		if (Expression.NodeType == ExpressionType.Constant)
		{
			return ReduceConstantTypeEqual();
		}
		if (Expression is ParameterExpression { IsByRef: false } parameterExpression)
		{
			return ByValParameterTypeEqual(parameterExpression);
		}
		ParameterExpression parameterExpression2 = System.Linq.Expressions.Expression.Parameter(typeof(object));
		return System.Linq.Expressions.Expression.Block(new TrueReadOnlyCollection<ParameterExpression>(parameterExpression2), new TrueReadOnlyCollection<Expression>(System.Linq.Expressions.Expression.Assign(parameterExpression2, Expression), ByValParameterTypeEqual(parameterExpression2)));
	}

	private Expression ByValParameterTypeEqual(ParameterExpression value)
	{
		Expression expression = System.Linq.Expressions.Expression.Call(value, CachedReflectionInfo.Object_GetType);
		if (TypeOperand.IsInterface)
		{
			ParameterExpression parameterExpression = System.Linq.Expressions.Expression.Parameter(typeof(Type));
			expression = System.Linq.Expressions.Expression.Block(new TrueReadOnlyCollection<ParameterExpression>(parameterExpression), new TrueReadOnlyCollection<Expression>(System.Linq.Expressions.Expression.Assign(parameterExpression, expression), parameterExpression));
		}
		return System.Linq.Expressions.Expression.AndAlso(System.Linq.Expressions.Expression.ReferenceNotEqual(value, Utils.Null), System.Linq.Expressions.Expression.ReferenceEqual(expression, System.Linq.Expressions.Expression.Constant(TypeOperand.GetNonNullableType(), typeof(Type))));
	}

	private Expression ReduceConstantTypeEqual()
	{
		ConstantExpression constantExpression = Expression as ConstantExpression;
		if (constantExpression.Value == null)
		{
			return Utils.Constant(value: false);
		}
		return Utils.Constant(TypeOperand.GetNonNullableType() == constantExpression.Value.GetType());
	}

	protected internal override Expression Accept(ExpressionVisitor visitor)
	{
		return visitor.VisitTypeBinary(this);
	}

	public TypeBinaryExpression Update(Expression expression)
	{
		if (expression == Expression)
		{
			return this;
		}
		if (NodeType == ExpressionType.TypeIs)
		{
			return System.Linq.Expressions.Expression.TypeIs(expression, TypeOperand);
		}
		return System.Linq.Expressions.Expression.TypeEqual(expression, TypeOperand);
	}
}
