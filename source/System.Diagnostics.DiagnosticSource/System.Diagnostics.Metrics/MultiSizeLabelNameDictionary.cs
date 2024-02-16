using System.Threading;

namespace System.Diagnostics.Metrics;

internal class MultiSizeLabelNameDictionary<TAggregator> where TAggregator : Aggregator
{
	private TAggregator NoLabelAggregator;

	private FixedSizeLabelNameDictionary<StringSequence1, ObjectSequence1, TAggregator> Label1;

	private FixedSizeLabelNameDictionary<StringSequence2, ObjectSequence2, TAggregator> Label2;

	private FixedSizeLabelNameDictionary<StringSequence3, ObjectSequence3, TAggregator> Label3;

	private FixedSizeLabelNameDictionary<StringSequenceMany, ObjectSequenceMany, TAggregator> LabelMany;

	public MultiSizeLabelNameDictionary(object initialLabelNameDict)
	{
		NoLabelAggregator = null;
		Label1 = null;
		Label2 = null;
		Label3 = null;
		LabelMany = null;
		if (!(initialLabelNameDict is TAggregator noLabelAggregator))
		{
			if (!(initialLabelNameDict is FixedSizeLabelNameDictionary<StringSequence1, ObjectSequence1, TAggregator> label))
			{
				if (!(initialLabelNameDict is FixedSizeLabelNameDictionary<StringSequence2, ObjectSequence2, TAggregator> label2))
				{
					if (!(initialLabelNameDict is FixedSizeLabelNameDictionary<StringSequence3, ObjectSequence3, TAggregator> label3))
					{
						if (initialLabelNameDict is FixedSizeLabelNameDictionary<StringSequenceMany, ObjectSequenceMany, TAggregator> labelMany)
						{
							LabelMany = labelMany;
						}
					}
					else
					{
						Label3 = label3;
					}
				}
				else
				{
					Label2 = label2;
				}
			}
			else
			{
				Label1 = label;
			}
		}
		else
		{
			NoLabelAggregator = noLabelAggregator;
		}
	}

	public TAggregator GetNoLabelAggregator(Func<TAggregator> createFunc)
	{
		if (NoLabelAggregator == null)
		{
			TAggregator val = createFunc();
			if (val != null)
			{
				Interlocked.CompareExchange(ref NoLabelAggregator, val, null);
			}
		}
		return NoLabelAggregator;
	}

	public FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator> GetFixedSizeLabelNameDictionary<TStringSequence, TObjectSequence>() where TStringSequence : IStringSequence, IEquatable<TStringSequence> where TObjectSequence : IObjectSequence, IEquatable<TObjectSequence>
	{
		TStringSequence val = default(TStringSequence);
		if (!(val is StringSequence1))
		{
			if (!(val is StringSequence2))
			{
				if (!(val is StringSequence3))
				{
					if (val is StringSequenceMany)
					{
						if (LabelMany == null)
						{
							Interlocked.CompareExchange(ref LabelMany, new FixedSizeLabelNameDictionary<StringSequenceMany, ObjectSequenceMany, TAggregator>(), null);
						}
						return (FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator>)(object)LabelMany;
					}
					return null;
				}
				if (Label3 == null)
				{
					Interlocked.CompareExchange(ref Label3, new FixedSizeLabelNameDictionary<StringSequence3, ObjectSequence3, TAggregator>(), null);
				}
				return (FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator>)(object)Label3;
			}
			if (Label2 == null)
			{
				Interlocked.CompareExchange(ref Label2, new FixedSizeLabelNameDictionary<StringSequence2, ObjectSequence2, TAggregator>(), null);
			}
			return (FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator>)(object)Label2;
		}
		if (Label1 == null)
		{
			Interlocked.CompareExchange(ref Label1, new FixedSizeLabelNameDictionary<StringSequence1, ObjectSequence1, TAggregator>(), null);
		}
		return (FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator>)(object)Label1;
	}

	public void Collect(Action<LabeledAggregationStatistics> visitFunc)
	{
		if (NoLabelAggregator != null)
		{
			IAggregationStatistics stats = NoLabelAggregator.Collect();
			visitFunc(new LabeledAggregationStatistics(stats));
		}
		Label1?.Collect(visitFunc);
		Label2?.Collect(visitFunc);
		Label3?.Collect(visitFunc);
		LabelMany?.Collect(visitFunc);
	}
}
