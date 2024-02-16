using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Interpreter;

internal sealed class DefaultValueInstruction : Instruction
{
	private readonly Type _type;

	public override int ProducedStack => 1;

	public override string InstructionName => "DefaultValue";

	internal DefaultValueInstruction(Type type)
	{
		_type = type;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2077:UnrecognizedReflectionPattern", Justification = "_type is a ValueType. You can always get an uninitialized ValueType.")]
	public override int Run(InterpretedFrame frame)
	{
		frame.Push(RuntimeHelpers.GetUninitializedObject(_type));
		return 1;
	}

	public override string ToString()
	{
		return "DefaultValue " + _type;
	}
}
