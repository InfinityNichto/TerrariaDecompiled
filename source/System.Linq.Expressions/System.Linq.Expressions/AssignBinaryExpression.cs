namespace System.Linq.Expressions;

internal class AssignBinaryExpression : BinaryExpression
{
	internal virtual bool IsByRef => false;

	public sealed override Type Type => base.Left.Type;

	public sealed override ExpressionType NodeType => ExpressionType.Assign;

	internal AssignBinaryExpression(Expression left, Expression right)
		: base(left, right)
	{
	}

	public static AssignBinaryExpression Make(Expression left, Expression right, bool byRef)
	{
		if (byRef)
		{
			return new ByRefAssignBinaryExpression(left, right);
		}
		return new AssignBinaryExpression(left, right);
	}
}
