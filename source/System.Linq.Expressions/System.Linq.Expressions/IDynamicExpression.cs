namespace System.Linq.Expressions;

public interface IDynamicExpression : IArgumentProvider
{
	Type DelegateType { get; }

	Expression Rewrite(Expression[] args);

	object CreateCallSite();
}
