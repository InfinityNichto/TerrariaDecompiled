namespace System.Diagnostics.Metrics;

internal interface IStringSequence
{
	Span<string> AsSpan();
}
