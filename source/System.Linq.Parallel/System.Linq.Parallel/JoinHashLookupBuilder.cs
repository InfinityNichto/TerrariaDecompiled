using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class JoinHashLookupBuilder<TElement, TOrderKey, THashKey> : HashLookupBuilder<TElement, TOrderKey, THashKey>
{
	private struct JoinBaseHashBuilder : IBaseHashBuilder<TElement, TOrderKey>
	{
		private readonly HashLookup<THashKey, HashLookupValueList<TElement, TOrderKey>> _base;

		public JoinBaseHashBuilder(HashLookup<THashKey, HashLookupValueList<TElement, TOrderKey>> baseLookup)
		{
			_base = baseLookup;
		}

		public bool Add(THashKey hashKey, TElement element, TOrderKey orderKey)
		{
			HashLookupValueList<TElement, TOrderKey> value = default(HashLookupValueList<TElement, TOrderKey>);
			if (!_base.TryGetValue(hashKey, ref value))
			{
				value = new HashLookupValueList<TElement, TOrderKey>(element, orderKey);
				_base.Add(hashKey, value);
				return false;
			}
			if (value.Add(element, orderKey))
			{
				_base[hashKey] = value;
			}
			return true;
		}
	}

	private sealed class JoinHashLookup : HashJoinHashLookup<THashKey, TElement, TOrderKey>
	{
		private readonly HashLookup<THashKey, HashLookupValueList<TElement, TOrderKey>> _base;

		internal JoinHashLookup(HashLookup<THashKey, HashLookupValueList<TElement, TOrderKey>> baseLookup)
		{
			_base = baseLookup;
		}

		public override bool TryGetValue(THashKey key, ref HashLookupValueList<TElement, TOrderKey> value)
		{
			return _base.TryGetValue(key, ref value);
		}
	}

	private readonly QueryOperatorEnumerator<Pair<TElement, THashKey>, TOrderKey> _dataSource;

	private readonly IEqualityComparer<THashKey> _keyComparer;

	internal JoinHashLookupBuilder(QueryOperatorEnumerator<Pair<TElement, THashKey>, TOrderKey> dataSource, IEqualityComparer<THashKey> keyComparer)
	{
		_dataSource = dataSource;
		_keyComparer = keyComparer;
	}

	public override HashJoinHashLookup<THashKey, TElement, TOrderKey> BuildHashLookup(CancellationToken cancellationToken)
	{
		HashLookup<THashKey, HashLookupValueList<TElement, TOrderKey>> baseLookup = new HashLookup<THashKey, HashLookupValueList<TElement, TOrderKey>>(_keyComparer);
		BuildBaseHashLookup<JoinBaseHashBuilder, TElement, TOrderKey>(baseHashBuilder: new JoinBaseHashBuilder(baseLookup), dataSource: _dataSource, cancellationToken: cancellationToken);
		return new JoinHashLookup(baseLookup);
	}

	protected override void Dispose(bool disposing)
	{
		_dataSource.Dispose();
	}
}
