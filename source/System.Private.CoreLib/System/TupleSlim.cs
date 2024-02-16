namespace System;

internal sealed class TupleSlim<T1, T2>
{
	public readonly T1 Item1;

	public readonly T2 Item2;

	public TupleSlim(T1 item1, T2 item2)
	{
		Item1 = item1;
		Item2 = item2;
	}
}
internal sealed class TupleSlim<T1, T2, T3>
{
	public readonly T1 Item1;

	public readonly T2 Item2;

	public readonly T3 Item3;

	public TupleSlim(T1 item1, T2 item2, T3 item3)
	{
		Item1 = item1;
		Item2 = item2;
		Item3 = item3;
	}
}
internal sealed class TupleSlim<T1, T2, T3, T4>
{
	public readonly T1 Item1;

	public readonly T2 Item2;

	public readonly T3 Item3;

	public readonly T4 Item4;

	public TupleSlim(T1 item1, T2 item2, T3 item3, T4 item4)
	{
		Item1 = item1;
		Item2 = item2;
		Item3 = item3;
		Item4 = item4;
	}
}
