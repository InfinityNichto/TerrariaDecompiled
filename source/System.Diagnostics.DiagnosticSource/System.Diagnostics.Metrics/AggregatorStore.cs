using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace System.Diagnostics.Metrics;

internal struct AggregatorStore<TAggregator> where TAggregator : Aggregator
{
	private volatile object _stateUnion;

	private volatile AggregatorLookupFunc<TAggregator> _cachedLookupFunc;

	private readonly Func<TAggregator> _createAggregatorFunc;

	public AggregatorStore(Func<TAggregator> createAggregator)
	{
		_stateUnion = null;
		_cachedLookupFunc = null;
		_createAggregatorFunc = createAggregator;
	}

	public TAggregator GetAggregator(ReadOnlySpan<KeyValuePair<string, object>> labels)
	{
		AggregatorLookupFunc<TAggregator> cachedLookupFunc = _cachedLookupFunc;
		if (cachedLookupFunc != null && cachedLookupFunc(labels, out var aggregator))
		{
			return aggregator;
		}
		return GetAggregatorSlow(labels);
	}

	private TAggregator GetAggregatorSlow(ReadOnlySpan<KeyValuePair<string, object>> labels)
	{
		TAggregator aggregator;
		bool flag = (_cachedLookupFunc = LabelInstructionCompiler.Create(ref this, _createAggregatorFunc, labels))(labels, out aggregator);
		return aggregator;
	}

	public void Collect(Action<LabeledAggregationStatistics> visitFunc)
	{
		object stateUnion = _stateUnion;
		object stateUnion2 = _stateUnion;
		if (!(stateUnion2 is TAggregator val))
		{
			if (!(stateUnion2 is FixedSizeLabelNameDictionary<StringSequence1, ObjectSequence1, TAggregator> fixedSizeLabelNameDictionary))
			{
				if (!(stateUnion2 is FixedSizeLabelNameDictionary<StringSequence2, ObjectSequence2, TAggregator> fixedSizeLabelNameDictionary2))
				{
					if (!(stateUnion2 is FixedSizeLabelNameDictionary<StringSequence3, ObjectSequence3, TAggregator> fixedSizeLabelNameDictionary3))
					{
						if (!(stateUnion2 is FixedSizeLabelNameDictionary<StringSequenceMany, ObjectSequenceMany, TAggregator> fixedSizeLabelNameDictionary4))
						{
							if (stateUnion2 is MultiSizeLabelNameDictionary<TAggregator> multiSizeLabelNameDictionary)
							{
								multiSizeLabelNameDictionary.Collect(visitFunc);
							}
						}
						else
						{
							fixedSizeLabelNameDictionary4.Collect(visitFunc);
						}
					}
					else
					{
						fixedSizeLabelNameDictionary3.Collect(visitFunc);
					}
				}
				else
				{
					fixedSizeLabelNameDictionary2.Collect(visitFunc);
				}
			}
			else
			{
				fixedSizeLabelNameDictionary.Collect(visitFunc);
			}
		}
		else
		{
			IAggregationStatistics stats = val.Collect();
			visitFunc(new LabeledAggregationStatistics(stats));
		}
	}

	public TAggregator GetAggregator()
	{
		MultiSizeLabelNameDictionary<TAggregator> multiSizeLabelNameDictionary2;
		while (true)
		{
			object stateUnion = _stateUnion;
			if (stateUnion == null)
			{
				TAggregator val = _createAggregatorFunc();
				if (val == null)
				{
					return val;
				}
				if (Interlocked.CompareExchange(ref _stateUnion, val, null) == null)
				{
					return val;
				}
				continue;
			}
			if (stateUnion is TAggregator result)
			{
				return result;
			}
			if (stateUnion is MultiSizeLabelNameDictionary<TAggregator> multiSizeLabelNameDictionary)
			{
				return multiSizeLabelNameDictionary.GetNoLabelAggregator(_createAggregatorFunc);
			}
			multiSizeLabelNameDictionary2 = new MultiSizeLabelNameDictionary<TAggregator>(stateUnion);
			if (Interlocked.CompareExchange(ref _stateUnion, multiSizeLabelNameDictionary2, stateUnion) == stateUnion)
			{
				break;
			}
		}
		return multiSizeLabelNameDictionary2.GetNoLabelAggregator(_createAggregatorFunc);
	}

	public ConcurrentDictionary<TObjectSequence, TAggregator> GetLabelValuesDictionary<TStringSequence, TObjectSequence>(in TStringSequence names) where TStringSequence : IStringSequence, IEquatable<TStringSequence> where TObjectSequence : IObjectSequence, IEquatable<TObjectSequence>
	{
		MultiSizeLabelNameDictionary<TAggregator> multiSizeLabelNameDictionary2;
		while (true)
		{
			object stateUnion = _stateUnion;
			if (stateUnion == null)
			{
				FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator> fixedSizeLabelNameDictionary = new FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator>();
				if (Interlocked.CompareExchange(ref _stateUnion, fixedSizeLabelNameDictionary, null) == null)
				{
					return fixedSizeLabelNameDictionary.GetValuesDictionary(in names);
				}
				continue;
			}
			if (stateUnion is FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator> fixedSizeLabelNameDictionary2)
			{
				return fixedSizeLabelNameDictionary2.GetValuesDictionary(in names);
			}
			if (stateUnion is MultiSizeLabelNameDictionary<TAggregator> multiSizeLabelNameDictionary)
			{
				return multiSizeLabelNameDictionary.GetFixedSizeLabelNameDictionary<TStringSequence, TObjectSequence>().GetValuesDictionary(in names);
			}
			multiSizeLabelNameDictionary2 = new MultiSizeLabelNameDictionary<TAggregator>(stateUnion);
			if (Interlocked.CompareExchange(ref _stateUnion, multiSizeLabelNameDictionary2, stateUnion) == stateUnion)
			{
				break;
			}
		}
		return multiSizeLabelNameDictionary2.GetFixedSizeLabelNameDictionary<TStringSequence, TObjectSequence>().GetValuesDictionary(in names);
	}
}
