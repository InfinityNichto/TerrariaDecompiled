using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions;

[DebuggerTypeProxy(typeof(NewExpressionProxy))]
public class NewExpression : Expression, IArgumentProvider
{
	private IReadOnlyList<Expression> _arguments;

	public override Type Type => Constructor.DeclaringType;

	public sealed override ExpressionType NodeType => ExpressionType.New;

	public ConstructorInfo? Constructor { get; }

	public ReadOnlyCollection<Expression> Arguments => ExpressionUtils.ReturnReadOnly(ref _arguments);

	public int ArgumentCount => _arguments.Count;

	public ReadOnlyCollection<MemberInfo>? Members { get; }

	internal NewExpression(ConstructorInfo constructor, IReadOnlyList<Expression> arguments, ReadOnlyCollection<MemberInfo> members)
	{
		Constructor = constructor;
		_arguments = arguments;
		Members = members;
	}

	public Expression GetArgument(int index)
	{
		return _arguments[index];
	}

	protected internal override Expression Accept(ExpressionVisitor visitor)
	{
		return visitor.VisitNew(this);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "A NewExpression has already been created. The original creator will get a warning that it is not trim compatible.")]
	public NewExpression Update(IEnumerable<Expression>? arguments)
	{
		if (ExpressionUtils.SameElements(ref arguments, Arguments))
		{
			return this;
		}
		if (Members == null)
		{
			return Expression.New(Constructor, arguments);
		}
		return Expression.New(Constructor, arguments, Members);
	}
}
