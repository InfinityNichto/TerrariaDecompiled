namespace System.Linq.Parallel;

internal enum OrdinalIndexState : byte
{
	Indexable,
	Correct,
	Increasing,
	Shuffled
}
