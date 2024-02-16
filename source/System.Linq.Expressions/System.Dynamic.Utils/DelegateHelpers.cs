using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace System.Dynamic.Utils;

internal static class DelegateHelpers
{
	private static readonly CacheDict<Type, MethodInfo> s_thunks = new CacheDict<Type, MethodInfo>(256);

	private static readonly MethodInfo s_FuncInvoke = typeof(Func<object[], object>).GetMethod("Invoke");

	private static readonly MethodInfo s_ArrayEmpty = GetEmptyObjectArrayMethod();

	private static readonly MethodInfo[] s_ActionThunks = GetActionThunks();

	private static readonly MethodInfo[] s_FuncThunks = GetFuncThunks();

	private static int s_ThunksCreated;

	internal static Delegate CreateObjectArrayDelegate(Type delegateType, Func<object[], object> handler)
	{
		return CreateObjectArrayDelegateRefEmit(delegateType, handler);
	}

	public static void ActionThunk(Func<object[], object> handler)
	{
		handler(Array.Empty<object>());
	}

	public static void ActionThunk1<T1>(Func<object[], object> handler, T1 t1)
	{
		handler(new object[1] { t1 });
	}

	public static void ActionThunk2<T1, T2>(Func<object[], object> handler, T1 t1, T2 t2)
	{
		handler(new object[2] { t1, t2 });
	}

	public static TReturn FuncThunk<TReturn>(Func<object[], object> handler)
	{
		return (TReturn)handler(Array.Empty<object>());
	}

	public static TReturn FuncThunk1<T1, TReturn>(Func<object[], object> handler, T1 t1)
	{
		return (TReturn)handler(new object[1] { t1 });
	}

	public static TReturn FuncThunk2<T1, T2, TReturn>(Func<object[], object> handler, T1 t1, T2 t2)
	{
		return (TReturn)handler(new object[2] { t1, t2 });
	}

	private static MethodInfo GetEmptyObjectArrayMethod()
	{
		return typeof(Array).GetMethod("Empty").MakeGenericMethod(typeof(object));
	}

	private static MethodInfo[] GetActionThunks()
	{
		Type typeFromHandle = typeof(DelegateHelpers);
		return new MethodInfo[3]
		{
			typeFromHandle.GetMethod("ActionThunk"),
			typeFromHandle.GetMethod("ActionThunk1"),
			typeFromHandle.GetMethod("ActionThunk2")
		};
	}

	private static MethodInfo[] GetFuncThunks()
	{
		Type typeFromHandle = typeof(DelegateHelpers);
		return new MethodInfo[3]
		{
			typeFromHandle.GetMethod("FuncThunk"),
			typeFromHandle.GetMethod("FuncThunk1"),
			typeFromHandle.GetMethod("FuncThunk2")
		};
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod", Justification = "The above ActionThunk and FuncThunk methods don't have trimming annotations.")]
	private static MethodInfo GetCSharpThunk(Type returnType, bool hasReturnValue, ParameterInfo[] parameters)
	{
		try
		{
			if (parameters.Length > 2)
			{
				return null;
			}
			if (returnType.IsByRefLike || returnType.IsByRef || returnType.IsPointer)
			{
				return null;
			}
			foreach (ParameterInfo parameterInfo in parameters)
			{
				Type parameterType = parameterInfo.ParameterType;
				if (parameterType.IsByRefLike || parameterType.IsByRef || parameterType.IsPointer)
				{
					return null;
				}
			}
			int num = parameters.Length;
			if (hasReturnValue)
			{
				num++;
			}
			Type[] array = ((num == 0) ? Type.EmptyTypes : new Type[num]);
			for (int j = 0; j < parameters.Length; j++)
			{
				array[j] = parameters[j].ParameterType;
			}
			MethodInfo methodInfo;
			if (hasReturnValue)
			{
				array[^1] = returnType;
				methodInfo = s_FuncThunks[parameters.Length];
			}
			else
			{
				methodInfo = s_ActionThunks[parameters.Length];
			}
			return (array.Length != 0) ? methodInfo.MakeGenericMethod(array) : methodInfo;
		}
		catch
		{
			return null;
		}
	}

	private static Delegate CreateObjectArrayDelegateRefEmit(Type delegateType, Func<object[], object> handler)
	{
		if (!s_thunks.TryGetValue(delegateType, out var value))
		{
			MethodInfo invokeMethod = delegateType.GetInvokeMethod();
			Type returnType = invokeMethod.ReturnType;
			bool flag = returnType != typeof(void);
			ParameterInfo[] parametersCached = invokeMethod.GetParametersCached();
			value = GetCSharpThunk(returnType, flag, parametersCached);
			if (value == null)
			{
				int value2 = Interlocked.Increment(ref s_ThunksCreated);
				Type[] array = new Type[parametersCached.Length + 1];
				array[0] = typeof(Func<object[], object>);
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("Thunk");
				stringBuilder.Append(value2);
				if (flag)
				{
					stringBuilder.Append("ret_");
					stringBuilder.Append(returnType.Name);
				}
				for (int i = 0; i < parametersCached.Length; i++)
				{
					stringBuilder.Append('_');
					stringBuilder.Append(parametersCached[i].ParameterType.Name);
					array[i + 1] = parametersCached[i].ParameterType;
				}
				DynamicMethod dynamicMethod = new DynamicMethod(stringBuilder.ToString(), returnType, array);
				value = dynamicMethod;
				ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
				LocalBuilder local = iLGenerator.DeclareLocal(typeof(object[]));
				LocalBuilder local2 = iLGenerator.DeclareLocal(typeof(object));
				if (parametersCached.Length == 0)
				{
					iLGenerator.Emit(OpCodes.Call, s_ArrayEmpty);
				}
				else
				{
					iLGenerator.Emit(OpCodes.Ldc_I4, parametersCached.Length);
					iLGenerator.Emit(OpCodes.Newarr, typeof(object));
				}
				iLGenerator.Emit(OpCodes.Stloc, local);
				bool flag2 = false;
				for (int j = 0; j < parametersCached.Length; j++)
				{
					bool isByRef = parametersCached[j].ParameterType.IsByRef;
					Type type = parametersCached[j].ParameterType;
					if (isByRef)
					{
						type = type.GetElementType();
					}
					flag2 = flag2 || isByRef;
					iLGenerator.Emit(OpCodes.Ldloc, local);
					iLGenerator.Emit(OpCodes.Ldc_I4, j);
					iLGenerator.Emit(OpCodes.Ldarg, j + 1);
					if (isByRef)
					{
						iLGenerator.Emit(OpCodes.Ldobj, type);
					}
					Type cls = ConvertToBoxableType(type);
					iLGenerator.Emit(OpCodes.Box, cls);
					iLGenerator.Emit(OpCodes.Stelem_Ref);
				}
				if (flag2)
				{
					iLGenerator.BeginExceptionBlock();
				}
				iLGenerator.Emit(OpCodes.Ldarg_0);
				iLGenerator.Emit(OpCodes.Ldloc, local);
				iLGenerator.Emit(OpCodes.Callvirt, s_FuncInvoke);
				iLGenerator.Emit(OpCodes.Stloc, local2);
				if (flag2)
				{
					iLGenerator.BeginFinallyBlock();
					for (int k = 0; k < parametersCached.Length; k++)
					{
						if (parametersCached[k].ParameterType.IsByRef)
						{
							Type elementType = parametersCached[k].ParameterType.GetElementType();
							iLGenerator.Emit(OpCodes.Ldarg, k + 1);
							iLGenerator.Emit(OpCodes.Ldloc, local);
							iLGenerator.Emit(OpCodes.Ldc_I4, k);
							iLGenerator.Emit(OpCodes.Ldelem_Ref);
							iLGenerator.Emit(OpCodes.Unbox_Any, elementType);
							iLGenerator.Emit(OpCodes.Stobj, elementType);
						}
					}
					iLGenerator.EndExceptionBlock();
				}
				if (flag)
				{
					iLGenerator.Emit(OpCodes.Ldloc, local2);
					iLGenerator.Emit(OpCodes.Unbox_Any, ConvertToBoxableType(returnType));
				}
				iLGenerator.Emit(OpCodes.Ret);
			}
			s_thunks[delegateType] = value;
		}
		return value.CreateDelegate(delegateType, handler);
	}

	private static Type ConvertToBoxableType(Type t)
	{
		if (!t.IsPointer)
		{
			return t;
		}
		return typeof(IntPtr);
	}
}
