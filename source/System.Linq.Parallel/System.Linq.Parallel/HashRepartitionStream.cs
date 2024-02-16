using System.Collections.Generic;

namespace System.Linq.Parallel;

internal abstract class HashRepartitionStream<TInputOutput, THashKey, TOrderKey> : PartitionedStream<Pair<TInputOutput, THashKey>, TOrderKey>
{
	private readonly IEqualityComparer<THashKey> _keyComparer;

	private readonly IEqualityComparer<TInputOutput> _elementComparer;

	private readonly int _distributionMod;

	internal HashRepartitionStream(int partitionsCount, IComparer<TOrderKey> orderKeyComparer, IEqualityComparer<THashKey> hashKeyComparer, IEqualityComparer<TInputOutput> elementComparer)
		: base(partitionsCount, orderKeyComparer, OrdinalIndexState.Shuffled)
	{
		_keyComparer = hashKeyComparer;
		_elementComparer = elementComparer;
		checked
		{
			for (_distributionMod = 503; _distributionMod < partitionsCount; _distributionMod *= 2)
			{
			}
		}
	}

	internal int GetHashCode(TInputOutput element)
	{
		return (0x7FFFFFFF & ((element != null) ? (_elementComparer?.GetHashCode(element) ?? element.GetHashCode()) : 0)) % _distributionMod;
	}

	internal int GetHashCode(THashKey key)
	{
		return (0x7FFFFFFF & ((key != null) ? (_keyComparer?.GetHashCode(key) ?? key.GetHashCode()) : 0)) % _distributionMod;
	}
}
