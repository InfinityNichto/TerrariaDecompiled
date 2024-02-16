using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Interpreter;

internal sealed class LocalVariable
{
	public readonly int Index;

	private int _flags;

	public bool IsBoxed
	{
		get
		{
			return (_flags & 1) != 0;
		}
		set
		{
			if (value)
			{
				_flags |= 1;
			}
			else
			{
				_flags &= -2;
			}
		}
	}

	public bool InClosure => (_flags & 2) != 0;

	internal LocalVariable(int index, bool closure)
	{
		Index = index;
		_flags = (closure ? 2 : 0);
	}

	public override string ToString()
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(3, 3, invariantCulture);
		handler.AppendFormatted(Index);
		handler.AppendLiteral(": ");
		handler.AppendFormatted(IsBoxed ? "boxed" : null);
		handler.AppendLiteral(" ");
		handler.AppendFormatted(InClosure ? "in closure" : null);
		return string.Create(invariantCulture, ref handler);
	}
}
