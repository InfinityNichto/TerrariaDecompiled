using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Interpreter;

internal sealed class ExceptionHandler
{
	private readonly Type _exceptionType;

	public readonly int LabelIndex;

	public readonly int HandlerStartIndex;

	public readonly int HandlerEndIndex;

	public readonly ExceptionFilter Filter;

	internal ExceptionHandler(int labelIndex, int handlerStartIndex, int handlerEndIndex, Type exceptionType, ExceptionFilter filter)
	{
		LabelIndex = labelIndex;
		_exceptionType = exceptionType;
		HandlerStartIndex = handlerStartIndex;
		HandlerEndIndex = handlerEndIndex;
		Filter = filter;
	}

	public bool Matches(Type exceptionType)
	{
		return _exceptionType.IsAssignableFrom(exceptionType);
	}

	public override string ToString()
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(13, 3, invariantCulture);
		handler.AppendLiteral("catch (");
		handler.AppendFormatted(_exceptionType.Name);
		handler.AppendLiteral(") [");
		handler.AppendFormatted(HandlerStartIndex);
		handler.AppendLiteral("->");
		handler.AppendFormatted(HandlerEndIndex);
		handler.AppendLiteral("]");
		return string.Create(invariantCulture, ref handler);
	}
}
