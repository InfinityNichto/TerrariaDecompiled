using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class OrderedGroupJoinHashLookupBuilder<TElement, TOrderKey, THashKey> : HashLookupBuilder<IEnumerable<TElement>, Pair<bool, TOrderKey>, THashKey>
{
	private struct OrderedGroupJoinBaseHashBuilder : IBaseHashBuilder<TElement, TOrderKey>
	{
		private readonly HashLookup<THashKey, GroupKeyData> _base;

		private readonly IComparer<TOrderKey> _orderKeyComparer;

		public OrderedGroupJoinBaseHashBuilder(HashLookup<THashKey, GroupKeyData> baseLookup, IComparer<TOrderKey> orderKeyComparer)
		{
			_base = baseLookup;
			_orderKeyComparer = orderKeyComparer;
		}

		public bool Add(THashKey hashKey, TElement element, TOrderKey orderKey)
		{
			bool result = true;
			GroupKeyData value = null;
			if (!_base.TryGetValue(hashKey, ref value))
			{
				value = new GroupKeyData(orderKey, hashKey, _orderKeyComparer);
				_base.Add(hashKey, value);
				result = false;
			}
			value._grouping.Add(element, orderKey);
			if (_orderKeyComparer.Compare(orderKey, value._orderKey) < 0)
			{
				value._orderKey = orderKey;
			}
			return result;
		}
	}

	private sealed class OrderedGroupJoinHashLookup : GroupJoinHashLookup<THashKey, TElement, GroupKeyData, Pair<bool, TOrderKey>>
	{
		protected override Pair<bool, TOrderKey> EmptyValueKey => default(Pair<bool, TOrderKey>);

		internal OrderedGroupJoinHashLookup(HashLookup<THashKey, GroupKeyData> lookup)
			: base(lookup)
		{
		}

		protected override Pair<IEnumerable<TElement>, Pair<bool, TOrderKey>> CreateValuePair(GroupKeyData baseValue)
		{
			return new Pair<IEnumerable<TElement>, Pair<bool, TOrderKey>>(baseValue._grouping, Wrap(baseValue._orderKey));
		}

		private Pair<bool, TOrderKey> Wrap(TOrderKey orderKey)
		{
			return new Pair<bool, TOrderKey>(first: true, orderKey);
		}
	}

	private sealed class GroupKeyData
	{
		internal TOrderKey _orderKey;

		internal OrderedGroupByGrouping<THashKey, TOrderKey, TElement> _grouping;

		internal GroupKeyData(TOrderKey orderKey, THashKey hashKey, IComparer<TOrderKey> orderComparer)
		{
			_orderKey = orderKey;
			_grouping = new OrderedGroupByGrouping<THashKey, TOrderKey, TElement>(hashKey, orderComparer);
		}
	}

	private readonly QueryOperatorEnumerator<Pair<TElement, THashKey>, TOrderKey> _dataSource;

	private readonly IEqualityComparer<THashKey> _keyComparer;

	private readonly IComparer<TOrderKey> _orderKeyComparer;

	internal OrderedGroupJoinHashLookupBuilder(QueryOperatorEnumerator<Pair<TElement, THashKey>, TOrderKey> dataSource, IEqualityComparer<THashKey> keyComparer, IComparer<TOrderKey> orderKeyComparer)
	{
		_dataSource = dataSource;
		_keyComparer = keyComparer;
		_orderKeyComparer = orderKeyComparer;
	}

	public override HashJoinHashLookup<THashKey, IEnumerable<TElement>, Pair<bool, TOrderKey>> BuildHashLookup(CancellationToken cancellationToken)
	{
		HashLookup<THashKey, GroupKeyData> hashLookup = new HashLookup<THashKey, GroupKeyData>(_keyComparer);
		BuildBaseHashLookup<OrderedGroupJoinBaseHashBuilder, TElement, TOrderKey>(baseHashBuilder: new OrderedGroupJoinBaseHashBuilder(hashLookup, _orderKeyComparer), dataSource: _dataSource, cancellationToken: cancellationToken);
		for (int i = 0; i < hashLookup.Count; i++)
		{
			hashLookup[i].Value._grouping.DoneAdding();
		}
		return new OrderedGroupJoinHashLookup(hashLookup);
	}
}
