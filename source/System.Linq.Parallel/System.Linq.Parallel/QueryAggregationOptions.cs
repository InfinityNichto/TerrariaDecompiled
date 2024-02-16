namespace System.Linq.Parallel;

[Flags]
internal enum QueryAggregationOptions
{
	None = 0,
	Associative = 1,
	Commutative = 2,
	AssociativeCommutative = 3
}
