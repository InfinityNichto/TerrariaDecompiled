using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Dynamic.Utils;
using System.Linq.Expressions;
using System.Linq.Expressions.Compiler;

namespace System.Runtime.CompilerServices;

[EditorBrowsable(EditorBrowsableState.Never)]
[DebuggerStepThrough]
public static class RuntimeOps
{
	internal sealed class RuntimeVariables : IRuntimeVariables
	{
		private readonly IStrongBox[] _boxes;

		int IRuntimeVariables.Count => _boxes.Length;

		object IRuntimeVariables.this[int index]
		{
			get
			{
				return _boxes[index].Value;
			}
			set
			{
				_boxes[index].Value = value;
			}
		}

		internal RuntimeVariables(IStrongBox[] boxes)
		{
			_boxes = boxes;
		}
	}

	internal sealed class MergedRuntimeVariables : IRuntimeVariables
	{
		private readonly IRuntimeVariables _first;

		private readonly IRuntimeVariables _second;

		private readonly int[] _indexes;

		public int Count => _indexes.Length;

		public object this[int index]
		{
			get
			{
				index = _indexes[index];
				if (index < 0)
				{
					return _second[-1 - index];
				}
				return _first[index];
			}
			set
			{
				index = _indexes[index];
				if (index >= 0)
				{
					_first[index] = value;
				}
				else
				{
					_second[-1 - index] = value;
				}
			}
		}

		internal MergedRuntimeVariables(IRuntimeVariables first, IRuntimeVariables second, int[] indexes)
		{
			_first = first;
			_second = second;
			_indexes = indexes;
		}
	}

	private sealed class ExpressionQuoter : ExpressionVisitor
	{
		private readonly HoistedLocals _scope;

		private readonly object[] _locals;

		private readonly Stack<HashSet<ParameterExpression>> _shadowedVars = new Stack<HashSet<ParameterExpression>>();

		internal ExpressionQuoter(HoistedLocals scope, object[] locals)
		{
			_scope = scope;
			_locals = locals;
		}

		protected internal override Expression VisitLambda<T>(Expression<T> node)
		{
			if (node.ParameterCount > 0)
			{
				HashSet<ParameterExpression> hashSet = new HashSet<ParameterExpression>();
				int i = 0;
				for (int parameterCount = node.ParameterCount; i < parameterCount; i++)
				{
					hashSet.Add(node.GetParameter(i));
				}
				_shadowedVars.Push(hashSet);
			}
			Expression expression = Visit(node.Body);
			if (node.ParameterCount > 0)
			{
				_shadowedVars.Pop();
			}
			if (expression == node.Body)
			{
				return node;
			}
			return node.Rewrite(expression, null);
		}

		protected internal override Expression VisitBlock(BlockExpression node)
		{
			if (node.Variables.Count > 0)
			{
				_shadowedVars.Push(new HashSet<ParameterExpression>(node.Variables));
			}
			Expression[] array = ExpressionVisitorUtils.VisitBlockExpressions(this, node);
			if (node.Variables.Count > 0)
			{
				_shadowedVars.Pop();
			}
			if (array == null)
			{
				return node;
			}
			return node.Rewrite(node.Variables, array);
		}

		protected override CatchBlock VisitCatchBlock(CatchBlock node)
		{
			if (node.Variable != null)
			{
				_shadowedVars.Push(new HashSet<ParameterExpression> { node.Variable });
			}
			Expression expression = Visit(node.Body);
			Expression expression2 = Visit(node.Filter);
			if (node.Variable != null)
			{
				_shadowedVars.Pop();
			}
			if (expression == node.Body && expression2 == node.Filter)
			{
				return node;
			}
			return Expression.MakeCatchBlock(node.Test, node.Variable, expression, expression2);
		}

		protected internal override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
		{
			int count = node.Variables.Count;
			List<IStrongBox> list = new List<IStrongBox>();
			List<ParameterExpression> list2 = new List<ParameterExpression>();
			int[] array = new int[count];
			for (int i = 0; i < array.Length; i++)
			{
				IStrongBox box = GetBox(node.Variables[i]);
				if (box == null)
				{
					array[i] = list2.Count;
					list2.Add(node.Variables[i]);
				}
				else
				{
					array[i] = -1 - list.Count;
					list.Add(box);
				}
			}
			if (list.Count == 0)
			{
				return node;
			}
			ConstantExpression constantExpression = Expression.Constant(new RuntimeVariables(list.ToArray()), typeof(IRuntimeVariables));
			if (list2.Count == 0)
			{
				return constantExpression;
			}
			return Expression.Call(CachedReflectionInfo.RuntimeOps_MergeRuntimeVariables, Expression.RuntimeVariables(new TrueReadOnlyCollection<ParameterExpression>(list2.ToArray())), constantExpression, Expression.Constant(array));
		}

		protected internal override Expression VisitParameter(ParameterExpression node)
		{
			IStrongBox box = GetBox(node);
			if (box == null)
			{
				return node;
			}
			return Utils.GetStrongBoxValueField(Expression.Constant(box));
		}

		private IStrongBox GetBox(ParameterExpression variable)
		{
			foreach (HashSet<ParameterExpression> shadowedVar in _shadowedVars)
			{
				if (shadowedVar.Contains(variable))
				{
					return null;
				}
			}
			HoistedLocals hoistedLocals = _scope;
			object[] array = _locals;
			while (true)
			{
				if (hoistedLocals.Indexes.TryGetValue(variable, out var value))
				{
					return (IStrongBox)array[value];
				}
				hoistedLocals = hoistedLocals.Parent;
				if (hoistedLocals == null)
				{
					break;
				}
				array = HoistedLocals.GetParent(array);
			}
			throw ContractUtils.Unreachable;
		}
	}

	private sealed class EmptyRuntimeVariables : IRuntimeVariables
	{
		int IRuntimeVariables.Count => 0;

		object IRuntimeVariables.this[int index]
		{
			get
			{
				throw new IndexOutOfRangeException();
			}
			set
			{
				throw new IndexOutOfRangeException();
			}
		}
	}

	private sealed class RuntimeVariableList : IRuntimeVariables
	{
		private readonly object[] _data;

		private readonly long[] _indexes;

		public int Count => _indexes.Length;

		public object this[int index]
		{
			get
			{
				return GetStrongBox(index).Value;
			}
			set
			{
				GetStrongBox(index).Value = value;
			}
		}

		internal RuntimeVariableList(object[] data, long[] indexes)
		{
			_data = data;
			_indexes = indexes;
		}

		private IStrongBox GetStrongBox(int index)
		{
			long num = _indexes[index];
			object[] array = _data;
			for (int num2 = (int)(num >> 32); num2 > 0; num2--)
			{
				array = HoistedLocals.GetParent(array);
			}
			return (IStrongBox)array[(int)num];
		}
	}

	[Obsolete("RuntimeOps has been deprecated and is not supported.", true)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static bool ExpandoTryGetValue(ExpandoObject expando, object? indexClass, int index, string name, bool ignoreCase, out object? value)
	{
		return expando.TryGetValue(indexClass, index, name, ignoreCase, out value);
	}

	[Obsolete("RuntimeOps has been deprecated and is not supported.", true)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static object? ExpandoTrySetValue(ExpandoObject expando, object? indexClass, int index, object? value, string name, bool ignoreCase)
	{
		expando.TrySetValue(indexClass, index, value, name, ignoreCase, add: false);
		return value;
	}

	[Obsolete("RuntimeOps has been deprecated and is not supported.", true)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static bool ExpandoTryDeleteValue(ExpandoObject expando, object? indexClass, int index, string name, bool ignoreCase)
	{
		return expando.TryDeleteValue(indexClass, index, name, ignoreCase, ExpandoObject.Uninitialized);
	}

	[Obsolete("RuntimeOps has been deprecated and is not supported.", true)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static bool ExpandoCheckVersion(ExpandoObject expando, object? version)
	{
		return expando.Class == version;
	}

	[Obsolete("RuntimeOps has been deprecated and is not supported.", true)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static void ExpandoPromoteClass(ExpandoObject expando, object oldClass, object newClass)
	{
		expando.PromoteClass(oldClass, newClass);
	}

	[Obsolete("RuntimeOps has been deprecated and is not supported.", true)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	[return: NotNullIfNotNull("expression")]
	public static Expression? Quote(Expression? expression, object hoistedLocals, object[] locals)
	{
		ExpressionQuoter expressionQuoter = new ExpressionQuoter((HoistedLocals)hoistedLocals, locals);
		return expressionQuoter.Visit(expression);
	}

	[Obsolete("RuntimeOps has been deprecated and is not supported.", true)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static IRuntimeVariables MergeRuntimeVariables(IRuntimeVariables first, IRuntimeVariables second, int[] indexes)
	{
		return new MergedRuntimeVariables(first, second, indexes);
	}

	[Obsolete("RuntimeOps has been deprecated and is not supported.", true)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static IRuntimeVariables CreateRuntimeVariables(object[] data, long[] indexes)
	{
		return new RuntimeVariableList(data, indexes);
	}

	[Obsolete("RuntimeOps has been deprecated and is not supported.", true)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static IRuntimeVariables CreateRuntimeVariables()
	{
		return new EmptyRuntimeVariables();
	}
}
