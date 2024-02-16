using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Interpreter;

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

	private RuntimeVariables(IStrongBox[] boxes)
	{
		_boxes = boxes;
	}

	internal static IRuntimeVariables Create(IStrongBox[] boxes)
	{
		return new RuntimeVariables(boxes);
	}
}
