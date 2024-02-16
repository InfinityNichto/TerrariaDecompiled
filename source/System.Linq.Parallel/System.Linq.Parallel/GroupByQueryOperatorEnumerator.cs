using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal abstract class GroupByQueryOperatorEnumerator<TSource, TGroupKey, TElement, TOrderKey> : QueryOperatorEnumerator<IGrouping<TGroupKey, TElement>, TOrderKey>
{
	private sealed class Mutables
	{
		internal HashLookup<Wrapper<TGroupKey>, ListChunk<TElement>> _hashLookup;

		internal int _hashLookupIndex;
	}

	protected readonly QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> _source;

	protected readonly IEqualityComparer<TGroupKey> _keyComparer;

	protected readonly CancellationToken _cancellationToken;

	private Mutables _mutables;

	protected GroupByQueryOperatorEnumerator(QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> source, IEqualityComparer<TGroupKey> keyComparer, CancellationToken cancellationToken)
	{
		_source = source;
		_keyComparer = keyComparer;
		_cancellationToken = cancellationToken;
	}

	internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref IGrouping<TGroupKey, TElement> currentElement, [AllowNull] ref TOrderKey currentKey)
	{
		Mutables mutables = _mutables;
		if (mutables == null)
		{
			mutables = (_mutables = new Mutables());
			mutables._hashLookup = BuildHashLookup();
			mutables._hashLookupIndex = -1;
		}
		if (++mutables._hashLookupIndex < mutables._hashLookup.Count)
		{
			currentElement = new GroupByGrouping<TGroupKey, TElement>(mutables._hashLookup[mutables._hashLookupIndex]);
			return true;
		}
		return false;
	}

	protected abstract HashLookup<Wrapper<TGroupKey>, ListChunk<TElement>> BuildHashLookup();

	protected override void Dispose(bool disposing)
	{
		_source.Dispose();
	}
}
