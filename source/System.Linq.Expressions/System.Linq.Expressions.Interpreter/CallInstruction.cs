using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions.Interpreter;

internal abstract class CallInstruction : Instruction
{
	private static readonly CacheDict<MethodInfo, CallInstruction> s_cache = new CacheDict<MethodInfo, CallInstruction>(256);

	public abstract int ArgumentCount { get; }

	public override string InstructionName => "Call";

	public override int ConsumedStack => ArgumentCount;

	public static CallInstruction Create(MethodInfo info)
	{
		return Create(info, info.GetParametersCached());
	}

	public static CallInstruction Create(MethodInfo info, ParameterInfo[] parameters)
	{
		int num = parameters.Length;
		if (!info.IsStatic)
		{
			num++;
		}
		if (info.DeclaringType != null && info.DeclaringType.IsArray && (info.Name == "Get" || info.Name == "Set"))
		{
			return GetArrayAccessor(info, num);
		}
		if (!info.IsStatic && info.DeclaringType.IsValueType)
		{
			return new MethodInfoCallInstruction(info, num);
		}
		if (num >= 5)
		{
			return new MethodInfoCallInstruction(info, num);
		}
		foreach (ParameterInfo parameterInfo in parameters)
		{
			if (parameterInfo.ParameterType.IsByRef)
			{
				return new MethodInfoCallInstruction(info, num);
			}
		}
		ShouldCache(info);
		if (s_cache.TryGetValue(info, out var value))
		{
			return value;
		}
		try
		{
			value = ((num >= 3) ? SlowCreate(info, parameters) : FastCreate(info, parameters));
		}
		catch (TargetInvocationException ex)
		{
			if (!(ex.InnerException is NotSupportedException))
			{
				throw;
			}
			value = new MethodInfoCallInstruction(info, num);
		}
		catch (NotSupportedException)
		{
			value = new MethodInfoCallInstruction(info, num);
		}
		ShouldCache(info);
		s_cache[info] = value;
		return value;
	}

	private static CallInstruction GetArrayAccessor(MethodInfo info, int argumentCount)
	{
		Type declaringType = info.DeclaringType;
		bool flag = info.Name == "Get";
		MethodInfo methodInfo = null;
		switch (declaringType.GetArrayRank())
		{
		case 1:
			methodInfo = (flag ? typeof(Array).GetMethod("GetValue", new Type[1] { typeof(int) }) : typeof(CallInstruction).GetMethod("ArrayItemSetter1"));
			break;
		case 2:
			methodInfo = (flag ? typeof(Array).GetMethod("GetValue", new Type[2]
			{
				typeof(int),
				typeof(int)
			}) : typeof(CallInstruction).GetMethod("ArrayItemSetter2"));
			break;
		case 3:
			methodInfo = (flag ? typeof(Array).GetMethod("GetValue", new Type[3]
			{
				typeof(int),
				typeof(int),
				typeof(int)
			}) : typeof(CallInstruction).GetMethod("ArrayItemSetter3"));
			break;
		}
		if ((object)methodInfo == null)
		{
			return new MethodInfoCallInstruction(info, argumentCount);
		}
		return Create(methodInfo);
	}

	public static void ArrayItemSetter1(Array array, int index0, object value)
	{
		array.SetValue(value, index0);
	}

	public static void ArrayItemSetter2(Array array, int index0, int index1, object value)
	{
		array.SetValue(value, index0, index1);
	}

	public static void ArrayItemSetter3(Array array, int index0, int index1, int index2, object value)
	{
		array.SetValue(value, index0, index1, index2);
	}

	private static bool ShouldCache(MethodInfo info)
	{
		return true;
	}

	private static Type TryGetParameterOrReturnType(MethodInfo target, ParameterInfo[] pi, int index)
	{
		if (!target.IsStatic)
		{
			index--;
			if (index < 0)
			{
				return target.DeclaringType;
			}
		}
		if (index < pi.Length)
		{
			return pi[index].ParameterType;
		}
		if (target.ReturnType == typeof(void) || index > pi.Length)
		{
			return null;
		}
		return target.ReturnType;
	}

	private static bool IndexIsNotReturnType(int index, MethodInfo target, ParameterInfo[] pi)
	{
		if (pi.Length == index)
		{
			if (pi.Length == index)
			{
				return !target.IsStatic;
			}
			return false;
		}
		return true;
	}

	private static CallInstruction SlowCreate(MethodInfo info, ParameterInfo[] pis)
	{
		List<Type> list = new List<Type>();
		if (!info.IsStatic)
		{
			list.Add(info.DeclaringType);
		}
		foreach (ParameterInfo parameterInfo in pis)
		{
			list.Add(parameterInfo.ParameterType);
		}
		if (info.ReturnType != typeof(void))
		{
			list.Add(info.ReturnType);
		}
		Type[] arrTypes = list.ToArray();
		try
		{
			return (CallInstruction)Activator.CreateInstance(GetHelperType(info, arrTypes), info);
		}
		catch (TargetInvocationException exception)
		{
			ExceptionHelpers.UnwrapAndRethrow(exception);
			throw ContractUtils.Unreachable;
		}
	}

	protected static bool TryGetLightLambdaTarget(object instance, [NotNullWhen(true)] out LightLambda lightLambda)
	{
		if (instance is Delegate { Target: Func<object[], object> target })
		{
			lightLambda = target.Target as LightLambda;
			if (lightLambda != null)
			{
				return true;
			}
		}
		lightLambda = null;
		return false;
	}

	protected object InterpretLambdaInvoke(LightLambda targetLambda, object[] args)
	{
		if (ProducedStack > 0)
		{
			return targetLambda.Run(args);
		}
		return targetLambda.RunVoid(args);
	}

	private static CallInstruction FastCreate(MethodInfo target, ParameterInfo[] pi)
	{
		Type type = TryGetParameterOrReturnType(target, pi, 0);
		if (type == null)
		{
			return new ActionCallInstruction(target);
		}
		if (type.IsEnum)
		{
			return SlowCreate(target, pi);
		}
		switch (type.GetTypeCode())
		{
		case TypeCode.Object:
			if (!(type != typeof(object)) || (!IndexIsNotReturnType(0, target, pi) && !type.IsValueType))
			{
				return FastCreate<object>(target, pi);
			}
			break;
		case TypeCode.Int16:
			return FastCreate<short>(target, pi);
		case TypeCode.Int32:
			return FastCreate<int>(target, pi);
		case TypeCode.Int64:
			return FastCreate<long>(target, pi);
		case TypeCode.Boolean:
			return FastCreate<bool>(target, pi);
		case TypeCode.Char:
			return FastCreate<char>(target, pi);
		case TypeCode.Byte:
			return FastCreate<byte>(target, pi);
		case TypeCode.Decimal:
			return FastCreate<decimal>(target, pi);
		case TypeCode.DateTime:
			return FastCreate<DateTime>(target, pi);
		case TypeCode.Double:
			return FastCreate<double>(target, pi);
		case TypeCode.Single:
			return FastCreate<float>(target, pi);
		case TypeCode.UInt16:
			return FastCreate<ushort>(target, pi);
		case TypeCode.UInt32:
			return FastCreate<uint>(target, pi);
		case TypeCode.UInt64:
			return FastCreate<ulong>(target, pi);
		case TypeCode.String:
			return FastCreate<string>(target, pi);
		case TypeCode.SByte:
			return FastCreate<sbyte>(target, pi);
		}
		return SlowCreate(target, pi);
	}

	private static CallInstruction FastCreate<T0>(MethodInfo target, ParameterInfo[] pi)
	{
		Type type = TryGetParameterOrReturnType(target, pi, 1);
		if (type == null)
		{
			if (target.ReturnType == typeof(void))
			{
				return new ActionCallInstruction<T0>(target);
			}
			return new FuncCallInstruction<T0>(target);
		}
		if (type.IsEnum)
		{
			return SlowCreate(target, pi);
		}
		switch (type.GetTypeCode())
		{
		case TypeCode.Object:
			if (!(type != typeof(object)) || (!IndexIsNotReturnType(1, target, pi) && !type.IsValueType))
			{
				return FastCreate<T0, object>(target, pi);
			}
			break;
		case TypeCode.Int16:
			return FastCreate<T0, short>(target, pi);
		case TypeCode.Int32:
			return FastCreate<T0, int>(target, pi);
		case TypeCode.Int64:
			return FastCreate<T0, long>(target, pi);
		case TypeCode.Boolean:
			return FastCreate<T0, bool>(target, pi);
		case TypeCode.Char:
			return FastCreate<T0, char>(target, pi);
		case TypeCode.Byte:
			return FastCreate<T0, byte>(target, pi);
		case TypeCode.Decimal:
			return FastCreate<T0, decimal>(target, pi);
		case TypeCode.DateTime:
			return FastCreate<T0, DateTime>(target, pi);
		case TypeCode.Double:
			return FastCreate<T0, double>(target, pi);
		case TypeCode.Single:
			return FastCreate<T0, float>(target, pi);
		case TypeCode.UInt16:
			return FastCreate<T0, ushort>(target, pi);
		case TypeCode.UInt32:
			return FastCreate<T0, uint>(target, pi);
		case TypeCode.UInt64:
			return FastCreate<T0, ulong>(target, pi);
		case TypeCode.String:
			return FastCreate<T0, string>(target, pi);
		case TypeCode.SByte:
			return FastCreate<T0, sbyte>(target, pi);
		}
		return SlowCreate(target, pi);
	}

	private static CallInstruction FastCreate<T0, T1>(MethodInfo target, ParameterInfo[] pi)
	{
		Type type = TryGetParameterOrReturnType(target, pi, 2);
		if (type == null)
		{
			if (target.ReturnType == typeof(void))
			{
				return new ActionCallInstruction<T0, T1>(target);
			}
			return new FuncCallInstruction<T0, T1>(target);
		}
		if (type.IsEnum)
		{
			return SlowCreate(target, pi);
		}
		switch (type.GetTypeCode())
		{
		case TypeCode.Object:
			if (!type.IsValueType)
			{
				return new FuncCallInstruction<T0, T1, object>(target);
			}
			break;
		case TypeCode.Int16:
			return new FuncCallInstruction<T0, T1, short>(target);
		case TypeCode.Int32:
			return new FuncCallInstruction<T0, T1, int>(target);
		case TypeCode.Int64:
			return new FuncCallInstruction<T0, T1, long>(target);
		case TypeCode.Boolean:
			return new FuncCallInstruction<T0, T1, bool>(target);
		case TypeCode.Char:
			return new FuncCallInstruction<T0, T1, char>(target);
		case TypeCode.Byte:
			return new FuncCallInstruction<T0, T1, byte>(target);
		case TypeCode.Decimal:
			return new FuncCallInstruction<T0, T1, decimal>(target);
		case TypeCode.DateTime:
			return new FuncCallInstruction<T0, T1, DateTime>(target);
		case TypeCode.Double:
			return new FuncCallInstruction<T0, T1, double>(target);
		case TypeCode.Single:
			return new FuncCallInstruction<T0, T1, float>(target);
		case TypeCode.UInt16:
			return new FuncCallInstruction<T0, T1, ushort>(target);
		case TypeCode.UInt32:
			return new FuncCallInstruction<T0, T1, uint>(target);
		case TypeCode.UInt64:
			return new FuncCallInstruction<T0, T1, ulong>(target);
		case TypeCode.String:
			return new FuncCallInstruction<T0, T1, string>(target);
		case TypeCode.SByte:
			return new FuncCallInstruction<T0, T1, sbyte>(target);
		}
		return SlowCreate(target, pi);
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	private static Type GetHelperType(MethodInfo info, Type[] arrTypes)
	{
		if (info.ReturnType == typeof(void))
		{
			return arrTypes.Length switch
			{
				0 => typeof(ActionCallInstruction), 
				1 => typeof(ActionCallInstruction<>).MakeGenericType(arrTypes), 
				2 => typeof(ActionCallInstruction<, >).MakeGenericType(arrTypes), 
				3 => typeof(ActionCallInstruction<, , >).MakeGenericType(arrTypes), 
				4 => typeof(ActionCallInstruction<, , , >).MakeGenericType(arrTypes), 
				_ => throw new InvalidOperationException(), 
			};
		}
		return arrTypes.Length switch
		{
			1 => typeof(FuncCallInstruction<>).MakeGenericType(arrTypes), 
			2 => typeof(FuncCallInstruction<, >).MakeGenericType(arrTypes), 
			3 => typeof(FuncCallInstruction<, , >).MakeGenericType(arrTypes), 
			4 => typeof(FuncCallInstruction<, , , >).MakeGenericType(arrTypes), 
			5 => typeof(FuncCallInstruction<, , , , >).MakeGenericType(arrTypes), 
			_ => throw new InvalidOperationException(), 
		};
	}
}
