using System.Collections.Concurrent;
using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

internal class FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator> : ConcurrentDictionary<TStringSequence, ConcurrentDictionary<TObjectSequence, TAggregator>> where TStringSequence : IStringSequence, IEquatable<TStringSequence> where TObjectSequence : IObjectSequence, IEquatable<TObjectSequence> where TAggregator : Aggregator
{
	public void Collect(Action<LabeledAggregationStatistics> visitFunc)
	{
		using IEnumerator<KeyValuePair<TStringSequence, ConcurrentDictionary<TObjectSequence, TAggregator>>> enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			KeyValuePair<TStringSequence, ConcurrentDictionary<TObjectSequence, TAggregator>> current = enumerator.Current;
			Span<string> span = current.Key.AsSpan();
			foreach (KeyValuePair<TObjectSequence, TAggregator> item in current.Value)
			{
				Span<object> span2 = item.Key.AsSpan();
				KeyValuePair<string, string>[] array = new KeyValuePair<string, string>[span.Length];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = new KeyValuePair<string, string>(span[i], span2[i]?.ToString() ?? "");
				}
				IAggregationStatistics stats = item.Value.Collect();
				visitFunc(new LabeledAggregationStatistics(stats, array));
			}
		}
	}

	public ConcurrentDictionary<TObjectSequence, TAggregator> GetValuesDictionary(in TStringSequence names)
	{
		return GetOrAdd(names, (TStringSequence _) => new ConcurrentDictionary<TObjectSequence, TAggregator>());
	}
}
