using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Compiler;

internal sealed class SpilledExpressionBlock : BlockN
{
	internal SpilledExpressionBlock(IReadOnlyList<Expression> expressions)
		: base(expressions)
	{
	}

	[ExcludeFromCodeCoverage(Justification = "Unreachable")]
	internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
	{
		throw ContractUtils.Unreachable;
	}
}
