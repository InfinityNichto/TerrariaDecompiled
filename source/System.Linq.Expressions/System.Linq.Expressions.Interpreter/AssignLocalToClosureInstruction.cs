using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Interpreter;

internal sealed class AssignLocalToClosureInstruction : LocalAccessInstruction
{
	public override int ConsumedStack => 1;

	public override int ProducedStack => 1;

	public override string InstructionName => "AssignLocalClosure";

	internal AssignLocalToClosureInstruction(int index)
		: base(index)
	{
	}

	public override int Run(InterpretedFrame frame)
	{
		IStrongBox strongBox = frame.Closure[_index];
		strongBox.Value = frame.Peek();
		return 1;
	}
}
