using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Interpreter;

internal sealed class LoadCachedObjectInstruction : Instruction
{
	private readonly uint _index;

	public override int ProducedStack => 1;

	public override string InstructionName => "LoadCachedObject";

	internal LoadCachedObjectInstruction(uint index)
	{
		_index = index;
	}

	public override int Run(InterpretedFrame frame)
	{
		frame.Data[frame.StackIndex++] = frame.Interpreter._objects[_index];
		return 1;
	}

	public override string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IReadOnlyList<object> objects)
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(14, 2, invariantCulture);
		handler.AppendLiteral("LoadCached(");
		handler.AppendFormatted(_index);
		handler.AppendLiteral(": ");
		handler.AppendFormatted<object>(objects[(int)_index]);
		handler.AppendLiteral(")");
		return string.Create(invariantCulture, ref handler);
	}

	public override string ToString()
	{
		return "LoadCached(" + _index + ")";
	}
}
