using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Interpreter;

internal sealed class ParameterByRefUpdater : ByRefUpdater
{
	private readonly LocalVariable _parameter;

	public ParameterByRefUpdater(LocalVariable parameter, int argumentIndex)
		: base(argumentIndex)
	{
		_parameter = parameter;
	}

	public override void Update(InterpretedFrame frame, object value)
	{
		if (_parameter.InClosure)
		{
			IStrongBox strongBox = frame.Closure[_parameter.Index];
			strongBox.Value = value;
		}
		else if (_parameter.IsBoxed)
		{
			IStrongBox strongBox2 = (IStrongBox)frame.Data[_parameter.Index];
			strongBox2.Value = value;
		}
		else
		{
			frame.Data[_parameter.Index] = value;
		}
	}
}
