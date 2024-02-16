namespace System.Linq.Expressions.Compiler;

internal enum LabelScopeKind
{
	Statement,
	Block,
	Switch,
	Lambda,
	Try,
	Catch,
	Finally,
	Filter,
	Expression
}
