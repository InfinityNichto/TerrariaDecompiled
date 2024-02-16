namespace System.Linq.Expressions;

internal sealed class ByRefAssignBinaryExpression : AssignBinaryExpression
{
	internal override bool IsByRef => true;

	internal ByRefAssignBinaryExpression(Expression left, Expression right)
		: base(left, right)
	{
	}
}
