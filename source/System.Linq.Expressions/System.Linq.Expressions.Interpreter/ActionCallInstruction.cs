using System.Reflection;

namespace System.Linq.Expressions.Interpreter;

internal sealed class ActionCallInstruction : CallInstruction
{
	private readonly Action _target;

	public override int ArgumentCount => 0;

	public override int ProducedStack => 0;

	public ActionCallInstruction(MethodInfo target)
	{
		_target = (Action)target.CreateDelegate(typeof(Action));
	}

	public override int Run(InterpretedFrame frame)
	{
		_target();
		frame.StackIndex = frame.StackIndex;
		return 1;
	}

	public override string ToString()
	{
		return "Call(" + _target.Method?.ToString() + ")";
	}
}
internal sealed class ActionCallInstruction<T0> : CallInstruction
{
	private readonly bool _isInstance;

	private readonly Action<T0> _target;

	public override int ProducedStack => 0;

	public override int ArgumentCount => 1;

	public ActionCallInstruction(MethodInfo target)
	{
		_isInstance = !target.IsStatic;
		_target = (Action<T0>)target.CreateDelegate(typeof(Action<T0>));
	}

	public override int Run(InterpretedFrame frame)
	{
		object obj = frame.Data[frame.StackIndex - 1];
		if (_isInstance)
		{
			Instruction.NullCheck(obj);
		}
		if (_isInstance && CallInstruction.TryGetLightLambdaTarget(obj, out var lightLambda))
		{
			InterpretLambdaInvoke(lightLambda, Array.Empty<object>());
		}
		else
		{
			_target((T0)obj);
		}
		frame.StackIndex--;
		return 1;
	}

	public override string ToString()
	{
		return "Call(" + _target.Method?.ToString() + ")";
	}
}
internal sealed class ActionCallInstruction<T0, T1> : CallInstruction
{
	private readonly bool _isInstance;

	private readonly Action<T0, T1> _target;

	public override int ProducedStack => 0;

	public override int ArgumentCount => 2;

	public ActionCallInstruction(MethodInfo target)
	{
		_isInstance = !target.IsStatic;
		_target = (Action<T0, T1>)target.CreateDelegate(typeof(Action<T0, T1>));
	}

	public override int Run(InterpretedFrame frame)
	{
		object obj = frame.Data[frame.StackIndex - 2];
		if (_isInstance)
		{
			Instruction.NullCheck(obj);
		}
		if (_isInstance && CallInstruction.TryGetLightLambdaTarget(obj, out var lightLambda))
		{
			InterpretLambdaInvoke(lightLambda, new object[1] { frame.Data[frame.StackIndex - 1] });
		}
		else
		{
			_target((T0)obj, (T1)frame.Data[frame.StackIndex - 1]);
		}
		frame.StackIndex -= 2;
		return 1;
	}

	public override string ToString()
	{
		return "Call(" + _target.Method?.ToString() + ")";
	}
}
internal sealed class ActionCallInstruction<T0, T1, T2> : CallInstruction
{
	private readonly bool _isInstance;

	private readonly Action<T0, T1, T2> _target;

	public override int ProducedStack => 0;

	public override int ArgumentCount => 3;

	public ActionCallInstruction(MethodInfo target)
	{
		_isInstance = !target.IsStatic;
		_target = (Action<T0, T1, T2>)target.CreateDelegate(typeof(Action<T0, T1, T2>));
	}

	public override int Run(InterpretedFrame frame)
	{
		object obj = frame.Data[frame.StackIndex - 3];
		if (_isInstance)
		{
			Instruction.NullCheck(obj);
		}
		if (_isInstance && CallInstruction.TryGetLightLambdaTarget(obj, out var lightLambda))
		{
			InterpretLambdaInvoke(lightLambda, new object[2]
			{
				frame.Data[frame.StackIndex - 2],
				frame.Data[frame.StackIndex - 1]
			});
		}
		else
		{
			_target((T0)obj, (T1)frame.Data[frame.StackIndex - 2], (T2)frame.Data[frame.StackIndex - 1]);
		}
		frame.StackIndex -= 3;
		return 1;
	}

	public override string ToString()
	{
		return "Call(" + _target.Method?.ToString() + ")";
	}
}
internal sealed class ActionCallInstruction<T0, T1, T2, T3> : CallInstruction
{
	private readonly bool _isInstance;

	private readonly Action<T0, T1, T2, T3> _target;

	public override int ProducedStack => 0;

	public override int ArgumentCount => 4;

	public ActionCallInstruction(MethodInfo target)
	{
		_isInstance = !target.IsStatic;
		_target = (Action<T0, T1, T2, T3>)target.CreateDelegate(typeof(Action<T0, T1, T2, T3>));
	}

	public override int Run(InterpretedFrame frame)
	{
		object obj = frame.Data[frame.StackIndex - 4];
		if (_isInstance)
		{
			Instruction.NullCheck(obj);
		}
		if (_isInstance && CallInstruction.TryGetLightLambdaTarget(obj, out var lightLambda))
		{
			InterpretLambdaInvoke(lightLambda, new object[3]
			{
				frame.Data[frame.StackIndex - 3],
				frame.Data[frame.StackIndex - 2],
				frame.Data[frame.StackIndex - 1]
			});
		}
		else
		{
			_target((T0)obj, (T1)frame.Data[frame.StackIndex - 3], (T2)frame.Data[frame.StackIndex - 2], (T3)frame.Data[frame.StackIndex - 1]);
		}
		frame.StackIndex -= 4;
		return 1;
	}

	public override string ToString()
	{
		return "Call(" + _target.Method?.ToString() + ")";
	}
}
