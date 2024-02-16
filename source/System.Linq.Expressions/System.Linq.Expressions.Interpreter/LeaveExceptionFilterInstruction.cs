using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Expressions.Interpreter;

internal sealed class LeaveExceptionFilterInstruction : Instruction
{
	internal static readonly LeaveExceptionFilterInstruction Instance = new LeaveExceptionFilterInstruction();

	public override string InstructionName => "LeaveExceptionFilter";

	public override int ConsumedStack => 2;

	private LeaveExceptionFilterInstruction()
	{
	}

	[ExcludeFromCodeCoverage(Justification = "Known to be a no-op, this instruction is skipped on execution")]
	public override int Run(InterpretedFrame frame)
	{
		return 1;
	}
}
