using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Linq.Expressions.Interpreter;

[DebuggerDisplay("{DebugView,nq}")]
public class LightLambda
{
	private sealed class DebugViewPrinter
	{
		private readonly Interpreter _interpreter;

		private readonly Dictionary<int, int> _tryStart = new Dictionary<int, int>();

		private readonly Dictionary<int, string> _handlerEnter = new Dictionary<int, string>();

		private readonly Dictionary<int, int> _handlerExit = new Dictionary<int, int>();

		private string _indent = "  ";

		public DebugViewPrinter(Interpreter interpreter)
		{
			_interpreter = interpreter;
			Analyze();
		}

		private void Analyze()
		{
			Instruction[] instructions = _interpreter.Instructions.Instructions;
			Instruction[] array = instructions;
			foreach (Instruction instruction in array)
			{
				if (instruction is EnterTryCatchFinallyInstruction { Handler: var handler })
				{
					AddTryStart(handler.TryStartIndex);
					AddHandlerExit(handler.TryEndIndex + 1);
					if (handler.IsFinallyBlockExist)
					{
						_handlerEnter.Add(handler.FinallyStartIndex, "finally");
						AddHandlerExit(handler.FinallyEndIndex);
					}
					if (handler.IsCatchBlockExist)
					{
						ExceptionHandler[] handlers = handler.Handlers;
						foreach (ExceptionHandler exceptionHandler in handlers)
						{
							_handlerEnter.Add(exceptionHandler.HandlerStartIndex - 1, exceptionHandler.ToString());
							AddHandlerExit(exceptionHandler.HandlerEndIndex);
							ExceptionFilter filter = exceptionHandler.Filter;
							if (filter != null)
							{
								_handlerEnter.Add(filter.StartIndex - 1, "filter");
								AddHandlerExit(filter.EndIndex);
							}
						}
					}
				}
				if (instruction is EnterTryFaultInstruction { Handler: var handler2 })
				{
					AddTryStart(handler2.TryStartIndex);
					AddHandlerExit(handler2.TryEndIndex + 1);
					_handlerEnter.Add(handler2.FinallyStartIndex, "fault");
					AddHandlerExit(handler2.FinallyEndIndex);
				}
			}
		}

		private void AddTryStart(int index)
		{
			if (!_tryStart.TryGetValue(index, out var value))
			{
				_tryStart.Add(index, 1);
			}
			else
			{
				_tryStart[index] = value + 1;
			}
		}

		private void AddHandlerExit(int index)
		{
			_handlerExit[index] = ((!_handlerExit.TryGetValue(index, out var value)) ? 1 : (value + 1));
		}

		private void Indent()
		{
			_indent = new string(' ', _indent.Length + 2);
		}

		private void Dedent()
		{
			_indent = new string(' ', _indent.Length - 2);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			string value = _interpreter.Name ?? "lambda_method";
			stringBuilder.Append("object ").Append(value).AppendLine("(object[])");
			stringBuilder.AppendLine("{");
			stringBuilder.Append("  .locals ").Append(_interpreter.LocalCount).AppendLine();
			stringBuilder.Append("  .maxstack ").Append(_interpreter.Instructions.MaxStackDepth).AppendLine();
			stringBuilder.Append("  .maxcontinuation ").Append(_interpreter.Instructions.MaxContinuationDepth).AppendLine();
			stringBuilder.AppendLine();
			Instruction[] instructions = _interpreter.Instructions.Instructions;
			InstructionArray.DebugView debugView = new InstructionArray.DebugView(_interpreter.Instructions);
			InstructionList.DebugView.InstructionView[] instructionViews = debugView.GetInstructionViews();
			for (int i = 0; i < instructions.Length; i++)
			{
				EmitExits(stringBuilder, i);
				if (_tryStart.TryGetValue(i, out var value2))
				{
					for (int j = 0; j < value2; j++)
					{
						stringBuilder.Append(_indent).AppendLine(".try");
						stringBuilder.Append(_indent).AppendLine("{");
						Indent();
					}
				}
				if (_handlerEnter.TryGetValue(i, out var value3))
				{
					stringBuilder.Append(_indent).AppendLine(value3);
					stringBuilder.Append(_indent).AppendLine("{");
					Indent();
				}
				InstructionList.DebugView.InstructionView instructionView = instructionViews[i];
				StringBuilder stringBuilder2 = stringBuilder;
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 3, stringBuilder2, invariantCulture);
				handler.AppendFormatted(_indent);
				handler.AppendLiteral("IP_");
				handler.AppendFormatted(i.ToString().PadLeft(4, '0'));
				handler.AppendLiteral(": ");
				handler.AppendFormatted(instructionView.GetValue());
				stringBuilder2.AppendLine(invariantCulture, ref handler);
			}
			EmitExits(stringBuilder, instructions.Length);
			stringBuilder.AppendLine("}");
			return stringBuilder.ToString();
		}

		private void EmitExits(StringBuilder sb, int index)
		{
			if (_handlerExit.TryGetValue(index, out var value))
			{
				for (int i = 0; i < value; i++)
				{
					Dedent();
					sb.Append(_indent).AppendLine("}");
				}
			}
		}
	}

	private readonly IStrongBox[] _closure;

	private readonly Interpreter _interpreter;

	private readonly LightDelegateCreator _delegateCreator;

	private string DebugView => new DebugViewPrinter(_interpreter).ToString();

	internal LightLambda(LightDelegateCreator delegateCreator, IStrongBox[] closure)
	{
		_delegateCreator = delegateCreator;
		_closure = closure;
		_interpreter = delegateCreator.Interpreter;
	}

	internal Delegate MakeDelegate(Type delegateType)
	{
		MethodInfo invokeMethod = delegateType.GetInvokeMethod();
		if (invokeMethod.ReturnType == typeof(void))
		{
			return DelegateHelpers.CreateObjectArrayDelegate(delegateType, RunVoid);
		}
		return DelegateHelpers.CreateObjectArrayDelegate(delegateType, Run);
	}

	private InterpretedFrame MakeFrame()
	{
		return new InterpretedFrame(_interpreter, _closure);
	}

	public object? Run(params object?[] arguments)
	{
		InterpretedFrame interpretedFrame = MakeFrame();
		for (int i = 0; i < arguments.Length; i++)
		{
			interpretedFrame.Data[i] = arguments[i];
		}
		InterpretedFrame prevFrame = interpretedFrame.Enter();
		try
		{
			_interpreter.Run(interpretedFrame);
		}
		finally
		{
			for (int j = 0; j < arguments.Length; j++)
			{
				arguments[j] = interpretedFrame.Data[j];
			}
			interpretedFrame.Leave(prevFrame);
		}
		return interpretedFrame.Pop();
	}

	public object? RunVoid(params object?[] arguments)
	{
		InterpretedFrame interpretedFrame = MakeFrame();
		for (int i = 0; i < arguments.Length; i++)
		{
			interpretedFrame.Data[i] = arguments[i];
		}
		InterpretedFrame prevFrame = interpretedFrame.Enter();
		try
		{
			_interpreter.Run(interpretedFrame);
		}
		finally
		{
			for (int j = 0; j < arguments.Length; j++)
			{
				arguments[j] = interpretedFrame.Data[j];
			}
			interpretedFrame.Leave(prevFrame);
		}
		return null;
	}
}
