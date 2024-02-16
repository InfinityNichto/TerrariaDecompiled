namespace System.Diagnostics.Metrics;

internal interface IObjectSequence
{
	Span<object> AsSpan();
}
