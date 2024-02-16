using System.Collections.Generic;

namespace System.Linq.Expressions.Compiler;

internal sealed class AnalyzedTree
{
	internal readonly Dictionary<object, CompilerScope> Scopes = new Dictionary<object, CompilerScope>();

	internal readonly Dictionary<LambdaExpression, BoundConstants> Constants = new Dictionary<LambdaExpression, BoundConstants>();
}
