namespace System.Linq.Expressions;

internal sealed class TypedConstantExpression : ConstantExpression
{
	public sealed override Type Type { get; }

	internal TypedConstantExpression(object value, Type type)
		: base(value)
	{
		Type = type;
	}
}
