using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class GroupJoinHashLookupBuilder<TElement, TOrderKey, THashKey> : HashLookupBuilder<IEnumerable<TElement>, int, THashKey>
{
	private struct GroupJoinBaseHashBuilder : IBaseHashBuilder<TElement, TOrderKey>
	{
		private readonly HashLookup<THashKey, ListChunk<TElement>> _base;

		public GroupJoinBaseHashBuilder(HashLookup<THashKey, ListChunk<TElement>> baseLookup)
		{
			_base = baseLookup;
		}

		public bool Add(THashKey hashKey, TElement element, TOrderKey orderKey)
		{
			bool result = true;
			ListChunk<TElement> value = null;
			if (!_base.TryGetValue(hashKey, ref value))
			{
				value = new ListChunk<TElement>(2);
				_base.Add(hashKey, value);
				result = false;
			}
			value.Add(element);
			return result;
		}
	}

	private sealed class GroupJoinHashLookup : GroupJoinHashLookup<THashKey, TElement, ListChunk<TElement>, int>
	{
		protected override int EmptyValueKey => -559038737;

		internal GroupJoinHashLookup(HashLookup<THashKey, ListChunk<TElement>> lookup)
			: base(lookup)
		{
		}

		protected override Pair<IEnumerable<TElement>, int> CreateValuePair(ListChunk<TElement> baseValue)
		{
			return new Pair<IEnumerable<TElement>, int>(baseValue, -559038737);
		}
	}

	private readonly QueryOperatorEnumerator<Pair<TElement, THashKey>, TOrderKey> _dataSource;

	private readonly IEqualityComparer<THashKey> _keyComparer;

	internal GroupJoinHashLookupBuilder(QueryOperatorEnumerator<Pair<TElement, THashKey>, TOrderKey> dataSource, IEqualityComparer<THashKey> keyComparer)
	{
		_dataSource = dataSource;
		_keyComparer = keyComparer;
	}

	public override HashJoinHashLookup<THashKey, IEnumerable<TElement>, int> BuildHashLookup(CancellationToken cancellationToken)
	{
		HashLookup<THashKey, ListChunk<TElement>> hashLookup = new HashLookup<THashKey, ListChunk<TElement>>(_keyComparer);
		BuildBaseHashLookup<GroupJoinBaseHashBuilder, TElement, TOrderKey>(baseHashBuilder: new GroupJoinBaseHashBuilder(hashLookup), dataSource: _dataSource, cancellationToken: cancellationToken);
		return new GroupJoinHashLookup(hashLookup);
	}
}
