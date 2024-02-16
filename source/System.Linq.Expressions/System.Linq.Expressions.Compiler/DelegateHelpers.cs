using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Dynamic.Utils;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Compiler;

internal static class DelegateHelpers
{
	internal sealed class TypeInfo
	{
		public Type DelegateType;

		public Dictionary<Type, TypeInfo> TypeChain;
	}

	private static readonly Type[] s_delegateCtorSignature = new Type[2]
	{
		typeof(object),
		typeof(IntPtr)
	};

	private static TypeInfo _DelegateCache = new TypeInfo();

	internal static Type MakeCallSiteDelegate(ReadOnlyCollection<Expression> types, Type returnType)
	{
		lock (_DelegateCache)
		{
			TypeInfo delegateCache = _DelegateCache;
			delegateCache = NextTypeInfo(typeof(CallSite), delegateCache);
			for (int i = 0; i < types.Count; i++)
			{
				delegateCache = NextTypeInfo(types[i].Type, delegateCache);
			}
			delegateCache = NextTypeInfo(returnType, delegateCache);
			if (delegateCache.DelegateType == null)
			{
				delegateCache.MakeDelegateType(returnType, types);
			}
			return delegateCache.DelegateType;
		}
	}

	internal static Type MakeDeferredSiteDelegate(DynamicMetaObject[] args, Type returnType)
	{
		lock (_DelegateCache)
		{
			TypeInfo delegateCache = _DelegateCache;
			delegateCache = NextTypeInfo(typeof(CallSite), delegateCache);
			foreach (DynamicMetaObject dynamicMetaObject in args)
			{
				Type type = dynamicMetaObject.Expression.Type;
				if (IsByRef(dynamicMetaObject))
				{
					type = type.MakeByRefType();
				}
				delegateCache = NextTypeInfo(type, delegateCache);
			}
			delegateCache = NextTypeInfo(returnType, delegateCache);
			if (delegateCache.DelegateType == null)
			{
				Type[] array = new Type[args.Length + 2];
				array[0] = typeof(CallSite);
				array[^1] = returnType;
				for (int j = 0; j < args.Length; j++)
				{
					DynamicMetaObject dynamicMetaObject2 = args[j];
					Type type2 = dynamicMetaObject2.Expression.Type;
					if (IsByRef(dynamicMetaObject2))
					{
						type2 = type2.MakeByRefType();
					}
					array[j + 1] = type2;
				}
				delegateCache.DelegateType = MakeNewDelegate(array);
			}
			return delegateCache.DelegateType;
		}
	}

	private static bool IsByRef(DynamicMetaObject mo)
	{
		if (mo.Expression is ParameterExpression parameterExpression)
		{
			return parameterExpression.IsByRef;
		}
		return false;
	}

	private static Type MakeNewCustomDelegate(Type[] types)
	{
		Type returnType = types[^1];
		Type[] parameterTypes = types.RemoveLast();
		TypeBuilder typeBuilder = AssemblyGen.DefineDelegateType("Delegate" + types.Length);
		typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName, CallingConventions.Standard, s_delegateCtorSignature).SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
		typeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.VtableLayoutMask, returnType, parameterTypes).SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
		return typeBuilder.CreateTypeInfo();
	}

	internal static Type MakeDelegateType(Type[] types)
	{
		lock (_DelegateCache)
		{
			TypeInfo typeInfo = _DelegateCache;
			for (int i = 0; i < types.Length; i++)
			{
				typeInfo = NextTypeInfo(types[i], typeInfo);
			}
			if (typeInfo.DelegateType == null)
			{
				typeInfo.DelegateType = MakeNewDelegate((Type[])types.Clone());
			}
			return typeInfo.DelegateType;
		}
	}

	internal static TypeInfo NextTypeInfo(Type initialArg)
	{
		lock (_DelegateCache)
		{
			return NextTypeInfo(initialArg, _DelegateCache);
		}
	}

	internal static TypeInfo GetNextTypeInfo(Type initialArg, TypeInfo curTypeInfo)
	{
		lock (_DelegateCache)
		{
			return NextTypeInfo(initialArg, curTypeInfo);
		}
	}

	private static TypeInfo NextTypeInfo(Type initialArg, TypeInfo curTypeInfo)
	{
		if (curTypeInfo.TypeChain == null)
		{
			curTypeInfo.TypeChain = new Dictionary<Type, TypeInfo>();
		}
		if (!curTypeInfo.TypeChain.TryGetValue(initialArg, out var value))
		{
			value = new TypeInfo();
			if (!initialArg.IsCollectible)
			{
				curTypeInfo.TypeChain[initialArg] = value;
			}
		}
		return value;
	}

	internal static Type MakeNewDelegate(Type[] types)
	{
		bool flag;
		if (types.Length > 17)
		{
			flag = true;
		}
		else
		{
			flag = false;
			foreach (Type type in types)
			{
				if (type.IsByRef || type.IsByRefLike || type.IsPointer)
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			return MakeNewCustomDelegate(types);
		}
		if (types[^1] == typeof(void))
		{
			return GetActionType(types.RemoveLast());
		}
		return GetFuncType(types);
	}

	internal static Type GetFuncType(Type[] types)
	{
		return types.Length switch
		{
			1 => typeof(Func<>).MakeGenericType(types), 
			2 => typeof(Func<, >).MakeGenericType(types), 
			3 => typeof(Func<, , >).MakeGenericType(types), 
			4 => typeof(Func<, , , >).MakeGenericType(types), 
			5 => typeof(Func<, , , , >).MakeGenericType(types), 
			6 => typeof(Func<, , , , , >).MakeGenericType(types), 
			7 => typeof(Func<, , , , , , >).MakeGenericType(types), 
			8 => typeof(Func<, , , , , , , >).MakeGenericType(types), 
			9 => typeof(Func<, , , , , , , , >).MakeGenericType(types), 
			10 => typeof(Func<, , , , , , , , , >).MakeGenericType(types), 
			11 => typeof(Func<, , , , , , , , , , >).MakeGenericType(types), 
			12 => typeof(Func<, , , , , , , , , , , >).MakeGenericType(types), 
			13 => typeof(Func<, , , , , , , , , , , , >).MakeGenericType(types), 
			14 => typeof(Func<, , , , , , , , , , , , , >).MakeGenericType(types), 
			15 => typeof(Func<, , , , , , , , , , , , , , >).MakeGenericType(types), 
			16 => typeof(Func<, , , , , , , , , , , , , , , >).MakeGenericType(types), 
			17 => typeof(Func<, , , , , , , , , , , , , , , , >).MakeGenericType(types), 
			_ => null, 
		};
	}

	internal static Type GetActionType(Type[] types)
	{
		return types.Length switch
		{
			0 => typeof(Action), 
			1 => typeof(Action<>).MakeGenericType(types), 
			2 => typeof(Action<, >).MakeGenericType(types), 
			3 => typeof(Action<, , >).MakeGenericType(types), 
			4 => typeof(Action<, , , >).MakeGenericType(types), 
			5 => typeof(Action<, , , , >).MakeGenericType(types), 
			6 => typeof(Action<, , , , , >).MakeGenericType(types), 
			7 => typeof(Action<, , , , , , >).MakeGenericType(types), 
			8 => typeof(Action<, , , , , , , >).MakeGenericType(types), 
			9 => typeof(Action<, , , , , , , , >).MakeGenericType(types), 
			10 => typeof(Action<, , , , , , , , , >).MakeGenericType(types), 
			11 => typeof(Action<, , , , , , , , , , >).MakeGenericType(types), 
			12 => typeof(Action<, , , , , , , , , , , >).MakeGenericType(types), 
			13 => typeof(Action<, , , , , , , , , , , , >).MakeGenericType(types), 
			14 => typeof(Action<, , , , , , , , , , , , , >).MakeGenericType(types), 
			15 => typeof(Action<, , , , , , , , , , , , , , >).MakeGenericType(types), 
			16 => typeof(Action<, , , , , , , , , , , , , , , >).MakeGenericType(types), 
			_ => null, 
		};
	}
}
