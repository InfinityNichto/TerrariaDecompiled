using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Expressions.Interpreter;

internal sealed class EnterExceptionFilterInstruction : Instruction
{
	internal static readonly EnterExceptionFilterInstruction Instance = new EnterExceptionFilterInstruction();

	public override string InstructionName => "EnterExceptionFilter";

	public override int ProducedStack => 1;

	private EnterExceptionFilterInstruction()
	{
	}

	[ExcludeFromCodeCoverage(Justification = "Known to be a no-op, this instruction is skipped on execution")]
	public override int Run(InterpretedFrame frame)
	{
		return 1;
	}
}
