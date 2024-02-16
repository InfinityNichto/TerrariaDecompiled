using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Compiler;

internal sealed class VariableBinder : ExpressionVisitor
{
	private readonly AnalyzedTree _tree = new AnalyzedTree();

	private readonly Stack<CompilerScope> _scopes = new Stack<CompilerScope>();

	private readonly Stack<BoundConstants> _constants = new Stack<BoundConstants>();

	private readonly StackGuard _guard = new StackGuard();

	private bool _inQuote;

	private string CurrentLambdaName
	{
		get
		{
			foreach (CompilerScope scope in _scopes)
			{
				if (scope.Node is LambdaExpression lambdaExpression)
				{
					return lambdaExpression.Name;
				}
			}
			throw ContractUtils.Unreachable;
		}
	}

	internal static AnalyzedTree Bind(LambdaExpression lambda)
	{
		VariableBinder variableBinder = new VariableBinder();
		variableBinder.Visit(lambda);
		return variableBinder._tree;
	}

	private VariableBinder()
	{
	}

	[return: NotNullIfNotNull("node")]
	public override Expression Visit(Expression node)
	{
		if (!_guard.TryEnterOnCurrentStack())
		{
			return _guard.RunOnEmptyStack((VariableBinder @this, Expression e) => @this.Visit(e), this, node);
		}
		return base.Visit(node);
	}

	protected internal override Expression VisitConstant(ConstantExpression node)
	{
		if (_inQuote)
		{
			return node;
		}
		if (ILGen.CanEmitConstant(node.Value, node.Type))
		{
			return node;
		}
		_constants.Peek().AddReference(node.Value, node.Type);
		return node;
	}

	protected internal override Expression VisitUnary(UnaryExpression node)
	{
		if (node.NodeType == ExpressionType.Quote)
		{
			bool inQuote = _inQuote;
			_inQuote = true;
			Visit(node.Operand);
			_inQuote = inQuote;
		}
		else
		{
			Visit(node.Operand);
		}
		return node;
	}

	protected internal override Expression VisitLambda<T>(Expression<T> node)
	{
		Stack<CompilerScope> scopes = _scopes;
		CompilerScope item = (_tree.Scopes[node] = new CompilerScope(node, isMethod: true));
		scopes.Push(item);
		Stack<BoundConstants> constants = _constants;
		BoundConstants item2 = (_tree.Constants[node] = new BoundConstants());
		constants.Push(item2);
		Visit(MergeScopes(node));
		_constants.Pop();
		_scopes.Pop();
		return node;
	}

	protected internal override Expression VisitInvocation(InvocationExpression node)
	{
		LambdaExpression lambdaOperand = node.LambdaOperand;
		if (lambdaOperand != null)
		{
			Stack<CompilerScope> scopes = _scopes;
			CompilerScope item = (_tree.Scopes[node] = new CompilerScope(lambdaOperand, isMethod: false));
			scopes.Push(item);
			Visit(MergeScopes(lambdaOperand));
			_scopes.Pop();
			int i = 0;
			for (int argumentCount = node.ArgumentCount; i < argumentCount; i++)
			{
				Visit(node.GetArgument(i));
			}
			return node;
		}
		return base.VisitInvocation(node);
	}

	protected internal override Expression VisitBlock(BlockExpression node)
	{
		if (node.Variables.Count == 0)
		{
			Visit(node.Expressions);
			return node;
		}
		Stack<CompilerScope> scopes = _scopes;
		CompilerScope item = (_tree.Scopes[node] = new CompilerScope(node, isMethod: false));
		scopes.Push(item);
		Visit(MergeScopes(node));
		_scopes.Pop();
		return node;
	}

	protected override CatchBlock VisitCatchBlock(CatchBlock node)
	{
		if (node.Variable == null)
		{
			Visit(node.Filter);
			Visit(node.Body);
			return node;
		}
		Stack<CompilerScope> scopes = _scopes;
		CompilerScope item = (_tree.Scopes[node] = new CompilerScope(node, isMethod: false));
		scopes.Push(item);
		Visit(node.Filter);
		Visit(node.Body);
		_scopes.Pop();
		return node;
	}

	private ReadOnlyCollection<Expression> MergeScopes(Expression node)
	{
		ReadOnlyCollection<Expression> readOnlyCollection = ((!(node is LambdaExpression lambdaExpression)) ? ((BlockExpression)node).Expressions : new ReadOnlyCollection<Expression>(new Expression[1] { lambdaExpression.Body }));
		CompilerScope compilerScope = _scopes.Peek();
		while (readOnlyCollection.Count == 1 && readOnlyCollection[0].NodeType == ExpressionType.Block)
		{
			BlockExpression blockExpression = (BlockExpression)readOnlyCollection[0];
			if (blockExpression.Variables.Count > 0)
			{
				foreach (ParameterExpression variable in blockExpression.Variables)
				{
					if (compilerScope.Definitions.ContainsKey(variable))
					{
						return readOnlyCollection;
					}
				}
				if (compilerScope.MergedScopes == null)
				{
					compilerScope.MergedScopes = new HashSet<BlockExpression>(ReferenceEqualityComparer.Instance);
				}
				compilerScope.MergedScopes.Add(blockExpression);
				foreach (ParameterExpression variable2 in blockExpression.Variables)
				{
					compilerScope.Definitions.Add(variable2, VariableStorageKind.Local);
				}
			}
			readOnlyCollection = blockExpression.Expressions;
		}
		return readOnlyCollection;
	}

	protected internal override Expression VisitParameter(ParameterExpression node)
	{
		Reference(node, VariableStorageKind.Local);
		CompilerScope compilerScope = null;
		foreach (CompilerScope scope in _scopes)
		{
			if (scope.IsMethod || scope.Definitions.ContainsKey(node))
			{
				compilerScope = scope;
				break;
			}
		}
		if (compilerScope.ReferenceCount == null)
		{
			compilerScope.ReferenceCount = new Dictionary<ParameterExpression, int>();
		}
		Helpers.IncrementCount(node, compilerScope.ReferenceCount);
		return node;
	}

	protected internal override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
	{
		foreach (ParameterExpression variable in node.Variables)
		{
			Reference(variable, VariableStorageKind.Hoisted);
		}
		return node;
	}

	private void Reference(ParameterExpression node, VariableStorageKind storage)
	{
		CompilerScope compilerScope = null;
		foreach (CompilerScope scope in _scopes)
		{
			if (scope.Definitions.ContainsKey(node))
			{
				compilerScope = scope;
				break;
			}
			scope.NeedsClosure = true;
			if (scope.IsMethod)
			{
				storage = VariableStorageKind.Hoisted;
			}
		}
		if (compilerScope == null)
		{
			throw Error.UndefinedVariable(node.Name, node.Type, CurrentLambdaName);
		}
		if (storage == VariableStorageKind.Hoisted)
		{
			if (node.IsByRef)
			{
				throw Error.CannotCloseOverByRef(node.Name, CurrentLambdaName);
			}
			compilerScope.Definitions[node] = VariableStorageKind.Hoisted;
		}
	}
}
