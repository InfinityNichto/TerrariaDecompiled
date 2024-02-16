using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions;

[DebuggerTypeProxy(typeof(SwitchExpressionProxy))]
public sealed class SwitchExpression : Expression
{
	public sealed override Type Type { get; }

	public sealed override ExpressionType NodeType => ExpressionType.Switch;

	public Expression SwitchValue { get; }

	public ReadOnlyCollection<SwitchCase> Cases { get; }

	public Expression? DefaultBody { get; }

	public MethodInfo? Comparison { get; }

	internal bool IsLifted
	{
		get
		{
			if (SwitchValue.Type.IsNullableType())
			{
				if (!(Comparison == null))
				{
					return !TypeUtils.AreEquivalent(SwitchValue.Type, Comparison.GetParametersCached()[0].ParameterType.GetNonRefType());
				}
				return true;
			}
			return false;
		}
	}

	internal SwitchExpression(Type type, Expression switchValue, Expression defaultBody, MethodInfo comparison, ReadOnlyCollection<SwitchCase> cases)
	{
		Type = type;
		SwitchValue = switchValue;
		DefaultBody = defaultBody;
		Comparison = comparison;
		Cases = cases;
	}

	protected internal override Expression Accept(ExpressionVisitor visitor)
	{
		return visitor.VisitSwitch(this);
	}

	public SwitchExpression Update(Expression switchValue, IEnumerable<SwitchCase>? cases, Expression? defaultBody)
	{
		if (switchValue == SwitchValue && defaultBody == DefaultBody && cases != null && ExpressionUtils.SameElements(ref cases, Cases))
		{
			return this;
		}
		return Expression.Switch(Type, switchValue, defaultBody, Comparison, cases);
	}
}
