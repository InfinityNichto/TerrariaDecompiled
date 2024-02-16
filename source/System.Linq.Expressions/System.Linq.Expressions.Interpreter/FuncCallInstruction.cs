using System.Reflection;

namespace System.Linq.Expressions.Interpreter;

internal sealed class FuncCallInstruction<TRet> : CallInstruction
{
	private readonly Func<TRet> _target;

	public override int ProducedStack => 1;

	public override int ArgumentCount => 0;

	public FuncCallInstruction(MethodInfo target)
	{
		_target = (Func<TRet>)target.CreateDelegate(typeof(Func<TRet>));
	}

	public override int Run(InterpretedFrame frame)
	{
		frame.Data[frame.StackIndex] = _target();
		frame.StackIndex -= -1;
		return 1;
	}

	public override string ToString()
	{
		return "Call(" + _target.Method?.ToString() + ")";
	}
}
internal sealed class FuncCallInstruction<T0, TRet> : CallInstruction
{
	private readonly bool _isInstance;

	private readonly Func<T0, TRet> _target;

	public override int ProducedStack => 1;

	public override int ArgumentCount => 1;

	public FuncCallInstruction(MethodInfo target)
	{
		_isInstance = !target.IsStatic;
		_target = (Func<T0, TRet>)target.CreateDelegate(typeof(Func<T0, TRet>));
	}

	public override int Run(InterpretedFrame frame)
	{
		object obj = frame.Data[frame.StackIndex - 1];
		if (_isInstance)
		{
			Instruction.NullCheck(obj);
		}
		LightLambda lightLambda;
		object obj2 = ((!_isInstance || !CallInstruction.TryGetLightLambdaTarget(obj, out lightLambda)) ? ((object)_target((T0)obj)) : InterpretLambdaInvoke(lightLambda, Array.Empty<object>()));
		frame.Data[frame.StackIndex - 1] = obj2;
		frame.StackIndex = frame.StackIndex;
		return 1;
	}

	public override string ToString()
	{
		return "Call(" + _target.Method?.ToString() + ")";
	}
}
internal sealed class FuncCallInstruction<T0, T1, TRet> : CallInstruction
{
	private readonly bool _isInstance;

	private readonly Func<T0, T1, TRet> _target;

	public override int ProducedStack => 1;

	public override int ArgumentCount => 2;

	public FuncCallInstruction(MethodInfo target)
	{
		_isInstance = !target.IsStatic;
		_target = (Func<T0, T1, TRet>)target.CreateDelegate(typeof(Func<T0, T1, TRet>));
	}

	public override int Run(InterpretedFrame frame)
	{
		object obj = frame.Data[frame.StackIndex - 2];
		if (_isInstance)
		{
			Instruction.NullCheck(obj);
		}
		LightLambda lightLambda;
		object obj2 = ((!_isInstance || !CallInstruction.TryGetLightLambdaTarget(obj, out lightLambda)) ? ((object)_target((T0)obj, (T1)frame.Data[frame.StackIndex - 1])) : InterpretLambdaInvoke(lightLambda, new object[1] { frame.Data[frame.StackIndex - 1] }));
		frame.Data[frame.StackIndex - 2] = obj2;
		frame.StackIndex--;
		return 1;
	}

	public override string ToString()
	{
		return "Call(" + _target.Method?.ToString() + ")";
	}
}
internal sealed class FuncCallInstruction<T0, T1, T2, TRet> : CallInstruction
{
	private readonly bool _isInstance;

	private readonly Func<T0, T1, T2, TRet> _target;

	public override int ProducedStack => 1;

	public override int ArgumentCount => 3;

	public FuncCallInstruction(MethodInfo target)
	{
		_isInstance = !target.IsStatic;
		_target = (Func<T0, T1, T2, TRet>)target.CreateDelegate(typeof(Func<T0, T1, T2, TRet>));
	}

	public override int Run(InterpretedFrame frame)
	{
		object obj = frame.Data[frame.StackIndex - 3];
		if (_isInstance)
		{
			Instruction.NullCheck(obj);
		}
		LightLambda lightLambda;
		object obj2 = ((!_isInstance || !CallInstruction.TryGetLightLambdaTarget(obj, out lightLambda)) ? ((object)_target((T0)obj, (T1)frame.Data[frame.StackIndex - 2], (T2)frame.Data[frame.StackIndex - 1])) : InterpretLambdaInvoke(lightLambda, new object[2]
		{
			frame.Data[frame.StackIndex - 2],
			frame.Data[frame.StackIndex - 1]
		}));
		frame.Data[frame.StackIndex - 3] = obj2;
		frame.StackIndex -= 2;
		return 1;
	}

	public override string ToString()
	{
		return "Call(" + _target.Method?.ToString() + ")";
	}
}
internal sealed class FuncCallInstruction<T0, T1, T2, T3, TRet> : CallInstruction
{
	private readonly bool _isInstance;

	private readonly Func<T0, T1, T2, T3, TRet> _target;

	public override int ProducedStack => 1;

	public override int ArgumentCount => 4;

	public FuncCallInstruction(MethodInfo target)
	{
		_isInstance = !target.IsStatic;
		_target = (Func<T0, T1, T2, T3, TRet>)target.CreateDelegate(typeof(Func<T0, T1, T2, T3, TRet>));
	}

	public override int Run(InterpretedFrame frame)
	{
		object obj = frame.Data[frame.StackIndex - 4];
		if (_isInstance)
		{
			Instruction.NullCheck(obj);
		}
		LightLambda lightLambda;
		object obj2 = ((!_isInstance || !CallInstruction.TryGetLightLambdaTarget(obj, out lightLambda)) ? ((object)_target((T0)obj, (T1)frame.Data[frame.StackIndex - 3], (T2)frame.Data[frame.StackIndex - 2], (T3)frame.Data[frame.StackIndex - 1])) : InterpretLambdaInvoke(lightLambda, new object[3]
		{
			frame.Data[frame.StackIndex - 3],
			frame.Data[frame.StackIndex - 2],
			frame.Data[frame.StackIndex - 1]
		}));
		frame.Data[frame.StackIndex - 4] = obj2;
		frame.StackIndex -= 3;
		return 1;
	}

	public override string ToString()
	{
		return "Call(" + _target.Method?.ToString() + ")";
	}
}
