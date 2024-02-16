using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Compiler;

internal sealed class StackSpiller
{
	private abstract class BindingRewriter
	{
		protected readonly MemberBinding _binding;

		protected readonly StackSpiller _spiller;

		protected RewriteAction _action;

		internal RewriteAction Action => _action;

		internal BindingRewriter(MemberBinding binding, StackSpiller spiller)
		{
			_binding = binding;
			_spiller = spiller;
		}

		internal abstract MemberBinding AsBinding();

		internal abstract Expression AsExpression(Expression target);

		internal static BindingRewriter Create(MemberBinding binding, StackSpiller spiller, Stack stack)
		{
			switch (binding.BindingType)
			{
			case MemberBindingType.Assignment:
			{
				MemberAssignment binding4 = (MemberAssignment)binding;
				return new MemberAssignmentRewriter(binding4, spiller, stack);
			}
			case MemberBindingType.ListBinding:
			{
				MemberListBinding binding3 = (MemberListBinding)binding;
				return new ListBindingRewriter(binding3, spiller, stack);
			}
			case MemberBindingType.MemberBinding:
			{
				MemberMemberBinding binding2 = (MemberMemberBinding)binding;
				return new MemberMemberBindingRewriter(binding2, spiller, stack);
			}
			default:
				throw Error.UnhandledBinding();
			}
		}

		protected void RequireNoValueProperty()
		{
			if (_binding.Member is PropertyInfo propertyInfo && propertyInfo.PropertyType.IsValueType)
			{
				throw Error.CannotAutoInitializeValueTypeMemberThroughProperty(propertyInfo);
			}
		}
	}

	private sealed class MemberMemberBindingRewriter : BindingRewriter
	{
		private readonly ReadOnlyCollection<MemberBinding> _bindings;

		private readonly BindingRewriter[] _bindingRewriters;

		internal MemberMemberBindingRewriter(MemberMemberBinding binding, StackSpiller spiller, Stack stack)
			: base(binding, spiller)
		{
			_bindings = binding.Bindings;
			int count = _bindings.Count;
			_bindingRewriters = new BindingRewriter[count];
			for (int i = 0; i < count; i++)
			{
				BindingRewriter bindingRewriter = BindingRewriter.Create(_bindings[i], spiller, stack);
				_action |= bindingRewriter.Action;
				_bindingRewriters[i] = bindingRewriter;
			}
		}

		internal override MemberBinding AsBinding()
		{
			switch (_action)
			{
			case RewriteAction.None:
				return _binding;
			case RewriteAction.Copy:
			{
				int count = _bindings.Count;
				MemberBinding[] array = new MemberBinding[count];
				for (int i = 0; i < count; i++)
				{
					array[i] = _bindingRewriters[i].AsBinding();
				}
				return new MemberMemberBinding(_binding.Member, new TrueReadOnlyCollection<MemberBinding>(array));
			}
			default:
				throw ContractUtils.Unreachable;
			}
		}

		internal override Expression AsExpression(Expression target)
		{
			RequireNoValueProperty();
			Expression expression = MemberExpression.Make(target, _binding.Member);
			Expression expression2 = _spiller.MakeTemp(expression.Type);
			int count = _bindings.Count;
			Expression[] array = new Expression[count + 2];
			array[0] = new AssignBinaryExpression(expression2, expression);
			for (int i = 0; i < count; i++)
			{
				BindingRewriter bindingRewriter = _bindingRewriters[i];
				array[i + 1] = bindingRewriter.AsExpression(expression2);
			}
			if (expression2.Type.IsValueType)
			{
				array[count + 1] = Expression.Block(typeof(void), new AssignBinaryExpression(MemberExpression.Make(target, _binding.Member), expression2));
			}
			else
			{
				array[count + 1] = Utils.Empty;
			}
			return MakeBlock(array);
		}
	}

	private sealed class ListBindingRewriter : BindingRewriter
	{
		private readonly ReadOnlyCollection<ElementInit> _inits;

		private readonly ChildRewriter[] _childRewriters;

		internal ListBindingRewriter(MemberListBinding binding, StackSpiller spiller, Stack stack)
			: base(binding, spiller)
		{
			_inits = binding.Initializers;
			int count = _inits.Count;
			_childRewriters = new ChildRewriter[count];
			for (int i = 0; i < count; i++)
			{
				ElementInit elementInit = _inits[i];
				ChildRewriter childRewriter = new ChildRewriter(spiller, stack, elementInit.Arguments.Count);
				childRewriter.Add(elementInit.Arguments);
				_action |= childRewriter.Action;
				_childRewriters[i] = childRewriter;
			}
		}

		internal override MemberBinding AsBinding()
		{
			switch (_action)
			{
			case RewriteAction.None:
				return _binding;
			case RewriteAction.Copy:
			{
				int count = _inits.Count;
				ElementInit[] array = new ElementInit[count];
				for (int i = 0; i < count; i++)
				{
					ChildRewriter childRewriter = _childRewriters[i];
					if (childRewriter.Action == RewriteAction.None)
					{
						array[i] = _inits[i];
					}
					else
					{
						array[i] = new ElementInit(_inits[i].AddMethod, new TrueReadOnlyCollection<Expression>(childRewriter[0, -1]));
					}
				}
				return new MemberListBinding(_binding.Member, new TrueReadOnlyCollection<ElementInit>(array));
			}
			default:
				throw ContractUtils.Unreachable;
			}
		}

		internal override Expression AsExpression(Expression target)
		{
			RequireNoValueProperty();
			Expression expression = MemberExpression.Make(target, _binding.Member);
			Expression expression2 = _spiller.MakeTemp(expression.Type);
			int count = _inits.Count;
			Expression[] array = new Expression[count + 2];
			array[0] = new AssignBinaryExpression(expression2, expression);
			for (int i = 0; i < count; i++)
			{
				ChildRewriter childRewriter = _childRewriters[i];
				array[i + 1] = childRewriter.Finish(new InstanceMethodCallExpressionN(_inits[i].AddMethod, expression2, childRewriter[0, -1])).Node;
			}
			if (expression2.Type.IsValueType)
			{
				array[count + 1] = Expression.Block(typeof(void), new AssignBinaryExpression(MemberExpression.Make(target, _binding.Member), expression2));
			}
			else
			{
				array[count + 1] = Utils.Empty;
			}
			return MakeBlock(array);
		}
	}

	private sealed class MemberAssignmentRewriter : BindingRewriter
	{
		private readonly Expression _rhs;

		internal MemberAssignmentRewriter(MemberAssignment binding, StackSpiller spiller, Stack stack)
			: base(binding, spiller)
		{
			Result result = spiller.RewriteExpression(binding.Expression, stack);
			_action = result.Action;
			_rhs = result.Node;
		}

		internal override MemberBinding AsBinding()
		{
			return _action switch
			{
				RewriteAction.None => _binding, 
				RewriteAction.Copy => new MemberAssignment(_binding.Member, _rhs), 
				_ => throw ContractUtils.Unreachable, 
			};
		}

		internal override Expression AsExpression(Expression target)
		{
			Expression expression = MemberExpression.Make(target, _binding.Member);
			Expression expression2 = _spiller.MakeTemp(expression.Type);
			return MakeBlock(new AssignBinaryExpression(expression2, _rhs), new AssignBinaryExpression(expression, expression2), Utils.Empty);
		}
	}

	private sealed class ChildRewriter
	{
		private readonly StackSpiller _self;

		private readonly Expression[] _expressions;

		private int _expressionsCount;

		private int _lastSpillIndex;

		private List<Expression> _comma;

		private RewriteAction _action;

		private Stack _stack;

		private bool _done;

		private bool[] _byRefs;

		internal bool Rewrite => _action != RewriteAction.None;

		internal RewriteAction Action => _action;

		internal Expression this[int index]
		{
			get
			{
				EnsureDone();
				if (index < 0)
				{
					index += _expressions.Length;
				}
				return _expressions[index];
			}
		}

		internal Expression[] this[int first, int last]
		{
			get
			{
				EnsureDone();
				if (last < 0)
				{
					last += _expressions.Length;
				}
				int num = last - first + 1;
				ContractUtils.RequiresArrayRange(_expressions, first, num, "first", "last");
				if (num == _expressions.Length)
				{
					return _expressions;
				}
				Expression[] array = new Expression[num];
				Array.Copy(_expressions, first, array, 0, num);
				return array;
			}
		}

		internal ChildRewriter(StackSpiller self, Stack stack, int count)
		{
			_self = self;
			_stack = stack;
			_expressions = new Expression[count];
		}

		internal void Add(Expression expression)
		{
			if (expression == null)
			{
				_expressions[_expressionsCount++] = null;
				return;
			}
			Result result = _self.RewriteExpression(expression, _stack);
			_action |= result.Action;
			_stack = Stack.NonEmpty;
			if (result.Action == RewriteAction.SpillStack)
			{
				_lastSpillIndex = _expressionsCount;
			}
			_expressions[_expressionsCount++] = result.Node;
		}

		internal void Add(ReadOnlyCollection<Expression> expressions)
		{
			int i = 0;
			for (int count = expressions.Count; i < count; i++)
			{
				Add(expressions[i]);
			}
		}

		internal void AddArguments(IArgumentProvider expressions)
		{
			int i = 0;
			for (int argumentCount = expressions.ArgumentCount; i < argumentCount; i++)
			{
				Add(expressions.GetArgument(i));
			}
		}

		private void EnsureDone()
		{
			if (_done)
			{
				return;
			}
			_done = true;
			if (_action != RewriteAction.SpillStack)
			{
				return;
			}
			Expression[] expressions = _expressions;
			int num = _lastSpillIndex + 1;
			List<Expression> list = new List<Expression>(num + 1);
			for (int i = 0; i < num; i++)
			{
				Expression expression = expressions[i];
				if (ShouldSaveToTemp(expression))
				{
					int num2 = i;
					StackSpiller self = _self;
					bool[] byRefs = _byRefs;
					expressions[num2] = self.ToTemp(expression, out var save, byRefs != null && byRefs[i]);
					list.Add(save);
				}
			}
			list.Capacity = list.Count + 1;
			_comma = list;
		}

		private static bool ShouldSaveToTemp(Expression expression)
		{
			if (expression == null)
			{
				return false;
			}
			switch (expression.NodeType)
			{
			case ExpressionType.Constant:
			case ExpressionType.Default:
				return false;
			case ExpressionType.RuntimeVariables:
				return false;
			case ExpressionType.MemberAccess:
			{
				MemberExpression memberExpression = (MemberExpression)expression;
				FieldInfo fieldInfo = memberExpression.Member as FieldInfo;
				if (fieldInfo != null)
				{
					if (fieldInfo.IsLiteral)
					{
						return false;
					}
					if (fieldInfo.IsInitOnly && fieldInfo.IsStatic)
					{
						return false;
					}
				}
				break;
			}
			}
			return true;
		}

		internal void MarkRefInstance(Expression expr)
		{
			if (IsRefInstance(expr))
			{
				MarkRef(0);
			}
		}

		internal void MarkRefArgs(MethodBase method, int startIndex)
		{
			ParameterInfo[] parametersCached = method.GetParametersCached();
			int i = 0;
			for (int num = parametersCached.Length; i < num; i++)
			{
				if (parametersCached[i].ParameterType.IsByRef)
				{
					MarkRef(startIndex + i);
				}
			}
		}

		private void MarkRef(int index)
		{
			if (_byRefs == null)
			{
				_byRefs = new bool[_expressions.Length];
			}
			_byRefs[index] = true;
		}

		internal Result Finish(Expression expression)
		{
			EnsureDone();
			if (_action == RewriteAction.SpillStack)
			{
				_comma.Add(expression);
				expression = MakeBlock(_comma);
			}
			return new Result(_action, expression);
		}
	}

	private enum Stack
	{
		Empty,
		NonEmpty
	}

	[Flags]
	private enum RewriteAction
	{
		None = 0,
		Copy = 1,
		SpillStack = 3
	}

	private readonly struct Result
	{
		internal readonly RewriteAction Action;

		internal readonly Expression Node;

		internal Result(RewriteAction action, Expression node)
		{
			Action = action;
			Node = node;
		}
	}

	private sealed class TempMaker
	{
		private int _temp;

		private List<ParameterExpression> _freeTemps;

		private Stack<ParameterExpression> _usedTemps;

		internal List<ParameterExpression> Temps { get; } = new List<ParameterExpression>();


		internal ParameterExpression Temp(Type type)
		{
			ParameterExpression parameterExpression;
			if (_freeTemps != null)
			{
				for (int num = _freeTemps.Count - 1; num >= 0; num--)
				{
					parameterExpression = _freeTemps[num];
					if (parameterExpression.Type == type)
					{
						_freeTemps.RemoveAt(num);
						return UseTemp(parameterExpression);
					}
				}
			}
			parameterExpression = ParameterExpression.Make(type, "$temp$" + _temp++, isByRef: false);
			Temps.Add(parameterExpression);
			return UseTemp(parameterExpression);
		}

		private ParameterExpression UseTemp(ParameterExpression temp)
		{
			if (_usedTemps == null)
			{
				_usedTemps = new Stack<ParameterExpression>();
			}
			_usedTemps.Push(temp);
			return temp;
		}

		private void FreeTemp(ParameterExpression temp)
		{
			if (_freeTemps == null)
			{
				_freeTemps = new List<ParameterExpression>();
			}
			_freeTemps.Add(temp);
		}

		internal int Mark()
		{
			return _usedTemps?.Count ?? 0;
		}

		internal void Free(int mark)
		{
			if (_usedTemps != null)
			{
				while (mark < _usedTemps.Count)
				{
					FreeTemp(_usedTemps.Pop());
				}
			}
		}
	}

	private readonly Stack _startingStack;

	private RewriteAction _lambdaRewrite;

	private readonly StackGuard _guard = new StackGuard();

	private readonly TempMaker _tm = new TempMaker();

	internal static LambdaExpression AnalyzeLambda(LambdaExpression lambda)
	{
		return lambda.Accept(new StackSpiller(Stack.Empty));
	}

	private StackSpiller(Stack stack)
	{
		_startingStack = stack;
	}

	internal Expression<T> Rewrite<T>(Expression<T> lambda)
	{
		Result result = RewriteExpressionFreeTemps(lambda.Body, _startingStack);
		_lambdaRewrite = result.Action;
		if (result.Action != 0)
		{
			Expression expression = result.Node;
			if (_tm.Temps.Count > 0)
			{
				expression = Expression.Block(_tm.Temps, new TrueReadOnlyCollection<Expression>(expression));
			}
			return Expression<T>.Create(expression, lambda.Name, lambda.TailCall, new ParameterList(lambda));
		}
		return lambda;
	}

	private Result RewriteExpressionFreeTemps(Expression expression, Stack stack)
	{
		int mark = Mark();
		Result result = RewriteExpression(expression, stack);
		Free(mark);
		return result;
	}

	private Result RewriteDynamicExpression(Expression expr)
	{
		IDynamicExpression dynamicExpression = (IDynamicExpression)expr;
		ChildRewriter childRewriter = new ChildRewriter(this, Stack.NonEmpty, dynamicExpression.ArgumentCount);
		childRewriter.AddArguments(dynamicExpression);
		if (childRewriter.Action == RewriteAction.SpillStack)
		{
			RequireNoRefArgs(dynamicExpression.DelegateType.GetInvokeMethod());
		}
		return childRewriter.Finish(childRewriter.Rewrite ? dynamicExpression.Rewrite(childRewriter[0, -1]) : expr);
	}

	private Result RewriteIndexAssignment(BinaryExpression node, Stack stack)
	{
		IndexExpression indexExpression = (IndexExpression)node.Left;
		ChildRewriter childRewriter = new ChildRewriter(this, stack, 2 + indexExpression.ArgumentCount);
		childRewriter.Add(indexExpression.Object);
		childRewriter.AddArguments(indexExpression);
		childRewriter.Add(node.Right);
		if (childRewriter.Action == RewriteAction.SpillStack)
		{
			childRewriter.MarkRefInstance(indexExpression.Object);
		}
		if (childRewriter.Rewrite)
		{
			node = new AssignBinaryExpression(new IndexExpression(childRewriter[0], indexExpression.Indexer, childRewriter[1, -2]), childRewriter[-1]);
		}
		return childRewriter.Finish(node);
	}

	private Result RewriteLogicalBinaryExpression(Expression expr, Stack stack)
	{
		BinaryExpression binaryExpression = (BinaryExpression)expr;
		Result result = RewriteExpression(binaryExpression.Left, stack);
		Result result2 = RewriteExpression(binaryExpression.Right, stack);
		Result result3 = RewriteExpression(binaryExpression.Conversion, stack);
		RewriteAction rewriteAction = result.Action | result2.Action | result3.Action;
		if (rewriteAction != 0)
		{
			expr = BinaryExpression.Create(binaryExpression.NodeType, result.Node, result2.Node, binaryExpression.Type, binaryExpression.Method, (LambdaExpression)result3.Node);
		}
		return new Result(rewriteAction, expr);
	}

	private Result RewriteReducibleExpression(Expression expr, Stack stack)
	{
		Result result = RewriteExpression(expr.Reduce(), stack);
		return new Result(result.Action | RewriteAction.Copy, result.Node);
	}

	private Result RewriteBinaryExpression(Expression expr, Stack stack)
	{
		BinaryExpression binaryExpression = (BinaryExpression)expr;
		ChildRewriter childRewriter = new ChildRewriter(this, stack, 3);
		childRewriter.Add(binaryExpression.Left);
		childRewriter.Add(binaryExpression.Right);
		childRewriter.Add(binaryExpression.Conversion);
		if (childRewriter.Action == RewriteAction.SpillStack)
		{
			RequireNoRefArgs(binaryExpression.Method);
		}
		return childRewriter.Finish(childRewriter.Rewrite ? BinaryExpression.Create(binaryExpression.NodeType, childRewriter[0], childRewriter[1], binaryExpression.Type, binaryExpression.Method, (LambdaExpression)childRewriter[2]) : expr);
	}

	private Result RewriteVariableAssignment(BinaryExpression node, Stack stack)
	{
		Result result = RewriteExpression(node.Right, stack);
		if (result.Action != 0)
		{
			node = new AssignBinaryExpression(node.Left, result.Node);
		}
		return new Result(result.Action, node);
	}

	private Result RewriteAssignBinaryExpression(Expression expr, Stack stack)
	{
		BinaryExpression binaryExpression = (BinaryExpression)expr;
		return binaryExpression.Left.NodeType switch
		{
			ExpressionType.Index => RewriteIndexAssignment(binaryExpression, stack), 
			ExpressionType.MemberAccess => RewriteMemberAssignment(binaryExpression, stack), 
			ExpressionType.Parameter => RewriteVariableAssignment(binaryExpression, stack), 
			ExpressionType.Extension => RewriteExtensionAssignment(binaryExpression, stack), 
			_ => throw Error.InvalidLvalue(binaryExpression.Left.NodeType), 
		};
	}

	private Result RewriteExtensionAssignment(BinaryExpression node, Stack stack)
	{
		node = new AssignBinaryExpression(node.Left.ReduceExtensions(), node.Right);
		Result result = RewriteAssignBinaryExpression(node, stack);
		return new Result(result.Action | RewriteAction.Copy, result.Node);
	}

	private static Result RewriteLambdaExpression(Expression expr)
	{
		LambdaExpression lambdaExpression = (LambdaExpression)expr;
		expr = AnalyzeLambda(lambdaExpression);
		RewriteAction action = ((expr != lambdaExpression) ? RewriteAction.Copy : RewriteAction.None);
		return new Result(action, expr);
	}

	private Result RewriteConditionalExpression(Expression expr, Stack stack)
	{
		ConditionalExpression conditionalExpression = (ConditionalExpression)expr;
		Result result = RewriteExpression(conditionalExpression.Test, stack);
		Result result2 = RewriteExpression(conditionalExpression.IfTrue, stack);
		Result result3 = RewriteExpression(conditionalExpression.IfFalse, stack);
		RewriteAction rewriteAction = result.Action | result2.Action | result3.Action;
		if (rewriteAction != 0)
		{
			expr = ConditionalExpression.Make(result.Node, result2.Node, result3.Node, conditionalExpression.Type);
		}
		return new Result(rewriteAction, expr);
	}

	private Result RewriteMemberAssignment(BinaryExpression node, Stack stack)
	{
		MemberExpression memberExpression = (MemberExpression)node.Left;
		ChildRewriter childRewriter = new ChildRewriter(this, stack, 2);
		childRewriter.Add(memberExpression.Expression);
		childRewriter.Add(node.Right);
		if (childRewriter.Action == RewriteAction.SpillStack)
		{
			childRewriter.MarkRefInstance(memberExpression.Expression);
		}
		if (childRewriter.Rewrite)
		{
			return childRewriter.Finish(new AssignBinaryExpression(MemberExpression.Make(childRewriter[0], memberExpression.Member), childRewriter[1]));
		}
		return new Result(RewriteAction.None, node);
	}

	private Result RewriteMemberExpression(Expression expr, Stack stack)
	{
		MemberExpression memberExpression = (MemberExpression)expr;
		Result result = RewriteExpression(memberExpression.Expression, stack);
		if (result.Action != 0)
		{
			if (result.Action == RewriteAction.SpillStack && memberExpression.Member is PropertyInfo)
			{
				RequireNotRefInstance(memberExpression.Expression);
			}
			expr = MemberExpression.Make(result.Node, memberExpression.Member);
		}
		return new Result(result.Action, expr);
	}

	private Result RewriteIndexExpression(Expression expr, Stack stack)
	{
		IndexExpression indexExpression = (IndexExpression)expr;
		ChildRewriter childRewriter = new ChildRewriter(this, stack, indexExpression.ArgumentCount + 1);
		childRewriter.Add(indexExpression.Object);
		childRewriter.AddArguments(indexExpression);
		if (childRewriter.Action == RewriteAction.SpillStack)
		{
			childRewriter.MarkRefInstance(indexExpression.Object);
		}
		if (childRewriter.Rewrite)
		{
			expr = new IndexExpression(childRewriter[0], indexExpression.Indexer, childRewriter[1, -1]);
		}
		return childRewriter.Finish(expr);
	}

	private Result RewriteMethodCallExpression(Expression expr, Stack stack)
	{
		MethodCallExpression methodCallExpression = (MethodCallExpression)expr;
		ChildRewriter childRewriter = new ChildRewriter(this, stack, methodCallExpression.ArgumentCount + 1);
		childRewriter.Add(methodCallExpression.Object);
		childRewriter.AddArguments(methodCallExpression);
		if (childRewriter.Action == RewriteAction.SpillStack)
		{
			childRewriter.MarkRefInstance(methodCallExpression.Object);
			childRewriter.MarkRefArgs(methodCallExpression.Method, 1);
		}
		if (childRewriter.Rewrite)
		{
			expr = ((methodCallExpression.Object == null) ? ((MethodCallExpression)new MethodCallExpressionN(methodCallExpression.Method, childRewriter[1, -1])) : ((MethodCallExpression)new InstanceMethodCallExpressionN(methodCallExpression.Method, childRewriter[0], childRewriter[1, -1])));
		}
		return childRewriter.Finish(expr);
	}

	private Result RewriteNewArrayExpression(Expression expr, Stack stack)
	{
		NewArrayExpression newArrayExpression = (NewArrayExpression)expr;
		if (newArrayExpression.NodeType == ExpressionType.NewArrayInit)
		{
			stack = Stack.NonEmpty;
		}
		ChildRewriter childRewriter = new ChildRewriter(this, stack, newArrayExpression.Expressions.Count);
		childRewriter.Add(newArrayExpression.Expressions);
		if (childRewriter.Rewrite)
		{
			expr = NewArrayExpression.Make(newArrayExpression.NodeType, newArrayExpression.Type, new TrueReadOnlyCollection<Expression>(childRewriter[0, -1]));
		}
		return childRewriter.Finish(expr);
	}

	private Result RewriteInvocationExpression(Expression expr, Stack stack)
	{
		InvocationExpression invocationExpression = (InvocationExpression)expr;
		LambdaExpression lambdaOperand = invocationExpression.LambdaOperand;
		ChildRewriter childRewriter;
		if (lambdaOperand != null)
		{
			childRewriter = new ChildRewriter(this, stack, invocationExpression.ArgumentCount);
			childRewriter.AddArguments(invocationExpression);
			if (childRewriter.Action == RewriteAction.SpillStack)
			{
				childRewriter.MarkRefArgs(Expression.GetInvokeMethod(invocationExpression.Expression), 0);
			}
			StackSpiller stackSpiller = new StackSpiller(stack);
			lambdaOperand = lambdaOperand.Accept(stackSpiller);
			if (childRewriter.Rewrite || stackSpiller._lambdaRewrite != 0)
			{
				invocationExpression = new InvocationExpressionN(lambdaOperand, childRewriter[0, -1], invocationExpression.Type);
			}
			Result result = childRewriter.Finish(invocationExpression);
			return new Result(result.Action | stackSpiller._lambdaRewrite, result.Node);
		}
		childRewriter = new ChildRewriter(this, stack, invocationExpression.ArgumentCount + 1);
		childRewriter.Add(invocationExpression.Expression);
		childRewriter.AddArguments(invocationExpression);
		if (childRewriter.Action == RewriteAction.SpillStack)
		{
			childRewriter.MarkRefArgs(Expression.GetInvokeMethod(invocationExpression.Expression), 1);
		}
		return childRewriter.Finish(childRewriter.Rewrite ? new InvocationExpressionN(childRewriter[0], childRewriter[1, -1], invocationExpression.Type) : expr);
	}

	private Result RewriteNewExpression(Expression expr, Stack stack)
	{
		NewExpression newExpression = (NewExpression)expr;
		ChildRewriter childRewriter = new ChildRewriter(this, stack, newExpression.ArgumentCount);
		childRewriter.AddArguments(newExpression);
		if (childRewriter.Action == RewriteAction.SpillStack)
		{
			childRewriter.MarkRefArgs(newExpression.Constructor, 0);
		}
		return childRewriter.Finish(childRewriter.Rewrite ? new NewExpression(newExpression.Constructor, childRewriter[0, -1], newExpression.Members) : expr);
	}

	private Result RewriteTypeBinaryExpression(Expression expr, Stack stack)
	{
		TypeBinaryExpression typeBinaryExpression = (TypeBinaryExpression)expr;
		Result result = RewriteExpression(typeBinaryExpression.Expression, stack);
		if (result.Action != 0)
		{
			expr = new TypeBinaryExpression(result.Node, typeBinaryExpression.TypeOperand, typeBinaryExpression.NodeType);
		}
		return new Result(result.Action, expr);
	}

	private Result RewriteThrowUnaryExpression(Expression expr, Stack stack)
	{
		UnaryExpression unaryExpression = (UnaryExpression)expr;
		Result result = RewriteExpressionFreeTemps(unaryExpression.Operand, Stack.Empty);
		RewriteAction rewriteAction = result.Action;
		if (stack != 0)
		{
			rewriteAction = RewriteAction.SpillStack;
		}
		if (rewriteAction != 0)
		{
			expr = new UnaryExpression(ExpressionType.Throw, result.Node, unaryExpression.Type, null);
		}
		return new Result(rewriteAction, expr);
	}

	private Result RewriteUnaryExpression(Expression expr, Stack stack)
	{
		UnaryExpression unaryExpression = (UnaryExpression)expr;
		Result result = RewriteExpression(unaryExpression.Operand, stack);
		if (result.Action == RewriteAction.SpillStack)
		{
			RequireNoRefArgs(unaryExpression.Method);
		}
		if (result.Action != 0)
		{
			expr = new UnaryExpression(unaryExpression.NodeType, result.Node, unaryExpression.Type, unaryExpression.Method);
		}
		return new Result(result.Action, expr);
	}

	private Result RewriteListInitExpression(Expression expr, Stack stack)
	{
		ListInitExpression listInitExpression = (ListInitExpression)expr;
		Result result = RewriteExpression(listInitExpression.NewExpression, stack);
		Expression node = result.Node;
		RewriteAction rewriteAction = result.Action;
		ReadOnlyCollection<ElementInit> initializers = listInitExpression.Initializers;
		int count = initializers.Count;
		ChildRewriter[] array = new ChildRewriter[count];
		for (int i = 0; i < count; i++)
		{
			ElementInit elementInit = initializers[i];
			ChildRewriter childRewriter = new ChildRewriter(this, Stack.NonEmpty, elementInit.Arguments.Count);
			childRewriter.Add(elementInit.Arguments);
			rewriteAction |= childRewriter.Action;
			array[i] = childRewriter;
		}
		switch (rewriteAction)
		{
		case RewriteAction.Copy:
		{
			ElementInit[] array2 = new ElementInit[count];
			for (int k = 0; k < count; k++)
			{
				ChildRewriter childRewriter3 = array[k];
				if (childRewriter3.Action == RewriteAction.None)
				{
					array2[k] = initializers[k];
				}
				else
				{
					array2[k] = new ElementInit(initializers[k].AddMethod, new TrueReadOnlyCollection<Expression>(childRewriter3[0, -1]));
				}
			}
			expr = new ListInitExpression((NewExpression)node, new TrueReadOnlyCollection<ElementInit>(array2));
			break;
		}
		case RewriteAction.SpillStack:
		{
			bool flag = IsRefInstance(listInitExpression.NewExpression);
			System.Collections.Generic.ArrayBuilder<Expression> expressions = new System.Collections.Generic.ArrayBuilder<Expression>(count + 2 + (flag ? 1 : 0));
			ParameterExpression parameterExpression = MakeTemp(node.Type);
			expressions.UncheckedAdd(new AssignBinaryExpression(parameterExpression, node));
			ParameterExpression parameterExpression2 = parameterExpression;
			if (flag)
			{
				parameterExpression2 = MakeTemp(parameterExpression.Type.MakeByRefType());
				expressions.UncheckedAdd(new ByRefAssignBinaryExpression(parameterExpression2, parameterExpression));
			}
			for (int j = 0; j < count; j++)
			{
				ChildRewriter childRewriter2 = array[j];
				expressions.UncheckedAdd(childRewriter2.Finish(new InstanceMethodCallExpressionN(initializers[j].AddMethod, parameterExpression2, childRewriter2[0, -1])).Node);
			}
			expressions.UncheckedAdd(parameterExpression);
			expr = MakeBlock(expressions);
			break;
		}
		default:
			throw ContractUtils.Unreachable;
		case RewriteAction.None:
			break;
		}
		return new Result(rewriteAction, expr);
	}

	private Result RewriteMemberInitExpression(Expression expr, Stack stack)
	{
		MemberInitExpression memberInitExpression = (MemberInitExpression)expr;
		Result result = RewriteExpression(memberInitExpression.NewExpression, stack);
		Expression node = result.Node;
		RewriteAction rewriteAction = result.Action;
		ReadOnlyCollection<MemberBinding> bindings = memberInitExpression.Bindings;
		int count = bindings.Count;
		BindingRewriter[] array = new BindingRewriter[count];
		for (int i = 0; i < count; i++)
		{
			MemberBinding binding = bindings[i];
			rewriteAction |= (array[i] = BindingRewriter.Create(binding, this, Stack.NonEmpty)).Action;
		}
		switch (rewriteAction)
		{
		case RewriteAction.Copy:
		{
			MemberBinding[] array2 = new MemberBinding[count];
			for (int k = 0; k < count; k++)
			{
				array2[k] = array[k].AsBinding();
			}
			expr = new MemberInitExpression((NewExpression)node, new TrueReadOnlyCollection<MemberBinding>(array2));
			break;
		}
		case RewriteAction.SpillStack:
		{
			bool flag = IsRefInstance(memberInitExpression.NewExpression);
			System.Collections.Generic.ArrayBuilder<Expression> expressions = new System.Collections.Generic.ArrayBuilder<Expression>(count + 2 + (flag ? 1 : 0));
			ParameterExpression parameterExpression = MakeTemp(node.Type);
			expressions.UncheckedAdd(new AssignBinaryExpression(parameterExpression, node));
			ParameterExpression parameterExpression2 = parameterExpression;
			if (flag)
			{
				parameterExpression2 = MakeTemp(parameterExpression.Type.MakeByRefType());
				expressions.UncheckedAdd(new ByRefAssignBinaryExpression(parameterExpression2, parameterExpression));
			}
			for (int j = 0; j < count; j++)
			{
				BindingRewriter bindingRewriter = array[j];
				Expression item = bindingRewriter.AsExpression(parameterExpression2);
				expressions.UncheckedAdd(item);
			}
			expressions.UncheckedAdd(parameterExpression);
			expr = MakeBlock(expressions);
			break;
		}
		default:
			throw ContractUtils.Unreachable;
		case RewriteAction.None:
			break;
		}
		return new Result(rewriteAction, expr);
	}

	private Result RewriteBlockExpression(Expression expr, Stack stack)
	{
		BlockExpression blockExpression = (BlockExpression)expr;
		int expressionCount = blockExpression.ExpressionCount;
		RewriteAction rewriteAction = RewriteAction.None;
		Expression[] array = null;
		for (int i = 0; i < expressionCount; i++)
		{
			Expression expression = blockExpression.GetExpression(i);
			Result result = RewriteExpression(expression, stack);
			rewriteAction |= result.Action;
			if (array == null && result.Action != 0)
			{
				array = Clone(blockExpression.Expressions, i);
			}
			if (array != null)
			{
				array[i] = result.Node;
			}
		}
		if (rewriteAction != 0)
		{
			expr = blockExpression.Rewrite(null, array);
		}
		return new Result(rewriteAction, expr);
	}

	private Result RewriteLabelExpression(Expression expr, Stack stack)
	{
		LabelExpression labelExpression = (LabelExpression)expr;
		Result result = RewriteExpression(labelExpression.DefaultValue, stack);
		if (result.Action != 0)
		{
			expr = new LabelExpression(labelExpression.Target, result.Node);
		}
		return new Result(result.Action, expr);
	}

	private Result RewriteLoopExpression(Expression expr, Stack stack)
	{
		LoopExpression loopExpression = (LoopExpression)expr;
		Result result = RewriteExpression(loopExpression.Body, Stack.Empty);
		RewriteAction rewriteAction = result.Action;
		if (stack != 0)
		{
			rewriteAction = RewriteAction.SpillStack;
		}
		if (rewriteAction != 0)
		{
			expr = new LoopExpression(result.Node, loopExpression.BreakLabel, loopExpression.ContinueLabel);
		}
		return new Result(rewriteAction, expr);
	}

	private Result RewriteGotoExpression(Expression expr, Stack stack)
	{
		GotoExpression gotoExpression = (GotoExpression)expr;
		Result result = RewriteExpressionFreeTemps(gotoExpression.Value, Stack.Empty);
		RewriteAction rewriteAction = result.Action;
		if (stack != 0)
		{
			rewriteAction = RewriteAction.SpillStack;
		}
		if (rewriteAction != 0)
		{
			expr = Expression.MakeGoto(gotoExpression.Kind, gotoExpression.Target, result.Node, gotoExpression.Type);
		}
		return new Result(rewriteAction, expr);
	}

	private Result RewriteSwitchExpression(Expression expr, Stack stack)
	{
		SwitchExpression switchExpression = (SwitchExpression)expr;
		Result result = RewriteExpressionFreeTemps(switchExpression.SwitchValue, stack);
		RewriteAction rewriteAction = result.Action;
		ReadOnlyCollection<SwitchCase> readOnlyCollection = switchExpression.Cases;
		SwitchCase[] array = null;
		for (int i = 0; i < readOnlyCollection.Count; i++)
		{
			SwitchCase switchCase = readOnlyCollection[i];
			Expression[] array2 = null;
			ReadOnlyCollection<Expression> readOnlyCollection2 = switchCase.TestValues;
			for (int j = 0; j < readOnlyCollection2.Count; j++)
			{
				Result result2 = RewriteExpression(readOnlyCollection2[j], stack);
				rewriteAction |= result2.Action;
				if (array2 == null && result2.Action != 0)
				{
					array2 = Clone(readOnlyCollection2, j);
				}
				if (array2 != null)
				{
					array2[j] = result2.Node;
				}
			}
			Result result3 = RewriteExpression(switchCase.Body, stack);
			rewriteAction |= result3.Action;
			if (result3.Action != 0 || array2 != null)
			{
				if (array2 != null)
				{
					readOnlyCollection2 = new ReadOnlyCollection<Expression>(array2);
				}
				switchCase = new SwitchCase(result3.Node, readOnlyCollection2);
				if (array == null)
				{
					array = Clone(readOnlyCollection, i);
				}
			}
			if (array != null)
			{
				array[i] = switchCase;
			}
		}
		Result result4 = RewriteExpression(switchExpression.DefaultBody, stack);
		rewriteAction |= result4.Action;
		if (rewriteAction != 0)
		{
			if (array != null)
			{
				readOnlyCollection = new ReadOnlyCollection<SwitchCase>(array);
			}
			expr = new SwitchExpression(switchExpression.Type, result.Node, result4.Node, switchExpression.Comparison, readOnlyCollection);
		}
		return new Result(rewriteAction, expr);
	}

	private Result RewriteTryExpression(Expression expr, Stack stack)
	{
		TryExpression tryExpression = (TryExpression)expr;
		Result result = RewriteExpression(tryExpression.Body, Stack.Empty);
		ReadOnlyCollection<CatchBlock> readOnlyCollection = tryExpression.Handlers;
		CatchBlock[] array = null;
		RewriteAction rewriteAction = result.Action;
		if (readOnlyCollection != null)
		{
			for (int i = 0; i < readOnlyCollection.Count; i++)
			{
				RewriteAction rewriteAction2 = result.Action;
				CatchBlock catchBlock = readOnlyCollection[i];
				Expression filter = catchBlock.Filter;
				if (catchBlock.Filter != null)
				{
					Result result2 = RewriteExpression(catchBlock.Filter, Stack.Empty);
					rewriteAction |= result2.Action;
					rewriteAction2 |= result2.Action;
					filter = result2.Node;
				}
				Result result3 = RewriteExpression(catchBlock.Body, Stack.Empty);
				rewriteAction |= result3.Action;
				if ((rewriteAction2 | result3.Action) != 0)
				{
					catchBlock = Expression.MakeCatchBlock(catchBlock.Test, catchBlock.Variable, result3.Node, filter);
					if (array == null)
					{
						array = Clone(readOnlyCollection, i);
					}
				}
				if (array != null)
				{
					array[i] = catchBlock;
				}
			}
		}
		Result result4 = RewriteExpression(tryExpression.Fault, Stack.Empty);
		rewriteAction |= result4.Action;
		Result result5 = RewriteExpression(tryExpression.Finally, Stack.Empty);
		rewriteAction |= result5.Action;
		if (stack != 0)
		{
			rewriteAction = RewriteAction.SpillStack;
		}
		if (rewriteAction != 0)
		{
			if (array != null)
			{
				readOnlyCollection = new ReadOnlyCollection<CatchBlock>(array);
			}
			expr = new TryExpression(tryExpression.Type, result.Node, result5.Node, result4.Node, readOnlyCollection);
		}
		return new Result(rewriteAction, expr);
	}

	private Result RewriteExtensionExpression(Expression expr, Stack stack)
	{
		Result result = RewriteExpression(expr.ReduceExtensions(), stack);
		return new Result(result.Action | RewriteAction.Copy, result.Node);
	}

	private static T[] Clone<T>(ReadOnlyCollection<T> original, int max)
	{
		T[] array = new T[original.Count];
		for (int i = 0; i < max; i++)
		{
			array[i] = original[i];
		}
		return array;
	}

	private static void RequireNoRefArgs(MethodBase method)
	{
		if (method != null && method.GetParametersCached().Any((ParameterInfo p) => p.ParameterType.IsByRef))
		{
			throw Error.TryNotSupportedForMethodsWithRefArgs(method);
		}
	}

	private static void RequireNotRefInstance(Expression instance)
	{
		if (IsRefInstance(instance))
		{
			throw Error.TryNotSupportedForValueTypeInstances(instance.Type);
		}
	}

	private static bool IsRefInstance([NotNullWhen(true)] Expression instance)
	{
		if (instance != null && instance.Type.IsValueType)
		{
			return instance.Type.GetTypeCode() == TypeCode.Object;
		}
		return false;
	}

	private Result RewriteExpression(Expression node, Stack stack)
	{
		if (node == null)
		{
			return new Result(RewriteAction.None, null);
		}
		if (!_guard.TryEnterOnCurrentStack())
		{
			return _guard.RunOnEmptyStack((StackSpiller @this, Expression n, Stack s) => @this.RewriteExpression(n, s), this, node, stack);
		}
		Result result;
		switch (node.NodeType)
		{
		case ExpressionType.Add:
		case ExpressionType.AddChecked:
		case ExpressionType.And:
		case ExpressionType.ArrayIndex:
		case ExpressionType.Divide:
		case ExpressionType.Equal:
		case ExpressionType.ExclusiveOr:
		case ExpressionType.GreaterThan:
		case ExpressionType.GreaterThanOrEqual:
		case ExpressionType.LeftShift:
		case ExpressionType.LessThan:
		case ExpressionType.LessThanOrEqual:
		case ExpressionType.Modulo:
		case ExpressionType.Multiply:
		case ExpressionType.MultiplyChecked:
		case ExpressionType.NotEqual:
		case ExpressionType.Or:
		case ExpressionType.Power:
		case ExpressionType.RightShift:
		case ExpressionType.Subtract:
		case ExpressionType.SubtractChecked:
			result = RewriteBinaryExpression(node, stack);
			break;
		case ExpressionType.AndAlso:
		case ExpressionType.Coalesce:
		case ExpressionType.OrElse:
			result = RewriteLogicalBinaryExpression(node, stack);
			break;
		case ExpressionType.Assign:
			result = RewriteAssignBinaryExpression(node, stack);
			break;
		case ExpressionType.ArrayLength:
		case ExpressionType.Convert:
		case ExpressionType.ConvertChecked:
		case ExpressionType.Negate:
		case ExpressionType.UnaryPlus:
		case ExpressionType.NegateChecked:
		case ExpressionType.Not:
		case ExpressionType.TypeAs:
		case ExpressionType.Decrement:
		case ExpressionType.Increment:
		case ExpressionType.Unbox:
		case ExpressionType.OnesComplement:
		case ExpressionType.IsTrue:
		case ExpressionType.IsFalse:
			result = RewriteUnaryExpression(node, stack);
			break;
		case ExpressionType.Throw:
			result = RewriteThrowUnaryExpression(node, stack);
			break;
		case ExpressionType.Call:
			result = RewriteMethodCallExpression(node, stack);
			break;
		case ExpressionType.Conditional:
			result = RewriteConditionalExpression(node, stack);
			break;
		case ExpressionType.Invoke:
			result = RewriteInvocationExpression(node, stack);
			break;
		case ExpressionType.Lambda:
			result = RewriteLambdaExpression(node);
			break;
		case ExpressionType.ListInit:
			result = RewriteListInitExpression(node, stack);
			break;
		case ExpressionType.MemberAccess:
			result = RewriteMemberExpression(node, stack);
			break;
		case ExpressionType.MemberInit:
			result = RewriteMemberInitExpression(node, stack);
			break;
		case ExpressionType.New:
			result = RewriteNewExpression(node, stack);
			break;
		case ExpressionType.NewArrayInit:
		case ExpressionType.NewArrayBounds:
			result = RewriteNewArrayExpression(node, stack);
			break;
		case ExpressionType.TypeIs:
		case ExpressionType.TypeEqual:
			result = RewriteTypeBinaryExpression(node, stack);
			break;
		case ExpressionType.Block:
			result = RewriteBlockExpression(node, stack);
			break;
		case ExpressionType.Dynamic:
			result = RewriteDynamicExpression(node);
			break;
		case ExpressionType.Extension:
			result = RewriteExtensionExpression(node, stack);
			break;
		case ExpressionType.Goto:
			result = RewriteGotoExpression(node, stack);
			break;
		case ExpressionType.Index:
			result = RewriteIndexExpression(node, stack);
			break;
		case ExpressionType.Label:
			result = RewriteLabelExpression(node, stack);
			break;
		case ExpressionType.Loop:
			result = RewriteLoopExpression(node, stack);
			break;
		case ExpressionType.Switch:
			result = RewriteSwitchExpression(node, stack);
			break;
		case ExpressionType.Try:
			result = RewriteTryExpression(node, stack);
			break;
		case ExpressionType.AddAssign:
		case ExpressionType.AndAssign:
		case ExpressionType.DivideAssign:
		case ExpressionType.ExclusiveOrAssign:
		case ExpressionType.LeftShiftAssign:
		case ExpressionType.ModuloAssign:
		case ExpressionType.MultiplyAssign:
		case ExpressionType.OrAssign:
		case ExpressionType.PowerAssign:
		case ExpressionType.RightShiftAssign:
		case ExpressionType.SubtractAssign:
		case ExpressionType.AddAssignChecked:
		case ExpressionType.MultiplyAssignChecked:
		case ExpressionType.SubtractAssignChecked:
		case ExpressionType.PreIncrementAssign:
		case ExpressionType.PreDecrementAssign:
		case ExpressionType.PostIncrementAssign:
		case ExpressionType.PostDecrementAssign:
			result = RewriteReducibleExpression(node, stack);
			break;
		case ExpressionType.Constant:
		case ExpressionType.Parameter:
		case ExpressionType.Quote:
		case ExpressionType.DebugInfo:
		case ExpressionType.Default:
		case ExpressionType.RuntimeVariables:
			result = new Result(RewriteAction.None, node);
			break;
		default:
			result = RewriteExpression(node.ReduceAndCheck(), stack);
			if (result.Action == RewriteAction.None)
			{
				result = new Result(result.Action | RewriteAction.Copy, result.Node);
			}
			break;
		}
		return result;
	}

	private static Expression MakeBlock(System.Collections.Generic.ArrayBuilder<Expression> expressions)
	{
		return new SpilledExpressionBlock(expressions.ToArray());
	}

	private static Expression MakeBlock(params Expression[] expressions)
	{
		return new SpilledExpressionBlock(expressions);
	}

	private static Expression MakeBlock(IReadOnlyList<Expression> expressions)
	{
		return new SpilledExpressionBlock(expressions);
	}

	private ParameterExpression MakeTemp(Type type)
	{
		return _tm.Temp(type);
	}

	private int Mark()
	{
		return _tm.Mark();
	}

	private void Free(int mark)
	{
		_tm.Free(mark);
	}

	private ParameterExpression ToTemp(Expression expression, out Expression save, bool byRef)
	{
		Type type = (byRef ? expression.Type.MakeByRefType() : expression.Type);
		ParameterExpression parameterExpression = MakeTemp(type);
		save = AssignBinaryExpression.Make(parameterExpression, expression, byRef);
		return parameterExpression;
	}
}
