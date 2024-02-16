using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Linq;

internal sealed class EnumerableRewriter : ExpressionVisitor
{
	private Dictionary<LabelTarget, LabelTarget> _targetCache;

	private Dictionary<Type, Type> _equivalentTypeCache;

	private static ILookup<string, MethodInfo> s_seqMethods;

	[RequiresUnreferencedCode("Enumerating in-memory collections as IQueryable can require unreferenced code because expressions referencing IQueryable extension methods can get rebound to IEnumerable extension methods. The IEnumerable extension methods could be trimmed causing the application to fail at runtime.")]
	public EnumerableRewriter()
	{
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This class's ctor is annotated as RequiresUnreferencedCode.")]
	protected override Expression VisitMethodCall(MethodCallExpression m)
	{
		Expression expression = Visit(m.Object);
		ReadOnlyCollection<Expression> readOnlyCollection = Visit(m.Arguments);
		if (expression != m.Object || readOnlyCollection != m.Arguments)
		{
			MethodInfo method = m.Method;
			Type[] typeArgs = (method.IsGenericMethod ? method.GetGenericArguments() : null);
			if ((method.IsStatic || method.DeclaringType.IsAssignableFrom(expression.Type)) && ArgsMatch(method, readOnlyCollection, typeArgs))
			{
				return Expression.Call(expression, method, readOnlyCollection);
			}
			if (method.DeclaringType == typeof(Queryable))
			{
				MethodInfo methodInfo = FindEnumerableMethodForQueryable(method.Name, readOnlyCollection, typeArgs);
				readOnlyCollection = FixupQuotedArgs(methodInfo, readOnlyCollection);
				return Expression.Call(expression, methodInfo, readOnlyCollection);
			}
			MethodInfo methodInfo2 = FindMethod(method.DeclaringType, method.Name, readOnlyCollection, typeArgs);
			readOnlyCollection = FixupQuotedArgs(methodInfo2, readOnlyCollection);
			return Expression.Call(expression, methodInfo2, readOnlyCollection);
		}
		return m;
	}

	private ReadOnlyCollection<Expression> FixupQuotedArgs(MethodInfo mi, ReadOnlyCollection<Expression> argList)
	{
		ParameterInfo[] parameters = mi.GetParameters();
		if (parameters.Length != 0)
		{
			List<Expression> list = null;
			int i = 0;
			for (int num = parameters.Length; i < num; i++)
			{
				Expression expression = argList[i];
				ParameterInfo parameterInfo = parameters[i];
				expression = FixupQuotedExpression(parameterInfo.ParameterType, expression);
				if (list == null && expression != argList[i])
				{
					list = new List<Expression>(argList.Count);
					for (int j = 0; j < i; j++)
					{
						list.Add(argList[j]);
					}
				}
				list?.Add(expression);
			}
			if (list != null)
			{
				argList = list.AsReadOnly();
			}
		}
		return argList;
	}

	private Expression FixupQuotedExpression(Type type, Expression expression)
	{
		Expression expression2 = expression;
		while (true)
		{
			if (type.IsAssignableFrom(expression2.Type))
			{
				return expression2;
			}
			if (expression2.NodeType != ExpressionType.Quote)
			{
				break;
			}
			expression2 = ((UnaryExpression)expression2).Operand;
		}
		if (!type.IsAssignableFrom(expression2.Type) && type.IsArray && expression2.NodeType == ExpressionType.NewArrayInit)
		{
			Type c = StripExpression(expression2.Type);
			if (type.IsAssignableFrom(c))
			{
				Type elementType = type.GetElementType();
				NewArrayExpression newArrayExpression = (NewArrayExpression)expression2;
				List<Expression> list = new List<Expression>(newArrayExpression.Expressions.Count);
				int i = 0;
				for (int count = newArrayExpression.Expressions.Count; i < count; i++)
				{
					list.Add(FixupQuotedExpression(elementType, newArrayExpression.Expressions[i]));
				}
				expression = Expression.NewArrayInit(elementType, list);
			}
		}
		return expression;
	}

	protected override Expression VisitLambda<T>(Expression<T> node)
	{
		return node;
	}

	private static Type GetPublicType(Type t)
	{
		if (t.IsGenericType && ImplementsIGrouping(t))
		{
			return typeof(IGrouping<, >).MakeGenericType(t.GetGenericArguments());
		}
		if (!t.IsNestedPrivate)
		{
			return t;
		}
		if (TryGetImplementedIEnumerable(t, out var interfaceType2))
		{
			return interfaceType2;
		}
		if (typeof(IEnumerable).IsAssignableFrom(t))
		{
			return typeof(IEnumerable);
		}
		return t;
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "The IGrouping<,> is kept since it's directly referenced hereand so it will also be preserved in all places where it's implemented.The GetInterfaces may return less after trimming but it will includethe IGrouping<,> if it was there before trimming, which is enough for thismethod to work.")]
		static bool ImplementsIGrouping(Type type)
		{
			return type.GetGenericTypeDefinition().GetInterfaces().Contains(typeof(IGrouping<, >));
		}
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "The IEnumerable<> is kept since it's directly referenced hereand so it will also be preserved in all places where it's implemented.The GetInterfaces may return less after trimming but it will includethe IEnumerable<> if it was there before trimming, which is enough for thismethod to work.")]
		static bool TryGetImplementedIEnumerable(Type type, [NotNullWhen(true)] out Type interfaceType)
		{
			Type[] interfaces = type.GetInterfaces();
			foreach (Type type2 in interfaces)
			{
				if (type2.IsGenericType && type2.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				{
					interfaceType = type2;
					return true;
				}
			}
			interfaceType = null;
			return false;
		}
	}

	private Type GetEquivalentType(Type type)
	{
		if (_equivalentTypeCache == null)
		{
			_equivalentTypeCache = new Dictionary<Type, Type>
			{
				{
					typeof(IQueryable),
					typeof(IEnumerable)
				},
				{
					typeof(IEnumerable),
					typeof(IEnumerable)
				}
			};
		}
		if (!_equivalentTypeCache.TryGetValue(type, out var value))
		{
			Type publicType = GetPublicType(type);
			if (publicType.IsInterface && publicType.IsGenericType)
			{
				Type genericTypeDefinition = publicType.GetGenericTypeDefinition();
				if (genericTypeDefinition == typeof(IOrderedEnumerable<>))
				{
					value = publicType;
				}
				else if (genericTypeDefinition == typeof(IOrderedQueryable<>))
				{
					value = typeof(IOrderedEnumerable<>).MakeGenericType(publicType.GenericTypeArguments[0]);
				}
				else if (genericTypeDefinition == typeof(IEnumerable<>))
				{
					value = publicType;
				}
				else if (genericTypeDefinition == typeof(IQueryable<>))
				{
					value = typeof(IEnumerable<>).MakeGenericType(publicType.GenericTypeArguments[0]);
				}
			}
			if (value == null)
			{
				value = GetEquivalentTypeToEnumerables(publicType);
			}
			_equivalentTypeCache.Add(type, value);
		}
		return value;
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "The enumerable interface type (IOrderedQueryable<>, IOrderedEnumerable<>, IQueryable<> and IEnumerable<>) is kept since it's directly referenced hereand so it will also be preserved in all places where it's implemented.The GetInterfaces may return less after trimming but it will includethe enumerable interface type if it was there before trimming, which is enough for thismethod to work.")]
		static Type GetEquivalentTypeToEnumerables(Type sourceType)
		{
			Type[] interfaces = sourceType.GetInterfaces();
			var source = (from i in interfaces
				where i.IsGenericType && i.GenericTypeArguments.Length == 1
				select new
				{
					Info = i,
					GenType = i.GetGenericTypeDefinition()
				}).ToArray();
			Type type2 = (from i in source
				where i.GenType == typeof(IOrderedQueryable<>) || i.GenType == typeof(IOrderedEnumerable<>)
				select i.Info.GenericTypeArguments[0]).Distinct().SingleOrDefault();
			if (type2 != null)
			{
				return typeof(IOrderedEnumerable<>).MakeGenericType(type2);
			}
			type2 = (from i in source
				where i.GenType == typeof(IQueryable<>) || i.GenType == typeof(IEnumerable<>)
				select i.Info.GenericTypeArguments[0]).Distinct().Single();
			return typeof(IEnumerable<>).MakeGenericType(type2);
		}
	}

	protected override Expression VisitConstant(ConstantExpression c)
	{
		if (c.Value is EnumerableQuery enumerableQuery)
		{
			if (enumerableQuery.Enumerable != null)
			{
				Type publicType = GetPublicType(enumerableQuery.Enumerable.GetType());
				return Expression.Constant(enumerableQuery.Enumerable, publicType);
			}
			Expression expression = enumerableQuery.Expression;
			if (expression != c)
			{
				return Visit(expression);
			}
		}
		return c;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod", Justification = "Enumerable methods don't have trim annotations.")]
	private static MethodInfo FindEnumerableMethodForQueryable(string name, ReadOnlyCollection<Expression> args, params Type[] typeArgs)
	{
		if (s_seqMethods == null)
		{
			s_seqMethods = GetEnumerableStaticMethods(typeof(Enumerable)).ToLookup((MethodInfo m) => m.Name);
		}
		MethodInfo methodInfo = s_seqMethods[name].FirstOrDefault((MethodInfo m) => ArgsMatch(m, args, typeArgs));
		if (typeArgs != null)
		{
			return methodInfo.MakeGenericMethod(typeArgs);
		}
		return methodInfo;
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "This method is intentionally hiding the Enumerable type from the trimmer so it doesn't preserve all Enumerable's methods. This is safe because all Queryable methods have a DynamicDependency to the corresponding Enumerable method.")]
		static MethodInfo[] GetEnumerableStaticMethods(Type type)
		{
			return type.GetMethods(BindingFlags.Static | BindingFlags.Public);
		}
	}

	[RequiresUnreferencedCode("Enumerating in-memory collections as IQueryable can require unreferenced code because expressions referencing IQueryable extension methods can get rebound to IEnumerable extension methods. The IEnumerable extension methods could be trimmed causing the application to fail at runtime.")]
	private static MethodInfo FindMethod(Type type, string name, ReadOnlyCollection<Expression> args, Type[] typeArgs)
	{
		using (IEnumerator<MethodInfo> enumerator = (from m in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
			where m.Name == name
			select m).GetEnumerator())
		{
			if (!enumerator.MoveNext())
			{
				throw Error.NoMethodOnType(name, type);
			}
			do
			{
				MethodInfo current = enumerator.Current;
				if (ArgsMatch(current, args, typeArgs))
				{
					return (typeArgs != null) ? current.MakeGenericMethod(typeArgs) : current;
				}
			}
			while (enumerator.MoveNext());
		}
		throw Error.NoMethodOnTypeMatchingArguments(name, type);
	}

	private static bool ArgsMatch(MethodInfo m, ReadOnlyCollection<Expression> args, Type[] typeArgs)
	{
		ParameterInfo[] array = m.GetParameters();
		if (array.Length != args.Count)
		{
			return false;
		}
		if (!m.IsGenericMethod && typeArgs != null && typeArgs.Length != 0)
		{
			return false;
		}
		if (!m.IsGenericMethodDefinition && m.IsGenericMethod && m.ContainsGenericParameters)
		{
			m = m.GetGenericMethodDefinition();
		}
		if (m.IsGenericMethodDefinition)
		{
			if (typeArgs == null || typeArgs.Length == 0)
			{
				return false;
			}
			if (m.GetGenericArguments().Length != typeArgs.Length)
			{
				return false;
			}
			array = GetConstrutedGenericParameters(m, typeArgs);
		}
		int i = 0;
		for (int count = args.Count; i < count; i++)
		{
			Type type = array[i].ParameterType;
			if (type == null)
			{
				return false;
			}
			if (type.IsByRef)
			{
				type = type.GetElementType();
			}
			Expression expression = args[i];
			if (!type.IsAssignableFrom(expression.Type))
			{
				if (expression.NodeType == ExpressionType.Quote)
				{
					expression = ((UnaryExpression)expression).Operand;
				}
				if (!type.IsAssignableFrom(expression.Type) && !type.IsAssignableFrom(StripExpression(expression.Type)))
				{
					return false;
				}
			}
		}
		return true;
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod", Justification = "MakeGenericMethod is only called to get the parameter types, which are only used to make a 'match' decision. The generic method is not invoked.")]
		static ParameterInfo[] GetConstrutedGenericParameters(MethodInfo method, Type[] genericTypes)
		{
			return method.MakeGenericMethod(genericTypes).GetParameters();
		}
	}

	private static Type StripExpression(Type type)
	{
		bool isArray = type.IsArray;
		Type type2 = (isArray ? type.GetElementType() : type);
		Type type3 = TypeHelper.FindGenericType(typeof(Expression<>), type2);
		if (type3 != null)
		{
			type2 = type3.GetGenericArguments()[0];
		}
		if (isArray)
		{
			int arrayRank = type.GetArrayRank();
			if (arrayRank != 1)
			{
				return type2.MakeArrayType(arrayRank);
			}
			return type2.MakeArrayType();
		}
		return type;
	}

	protected override Expression VisitConditional(ConditionalExpression c)
	{
		Type type = c.Type;
		if (!typeof(IQueryable).IsAssignableFrom(type))
		{
			return base.VisitConditional(c);
		}
		Expression test = Visit(c.Test);
		Expression expression = Visit(c.IfTrue);
		Expression expression2 = Visit(c.IfFalse);
		Type type2 = expression.Type;
		Type type3 = expression2.Type;
		if (type2.IsAssignableFrom(type3))
		{
			return Expression.Condition(test, expression, expression2, type2);
		}
		if (type3.IsAssignableFrom(type2))
		{
			return Expression.Condition(test, expression, expression2, type3);
		}
		return Expression.Condition(test, expression, expression2, GetEquivalentType(type));
	}

	protected override Expression VisitBlock(BlockExpression node)
	{
		Type type = node.Type;
		if (!typeof(IQueryable).IsAssignableFrom(type))
		{
			return base.VisitBlock(node);
		}
		ReadOnlyCollection<Expression> expressions = Visit(node.Expressions);
		ReadOnlyCollection<ParameterExpression> variables = VisitAndConvert(node.Variables, "EnumerableRewriter.VisitBlock");
		if (type == node.Expressions.Last().Type)
		{
			return Expression.Block(variables, expressions);
		}
		return Expression.Block(GetEquivalentType(type), variables, expressions);
	}

	protected override Expression VisitGoto(GotoExpression node)
	{
		Type type = node.Value.Type;
		if (!typeof(IQueryable).IsAssignableFrom(type))
		{
			return base.VisitGoto(node);
		}
		LabelTarget target = VisitLabelTarget(node.Target);
		Expression expression = Visit(node.Value);
		return Expression.MakeGoto(node.Kind, target, expression, GetEquivalentType(typeof(EnumerableQuery).IsAssignableFrom(type) ? expression.Type : type));
	}

	protected override LabelTarget VisitLabelTarget(LabelTarget node)
	{
		LabelTarget value;
		if (_targetCache == null)
		{
			_targetCache = new Dictionary<LabelTarget, LabelTarget>();
		}
		else if (_targetCache.TryGetValue(node, out value))
		{
			return value;
		}
		Type type = node.Type;
		value = (typeof(IQueryable).IsAssignableFrom(type) ? Expression.Label(GetEquivalentType(type), node.Name) : base.VisitLabelTarget(node));
		_targetCache.Add(node, value);
		return value;
	}
}
