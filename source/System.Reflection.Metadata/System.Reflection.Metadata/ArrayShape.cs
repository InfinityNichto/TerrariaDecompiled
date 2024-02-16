using System.Collections.Immutable;

namespace System.Reflection.Metadata;

public readonly struct ArrayShape
{
	public int Rank { get; }

	public ImmutableArray<int> Sizes { get; }

	public ImmutableArray<int> LowerBounds { get; }

	public ArrayShape(int rank, ImmutableArray<int> sizes, ImmutableArray<int> lowerBounds)
	{
		Rank = rank;
		Sizes = sizes;
		LowerBounds = lowerBounds;
	}
}
