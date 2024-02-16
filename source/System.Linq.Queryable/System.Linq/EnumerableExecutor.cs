using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace System.Linq;

public abstract class EnumerableExecutor
{
	[RequiresUnreferencedCode("Enumerating in-memory collections as IQueryable can require unreferenced code because expressions referencing IQueryable extension methods can get rebound to IEnumerable extension methods. The IEnumerable extension methods could be trimmed causing the application to fail at runtime.")]
	internal abstract object ExecuteBoxed();

	internal EnumerableExecutor()
	{
	}

	internal static EnumerableExecutor Create(Expression expression)
	{
		Type type = typeof(EnumerableExecutor<>).MakeGenericType(expression.Type);
		return (EnumerableExecutor)Activator.CreateInstance(type, expression);
	}
}
public class EnumerableExecutor<T> : EnumerableExecutor
{
	private readonly Expression _expression;

	public EnumerableExecutor(Expression expression)
	{
		_expression = expression;
	}

	[RequiresUnreferencedCode("Enumerating in-memory collections as IQueryable can require unreferenced code because expressions referencing IQueryable extension methods can get rebound to IEnumerable extension methods. The IEnumerable extension methods could be trimmed causing the application to fail at runtime.")]
	internal override object ExecuteBoxed()
	{
		return Execute();
	}

	[RequiresUnreferencedCode("Enumerating in-memory collections as IQueryable can require unreferenced code because expressions referencing IQueryable extension methods can get rebound to IEnumerable extension methods. The IEnumerable extension methods could be trimmed causing the application to fail at runtime.")]
	internal T Execute()
	{
		EnumerableRewriter enumerableRewriter = new EnumerableRewriter();
		Expression body = enumerableRewriter.Visit(_expression);
		Expression<Func<T>> expression = Expression.Lambda<Func<T>>(body, (IEnumerable<ParameterExpression>?)null);
		Func<T> func = expression.Compile();
		return func();
	}
}
