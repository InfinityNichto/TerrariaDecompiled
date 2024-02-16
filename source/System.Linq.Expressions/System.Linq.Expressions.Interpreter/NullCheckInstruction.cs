namespace System.Linq.Expressions.Interpreter;

internal sealed class NullCheckInstruction : Instruction
{
	public static readonly Instruction Instance = new NullCheckInstruction();

	public override int ConsumedStack => 1;

	public override int ProducedStack => 1;

	public override string InstructionName => "Unbox";

	private NullCheckInstruction()
	{
	}

	public override int Run(InterpretedFrame frame)
	{
		Instruction.NullCheck(frame.Peek());
		return 1;
	}
}
